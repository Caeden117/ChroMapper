using System;
using UnityEngine;

[ExecuteAlways]
public class SDFArrayManager : MonoBehaviour
{
    [SerializeField] public SDFPoint[] SDFPointArray = Array.Empty<SDFPoint>();

    private Vector4[] sdfArrayValues;
    private bool isInitialized;

    private static readonly int sdfPointsArray = Shader.PropertyToID("_SDFPointArray");

    protected void Awake()
    {
        if (SDFPointArray is { Length: > 0 }) SetSdfPoints(SDFPointArray);
    }

    public void SetSdfPoints(SDFPoint[] points)
    {
        if (points == null || points.Length != 3)
            throw new InvalidOperationException("SDF array manager requires exactly three points.");
        if (Array.Exists(points, point => point == null))
            throw new InvalidOperationException("SDF array manager points must not be null.");

        SDFPointArray = points;
        sdfArrayValues = new Vector4[3];
        isInitialized = true;
    }

    protected void Update()
    {
        if (!isInitialized) return;
        for (var i = 0; i < 3; i++)
        {
            var position = SDFPointArray[i].transform.position;
            sdfArrayValues[i] = new Vector4(position.x, position.y, position.z, SDFPointArray[i].SqrtRadius);
        }

        Shader.SetGlobalVectorArray(sdfPointsArray, sdfArrayValues);
    }
}
