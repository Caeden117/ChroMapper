using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LightManager : MonoBehaviour
{
    private static readonly int directionalLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static readonly int directionalLightPositionId = Shader.PropertyToID("_DirectionalLightPositions");
    private static readonly int directionalLightRadiiId = Shader.PropertyToID("_DirectionalLightRadii");
    private static readonly int directionalLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static readonly int pointLightPositionsId = Shader.PropertyToID("_PointLightPositions");
    private static readonly int pointLightColorsId = Shader.PropertyToID("_PointLightColors");

    private readonly Vector4[] directionalLightDirections = new Vector4[5];
    private readonly Vector4[] directionalLightColors = new Vector4[5];
    private readonly Vector4[] directionalLightPositions = new Vector4[5];
    private readonly float[] directionalLightRadii = new float[5];
    private readonly Vector4[] pointLightPositions = new Vector4[1];
    private readonly Vector4[] pointLightColors = new Vector4[1];

    private int lastRefreshFrameNum = -1;

    private void OnEnable()
    {
        DirectionalLight.Lights ??= new List<DirectionalLight>();
        PointLight.Lights ??= new List<PointLight>();
        Camera.onPreRender = (Camera.CameraCallback)Delegate.Combine(
            Camera.onPreRender,
            new Camera.CameraCallback(OnCameraPreRender));
    }

    private void OnDisable() =>
        Camera.onPreRender = (Camera.CameraCallback)Delegate.Remove(
            Camera.onPreRender,
            new Camera.CameraCallback(OnCameraPreRender));

    private void OnCameraPreRender(Camera currentCamera)
    {
        if (currentCamera.cullingMask != (currentCamera.cullingMask | (1 << gameObject.layer))
            || lastRefreshFrameNum == Time.frameCount)
            return;

        lastRefreshFrameNum = Time.frameCount;
        var dirLight = DirectionalLight.Lights;
        for (var i = 0; i < 5; i++)
        {
            if (i < dirLight.Count && dirLight[i].isActiveAndEnabled)
            {
                var directionalLight = dirLight[i];
                var tr = directionalLight.transform;
                directionalLightPositions[i] = tr.position;
                directionalLightDirections[i] = -tr.forward;
                directionalLightColors[i] = (directionalLight.Color * directionalLight.Intensity).linear;
                directionalLightRadii[i] = directionalLight.Radius;
            }
            else
            {
                directionalLightColors[i] = new Color(0f, 0f, 0f, 0f);
                directionalLightRadii[i] = 100f;
            }
        }

        Shader.SetGlobalVectorArray(directionalLightDirectionsId, directionalLightDirections);
        Shader.SetGlobalVectorArray(directionalLightPositionId, directionalLightPositions);
        Shader.SetGlobalFloatArray(directionalLightRadiiId, directionalLightRadii);
        Shader.SetGlobalVectorArray(directionalLightColorsId, directionalLightColors);
        var pLight = PointLight.Lights;
        for (var j = 0; j < 1; j++)
        {
            if (j < pLight.Count && pLight[j].isActiveAndEnabled)
            {
                var pointLight = pLight[j];
                pointLightPositions[j] = pointLight.transform.position;
                pointLightColors[j] = (pointLight.Color * pointLight.Intensity).linear;
            }
            else
                pointLightColors[j] = new Color(0f, 0f, 0f, 0f);
        }

        Shader.SetGlobalVectorArray(pointLightPositionsId, pointLightPositions);
        Shader.SetGlobalVectorArray(pointLightColorsId, pointLightColors);
    }

    protected void OnDestroy() => ResetColors();

    private void ResetColors()
    {
        for (var i = 0; i < 5; i++) directionalLightColors[i] = new Color(0f, 0f, 0f, 0f);
        for (var j = 0; j < 1; j++) pointLightColors[j] = new Color(0f, 0f, 0f, 0f);

        Shader.SetGlobalVectorArray(directionalLightDirectionsId, directionalLightDirections);
        Shader.SetGlobalVectorArray(directionalLightColorsId, directionalLightColors);
        Shader.SetGlobalVectorArray(pointLightColorsId, pointLightColors);
    }
}
