using UnityEngine;

public class LightRotation : MonoBehaviour
{
    public Transform Transform;
    public Quaternion StartRotation;
    public Vector3 RotationVector;
    public float SpeedMultiplier;

    private bool initialized;

    private void Start() => Initialize();

    // Capture late builder wiring once without changing the authored rest pose on reinitialization.
    public void Initialize()
    {
        if (initialized)
            return;

        if (Transform == null)
            Transform = transform;

        StartRotation = Transform.rotation;
        initialized = true;
    }

    // Cached angles keep pause and scrub rendering independent of Time.deltaTime.
    public void Apply(float angle)
    {
        Transform.localRotation = StartRotation * Quaternion.Euler(RotationVector * angle);
    }
}
