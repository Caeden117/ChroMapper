using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PointLight : MonoBehaviour
{
    public Color Color;
    public float Intensity;

    public static List<PointLight> Lights;

    private void OnEnable()
    {
        Lights ??= new List<PointLight>();
        Lights.Add(this);
    }

    private void OnDisable()
    {
        Lights ??= new List<PointLight>();
        Lights.Remove(this);
    }
}
