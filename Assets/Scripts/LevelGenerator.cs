using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour {

    private Track track;    

    GameObject levelFloor;
    GameObject levelBar;
    private float levelLength;

    //Interpolation Variables
    private float lerpTime;
    private float lerpLength;

    Vector3 levelBarStartPos;
    Vector3 levelBarEndPos;

    private string levelFloorNameString = "levelFloor";
    private string levelBarNameString = "levelBar";

    public void GenerateLevel(Track _track)
    {
        
        if (gameObject.transform.Find(levelFloorNameString))
        {
            Destroy(gameObject.transform.Find("levelFloor").gameObject);
        }
        lerpLength = 0;
            

        track = _track;
        levelLength = _track.GetTrackLength();
        lerpTime = 0;

        //Create Floor
        levelFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelFloor.name = levelFloorNameString;
        levelFloor.transform.position = new Vector3(0, 0, 0);
        levelFloor.transform.localScale = new Vector3(levelLength, 1, 1);
        levelFloor.transform.SetParent(transform); //Make sure to set parent after transform and position have been set :)

        //Add and Create Material and Shader
        Shader shader = Shader.Find("Standard");
        Material floorMaterial = new Material(shader);
        floorMaterial.color = Color.red;
        levelFloor.GetComponent<Renderer>().material = floorMaterial;

        levelBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelBar.name = levelBarNameString;
        float barWidth = levelLength / 20;
        levelBarStartPos = levelFloor.transform.position + new Vector3(-levelLength / 2 + barWidth / 2, 0, -1);        
        levelBarEndPos = levelFloor.transform.position + new Vector3(levelLength / 2 - barWidth / 2, 0, -1);        
        levelBar.transform.localScale = new Vector3(barWidth, 1, 1);
        levelBar.transform.SetParent(levelFloor.transform);

        lerpTime = Time.time;
        lerpLength = Vector3.Distance(levelBarStartPos, levelBarEndPos);
        levelBar.transform.position = levelBarStartPos;
    }

    public void Update()
    {
        lerpTime += Time.deltaTime / levelLength;
        levelBar.transform.position = Vector3.Lerp(levelBarStartPos, levelBarEndPos, lerpTime);
    }
}
