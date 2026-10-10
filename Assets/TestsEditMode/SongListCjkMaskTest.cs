using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TestsEditMode
{
    // Inspect the imported scene so synthetic render tests cannot miss viewport wiring regressions.
    public class SongListCjkMaskTest
    {
        private Scene scene;
        private GameObject viewport;

        // Preview scenes preserve the user's current editor state while exposing the imported UI components.
        [OneTimeSetUp]
        public void OpenSongSelectPreview()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/01_SongSelectMenu.unity");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (candidate.name == "Viewport" && HasAncestorNamed(candidate, "SongList"))
                    {
                        viewport = candidate.gameObject;
                        break;
                    }
                }

                if (viewport != null)
                {
                    break;
                }
            }

            Assert.That(viewport, Is.Not.Null, "SongList viewport is missing from the song-select scene.");
        }

        // Closing the preview prevents test inspection from changing or saving the production scene.
        [OneTimeTearDown]
        public void CloseSongSelectPreview()
        {
            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Stencil masking also clips fallback submeshes created after the canvas clipping pass.
        [Test]
        public void SongListViewportUsesNativeStencilMask()
        {
            Assert.That(viewport.GetComponent<Mask>(), Is.Not.Null,
                "SongList viewport must stencil-mask newly created fallback submeshes before their first draw.");
            Assert.That(viewport.GetComponent<Image>(), Is.Not.Null,
                "The stencil mask requires the viewport's graphic.");
            Assert.That(viewport.GetComponent<RectMask2D>(), Is.Null,
                "SongList must not depend on a clipping pass that precedes fallback mesh creation.");
        }

        // Direct hierarchy traversal keeps the assertion tied to the SongList when other viewports exist.
        private static bool HasAncestorNamed(Transform child, string name)
        {
            for (var current = child.parent; current != null; current = current.parent)
            {
                if (current.name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
