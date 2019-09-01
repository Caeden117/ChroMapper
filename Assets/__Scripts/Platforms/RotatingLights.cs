using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingLights : MonoBehaviour {

    public int Speed = 0;
    public float Offset;

    private float multiplier = 30;
    private float f;

    void Start()
    {
        Offset = Random.Range(-10f, 10);
        multiplier = Mathf.Abs(transform.parent.localEulerAngles.z);
    }

    void Update()
    {
        f += Time.deltaTime;
        if (Speed != 0)
            transform.localEulerAngles = new Vector3(0, 0, multiplier + Mathf.Sin((f + Offset) * Speed) * multiplier);
        else transform.localEulerAngles = Vector3.zero;
    }
}
