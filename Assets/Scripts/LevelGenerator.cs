using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour {

    public GameObject player;

    private Track track;    

    GameObject levelFloor;
    GameObject levelBar;
    private float levelLength = 300;

    //Interpolation Variables
    private float lerpTime;

    Vector3 levelBarStartPos;
    Vector3 levelBarEndPos;

    private string levelFloorNameString = "levelFloor";
    private string levelBarNameString = "levelBar";

    private bool isGenerated = false;

    public void GenerateLevel(Track _track)
    {
        isGenerated = false;

        if (gameObject.transform.Find(levelFloorNameString))
        {
            Destroy(gameObject.transform.Find("levelFloor").gameObject);
        }
        print(_track);
        track = _track;
        levelLength = _track.GetTrackLength();
        lerpTime = 0;

        //Create Floor
        levelFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelFloor.name = levelFloorNameString;
        levelFloor.transform.position = new Vector3(0, 0, 0);
        levelFloor.transform.localScale = new Vector3(levelLength, 1, 2);
        levelFloor.transform.SetParent(transform); //Make sure to set parent after transform and position have been set :)
        DestroyImmediate(levelFloor.GetComponent<BoxCollider>());
        BoxCollider2D bx = levelFloor.AddComponent<BoxCollider2D>();
        levelFloor.tag = "Ground";

        //Add and Create Material and Shader for floor
        Shader shader = Shader.Find("Standard");
        Material floorMaterial = new Material(shader);
        floorMaterial.color = Color.red;
        levelFloor.GetComponent<Renderer>().material = floorMaterial;

        levelBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelBar.name = levelBarNameString;
        float barWidth = levelLength / 20;
        levelBarStartPos = levelFloor.transform.position + new Vector3(-levelLength / 2 + barWidth / 2, 0, -1);        
        levelBarEndPos = levelFloor.transform.position + new Vector3(levelLength / 2 - barWidth / 2, 0, -1);
        levelBar.transform.position = levelBarStartPos;
        levelBar.transform.localScale = new Vector3(barWidth, 1, 1);
        levelBar.transform.SetParent(levelFloor.transform);

        //Add and Create Material and Shader for bar
        Material barMaterial = new Material(shader);
        barMaterial.color = Color.green;
        levelBar.GetComponent<Renderer>().material = barMaterial;

        player.transform.position = new Vector3(levelBar.transform.position.x, levelBar.transform.position.y + 1, -1);

        lerpTime = 0;

        isGenerated = true;
    }

    public void Update()
    {
        if (isGenerated)
        {
            lerpTime += Time.deltaTime / levelLength;
            levelBar.transform.position = Vector3.Lerp(levelBarStartPos, levelBarEndPos, lerpTime);
        }
    }
}
