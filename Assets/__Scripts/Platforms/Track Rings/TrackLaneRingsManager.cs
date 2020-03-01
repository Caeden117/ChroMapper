using UnityEngine;

public class TrackLaneRingsManager : MonoBehaviour
{
    public TrackLaneRing[] rings { get; private set; }

    public int ringCount = 10;
    public TrackLaneRing prefab;
    public float minPositionStep = 1;
    public float maxPositionStep = 2;
    public float ringPositionStep = 2;
    public float moveSpeed = 1;
    [Header("Rotation")]
    public float rotationStep = 5;
    public float propagationSpeed = 1;
    public float flexySpeed = 1;
    public TrackLaneRingsRotationEffect rotationEffect;

    private bool zoomed = false;

    public void Start()
    {
        prefab.gameObject.SetActive(false);
        rings = new TrackLaneRing[ringCount];
        for (int i = 0; i < rings.Length; i++)
        {
            rings[i] = Instantiate(prefab, transform);
            rings[i].gameObject.SetActive(true);
            Vector3 pos = new Vector3(0, 0, i * ringPositionStep);
            rings[i].Init(pos, Vector3.zero);
        }
    }

    public void HandlePositionEvent()
    {
        float step = zoomed ? maxPositionStep : minPositionStep;
        zoomed = !zoomed;
        for (int i = 0; i < rings.Length; i++)
        {
            float destPosZ = (i * step) + (i * ringPositionStep);
            rings[i].SetPosition(destPosZ, moveSpeed);
        }
    }

    public void HandleRotationEvent()
    {
        rotationEffect.AddRingRotationEvent(rings[0].GetDestinationRotation() + 90f * (Random.value < 0.5f ? 1 : -1),
            Random.Range(0, rotationStep), propagationSpeed, flexySpeed);
    }

    private void Update()
    {
        foreach (TrackLaneRing ring in rings) ring.UpdateRing(Time.deltaTime);
    }
}
