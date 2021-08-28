using UnityEngine;
using UnityEngine.Serialization;

public class RotatingWheel : MonoBehaviour
{
    [FormerlySerializedAs("spinSpeed")] public float SpinSpeed = 25;

    private void Update() => transform.localEulerAngles += Vector3.back * (SpinSpeed * Time.deltaTime);
}
