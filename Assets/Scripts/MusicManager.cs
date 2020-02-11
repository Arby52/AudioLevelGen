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

    [SerializeField]
    private int songIndexToPlay = 0;
    private int previousSongIndex;
    public float songTimeElapsed { get; private set; } = 0;

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
            }

            //Play current track
            Track current = playlist.audioTracks[songIndexToPlay]; //Store the object once so it dosen't have to search the array multiple times. Negligable performance benefit but still a benefit.
            levelGenerator.GenerateLevel(current);
            songTimeElapsed = current.GetTrackLength();
            current.Play();
            print("Now Playing: " + current.name);
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
	}
}
