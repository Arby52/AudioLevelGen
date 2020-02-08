using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour {

    private Object[] objectsToLoad;
    private List<Track> loadedAudioTracks = new List<Track>();  
    public Playlist playlist { get; private set; }

    public AudioMixerGroup mixer;

    public int songIndexToPlay = 0;

	// Use this for initialization
	void Start () {        

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
        if (!(songIndexToPlay >= playlist.audioTracks.Count))
        {
            playlist.audioTracks[songIndexToPlay].Play();
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
