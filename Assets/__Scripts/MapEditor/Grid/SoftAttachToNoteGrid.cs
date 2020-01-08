using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoftAttachToNoteGrid : MonoBehaviour
{
    [SerializeField] private Vector3 attachedPosition;
    [SerializeField] private Vector3 unattachedPosition;
    [SerializeField] private Transform noteGrid;
    
    [SerializeField] private UIWorkflowToggle UIWorkflowToggle;

    private List<Renderer> gridXRenderers = new List<Renderer>();

    public bool AttachedToNoteGrid = true;
    public bool InverseXExpansion;

    private float originalStart = 0.41f;
    private static readonly int Offset = Shader.PropertyToID("_Offset");
    private bool ignoreWorkflow;

    private void Start()
    {
        gridXRenderers = GetComponentsInChildren<Renderer>().Where(x => x.material.shader.name.Contains("Grid X")).ToList();
        ignoreWorkflow = UIWorkflowToggle == null;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ignoreWorkflow && Settings.Instance.WaveformWorkflow && UIWorkflowToggle.SelectedWorkflowGroup == 1)
        {
            var transform1 = transform;
            Vector3 vec = transform1.position;
            vec.x = 24;
            transform1.position = vec;
            return;
        }
        if (AttachedToNoteGrid)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, noteGrid.localEulerAngles.y,
                transform.localEulerAngles.z);
            float x = (noteGrid.localScale.x - originalStart) * 5 * (InverseXExpansion ? -1 : 1);
            Vector3 side = noteGrid.right.normalized * (attachedPosition.x + x);
            Vector3 up = noteGrid.up.normalized * attachedPosition.y;
            Vector3 forward = noteGrid.forward.normalized * attachedPosition.z;
            Vector3 total = side + up + forward;
            transform.position = noteGrid.position + total;
            foreach (Renderer g in gridXRenderers)
                g.material.SetFloat(Offset, noteGrid.position.x * -1);
        }
        else
        {
            transform.localEulerAngles = Vector3.zero;
            transform.position = unattachedPosition;
        }
    }
}
