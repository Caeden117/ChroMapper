using UnityEngine;

public class ReflectionProbeSnapToY : MonoBehaviour
{
    void Update()
    {
        transform.localPosition = transform.parent.worldToLocalMatrix.MultiplyPoint(
            new Vector3(transform.parent.position.x, -transform.parent.position.y * 1.5120233035103884909193591534643f, transform.parent.position.z));
    }
}
