using System.Collections;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class FullscreenPointerTest
    {
        private static readonly (Vector2Int Size, Vector2Int Position)[] FullscreenClients =
        {
            (new Vector2Int(3840, 2160), new Vector2Int(750, 1619)),
            (new Vector2Int(3840, 1080), new Vector2Int(1335, 809)),
            (new Vector2Int(1920, 1440), new Vector2Int(375, 989))
        };

        [UnityTest]
        public IEnumerator PointerHitsUiAndGridAfterFullscreenAndReturnToWindowed(
            [ValueSource(nameof(FullscreenClients))] (Vector2Int Size, Vector2Int Position) fullscreenClient)
        {
            yield return null;

            var cameraObject = new GameObject("Pointer test camera", typeof(Camera));
            var targetObject = new GameObject("Pointer test hitbox", typeof(RectTransform));
            var inputFixture = new IsolatedInputTestFixture();
            inputFixture.Setup();
            try
            {
                var mouse = InputSystem.AddDevice<Mouse>();
                using var input = new CMInput();
                var gridPointer = input.Utils.MouseMovement;
                gridPointer.Enable();

                var screenSize = new Vector2Int(1920, 1080);
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.pixelRect = new Rect(0, 0, screenSize.x, screenSize.y);
                var hitbox = targetObject.GetComponent<RectTransform>();
                hitbox.position = camera.ScreenToWorldPoint(new Vector3(375, 270, 10));
                hitbox.sizeDelta = Vector2.one * 0.1f;
                var windowedPosition = new Vector2Int(375, 809);

                AssertPointerHits(windowedPosition, screenSize, false);
                AssertPointerHits(fullscreenClient.Position, fullscreenClient.Size, true);
                AssertPointerHits(windowedPosition, screenSize, false);

                void AssertPointerHits(Vector2Int clientPosition, Vector2Int clientSize, bool letterboxed)
                {
                    var position = WindowsHighPollingRateWorkaround.MapClientPosition(
                        clientPosition,
                        clientSize,
                        screenSize,
                        letterboxed);
                    WindowsHighPollingRateWorkaround.ApplyMouseState(
                        mouse,
                        position,
                        Vector2.zero,
                        Vector2.zero,
                        0);

                    var uiPosition = mouse.position.ReadValue();
                    var uiHit = RectTransformUtility.RectangleContainsScreenPoint(hitbox, uiPosition, camera);
                    var gridPosition = gridPointer.ReadValue<Vector2>();
                    var gridRay = camera.ScreenPointToRay(gridPosition);
                    var gridHit = new Plane(Vector3.back, hitbox.position).Raycast(gridRay, out var distance)
                        && (gridRay.GetPoint(distance) - hitbox.position).sqrMagnitude < 0.0001f;
                    Assert.That(
                        (uiHit, gridHit),
                        Is.EqualTo((true, true)),
                        $"Pointer at {clientPosition} in a {clientSize} client missed the target. UI: {uiPosition}, grid: {gridPosition}.");
                }
            }
            finally
            {
                inputFixture.TearDown();
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
