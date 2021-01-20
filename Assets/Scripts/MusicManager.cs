using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class BeatSubband
{    
    public float instantEnergy = 0;
    public float[] historyBuffer = new float[43]; //Store the history of instant energy values to get the average.
    public int populatedHistory = 0; //Store the amount of values in historyBuffer.
}

public class MusicManager : MonoBehaviour {

    public GameObject player;
    public Camera cam;
    public Camera lineCam; //Used to display beat lines in the top left.
    bool playerDeath = false;

    //Track loading and playback

    public Playlist playlist;
    Track currentSong;
    public int songIndexToPlay = 0;
    private int previousSongIndex;
    public float songTimeElapsed { get; private set; } = 0;
    public bool songPlaying { get; private set; } = false;

    private LevelGenerator levelGenerator;
    public AudioMixerGroup mixer;

    //Tracks
    private Object[] objectsToLoad; // Loading in music tracks
    private List<Track> loadedAudioTracks = new List<Track>();

    //Visualiser Variables
    GameObject levelFloor;
    public GameObject visualiserCubePrefab;
    public GameObject visualiserHolder;
    GameObject[] visualiserCubes = new GameObject[59];  //if using 64 bands, set to 59 as the last 5 never really get hit
    Color[] currentCubeColor = new Color[64];
    public float startScale;
    public float scaleMultiplier;

    [Range(0,1)]
    public float cutoff;

    //Audio Frequency data stuff    
    float[] spectrumDataLeft = new float[512];
    float[] spectrumDataRight = new float[512];

    //8 bands
    float[] freqencyBand8 = new float[8];
    float[] frequencyBandHighest8 = new float[8];

    //64 bands
    float[] freqencyBand64 = new float[64];    
    float[] frequencyBandHighest64 = new float[64];   

    //Normalised Frequency Data
    float[] frequencyBandNormalised8 = new float[8]; 
    float[] frequencyBandNormalised64 = new float[64];

    //Beat detection advanced
    public UnityEvent OnBeat;  
    float lastBeat;
    BeatSubband[] beatSubbands = new BeatSubband[64];

    //Beat Lines. To display beat detection.
    LineRenderer[] BeatLines1 = new LineRenderer[64];
    LineRenderer[] BeatLines2 = new LineRenderer[64];
    bool renderBeatLines = false;

    // Use this for initialization
    void Start () {

        //Populating variables
        levelGenerator = GetComponent<LevelGenerator>();
        previousSongIndex = songIndexToPlay;

        LoadTracks();

        for(int i = 0; i < beatSubbands.Length; i++)
        {
            beatSubbands[i] = new BeatSubband();
        }

        if (OnBeat == null)
        {
            OnBeat = new UnityEvent();
        }

        for (int i = 0; i < BeatLines1.Length; i++)
        {            
            BeatLines1[i] = (new GameObject("line")).AddComponent<LineRenderer>();
            BeatLines1[i].material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            BeatLines1[i].startColor = Color.red;
            BeatLines1[i].endColor = Color.red;
            BeatLines1[i].startWidth = 0.05f;
            BeatLines1[i].transform.parent = lineCam.transform;
            BeatLines1[i].enabled = false;
        }

        for (int i = 0; i < BeatLines2.Length; i++)
        {            
            BeatLines2[i] = (new GameObject("line")).AddComponent<LineRenderer>();
            BeatLines2[i].material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            BeatLines2[i].startColor = Color.cyan;
            BeatLines2[i].endColor = Color.cyan;
            BeatLines2[i].startWidth = 0.05f;
            BeatLines2[i].transform.parent = lineCam.transform;
            BeatLines2[i].enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Need to use a boolean to check for death on the next frame as DestroyImmidiate (neccessary for level generation when going to the next song) cannot be called from a physics collision.
        if (playerDeath)
        {
            PlayNextSong();
            playerDeath = false;
        }

        //Inputs
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            PlayNextSong();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        //Turn on and off the beat detection lines display.
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (renderBeatLines)
            {
                foreach (var line in BeatLines1)
                {
                    line.enabled = false;
                }
                foreach (var line in BeatLines2)
                {
                    line.enabled = false;
                }

                renderBeatLines = false;
            }
            else
            {
                foreach (var line in BeatLines1)
                {
                    line.enabled = true;
                }
                foreach (var line in BeatLines2)
                {
                    line.enabled = true;
                }
                renderBeatLines = true;
            }
        }

        //Song countdown.
        songTimeElapsed -= Time.deltaTime;
        if (songTimeElapsed == 0)
        {
            PlayNextSong();
        } 

        //take audio data from current song and create a visualiser.
        if (currentSong != null)
        {
            AudioVisualisation();
            BeatDetection();
        }
        
        //Update visualiser cube positions to stay in the floor and middle of the screen.
        if (levelFloor != null)
        {
            foreach (var cube in visualiserCubes)
            {
                cube.transform.position = new Vector3(cube.transform.position.x, levelFloor.transform.position.y, -1);
                visualiserHolder.transform.position = new Vector3(cam.transform.position.x, visualiserHolder.transform.position.y, visualiserHolder.transform.position.z);
            }
        }
    }

