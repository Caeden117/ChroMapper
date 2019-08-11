using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingLights : MonoBehaviour {

    public int Speed = 0;
    public float Offset;

    [SerializeField] private float Multiplier = 30;

    private float f;
    private float ogZ = 0;

    void Start()
    {
        Offset = Random.Range(-10f, 10);
        ogZ = transform.localRotation.eulerAngles.z;
    }

    void Update()
    {
        f += Time.deltaTime;
        transform.localRotation = Quaternion.Euler(0, 0,
            ogZ + Mathf.Sin((f + Offset) * Speed) * Multiplier);
    }
}
