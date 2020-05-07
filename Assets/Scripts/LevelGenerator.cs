using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour {

    public GameObject player;
    public GameObject backgroundPFab;
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

    public GameObject GenerateLevel(Track _track)
    {
        isGenerated = false;

        if (gameObject.transform.Find(levelFloorNameString))
        {
            Destroy(gameObject.transform.Find(levelFloorNameString).gameObject);
        }
        if (gameObject.transform.Find("hiderL"))
        {
            Destroy(gameObject.transform.Find("hiderL").gameObject);
        }
        if (gameObject.transform.Find("hiderR"))
        {
            Destroy(gameObject.transform.Find("hiderR").gameObject);
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
        //levelFloor.GetComponent<Renderer>().enabled = false;
        levelFloor.tag = "Ground";

        //Add and Create Material and Shader for floor
        Shader shader = Shader.Find("Standard");
        Material floorMaterial = new Material(shader);
        floorMaterial.color = Color.red;
        levelFloor.GetComponent<Renderer>().material = floorMaterial;

        levelBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelBar.name = levelBarNameString;
        float barWidth = levelLength / 20;
        levelBarStartPos = levelFloor.transform.position + new Vector3(-levelLength / 2 + barWidth / 2, 0, -4);        
        levelBarEndPos = levelFloor.transform.position + new Vector3(levelLength / 2 - barWidth / 2, 0, -4);
        levelBar.transform.position = levelBarStartPos;
        levelBar.transform.localScale = new Vector3(barWidth, 1, 1);
        levelBar.transform.SetParent(levelFloor.transform);

        GameObject barHiderL = Instantiate(backgroundPFab);
        barHiderL.name = "hiderL";
        barHiderL.transform.localScale = new Vector3(30, levelBar.transform.localScale.y, 0.5f);
        barHiderL.transform.position = new Vector3((levelFloor.transform.position.x - levelFloor.transform.localScale.x/2) - barHiderL.transform.localScale.x/2, levelBar.transform.position.y, -2);
        barHiderL.transform.SetParent(transform);

        GameObject barHiderR = Instantiate(backgroundPFab);
        barHiderR.name = "hiderR";
        barHiderR.transform.localScale = new Vector3(30, levelBar.transform.localScale.y, 0.5f);
        barHiderR.transform.position = new Vector3((levelFloor.transform.position.x + levelFloor.transform.localScale.x / 2) + barHiderR.transform.localScale.x / 2, levelBar.transform.position.y, -2);
        barHiderR.transform.SetParent(transform);

        //Add and Create Material and Shader for bar
        Material barMaterial = new Material(shader);
        barMaterial.color = Color.green;
        levelBar.GetComponent<Renderer>().material = barMaterial;

        player.transform.position = new Vector3(levelBar.transform.position.x, levelBar.transform.position.y + 1, -2);

        lerpTime = 0;

        isGenerated = true;

        return levelFloor;
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
