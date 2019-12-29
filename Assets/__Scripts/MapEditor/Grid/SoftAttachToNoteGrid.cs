using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftAttachToNoteGrid : MonoBehaviour
{
    [SerializeField] private Vector3 attachedPosition;
    [SerializeField] private Vector3 unattachedPosition;
    [SerializeField] private Transform noteGrid;

    public bool AttachedToNoteGrid = true;
    public bool InverseXExpansion = false;

    private float originalStart = 0.4f;

    // Update is called once per frame
    void Update()
    {
        if (AttachedToNoteGrid)
        {
            transform.localEulerAngles = noteGrid.localEulerAngles;
            float x = (noteGrid.localScale.x - originalStart - 0.01f) * 5 * (InverseXExpansion ? -1 : 1);
            Vector3 side = noteGrid.right * (attachedPosition.x + x);
            Vector3 up = noteGrid.up * attachedPosition.y;
            Vector3 forward = noteGrid.forward * attachedPosition.z;
            Vector3 total = side + up + forward;
            transform.position = noteGrid.position + total;
        }
        else
        {
            transform.localEulerAngles = Vector3.zero;
            transform.position = unattachedPosition;
        }
    }
}
