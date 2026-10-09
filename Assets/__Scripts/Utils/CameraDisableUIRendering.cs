using System.Reflection;
using UnityEngine;

public class CameraDisableUIRendering : MonoBehaviour
{
    private static readonly FieldInfo willRenderCanvases = typeof(Canvas).GetField("willRenderCanvases", BindingFlags.NonPublic | BindingFlags.Static);

    private object canvasHackObject;

    public void SuppressCanvasCallbacks()
    {
        canvasHackObject = willRenderCanvases.GetValue(null);
        willRenderCanvases.SetValue(null, null);
    }

    public void RestoreCanvasCallbacks()
    {
        willRenderCanvases.SetValue(null, canvasHackObject);
    }
}
