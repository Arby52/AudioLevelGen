using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour {

    static public float width = 15;
    public bool color = false;
    
    public enum CellVarient
    {
        Stairs //0
    }

    public enum CellClass
    {
        Start, //0
        Middle,
        End
    }
    
    public CellVarient cellVarientType;
    public CellClass cellClassType;

    public void Initialize()
    {

    }
}
