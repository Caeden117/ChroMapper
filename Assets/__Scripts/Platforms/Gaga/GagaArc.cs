using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GagaArc : MonoBehaviour
{
    public GameObject TargetObject;
    public Material ArcMaterial;
    private const float thickness = 1f;

    private GameObject lightningMesh;
    void Start()
    {
        lightningMesh = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var meshRenderer = lightningMesh.GetComponent<Renderer>();
        meshRenderer.material = ArcMaterial;
        lightningMesh.transform.SetParent(gameObject.transform, false);
        lightningMesh.transform.position = Vector3.zero;
    }
    void Update()
    {
        // Update plane transform between two points.
        var sPosition = transform.position;
        var tPosition = TargetObject.transform.position;
       
        // A multiplier (-1 or 1) for the rotation of the plane to make it face the correct direction.
        // Also accounts for inner towers as they face the opposite direction compared to the outer towers.
        float towerFilter = -Math.Sign(tPosition.x - (Math.Sign(tPosition.x) * 14.08)); 
       
        var direction = new Vector3(tPosition.x - sPosition.x, 0, tPosition.z - sPosition.z);
        var rot = Quaternion.LookRotation(direction).eulerAngles;
        var heightAngle = Mathf.Atan((tPosition.y - sPosition.y) / (tPosition.z - sPosition.z)) * Mathf.Rad2Deg;
        if (Mathf.Abs(tPosition.x) < 14.08) heightAngle = -heightAngle;
        
        lightningMesh.transform.SetPositionAndRotation(
            (sPosition + tPosition) / 2f,
            Quaternion.Euler((-90 * towerFilter) - heightAngle, rot.y, 90));
        
        lightningMesh.transform.localScale = new Vector3(Vector3.Distance(sPosition,tPosition) / 10f, 1, thickness); 
    }
}
