using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteLanesController : MonoBehaviour {

    public Transform[] noteInterfaces;

    public float NoteLanes
    {
        get
        {
            return (noteInterfaces[0].localScale.x - 0.01f) * 10;
        }
    }

    public void UpdateNoteLanes(string noteLanesText)
    {
        int noteLanes = -1;
        if (int.TryParse(noteLanesText, out noteLanes))
        {
            if (noteLanes < 4) return;
            noteLanes = noteLanes - (noteLanes % 2); //Sticks to even numbers for note lanes.
            foreach (Transform t in noteInterfaces)
                t.localScale = new Vector3((float)noteLanes / 10 + 0.01f, 1, t.localScale.z);
        }
    }
}
