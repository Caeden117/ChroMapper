using UnityEngine;

public class RotatingWheel : MonoBehaviour {

    public float spinSpeed = 25;

    void Update()
    {
        transform.localEulerAngles += Vector3.back * (spinSpeed * Time.deltaTime);
    }
}
