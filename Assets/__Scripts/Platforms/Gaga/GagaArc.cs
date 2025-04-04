using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GagaArc : MonoBehaviour
{
    public GameObject TargetObject;
    public Material ArcMaterial;
    private readonly float thickness = 5f;
    private readonly int increments = 5;
    
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
            float t = (float)i / (increments - 1);
            lineRenderer.SetPosition(i, Vector3.Lerp(
                gameObject.transform.position,
                TargetObject.transform.position,
                t)
            );
        }
    }
}
