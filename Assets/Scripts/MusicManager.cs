﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class BeatSubband
{    
    public float instantEnergy = 0;
    public float[] historyBuffer = new float[43];
    public int populatedHistory = 0;
}

public class MusicManager : MonoBehaviour {

    public GameObject player;
    public Camera cam;
    bool playerDeath = false;

    //Track loading and playback
    TrackLoader trackLoader;

    public Playlist playlist;
    Track currentSong;
    public int songIndexToPlay = 0;
    private int previousSongIndex;
    public float songTimeElapsed { get; private set; } = 0;
    public bool songPlaying { get; private set; } = false;

    private LevelGenerator levelGenerator;
    public AudioMixerGroup mixer;

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
    float[] bufferDecrease8 = new float[8];
    float[] frequencyBandHighest8 = new float[8];

    //64 bands
    float[] freqencyBand64 = new float[64];    
    float[] bufferDecrease64 = new float[64];
    float[] frequencyBandHighest64 = new float[64];

    float lastBeat;

    public float decrease;
    public float increase;
    public bool useBuffer;

    //Normalised Frequency Data
    float[] frequencyBandNormalised8 = new float[8]; 
    float[] frequencyBandNormalised64 = new float[64];

    public UnityEvent OnBeat;

    //Beat Detection Variables    
    float[] historyBuffer = new float[43];
    int populatedHistory = 0;
    private bool beat = false;

    //Beat detection advanced
    BeatSubband[] beatSubbands = new BeatSubband[64];

