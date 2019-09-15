using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectionProbeSnapToY : MonoBehaviour
{
    void Update()
    {
        transform.localPosition = new Vector3(0, -transform.parent.position.y, 0);
    }
}
