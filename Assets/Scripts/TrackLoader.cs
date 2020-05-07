using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.Audio;

public class TrackLoader : MonoBehaviour {

    private Object[] objectsToLoad; // Loading in music tracks
    private List<Track> loadedAudioTracks = new List<Track>();

    public void LoadTracks(AudioMixerGroup _mixer, ref Playlist _playlist)
    {

        GameObject tracksgo = new GameObject();
        tracksgo.name = "Tracks";
        tracksgo.transform.parent = gameObject.transform;

        if (Application.isEditor)
        {

            objectsToLoad = Resources.LoadAll("Music", typeof(AudioClip));

            print(objectsToLoad.Length + " objects loaded");

            foreach (var t in objectsToLoad)
            {
                GameObject go = new GameObject();
                Track track = go.AddComponent<Track>();
                track.clip = (AudioClip)t;
                track.name = t.name;
                track.mixer = _mixer;
                track.Initialise();
                loadedAudioTracks.Add(track);

                go.transform.parent = tracksgo.transform;
            }

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
                StartCoroutine(LoadFile(i.FullName, tracksgo, _mixer));
            }

        }

        _playlist = new Playlist(loadedAudioTracks);
    }

    IEnumerator LoadFile(string _path, GameObject _tracksgo, AudioMixerGroup _mixer)
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
        track.mixer = _mixer;
        track.Initialise();
        loadedAudioTracks.Add(track);

        go.transform.parent = _tracksgo.transform;

        //Play();
    }
}
