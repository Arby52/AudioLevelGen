using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour {

    private Track track;
    private float levelLength;
    private string levelFloorString = "levelFloor";

    public void GenerateLevel(Track _track)
    {
        if (gameObject.transform.Find(levelFloorString))
        {
            Destroy(gameObject.transform.Find("levelFloor").gameObject);
        }       

        track = _track;
        levelLength = _track.GetTrackLength();

        GameObject levelFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelFloor.transform.SetParent(transform);
        levelFloor.name = levelFloorString;
        levelFloor.transform.position = new Vector3(0, 0, 0);
        levelFloor.transform.localScale = new Vector3(levelLength, 1, 1);        
    }
}
