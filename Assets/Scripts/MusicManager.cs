﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class BeatSubband
{    
    public float instantEnergy = 0;
    public float[] historyBuffer = new float[43];
    public int populatedHistory = 0;
}

public class MusicManager : MonoBehaviour {

    public Camera cam;

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

    [Range(0,250)]
    public float constant;

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
    float[] bandBuffer8 = new float[8];
    float[] bufferDecrease8 = new float[8];
    float[] frequencyBandHighest8 = new float[8];

    //64 bands
    float[] freqencyBand64 = new float[64];    
    float[] bandBuffer64 = new float[64];
    float[] bufferDecrease64 = new float[64];
    float[] frequencyBandHighest64 = new float[64];

    public float decrease;
    public float increase;
    public bool useBuffer;

    //Normalised Frequency Data
    float[] frequencyBandNormalised8 = new float[8];  //Use this in gameplay and mechanics when not using buffer.
    float[] bandBufferNormalised8 = new float[8];  //Use this in gameplay and mechanics when using buffer.   

    float[] frequencyBandNormalised64 = new float[64];  
    float[] bandBufferNormalised64 = new float[64];

    //Beat Detection Variables
    BeatSubband[] beatSubbands = new BeatSubband[32];

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
            levelFloor = levelGenerator.GenerateLevel(currentSong);
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
        visualiserHolder.transform.parent = cam.transform;
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
	
	// Update is called once per frame
	void Update () {
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
            }
        }
	}

    void BeatDetection()
    {
        float[] beatSpecL = new float[1024];
        float[] beatSpecR = new float[1024];
        float[] beatSpecAdded = new float[1024];

        currentSong.source.GetSpectrumData(beatSpecL, 0, FFTWindow.BlackmanHarris);
        currentSong.source.GetSpectrumData(beatSpecR, 1, FFTWindow.BlackmanHarris);

        bool beat = false;

        for(int i = 0; i < beatSpecAdded.Length; i++)
        {
            beatSpecAdded[i] = beatSpecL[i] + beatSpecR[i];
        }

        //compute instant energy
        for (int i = 0; i < beatSubbands.Length; i++)
        {

            for (int j = i * 32; j < (i+1) * 32; j++)
            {
                beatSubbands[i].instantEnergy += beatSpecAdded[j];
            }

            beatSubbands[i].instantEnergy /= 32;

            beatSubbands[i].historyBuffer[0] = beatSubbands[i].instantEnergy;
            beatSubbands[i].populatedHistory++;

            if (beatSubbands[i].populatedHistory >= beatSubbands[i].historyBuffer.Length)
            {
                beatSubbands[i].populatedHistory = beatSubbands[i].historyBuffer.Length;
            }

            //local average energy
            float localAverageEnergy = 0f;

            for (int j = 0; j < beatSubbands[i].populatedHistory; j++)
            {
                localAverageEnergy += beatSubbands[i].historyBuffer[j];
                //  localAverageEnergy += historyBuffer[i]; //unsure if need to pow^2 or not. not seems to work but all the algorithms say i need to.
            }

            localAverageEnergy /= 43;

            //constant
            

            //shift data on the buffer
            float[] shiftHistory = new float[beatSubbands[i].historyBuffer.Length];

            for (int j = 0; j < beatSubbands[i].historyBuffer.Length - 1; j++)
            {
                shiftHistory[j + 1] = beatSubbands[i].historyBuffer[j];
            }

            shiftHistory[0] = beatSubbands[i].instantEnergy;

            for (int j = 0; j < beatSubbands[i].historyBuffer.Length; j++)
            {
                beatSubbands[i].historyBuffer[j] = shiftHistory[j];
            }

            //check for beat. 
            if (beatSubbands[i].instantEnergy > (constant * localAverageEnergy))
            {
                beat = true;
            }
        }

        if (beat)
        {
            print("Beat");
        }
        else
        {
            print("no beat");
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
            BandBuffer64();
            CreateAudioBands64();

            for (int i = 0; i < visualiserCubes.Length; i++)
            {
                float iNormal = ((float)i / visualiserCubes.Length) * 0.8f; //times 0.75 to make it a scale of 0 - 0.75
                if (visualiserCubes != null)
                {           
                    
                    if (useBuffer) //outdated and kinda useless since Im not using scale anymore
                    {

                        currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, 0f);

                        if (frequencyBandNormalised64[i] > cutoff)
                        {
                            currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, 0.7f);
                            //currentCubeColor[i] = Color.HSVToRGB(iNormal, 0.9f, Mathf.Clamp(bandBufferNormalised64[i], 0, 0.7f));
                        }
                        else
                        {
                            currentCubeColor[i] -= Color.HSVToRGB(0, 0f, 0.05f);
                        }
                        
                        visualiserCubes[i].GetComponent<Renderer>().material.color = currentCubeColor[i];
                        visualiserCubes[i].GetComponent<Renderer>().material.SetColor("_EmissionColor", currentCubeColor[i]);
                        
                        //visualiserCubes[i].transform.localScale = new Vector3(visualiserCubes[i].transform.localScale.x, (bandBuffer[i] * scaleMultiplier) + startScale, 1);
                    } else
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
            bandBufferNormalised8[i] = (bandBuffer8[i] / frequencyBandHighest8[i]);

        }
    }

    void BandBuffer8()
    {
        for (int i = 0; i < 8; i++)
        {
            if (freqencyBand8[i] > bandBuffer8[i])
            {
                bandBuffer8[i] = freqencyBand8[i];
                bufferDecrease8[i] = decrease;

            }
            else if (freqencyBand8[i] < bandBuffer8[i])
            {
                bandBuffer8[i] -= bufferDecrease8[i];
                bufferDecrease8[i] *= increase;
            }
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
            bandBufferNormalised64[i] = (bandBuffer64[i] / frequencyBandHighest64[i]);

        }
    }

    void BandBuffer64()
    {
        for(int i = 0; i < 64; i++)
        {
            if(freqencyBand64[i] > bandBuffer64[i])
            {
                bandBuffer64[i] = freqencyBand64[i];
                bufferDecrease64[i] = decrease;

            } else if (freqencyBand64[i] < bandBuffer64[i])
            {
                bandBuffer64[i] -= bufferDecrease64[i];
                bufferDecrease64[i] *= increase;
            }
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
        6000-20000 - Brilliance
        */

        int count = 0;
        int sampleBand = 1;
        int power = 0;

        for(int i = 0; i < 64; i++) //algorithm by peer play
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

            freqencyBand64[i] = average * 80;
        }
    }    
}
