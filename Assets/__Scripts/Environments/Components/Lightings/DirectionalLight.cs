using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DirectionalLight : MonoBehaviour
{
    [ColorUsage(false)] public Color Color;
    public float Intensity;
    public float Radius = 50f;

    public static List<DirectionalLight> Lights;

    private void OnEnable()
    {
        Lights ??= new List<DirectionalLight>();
        Lights.Add(this);
    }

    private void OnDisable()
    {
        Lights ??= new List<DirectionalLight>();
        Lights.Remove(this);
    }
}
