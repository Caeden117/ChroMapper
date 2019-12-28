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
            foreach(BoxCollider boxCollider in noteGrid.GetComponentsInChildren<BoxCollider>())
            {
                if (boxCollider.transform.GetComponent<PlacementMessageSender>() == null) continue;
                float scaleX = 10 / noteGrid.localScale.x;
                float scaleZ = 10 / boxCollider.transform.parent.localScale.z;
                    boxCollider.size = new Vector3(scaleX * (noteGrid.localScale.x - 0.02f),
                    boxCollider.size.y, scaleZ * (boxCollider.transform.parent.localScale.z - 0.2f));
            }
        }
    }
}
