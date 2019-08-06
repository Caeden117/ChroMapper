using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SmallRing : MonoBehaviour {

    public PlatformDescriptor descriptor;
    public float RotationOffset = 14f;
    public float distance = 5;
    int num;
    int Rotations = 0;

    // Use this for initialization
    void Start()
    {
        num = int.Parse(transform.name.Split(' ').Last());
        if (descriptor.SmallRingsSpawned < descriptor.SmallRingsToSpawn)
        {
            descriptor.SmallRingsSpawned++;
            GameObject newRing = Instantiate(gameObject, transform);
            newRing.name = "Small Ring " + descriptor.SmallRingsSpawned;
            newRing.transform.localRotation = Quaternion.identity;
            newRing.transform.localPosition = new Vector3(-descriptor.SmallRingsExpandedDistance, 0, 0);
            newRing.GetComponent<SmallRing>().distance = descriptor.SmallRingsExpandedDistance;
        }	
	}

    public void Rotate(bool SpinRight, bool Spin = true, bool Expanded = true)
    {
        if (Spin) Rotations += 1 * (SpinRight ? 1 : -1);
        if (transform.Find("Small Ring " + (num + 1).ToString()))
        {
            SmallRing ring = transform.Find("Small Ring " + (num + 1).ToString()).GetComponent<SmallRing>();
            ring.RotationOffset = RotationOffset;
            ring.distance = Expanded ? descriptor.SmallRingsExpandedDistance : descriptor.SmallRingsZoomedDistance;
            ring.Rotate(SpinRight, Spin, Expanded);
        }
    }

    void Update()
    {
        if (transform.name == "Small Ring 0") {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                Quaternion.Euler(Rotations * 90, 90, 0),
                descriptor.SmallRingsSpinSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                new Vector3(-distance, 0, 0),
                descriptor.SmallRingsSpinSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                Quaternion.Euler(RotationOffset, 0, 0),
                descriptor.SmallRingsSpinSpeed * Time.deltaTime
                );
        }
    }
}
