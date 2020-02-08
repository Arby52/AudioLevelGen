using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Track : MonoBehaviour {

    public AudioSource source;
    public AudioClip clip;
    public AudioMixerGroup mixer;
    public string name;
    

    public void Initialise()
    {
        source = gameObject.AddComponent<AudioSource>();
        print(source);
        source.clip = clip;
        source.name = name;
        source.outputAudioMixerGroup = mixer;
    }

    public void Play()
    {
        source.Play();
    }

}
