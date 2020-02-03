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
    private bool isWaveform;

    private void Start()
    {
        gridXRenderers = GetComponentsInChildren<Renderer>().Where(x => x.material.shader.name.Contains("Grid X")).ToList();
        isWaveform = name == "Waveform Chunks Grid" || name == "Spectrogram Grid";
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Settings.Instance.WaveformWorkflow && isWaveform)
        {
            InverseXExpansion = UIWorkflowToggle.SelectedWorkflowGroup == 0;
            if (!InverseXExpansion)
                attachedPosition = new Vector3(23.5f, attachedPosition.y, attachedPosition.z);
            else attachedPosition = new Vector3(-8, attachedPosition.y, attachedPosition.z);
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
