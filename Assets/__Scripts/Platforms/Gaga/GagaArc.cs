using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GagaArc : MonoBehaviour
{
    public GameObject targetObject;
    public Material arcMaterial;
    private float thickness = 1.25f;
    
    private LineRenderer lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = arcMaterial;
       // lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, gameObject.transform.position);
        lineRenderer.SetPosition(1, targetObject.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(0, gameObject.transform.position);
        lineRenderer.SetPosition(1, targetObject.transform.position);
    }
}
