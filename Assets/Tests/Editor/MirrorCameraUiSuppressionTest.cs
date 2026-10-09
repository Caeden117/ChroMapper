using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.Editor
{
    public class MirrorCameraUiSuppressionTest
    {
        // The real mirror renderer must suppress canvas work before its nested Camera.Render;
        // directly calling the suppressor would miss a regression in the production call order.
        [UnityTest]
        public IEnumerator MirrorRenderSkipsCanvasCallbacksAndRestoresThemAfterward()
        {
            var cameraObject = new GameObject("Mirror UI callback test camera", typeof(Camera));
            var canvasObject = new GameObject("Mirror UI callback test canvas", typeof(RectTransform), typeof(Canvas));
            var imageObject = new GameObject("Mirror UI callback test image", typeof(RectTransform), typeof(Image));
            var camera = cameraObject.GetComponent<Camera>();
            var canvas = canvasObject.GetComponent<Canvas>();
            var previousMirrorQuality = Settings.Instance.MirrorQuality;
            Settings.Instance.MirrorQuality = (int)MirrorRendererSO.MirrorQuality.Low;
            var mirrorRenderer = ScriptableObject.CreateInstance<MirrorRendererSO>();
            var callbackCount = 0;
            Canvas.WillRenderCanvases countCallback = () => callbackCount++;

            try
            {
                camera.enabled = false;
                camera.cullingMask = 0;
                camera.transform.position = new Vector3(0f, 1f, -3f);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                imageObject.transform.SetParent(canvasObject.transform, false);
                Canvas.willRenderCanvases += countCallback;

                yield return null;
                callbackCount = 0;
                // Exercise the public mirror path, including its internal camera creation and callback scope.
                var reflectionTexture = mirrorRenderer.RenderMirrorTexture(camera, Vector3.zero, Vector3.up);
                Assert.That(reflectionTexture, Is.Not.Null, "The test did not actually render a mirror texture.");
                Assert.That(callbackCount, Is.Zero, "The mirror camera ran UI callbacks during its reflection render.");

                Canvas.ForceUpdateCanvases();
                Assert.That(callbackCount, Is.GreaterThan(0), "Mirror suppression did not restore UI callbacks afterward.");
            }
            finally
            {
                Canvas.willRenderCanvases -= countCallback;
                UnityEngine.Object.DestroyImmediate(mirrorRenderer);
                Settings.Instance.MirrorQuality = previousMirrorQuality;
                UnityEngine.Object.DestroyImmediate(imageObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
