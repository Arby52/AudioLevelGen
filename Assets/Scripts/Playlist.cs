using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Playlist  {

    public List<Track> audioTracks { get; private set; }

    public Playlist(List<Track> _audioTracks)
    {
        audioTracks = _audioTracks;
    }
}