    void LoadTracks()
    {

        GameObject tracksgo = new GameObject();
        tracksgo.name = "Tracks";
        tracksgo.transform.parent = gameObject.transform;

        if (Application.isEditor)
        {
            print("1");
            objectsToLoad = Resources.LoadAll("Music", typeof(AudioClip));

            print(objectsToLoad.Length + " objects loaded");

            foreach (var t in objectsToLoad)
            {
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
            Play();
        }
        else
        {
            FileInfo[] audioFiles;
            List<string> extensions = new List<string> { ".ogg", ".wav" };
            string path = "./music";

            var info = new DirectoryInfo(path);
            audioFiles = info.GetFiles()
                .Where(f => extensions.Contains(Path.GetExtension(f.Name))) //make sure to only add files with the approved extensions
                .ToArray();

            foreach (var i in audioFiles)
            {
                StartCoroutine(LoadFile(i.FullName, tracksgo));
            }
            playlist = new Playlist(loadedAudioTracks);
            
        }        
    }

    IEnumerator LoadFile(string _path, GameObject _tracksgo)
    {
        WWW www = new WWW("file://" + _path);
        AudioClip clip = www.GetAudioClip(false);

        while (clip.loadState != AudioDataLoadState.Loaded)
            yield return www;

        clip.name = Path.GetFileName(_path);

        GameObject go = new GameObject();
        Track track = go.AddComponent<Track>();
        track.clip = clip;
        track.name = clip.name;
        track.mixer = mixer;
        track.Initialise();
        loadedAudioTracks.Add(track);

        go.transform.parent = _tracksgo.transform;

        Play();
    }

    public void Play()
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
            levelFloor = levelGenerator.GenerateLevel(currentSong, ref player);

            songTimeElapsed = currentSong.GetTrackLength();
            currentSong.Play();
            InstantiateCubes();
            songPlaying = true;
            //print("Now Playing: " + currentSong.name);
        } 
    }

    void PlayNextSong()
    {
        previousSongIndex = songIndexToPlay;
        //If at the end of the playlist, loop back around to the start.
        if (songIndexToPlay >= playlist.audioTracks.Count - 1 )
        {            
            songIndexToPlay = 0;
        } else
        {
            songIndexToPlay++;
        }
        Play();
    }

    void InstantiateCubes()
    {
        float maxWidth = Vector2.Distance(cam.ScreenToWorldPoint(new Vector2(0, 0)), cam.ScreenToWorldPoint(new Vector2(0, cam.pixelWidth)));
        float barWidth = maxWidth / visualiserCubes.Length;
        float barHeight = levelFloor.transform.localScale.y;

        if (visualiserHolder != null)
        {
            DestroyImmediate(visualiserHolder);
        }
        visualiserHolder = new GameObject();        
        visualiserHolder.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z);
        visualiserHolder.name = "Visual Holder";

        Vector3 prevPos = new Vector3((cam.ScreenToWorldPoint(new Vector2(0, cam.pixelHeight / 2)).x) - barWidth / 2, visualiserHolder.transform.position.y, 0);

        for (int i = 0; i < visualiserCubes.Length; i++)
        {
            GameObject cube = (GameObject)Instantiate(visualiserCubePrefab);
            Destroy(cube.GetComponent<BoxCollider>());
            cube.transform.localScale = new Vector3(barWidth, barHeight, 1);
            cube.transform.position = prevPos + new Vector3(barWidth, 0, 0);
            prevPos = cube.transform.position;
            cube.transform.parent = visualiserHolder.transform;
            cube.name = "Cube " + i;

            Shader shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = Color.black;

            cube.GetComponent<Renderer>().material = mat;
            cube.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");

            cube.GetComponent<Renderer>().material.color = currentCubeColor[i];
            cube.GetComponent<Renderer>().material.SetColor("_EmissionColor", currentCubeColor[i]);

            visualiserCubes[i] = cube;
        }
    }
	
    public void PlayerDeath()
    {
        if (!playerDeath)
        {
            playerDeath = true;
        }
    }

    /*
     * Theres a bug where the first song's visualised data will be really weird. 
     * the last 5 frequencies of low amplitude songs seem to be always peaked but only on the first song played.
     * Essentially, the frequencies of the first song are a bit weird/amplified and idk why. 
     * Not really a big problem though.
     */
    void AudioVisualisation() 
    {
        if (songPlaying)
        {
            //The higher the array, the most accurate the data. However it will take longer to use. Needs to be multiple of 8.            
            FFTWindow window = FFTWindow.BlackmanHarris;  //A "good general purpose" window.
            currentSong.source.GetSpectrumData(spectrumDataLeft, 0, window);
            currentSong.source.GetSpectrumData(spectrumDataRight, 1, window);

            //CreateFrequencyBands8();
            //CreateAudioBands8();
            CreateFrequencyBands64();
            CreateNormalisedBands64();

            //Change the colour of the visualiser cubes based on their respective frequency band.
            for (int i = 0; i < visualiserCubes.Length; i++)
            {
                float iNormal = ((float)i / visualiserCubes.Length) * 0.8f; //times 0.75 to make it a scale of 0 - 0.75. Just looks a bit better.
                if (visualiserCubes != null)
                {                                   
                    currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, 0f);

                    if (frequencyBandNormalised64[i] > cutoff)
                    {
                        currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, 0.7f);                          
                    }
                    else
                    {
                        currentCubeColor[i] -= Color.HSVToRGB(0, 0f, 0.05f);
                    }
                    visualiserCubes[i].GetComponent<Renderer>().material.color = currentCubeColor[i];
                    visualiserCubes[i].GetComponent<Renderer>().material.SetColor("_EmissionColor", currentCubeColor[i]);
                }
            }
        }
    }


    //8 Bands, not used anymore but could be useful in the future?
    void CreateNormalisedBands8()
    {
        for (int i = 0; i < 8; i++)
        {
            //Set the band roof to the highest the band has been.
            if (freqencyBand8[i] > frequencyBandHighest8[i])
            {
                frequencyBandHighest8[i] = freqencyBand8[i];
            }

            frequencyBandNormalised8[i] = (freqencyBand8[i] / frequencyBandHighest8[i]);

        }
    }

    void CreateFrequencyBands8()
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

        for (int i = 0; i < 8; i++) //algorithm by peer play
        {
            float average = 0;
            int sampleBand = (int)Mathf.Pow(2, i) * 2; //width of the current band

            if (i == 7)
            {  
               sampleBand += 2;
            }

            for (int j = 0; j < sampleBand; j++)
            {
                average += spectrumDataLeft[count] + spectrumDataRight[count] * (count + 1);
                count++;
            }

            average /= count;

            freqencyBand8[i] = average * 10;
        }
    }

    void CreateNormalisedBands64()
    {
        for(int i = 0; i < 64; i++)
        {
            //Set the band roof to the highest the band has been.
            if(freqencyBand64[i] > frequencyBandHighest64[i])
            {
                frequencyBandHighest64[i] = freqencyBand64[i];
            }

            frequencyBandNormalised64[i] = (freqencyBand64[i] / frequencyBandHighest64[i]);            
        }
    }

    void CreateFrequencyBands64()
    {
        /*
        20-60 - Sub Bass
        60-250 - Bass
        250-500 - Low Midrange 
        500-2000 - Midrange
        2000-4000 - Upper Midrange
        4000-6000 - Presence
        6000-20000 - Brilliance. 20 - 20000Hz is the commonly stated range of human hearing. The average highest frequency that can bea heard by a person goes down with age.
        */

        //song hrtz is either 44100 or 48000. The maximum frequency produced is around half the frequency known as the "Nyquist Frequency"
        //44100 / 2 / 512 = 43hz per sample
        //48000 / 2 /  512 = 47hz per sample
        //Average at around 45hz per sample.

        //Kick drum is between 60-150. Bands 1 - 3. (starting at 0)
        //snare drum is between 120-250. Bands 2 - 5 (starting at 0)
        //For a very basic drum detection, check for peaks between bands 1 - 5 ish. A few more would probably be useful too

        /* rework to get 1024 samples?
        0-15 - 1 sample -    1 * 16 =  16
        16-31 - 2 samples  - 2 * 16 =  32
        32-39 - 4 samples -  8 * 4  =  32
        40-47 - 6 samples -  8 * 6  =  48
        48-55 - 16 samples - 8 * 16 = 128
        56-63 - 32 samples - 8 * 32 = 256   
                                      512
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
                sampleBand = (int)Mathf.Pow(2,power); //to hit the 512 samples maximum.
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

            freqencyBand64[i] = average * 80;
        }
    }

    //http://archive.gamedev.net/archive/reference/programming/features/beatdetection/
    void BeatDetection()
    {
        bool beat = false;

        float[] beatSpecL = new float[512];
        float[] beatSpecR = new float[512];
        float[] beatSpecAdded = new float[512];        

        currentSong.source.GetSpectrumData(beatSpecL, 0, FFTWindow.BlackmanHarris);
        currentSong.source.GetSpectrumData(beatSpecR, 1, FFTWindow.BlackmanHarris);

        //Add the channels together.
        for (int i = 0; i < beatSpecAdded.Length; i++)
        {
            beatSpecAdded[i] = beatSpecL[i] + beatSpecR[i];
        }

        float[] dividedSubbands = new float[64];

        //Split into 64 logrithmic bands like the CreateFrequencyBands function. Should probably not "re-use" code and make it use the same function.
        int count = 0;
        int sampleBand = 1;
        int power = 0;

        for (int i = 0; i < 64; i++)
        {
            float average = 0;

            if (i == 16 || i == 32 || i == 40 || i == 48 || i == 56)
            {
                power++;
                sampleBand = (int)Mathf.Pow(2, power);
                if (power == 3)
                {
                    sampleBand -= 2;
                }
            }

            for (int j = 0; j < sampleBand; j++)
            {
                average += beatSpecAdded[count] * (count + 1);
                count++;
            }

            average /= count;

            dividedSubbands[i] = average * 80;
        }

        //compute instant energy
        for (int i = 0; i < beatSubbands.Length; i++)
        {
            if (i < 64 && i > 47) //make it so only the drum area is picked up. the "i" is reversed.
            {
                beatSubbands[i].instantEnergy = Mathf.Pow(dividedSubbands[i], 2); //Intensity of a wave is proportional to the square of its amplitude.

                beatSubbands[i].instantEnergy /= 64; //It might make some sense to divide by the number of samples in the band? Not sure.

                beatSubbands[i].historyBuffer[0] = beatSubbands[i].instantEnergy;
                beatSubbands[i].populatedHistory++;

                //make it so populatedHistory can't go above historyBuffer length.
                if (beatSubbands[i].populatedHistory >= beatSubbands[i].historyBuffer.Length)
                {
                    beatSubbands[i].populatedHistory = beatSubbands[i].historyBuffer.Length;
                }


                //local average energy
                float localAverageEnergy = 0f;

                for (int j = 0; j < beatSubbands[i].populatedHistory; j++)
                {
                    localAverageEnergy += beatSubbands[i].historyBuffer[j];
                }

                localAverageEnergy /= beatSubbands[i].populatedHistory;

                //shift data on the buffer
                float[] shiftHistory = new float[beatSubbands[i].historyBuffer.Length];

                for (int j = 0; j < beatSubbands[i].populatedHistory - 1; j++)
                {
                    shiftHistory[j + 1] = beatSubbands[i].historyBuffer[j];
                }

                shiftHistory[0] = beatSubbands[i].instantEnergy;

                for (int j = 0; j < beatSubbands[i].populatedHistory; j++)
                {
                    beatSubbands[i].historyBuffer[j] = shiftHistory[j];
                }
                
                /*
                The algorithm says it needs to be about 250. 5 works perfectly. I don't know why.
                Honestly, I think I've gotten the right solution with the wrong formula with this whole function. 
                Maybe its to do ith the whole divide by band samples instead of 64 thing?        
                But hey, it works.
                */
                float constant = 5;
                
                //For displaying the beat detection lines.
                //Vectors taken from unity API example on getSpectrumData
                int xOffset = 55;
                BeatLines1[i].SetPosition(0, new Vector3(i - 1 - xOffset, 90 + Mathf.Log(beatSubbands[i].instantEnergy) + 10, 2));
                BeatLines1[i].SetPosition(1, new Vector3(i - xOffset, 90 + Mathf.Log(beatSubbands[i].instantEnergy) + 10, 2));

                BeatLines2[i].SetPosition(0, new Vector3(i - 1 - xOffset, 90 + Mathf.Log(constant * localAverageEnergy) + 10, 2));
                BeatLines2[i].SetPosition(1, new Vector3(i - xOffset, 90 + Mathf.Log(constant * localAverageEnergy) + 10, 2));
        
                //check for a beat occurance in every band.
                if (beatSubbands[i].instantEnergy > (constant * localAverageEnergy) && Time.time - lastBeat >= 0.30)
                {
                    beat = true;
                }
            }
        }

        //If there was a beat, invoke the event.
        if (beat)
        {
            lastBeat = Time.time;
            OnBeat.Invoke();
        }
    }     
}
