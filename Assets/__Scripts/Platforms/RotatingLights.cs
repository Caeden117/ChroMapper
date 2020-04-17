using UnityEngine;

public class RotatingLights : MonoBehaviour {

    private int speed;
    private Vector3 rotationVector = Vector3.up;

    [SerializeField] public float multiplier = 20;
    [SerializeField] private float rotationSpeed = 0;
    [SerializeField] private float zPositionModifier = 1;
    private Quaternion startRotation;

    public bool OverrideLightGroup = false;
    public int OverrideLightGroupID = 0;
    public bool UseZPositionForAngleOffset = false;

    private void Start()
    {
        startRotation = transform.localRotation;
        if (OverrideLightGroup)
        {
            PlatformDescriptor descriptor = GetComponentInParent<PlatformDescriptor>();
            descriptor?.LightingManagers[OverrideLightGroupID].RotatingLights.Add(this);
        }
    }

    private void Update()
    {
        transform.Rotate(rotationVector, Time.deltaTime * rotationSpeed, Space.Self);
    }

    public void UpdateOffset(int Speed, float Rotation, bool RotateForwards)
    {
        speed = Speed;
        transform.localRotation = startRotation;
        if (Speed > 0)
        {
            if (UseZPositionForAngleOffset)
            {
                Rotation = Time.frameCount + (transform.position.z * zPositionModifier);
            }
            transform.Rotate(rotationVector, Rotation, Space.Self);
            rotationSpeed = speed * multiplier * (RotateForwards ? 1 : -1);
        }
        else rotationSpeed = 0;
    }
}
