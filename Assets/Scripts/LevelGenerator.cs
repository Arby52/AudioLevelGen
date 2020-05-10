﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelGenerator : MonoBehaviour {
    
    //prefabs
    public GameObject backgroundPFab;

    public GameObject stairsStartPFab;
    public GameObject stairsMiddlePFab;
    public GameObject stairsEndPFab;

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

    bool groundCol = true;
    Material terrainMaterial;
    Shader shader;

    List<GameObject> cellList = new List<GameObject>();
    GameObject[] shrinkingPlatforms;

    //Shrinking Platform Lerp
    Vector3 shrinkingPlatformOriginal = new Vector3(6, 1, 1);
    Vector3 shrinkingPlatformShrunk = new Vector3(1, 1, 1);
    bool shouldShrink = false;

    private bool isGenerated = false;

    private void Awake()
    {
        shader = Shader.Find("Standard");
        terrainMaterial = new Material(shader);
        terrainMaterial.color = Color.green;
    }

    public GameObject GenerateLevel(Track _track, ref GameObject _player)
    {
        isGenerated = false;

        CreateFloor(_track, ref _player);
        CreateCells();
        PopulateContainers();

        isGenerated = true;
        return levelFloor;
    }

    void PopulateContainers()
    {
        shrinkingPlatforms = GameObject.FindGameObjectsWithTag("ShrinkingPlatform");
    }

    void CreateCells()
    {

        //delete all cells if there are any from before
        if (cellList.Count >= 0) {
            for (int i = 0; i < cellList.Count; i++)
            {
                Destroy(cellList[i]);
            }
        }
        cellList.Clear();        

        float startBuffer = levelBar.transform.localScale.x; //A buffer before any cells spawn
        float endBuffer = levelBar.transform.localScale.x; //A buffer at the end of the level where cells cant spawn
        Vector3 firstCellStart = new Vector3(levelBar.transform.position.x + (levelBar.transform.localScale.x/2) + Cell.width/2, 1, 1);

        //If the song/level is too short to support the smallest cell pattern, return.
        if(startBuffer + endBuffer + (Cell.width*3) > levelLength)
        {
            //maybe add a smaller adjustable cell?
            return;
        }

        //Calculate the number of cells that can spawn in the level. Remaining length variable used to see how much width is left for cell spawning. Important to make sure we always end on the correct cell type.
        int numOfCells = Mathf.FloorToInt(levelLength / Cell.width + startBuffer + endBuffer);
        float remainingLength = levelLength - startBuffer;

        Cell.CellClass previousType = Cell.CellClass.End;

        for (int i = 0; i < numOfCells; i++)
        {
            GameObject cellObj = new GameObject();
            cellObj.transform.position = new Vector3(firstCellStart.x + (Cell.width * i), firstCellStart.y, firstCellStart.z);
            cellObj.transform.parent = levelFloor.transform;

            Cell cell;
            cell = cellObj.AddComponent<Cell>();

            int cellVarient = Random.Range(0, 0);
            bool breakOutOfLoop = false; //Since cell creation is inside a switch, you can't use "break", used to break from the loop once the switch iteration is complete.     

            switch (previousType)
            {
                case Cell.CellClass.Start: //Make a middle one
                    //If making a middle one and the remaining distance is greater than cellwidth*2 + endbuffer, then theres a chance to make another middle one after.
                    if(remainingLength >= (Cell.width*2 + endBuffer))
                    {
                        int chanceOfSecondMiddle = Random.Range(1, 3);
                        if(chanceOfSecondMiddle == 1) //1 in 3 chance to end the middle spawns
                        {
                            cell.cellClassType = Cell.CellClass.Middle;
                            previousType = Cell.CellClass.Middle;
                        } else //2 in 3 chance to add another middle
                        {
                            cell.cellClassType = Cell.CellClass.Start;
                            previousType = Cell.CellClass.Start;
                        }                      
                    } else
                    {
                        cell.cellClassType = Cell.CellClass.Middle;
                        previousType = Cell.CellClass.Middle;
                    }                        

                    //Expands as more varients are added.
                    switch (cellVarient)
                    {
                        case 0: // Stairs Varient
                            cell.cellVarientType = Cell.CellVarient.Stairs;
                            Instantiate(stairsMiddlePFab, cellObj.transform);    
                            break;
                    }
                    break;

                case Cell.CellClass.Middle: //Make an end one
                    cell.cellClassType = Cell.CellClass.End;
                    previousType = Cell.CellClass.End;

                    switch (cellVarient)
                    {
                        case 0: // Stairs Varient
                            cell.cellVarientType = Cell.CellVarient.Stairs;
                            Instantiate(stairsEndPFab, cellObj.transform);
                            break;
                    }
                    break;

                case Cell.CellClass.End: //Make a start one
                    //If there isn't enough space left for a full minimum sized cell pattern, don't create any more.
                    if (remainingLength <= (Cell.width*3) + endBuffer)
                    {
                        Destroy(cellObj);
                        breakOutOfLoop = true;                        
                    } else
                    {
                        cell.cellClassType = Cell.CellClass.Start;
                        previousType = Cell.CellClass.Start;

                        switch (cellVarient)
                        {
                            case 0: // Stairs Varient
                                cell.cellVarientType = Cell.CellVarient.Stairs;
                                Instantiate(stairsStartPFab, cellObj.transform);
                                break;
                        }
                    }
                    break;
            }

            if (breakOutOfLoop)
            {
                break;
            }

            remainingLength -= Cell.width;
            cellList.Add(cellObj);
        }
    }

    void CreateFloor(Track _track, ref GameObject _player)
    {
        //Make sure objects from old song are destroyed.
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
        levelLength = _track.GetTrackLength() * 2;
        lerpTime = 0;

        //Create Floor
        levelFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelFloor.name = levelFloorNameString;
        levelFloor.transform.position = new Vector3(0, 0, 0);
        levelFloor.transform.localScale = new Vector3(levelLength, 1, 2);
        levelFloor.transform.SetParent(transform); //Make sure to set parent after transform and position have been set :)
        DestroyImmediate(levelFloor.GetComponent<BoxCollider>());
        levelFloor.AddComponent<BoxCollider2D>();
        //levelFloor.GetComponent<Renderer>().enabled = false;
        levelFloor.tag = "Ground";
        levelFloor.layer = 8; //ground layer

        //Add and Create Material and Shader for floor        
        Material floorMaterial = new Material(shader);
        floorMaterial.color = Color.red;
        levelFloor.GetComponent<Renderer>().material = floorMaterial;

        //Level Bar
        levelBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        levelBar.name = levelBarNameString;
        float barWidth = levelLength / 40;
        levelBarStartPos = levelFloor.transform.position + new Vector3(-levelLength / 2 + barWidth / 2, 0, -4);
        levelBarEndPos = levelFloor.transform.position + new Vector3(levelLength / 2 - barWidth / 2, 0, -4);
        levelBar.transform.position = levelBarStartPos;
        levelBar.transform.localScale = new Vector3(barWidth, 1, 1);
        levelBar.transform.SetParent(levelFloor.transform);

        //Add and Create Material and Shader for bar        
        levelBar.GetComponent<Renderer>().material = terrainMaterial;
        levelBar.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        levelBar.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);

        //Make hiders to cover the visualisation bars at the start and end of the track.
        GameObject barHiderL = Instantiate(backgroundPFab);
        barHiderL.name = "hiderL";
        barHiderL.transform.localScale = new Vector3(30, levelBar.transform.localScale.y, 0.5f);
        barHiderL.transform.position = new Vector3((levelFloor.transform.position.x - levelFloor.transform.localScale.x / 2) - barHiderL.transform.localScale.x / 2, levelBar.transform.position.y, -2);
        barHiderL.transform.SetParent(transform);

        GameObject barHiderR = Instantiate(backgroundPFab);
        barHiderR.name = "hiderR";
        barHiderR.transform.localScale = new Vector3(30, levelBar.transform.localScale.y, 0.5f);
        barHiderR.transform.position = new Vector3((levelFloor.transform.position.x + levelFloor.transform.localScale.x / 2) + barHiderR.transform.localScale.x / 2, levelBar.transform.position.y, -2);
        barHiderR.transform.SetParent(transform);

        //Set player's position to the start of the new level.
        _player.transform.position = new Vector3(levelBar.transform.position.x, levelBar.transform.position.y + 1, -2);

        lerpTime = 0;
    }

    //Invert Shrinking Bool, called on beat detection event from Music Manager.
    public void ShrinkingPlatform()
    {
        shouldShrink = !shouldShrink;
    }

    //Called on beat detection event from Music Manager.
    public void SwitchTerrainColor()
    {
        GameObject[] a = GameObject.FindGameObjectsWithTag("Ground");
        GameObject[] b = GameObject.FindGameObjectsWithTag("ShrinkingPlatform");

        GameObject[] ground = a.Concat(b).ToArray();

        if (groundCol)
        {
            foreach (var g in ground)
            {
                g.GetComponent<Renderer>().material = terrainMaterial;
                g.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                g.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
            }
            levelBar.GetComponent<Renderer>().material = terrainMaterial;
            levelBar.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            levelBar.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
            groundCol = false;
        }  else
        {
            foreach (var g in ground)
            {
                g.GetComponent<Renderer>().material = terrainMaterial;
                g.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                g.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.blue);
            }
            levelBar.GetComponent<Renderer>().material = terrainMaterial;
            levelBar.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            levelBar.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.blue);
            groundCol = true;
        }             
    }

    public void Update()
    {
        if (isGenerated)
        {
            lerpTime += Time.deltaTime / levelLength;
            levelBar.transform.position = Vector3.Lerp(levelBarStartPos, levelBarEndPos, lerpTime);

            foreach(var platform in shrinkingPlatforms)
            {
                if (platform != null)
                {
                    if (shouldShrink)
                    {
                        platform.transform.localScale = Vector3.Lerp(platform.transform.localScale, shrinkingPlatformShrunk, 0.2f);
                    }
                    else
                    {
                        platform.transform.localScale = Vector3.Lerp(platform.transform.localScale, shrinkingPlatformOriginal, 0.2f);
                    }
                }
            }
        }
    }
}
