using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Track : MonoBehaviour {

    public AudioSource source;
    public AudioClip clip;
    public AudioMixerGroup mixer;
    public string clipName;
    

    public void Initialise()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.name = clipName;
        source.outputAudioMixerGroup = mixer;
    }

    public void Play()
    {
        source.Play();
    }

    public void Stop()
    {
        source.Stop();
    }

    public void Pause()
    {
        source.Pause();
    }

    public void UnPause()
    {
        source.UnPause();
    }

    public float GetTrackLength()
    {
        return clip.length;
    }

    public bool IsPlaying()
    {
        return source.isPlaying;
    }

}
