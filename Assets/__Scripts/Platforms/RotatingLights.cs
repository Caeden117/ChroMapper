using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingLights : MonoBehaviour {

    public int Speed = 0;
    public float Offset;

    private float multiplier = 30;
    private Vector3 oldRot = Vector3.zero;
    private bool alternateSpin = false;
    private bool isLeft = false;
    private float f;

    void Start()
    {
        Offset = UnityEngine.Random.Range(-20f, 20);
        oldRot = transform.localEulerAngles;
        multiplier = Mathf.Abs(transform.parent.localEulerAngles.z);
        if (360 - multiplier <= multiplier) multiplier = 360 - multiplier;
        isLeft = transform.parent.name.Contains("Left");
        if (multiplier == 0)
            alternateSpin = true;
    }

    void Update()
    {
        f += Time.deltaTime;
        if (Speed == 0)
        {
            transform.localEulerAngles = oldRot;
            return;
        }
        if (!alternateSpin)
        {
            float rot = (multiplier + Mathf.Sin((f + Offset) * Speed * Time.deltaTime) * multiplier) * (isLeft ? 1 : -1);
            transform.localEulerAngles = new Vector3(0, 0, rot);
        }
        else
        {
            float mult = (float)Math.Pow(2, Mathf.Sin((f + Offset) * Speed * Time.deltaTime) + 1.1f);
            Vector3 rotateAround = new Vector3(0, 0, ((f + Offset) * mult));
            transform.Rotate(rotateAround, Space.Self);
        }
    }
}
