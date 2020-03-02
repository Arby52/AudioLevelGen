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
    GameObject[] visualiserCubes = new GameObject[512];

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
        float divisionSpace = 512 / currentSong.GetTrackLength();

        GameObject visualiserHolder = new GameObject();
        visualiserHolder.transform.position = transform.position;
        visualiserHolder.transform.parent = transform;
        visualiserHolder.name = "Visual Holder";
        for(int i = 0; i < 512; i++)
        {
            GameObject cube = Instantiate(visualiserCubePrefab);
            cube.transform.position = transform.position;
            cube.transform.parent = visualiserHolder.transform;
            cube.name = "Cube " + i;
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
        if (songPlaying)
        {
            //The higher the array, the most accurate the data. However it will take longer to use. Needs to be multiple of 8.
            float[] spectrumData = new float[512];
            FFTWindow window = FFTWindow.Hanning;  //compare all windowing to see which works best for the system.
            currentSong.source.GetSpectrumData(spectrumData, 0, window);
            for(int i = 1; i < spectrumData.Length-1; i++)
            {
                Debug.DrawLine(new Vector3(i - 1, spectrumData[i] + 10, 0), new Vector3(i, spectrumData[i + 1] + 10, 0), Color.red);
                Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrumData[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrumData[i]) + 10, 2), Color.cyan);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrumData[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrumData[i] - 10, 1), Color.green);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrumData[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrumData[i]), 3), Color.blue);
            }
        }
	}
}
