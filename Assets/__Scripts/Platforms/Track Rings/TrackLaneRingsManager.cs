using UnityEngine;

public class TrackLaneRingsManager : MonoBehaviour
{
    public TrackLaneRing[] rings { get; private set; }

    [SerializeField] public int ringCount = 10;
    [SerializeField] public TrackLaneRing prefab;
    [SerializeField] public float minPositionStep = 1;
    [SerializeField] public float maxPositionStep = 2;
    [SerializeField] public float moveSpeed = 1;
    [Header("Rotation")]
    [SerializeField] public float rotationStep = 5;
    [SerializeField] public float propagationSpeed = 1;
    [SerializeField] public float flexySpeed = 1;
    [SerializeField] public TrackLaneRingsRotationEffect rotationEffect;

    private bool zoomed = false;

    public void Start()
    {
        prefab.gameObject.SetActive(false);
        rings = new TrackLaneRing[ringCount];
        for (int i = 0; i < rings.Length; i++)
        {
            rings[i] = Instantiate(prefab, transform);
            rings[i].gameObject.SetActive(true);
            Vector3 pos = new Vector3(0, 0, i * maxPositionStep);
            rings[i].Init(pos, Vector3.zero);
        }
    }

    public void HandlePositionEvent()
    {
        float step = zoomed ? maxPositionStep : minPositionStep;
        zoomed = !zoomed;
        for (int i = 0; i < rings.Length; i++)
        {
            float destPosZ = i * step;
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
