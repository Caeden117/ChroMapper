using UnityEngine;
using UnityEngine.XR;

[ExecuteAlways]
public sealed class BlueNoiseDitheringUpdater : MonoBehaviour
{
    [SerializeField] private BlueNoiseDithering blueNoiseDithering;
    [SerializeField] private RandomValueToShader randomValueToShader;

    private void OnEnable()
    {
        Camera.onPreRender -= HandleCameraPreRender;
        Camera.onPreRender += HandleCameraPreRender;
    }

    private void OnDisable() => Camera.onPreRender -= HandleCameraPreRender;

    private void HandleCameraPreRender(Camera renderingCamera)
    {
        randomValueToShader.SetRandomValueToShaders();
        var width = renderingCamera.stereoEnabled ? XRSettings.eyeTextureWidth : renderingCamera.pixelWidth;
        var height = renderingCamera.stereoEnabled ? XRSettings.eyeTextureHeight : renderingCamera.pixelHeight;
        blueNoiseDithering.SetBlueNoiseShaderParams(
            Mathf.Max(width, 1),
            Mathf.Max(height, 1));
    }
}
