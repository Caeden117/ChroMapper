using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LargeRing : MonoBehaviour
{
    public PlatformDescriptor descriptor;
    public float RotationOffset = 1;
    public int Rotations = 0;

    private LargeRing nextRing = null;

    int num;

    // Use this for initialization
    void Start()
    {
        num = int.Parse(transform.name.Split(' ').Last());
        if (descriptor.BigRingsSpawned < descriptor.BigRingsToSpawn)
        {
            descriptor.BigRingsSpawned++;
            GameObject newRing = Instantiate(gameObject, transform.parent);
            newRing.name = "Big Ring " + transform.parent.childCount;
            newRing.transform.localPosition = new Vector3(0, 0, descriptor.BigRingsDistance * descriptor.BigRingsSpawned);
            nextRing = newRing.GetComponent<LargeRing>();
        }
    }

    void Update()
    {
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            Quaternion.Euler((Rotations * 90) + (RotationOffset * num), 90, 0),
            descriptor.BigRingsSpinSpeed * Time.deltaTime
            );
    }

    public IEnumerator Rotate(bool SpinRight)
    {
        Rotations += SpinRight ? 1 : -1;
        yield return new WaitForSeconds(descriptor.BigRingsTimeBetweenSpins);
        if (nextRing == null) yield break;
        nextRing.RotationOffset = RotationOffset;
        nextRing.StartCoroutine(nextRing.Rotate(SpinRight));
    }
}
