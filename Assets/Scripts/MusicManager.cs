using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour {

    public Camera cam;

    public float test;

    //Track loading and playback
    private Object[] objectsToLoad; // Loading in music tracks
    private List<Track> loadedAudioTracks = new List<Track>();
    public Playlist playlist { get; private set; }

    Track currentSong;
    public int songIndexToPlay = 0;
    private int previousSongIndex;
    public float songTimeElapsed { get; private set; } = 0;
    public bool songPlaying { get; private set; } = false;

    private LevelGenerator levelGenerator;
    public AudioMixerGroup mixer;

    //Visualiser Variables
    public GameObject visualiserCubePrefab;
    public GameObject visualiserHolder;
    GameObject[] visualiserCubes = new GameObject[59];
    public float startScale;
    public float scaleMultiplier;

    //Audio Frequency data stuff
    const int bands = 8;
    
    float[] spectrumDataLeft = new float[512];
    float[] spectrumDataRight = new float[512];

    //64 bands
    float[] freqencyBand = new float[64];    
    float[] bandBuffer = new float[64];
    float[] bufferDecrease = new float[64];
    float[] frequencyBandHighest = new float[64];

    public float decrease;
    public float increase;
    public bool useBuffer;

    //Normalised Frequency Data
    float[] frequencyBandNormalised = new float[64];  //Use this in gameplay and mechanics when not using buffer.
    float[] bandBufferNormalised = new float[64];  //Use this in gameplay and mechanics when using buffer.      

    //Beat Detection Variables


    // Use this for initialization
    void Start () {

        //Populating variables
        levelGenerator = GetComponent<LevelGenerator>();
        previousSongIndex = songIndexToPlay;
        

        LoadTracks();
        Play();

	}

    void LoadTracks()
    {
        GameObject tracksgo = new GameObject();
        tracksgo.name = "Tracks";
        tracksgo.transform.parent = gameObject.transform;

        try
        {
            objectsToLoad = Resources.LoadAll("Music", typeof(AudioClip));

            print(objectsToLoad.Length + " objects loaded");

            foreach (var t in objectsToLoad)
            {
                //Possibly load the audio clips into a prefab to set the loadtype to streaming.
                GameObject go = new GameObject();
                Track track = go.AddComponent<Track>();
                track.clip = (AudioClip)t;
                track.name = t.name;
                track.mixer = mixer;
                track.Initialise();
                loadedAudioTracks.Add(track);

                go.transform.parent = tracksgo.transform;

            }
            playlist = new Playlist(loadedAudioTracks);
        }
        catch (System.InvalidCastException e)
        {
            //Should be impossible for this exception to happen because of the "typeof(AudioClip)" when loading in the resources.
            Debug.LogException(e);
        }
    }

    void Play()
    {
        if (songIndexToPlay < playlist.audioTracks.Count)
        {
            //Stop previous track
            Track previous = playlist.audioTracks[previousSongIndex];
            if (previous.IsPlaying())
            {                
                previous.Stop();
                songPlaying = false;
            }

            //Play current track
            currentSong = playlist.audioTracks[songIndexToPlay]; //Store the object once so it dosen't have to search the array multiple times. Negligable performance benefit but still a benefit.
            levelGenerator.GenerateLevel(currentSong);
            songTimeElapsed = currentSong.GetTrackLength();
            currentSong.Play();
            InstantiateCubes();
            songPlaying = true;
            print("Now Playing: " + currentSong.name);
        }
    }

    void PlayNextSong()
    {
        previousSongIndex = songIndexToPlay;
        if (songIndexToPlay >= playlist.audioTracks.Count - 1 )
        {            
            songIndexToPlay = 0;
        } else
        {
            songIndexToPlay++;
        }
        print(songIndexToPlay);

        Play();
    }

    void InstantiateCubes()
    {
        float maxWidth = Vector2.Distance(cam.ScreenToWorldPoint(new Vector2(0, 0)), cam.ScreenToWorldPoint(new Vector2(0, cam.pixelWidth)));
        float barWidth = maxWidth / visualiserCubes.Length;
        if(visualiserHolder != null)
        {
            DestroyImmediate(visualiserHolder);
        }
        visualiserHolder = new GameObject();        
        visualiserHolder.transform.position = cam.transform.position;
        visualiserHolder.transform.parent = cam.transform;
        visualiserHolder.name = "Visual Holder";

        Vector3 prevPos = new Vector3((cam.ScreenToWorldPoint(new Vector2(0, cam.pixelHeight/2)).x) - barWidth/2 , visualiserHolder.transform.position.y, 0);


        for (int i = 0; i < visualiserCubes.Length; i++)
        {
            GameObject cube = (GameObject)Instantiate(visualiserCubePrefab);
            cube.transform.localScale = new Vector3(barWidth, 1, 1);
            cube.transform.position = prevPos + new Vector3(barWidth, 0, 0);
            prevPos = cube.transform.position;
            cube.transform.parent = visualiserHolder.transform;
            cube.name = "Cube " + i;

            Shader shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = Color.green;
            cube.GetComponent<Renderer>().material = mat;


            visualiserCubes[i] = cube;
        }
    }
	
	// Update is called once per frame
	void Update () {
        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            PlayNextSong();
        }

        songTimeElapsed -= Time.deltaTime;
        if(songTimeElapsed == 0)
        {
            PlayNextSong();
        }
        //print("Time Left: " + songTimeElapsed);        

        //take audio data from current song and create a visualiser.
        AudioVisualisation();
        BeatDetection();
	}

    void BeatDetection()
    {


    }



    void AudioVisualisation()
    {
        if (songPlaying)
        {
            //The higher the array, the most accurate the data. However it will take longer to use. Needs to be multiple of 8.
            
            FFTWindow window = FFTWindow.Blackman;  //compare all windowing to see which works best for the system.
            currentSong.source.GetSpectrumData(spectrumDataLeft, 0, window);
            currentSong.source.GetSpectrumData(spectrumDataRight, 1, window);

            CreateFrequencyBands();
            BandBuffer();
            CreateAudioBands();

            for (int i = 0; i < visualiserCubes.Length; i++)
            {
                if (visualiserCubes != null)
                {
                    if (useBuffer)
                    {
                        visualiserCubes[i].transform.localScale = new Vector3(visualiserCubes[i].transform.localScale.x, (bandBuffer[i] * scaleMultiplier) + startScale, 1);
                    } else
                    {
                        visualiserCubes[i].transform.localScale = new Vector3(visualiserCubes[i].transform.localScale.x, (freqencyBand[i] * scaleMultiplier) + startScale, 1);
                    }
                }
            }
        }
    }

    void CreateAudioBands()
    {
        for(int i = 0; i < 64; i++)
        {
            //Set the band roof to the highest the band has been.
            if(freqencyBand[i] > frequencyBandHighest[i])
            {
                frequencyBandHighest[i] = freqencyBand[i];
            }

            frequencyBandNormalised[i] = (freqencyBand[i] / frequencyBandHighest[i]);            
            bandBufferNormalised[i] = (bandBuffer[i] / frequencyBandHighest[i]);

        }
    }

    void BandBuffer()
    {
        for(int i = 0; i < 64; i++)
        {
            if(freqencyBand[i] > bandBuffer[i])
            {
                bandBuffer[i] = freqencyBand[i];
                bufferDecrease[i] = decrease;

            } else if (freqencyBand[i] < bandBuffer[i])
            {
                bandBuffer[i] -= bufferDecrease[i];
                bufferDecrease[i] *= increase;
            }
        }
    }

    void CreateFrequencyBands()
    {
        /*
        20-60 - Sub Bass
        60-250 - Bass
        250-500 - Low Midrange 
        500-2000 - Midrange
        2000-4000 - Upper Midrange
        4000-6000 - Presence
        6000-20000 - Brilliance
        */

        int count = 0;
        int sampleBand = 1;
        int power = 0;

        for(int i = 0; i < 64; i++)
        {
            float average = 0;

            if(i == 16 || i ==32 || i == 40 || i == 48 || i == 56)
            {
                power++;
                sampleBand = (int)Mathf.Pow(2,power); //to hit the 512 smaples maximum.
                if(power == 3)
                {
                    sampleBand -= 2;
                }
            }

            for(int j = 0; j < sampleBand; j++)
            {
                average += spectrumDataLeft[count] + spectrumDataRight[count] * (count+1);
                count++;
            }

            average /= count;

            freqencyBand[i] = average * 80;
        }
    }
}
