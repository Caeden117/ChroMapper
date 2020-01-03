using UnityEngine;

public class RotatingWheel : MonoBehaviour {

    public float SpinSpeed = 25;

    private float t; //todo: Is this useful?

    void Update()
    {
        t += Time.deltaTime; //todo: Is this useful?
        transform.localEulerAngles += Vector3.back * (SpinSpeed * Time.deltaTime);
    }
}
