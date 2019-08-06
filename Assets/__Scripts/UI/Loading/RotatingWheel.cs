using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingWheel : MonoBehaviour {

    public float SpinSpeed = 25;

    private float t;

    void Update()
    {
        t += Time.deltaTime;
        transform.localEulerAngles += Vector3.back * SpinSpeed * Time.deltaTime;
    }
}
