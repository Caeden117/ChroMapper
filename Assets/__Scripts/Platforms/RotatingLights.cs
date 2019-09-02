using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingLights : MonoBehaviour {

    public int Speed = 0;
    public float Offset;

    private float multiplier = 30;
    private bool useTan = false;
    private bool isLeft = false;
    private float f;

    void Start()
    {
        Offset = Random.Range(-10f, 10);
        multiplier = Mathf.Abs(transform.parent.localEulerAngles.z);
        if (360 - multiplier <= multiplier) multiplier = 360 - multiplier;
        isLeft = transform.parent.name.Contains("Left");
        if (multiplier == 0)
        {
            multiplier = transform.localEulerAngles.z;
            useTan = true;
        }
    }

    void Update()
    {
        f += Time.deltaTime;
        if (!useTan)
        {
            float rot = (multiplier + Mathf.Sin((f + Offset) * Speed) * multiplier) * (isLeft ? 1 : -1);
            if (Speed != 0)
                transform.localEulerAngles = new Vector3(0, 0, rot);
            else transform.localEulerAngles = Vector3.zero;
        }
        else
            transform.localEulerAngles = new Vector3(0, 0, multiplier + Mathf.Tan((f + Offset) * Speed));
    }
}
