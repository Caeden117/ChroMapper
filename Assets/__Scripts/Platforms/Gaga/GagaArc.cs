using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GagaArc : MonoBehaviour
{
    public GameObject TargetObject;
    public Material ArcMaterial;
    private readonly float thickness = 3.5f;
    private readonly int increments = 10;
    
    private LineRenderer lineRenderer;
    // TODO: Align PROPERLY with Z.
    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = ArcMaterial;
        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;
        lineRenderer.positionCount = increments;
    }
    void Update()
    {
        for (int i = 0; i < increments; i++)
        {
            float t = (float)i / (increments - 1); // Normalize t between 0 and 1
            lineRenderer.SetPosition(i, Vector3.Lerp(
                gameObject.transform.position,
                TargetObject.transform.position,
                t)
            );
        }
    }
}
