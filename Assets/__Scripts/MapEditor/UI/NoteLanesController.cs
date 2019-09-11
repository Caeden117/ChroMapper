using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteLanesController : MonoBehaviour {

    public Transform noteGrid;

    public float NoteLanes
    {
        get
        {
            return (noteGrid.localScale.x - 0.01f) * 10;
        }
    }

    public void UpdateNoteLanes(string noteLanesText)
    {
        if (int.TryParse(noteLanesText, out int noteLanes))
        {
            if (noteLanes < 4) return;
            noteLanes = noteLanes - (noteLanes % 2); //Sticks to even numbers for note lanes.
            noteGrid.localScale = new Vector3((float)noteLanes / 10 + 0.01f, 1, noteGrid.localScale.z);
        }
    }
}