    // Use this for initialization
    void Start () {

        //Populating variables
        levelGenerator = GetComponent<LevelGenerator>();
        previousSongIndex = songIndexToPlay;

        trackLoader = gameObject.AddComponent<TrackLoader>();
        trackLoader.LoadTracks(mixer, ref playlist);
        Play();
        
        for(int i = 0; i < beatSubbands.Length; i++)
        {
            beatSubbands[i] = new BeatSubband();
        }

        if (OnBeat == null)
        {
            OnBeat = new UnityEvent();
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
            levelFloor = levelGenerator.GenerateLevel(currentSong, ref player);
            songTimeElapsed = currentSong.GetTrackLength();
            currentSong.Play();
            print("freq " + currentSong.clip.frequency);
            InstantiateCubes();
            songPlaying = true;
            //print("Now Playing: " + currentSong.name);
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
        Play();
    }

    void InstantiateCubes()
    {
        float maxWidth = Vector2.Distance(cam.ScreenToWorldPoint(new Vector2(0, 0)), cam.ScreenToWorldPoint(new Vector2(0, cam.pixelWidth)));
        float barWidth = maxWidth / visualiserCubes.Length;
        //float barHeight = Vector2.Distance(cam.ScreenToWorldPoint(new Vector2(0, 0)), cam.ScreenToWorldPoint(new Vector2(0, cam.pixelHeight)));
        float barHeight = levelFloor.transform.localScale.y;
        if(visualiserHolder != null)
        {
            DestroyImmediate(visualiserHolder);
        }
        visualiserHolder = new GameObject();        
        visualiserHolder.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z);
        //visualiserHolder.transform.parent = cam.transform;
        visualiserHolder.name = "Visual Holder";

        Vector3 prevPos = new Vector3((cam.ScreenToWorldPoint(new Vector2(0, cam.pixelHeight/2)).x) - barWidth/2 , visualiserHolder.transform.position.y, 0);


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

	// Update is called once per frame
	void Update () {
        if (playerDeath)
        {
            PlayNextSong();
            playerDeath = false;
        }

        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            PlayNextSong();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        songTimeElapsed -= Time.deltaTime;
        if(songTimeElapsed == 0)
        {
            PlayNextSong();
        }
        //print("Time Left: " + songTimeElapsed);        

        //take audio data from current song and create a visualiser.
        if (currentSong != null)
        {
            AudioVisualisation();
            BeatDetection();
        }

        if(levelFloor != null)
        {
            foreach(var cube in visualiserCubes)
            {
                cube.transform.position = new Vector3(cube.transform.position.x, levelFloor.transform.position.y, -1);
                visualiserHolder.transform.position = new Vector3(cam.transform.position.x, visualiserHolder.transform.position.y, visualiserHolder.transform.position.z);
            }
        }
	}  

    void AudioVisualisation() //Theres a bug where the first song's visualised data will be really weird. the last 5 frequencies of low amplitude songs seem to be always peaked but only on the first song played.
    {
        if (songPlaying)
        {
            //The higher the array, the most accurate the data. However it will take longer to use. Needs to be multiple of 8.
            
            FFTWindow window = FFTWindow.BlackmanHarris;  //compare all windowing to see which works best for the system.
            currentSong.source.GetSpectrumData(spectrumDataLeft, 0, window);
            currentSong.source.GetSpectrumData(spectrumDataRight, 1, window);

            //CreateFrequencyBands8();
            //BandBuffer8();
            //CreateAudioBands8();

            CreateFrequencyBands64();
            CreateAudioBands64();

            for (int i = 0; i < visualiserCubes.Length; i++)
            {
                float iNormal = ((float)i / visualiserCubes.Length) * 0.8f; //times 0.75 to make it a scale of 0 - 0.75
                if (visualiserCubes != null)
                {               
                    
                    currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, 0f);

                    if (frequencyBandNormalised64[i] > cutoff)
                    {
                        currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, 0.7f);
                        //currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, Mathf.Clamp(frequencyBandNormalised64[i], 0, 0.7f));
                          
                    }
                    else
                    {
                        currentCubeColor[i] -= Color.HSVToRGB(0, 0f, 0.05f);
                    }

                    visualiserCubes[i].GetComponent<Renderer>().material.color = currentCubeColor[i];
                    visualiserCubes[i].GetComponent<Renderer>().material.SetColor("_EmissionColor", currentCubeColor[i]);

                    //visualiserCubes[i].transform.localScale = new Vector3(visualiserCubes[i].transform.localScale.x, (freqencyBand[i] * scaleMultiplier) + startScale, 1);  
                }
            }
        }
    }

    void CreateAudioBands8()
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

    void CreateAudioBands64()
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
        6000-20000 - Brilliance. This is around the maximum a human can hear. Older people can't usually hear this high though.
        */

        //song hrtz is either 44100 or 48000. The maximum frequency produced is around half the frequency known as the "Nyquist Frequency"
        //44100 / 2 / 512 = 43hz per sample
        //48000 / 2 /  512 = 47hz per sample
        //Average at around 45hz per sample.

        //Kick drum is between 60-150. Bands 1 - 3. (starting at 0)
        //snare drum is between 120-250. Bands 2 - 5 (starting at 0)
        //For a very basic drum detection, check for peaks between bands 1 - 5.

        /* rework to get 1024 samples?
        0-15 - 1 sample -              16
        16-31 - 2 samples  - 16 * 2 =  32
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

    void BeatDetection()
    {
        float[] beatSpecL = new float[512];
        float[] beatSpecR = new float[512];
        float[] beatSpecAdded = new float[512]; //BUFFER        

        currentSong.source.GetSpectrumData(beatSpecL, 0, FFTWindow.BlackmanHarris);
        currentSong.source.GetSpectrumData(beatSpecR, 1, FFTWindow.BlackmanHarris);

        bool beat = false;

        for (int i = 0; i < beatSpecAdded.Length; i++)
        {
            beatSpecAdded[i] = beatSpecL[i] + beatSpecR[i];
        }

        float[] dividedSubbands = new float[64];

        //Split into 64 logrithmic bands
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
            if (i < 63 && i > 47) //make it so only the drum area is picked up. the "i" is reversed.
            {
                beatSubbands[i].instantEnergy = Mathf.Pow(dividedSubbands[i], 2);

                beatSubbands[i].instantEnergy /= 64;

                beatSubbands[i].historyBuffer[0] = beatSubbands[i].instantEnergy;
                beatSubbands[i].populatedHistory++;

                //make it so it can't go above 42
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

                float constant = 5;

                //Debug.DrawLine(new Vector3(i - 1, beatSubbands[i].instantEnergy + 10, 0), new Vector3(i, beatSubbands[i].instantEnergy + 10, 0), Color.red);
                Debug.DrawLine(new Vector3(i - 1, Mathf.Log(beatSubbands[i].instantEnergy) + 10, 2), new Vector3(i, Mathf.Log(beatSubbands[i].instantEnergy) + 10, 2), Color.red);
                //Debug.DrawLine(new Vector3(i - 1, constant * localAverageEnergy + 10, 0), new Vector3(i, constant * localAverageEnergy + 10, 0), Color.cyan);
                Debug.DrawLine(new Vector3(i - 1, Mathf.Log(constant * localAverageEnergy) + 10, 2), new Vector3(i, Mathf.Log(constant * localAverageEnergy) + 10, 2), Color.cyan);

                //check for beat. 
                if (beatSubbands[i].instantEnergy > (constant * localAverageEnergy) && Time.time - lastBeat >= 0.30)
                {
                    beat = true;
                }
            }
        }

        if (beat)
        {
            lastBeat = Time.time;
            OnBeat.Invoke();
        }
    }
}
