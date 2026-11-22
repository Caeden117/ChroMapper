using NUnit.Framework;
using UnityEngine;

namespace TestsEditMode
{
    public class FullscreenResolutionTest
    {
        [TestCase(3840, 2160)]
        [TestCase(3840, 1080)]
        public void FullscreenUsesDisplayResolutionAndRestoresWindowedSize(int displayWidth, int displayHeight)
        {
            var windowed = new Vector2Int(1536, 864);
            var display = new Vector2Int(displayWidth, displayHeight);
            Vector2Int? savedWindowed = null;

            var entering = CameraController.GetFullscreenRequest(
                false,
                windowed,
                display,
                ref savedWindowed);
            Assert.That(entering.Mode, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(entering.Resolution, Is.EqualTo(display), "Fullscreen must request a display-sized render surface.");

            var leaving = CameraController.GetFullscreenRequest(
                true,
                display,
                display,
                ref savedWindowed);
            Assert.That(leaving.Mode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(leaving.Resolution, Is.EqualTo(windowed));
        }

        [Test]
        public void FullscreenAtStartupUsesUnityWindowRestore()
        {
            var display = new Vector2Int(3840, 1080);
            Vector2Int? savedWindowed = null;

            var leaving = CameraController.GetFullscreenRequest(
                true,
                display,
                display,
                ref savedWindowed);

            Assert.That(leaving.Mode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(leaving.Resolution, Is.Null);
        }
    }
}
