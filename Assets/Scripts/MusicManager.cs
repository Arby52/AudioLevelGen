using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour {

    private Object[] objectsToLoad;
    private List<Track> loadedAudioTracks = new List<Track>();
    public Playlist playlist { get; private set; }

    private LevelGenerator levelGenerator;
    public AudioMixerGroup mixer;
    public GameObject visualiserCubePrefab;
    public GameObject visualiserHolder;
    GameObject[] visualiserCubes = new GameObject[8];
    public float startScale;
    public float scaleMultiplier;

    Track currentSong;
    [SerializeField]
    private int songIndexToPlay = 0;
    private int previousSongIndex;
    public float songTimeElapsed { get; private set; } = 0;
    public bool songPlaying { get; private set; } = false;

	// Use this for initialization
	void Start () {

        levelGenerator = GetComponent<LevelGenerator>();

        previousSongIndex = songIndexToPlay;

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

        Play();

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
        if(visualiserHolder != null)
        {
            DestroyImmediate(visualiserHolder);
        }
        visualiserHolder = new GameObject();
        float divisionSpace = 512 / currentSong.GetTrackLength();
        Vector3 prevPos = new Vector3(transform.position.x - (currentSong.GetTrackLength() / 2), transform.position.y, 0);
        visualiserHolder.transform.position = transform.position;
        visualiserHolder.transform.parent = transform;
        visualiserHolder.name = "Visual Holder";
        divisionSpace -= visualiserCubePrefab.transform.localScale.x;
        print(divisionSpace);
        for(int i = 0; i < visualiserCubes.Length; i++)
        {
            GameObject cube = (GameObject)Instantiate(visualiserCubePrefab);
            cube.transform.position = prevPos + new Vector3(divisionSpace, 0, 0);
            prevPos = cube.transform.position;
            cube.transform.parent = visualiserHolder.transform;
            cube.name = "Cube " + i;
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
	}

    void AudioVisualisation()
    {
        if (songPlaying)
        {
            //The higher the array, the most accurate the data. However it will take longer to use. Needs to be multiple of 8.
            float[] spectrumData = new float[512];
            FFTWindow window = FFTWindow.Hanning;  //compare all windowing to see which works best for the system.
            currentSong.source.GetSpectrumData(spectrumData, 0, window);

            float[] freqencyBand = new float[8];
            CreateFrequencyBands(ref spectrumData, ref freqencyBand);

            for (int i = 0; i < freqencyBand.Length; i++)
            {
                if (visualiserCubes != null)
                {
                    visualiserCubes[i].transform.localScale = new Vector3(1, (freqencyBand[i] * scaleMultiplier) + startScale, 1);
                }
            }
        }
    }

    void CreateFrequencyBands(ref float[] _spectrumData, ref float[] _frequencyBand)
    {
        /*
        20-60 - Sub Bass
        60-250 - Bass
        250-500 - Low Midrange 
        500-2000 - Midrange
        2000-4000 - Upper Midrange
        4000-6000 - Presence
        6000-20000 - Brilliance

        0 - 2 = 86hz      -  86 - Sub bass
        1 - 4 = 172hz     -  87 - 258  - Bass
        2 - 8 = 344hz     -  259 - 602 - Low Midrange
        3 - 16 = 688hz    -  603 - 1290  - Midrange
        4 - 32 = 1376hz   -  1291 - 2666  - Midrange/ Upper Midrange
        5 - 64 = 2752hz   -  2667 - 5418  - Upper Midrange/ Presence
        6 - 128 = 5504hz  -  5419 - 10922  - Presence/Brilliance
        7 - 256 = 11008hz -  10923 - 21930  - Brilliance
        */

        int count = 0;

        for(int i = 0; i < 8; i++)
        {
            float average = 0;
            int sampleBand = (int)Mathf.Pow(2, i) * 2; //2, 4, 8, 16, etc.

            if(i == 7)
            {
                sampleBand += 2; //to hit the 512 smaples maximum.
            }

            for(int j = 0; j < sampleBand; j++)
            {
                average += _spectrumData[count] * (count+1);
                count++;
            }

            average /= count;

            _frequencyBand[i] = average * 10;
        }
    }
}
