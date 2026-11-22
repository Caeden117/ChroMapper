using System.Collections;
using NUnit.Framework;
using TMPro;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.Editor
{
    public class PersistentToastAppearanceTest : TestBase
    {
        [Test]
        public void BottomToastHasDarkBackgroundAboveTimeline()
        {
            var persistentUi = Object.FindAnyObjectByType<PersistentUI>();
            var bottomToast = FindChild(persistentUi.transform, "Small Message");

            Assert.That(bottomToast, Is.Not.Null);
            var background = bottomToast.GetComponent<Image>();
            Assert.That(background, Is.Not.Null);
            Assert.That(background.color.r, Is.LessThan(0.2f));
            Assert.That(background.color.g, Is.LessThan(0.2f));
            Assert.That(background.color.b, Is.LessThan(0.2f));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Global/PersistentCanvas.prefab");
            var idleToast = FindChild(prefab.transform, "Small Message");
            var idleBackground = idleToast.GetComponent<Image>();
            Assert.That(idleBackground.color.a, Is.GreaterThan(0.5f));
            Assert.That(idleBackground.enabled, Is.False,
                "The toast background must start hidden in the prefab.");
            var rect = (RectTransform)bottomToast.transform;
            var canvasRect = (RectTransform)persistentUi.transform;
            Assert.That(rect.rect.height, Is.LessThanOrEqualTo(36f));
            Assert.That(rect.rect.width, Is.LessThanOrEqualTo(canvasRect.rect.width * 0.75f));
            Assert.That(rect.anchoredPosition.y, Is.GreaterThanOrEqualTo(30f));
            Assert.That(rect.anchoredPosition.y, Is.LessThanOrEqualTo(50f));
            var label = bottomToast.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            Assert.That(label.gameObject, Is.Not.SameAs(bottomToast));
            Assert.That(label.GetComponent<CanvasRenderer>(), Is.Not.SameAs(background.GetComponent<CanvasRenderer>()));
            Assert.That(label.enableWordWrapping, Is.False);
        }

        [UnityTest]
        public IEnumerator BottomToastBackgroundFollowsMessageLifetime()
        {
            var persistentUi = Object.FindAnyObjectByType<PersistentUI>();
            var toast = new GameObject("Toast lifecycle test", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ((RectTransform)toast.transform).sizeDelta = new Vector2(1000f, 32f);
            var text = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            text.transform.SetParent(toast.transform);
            var background = toast.GetComponent<Image>();
            background.enabled = false;
            var label = text.GetComponent<TMP_Text>();
            var displayer = new PersistentUI.MessageDisplayer(persistentUi, label, background);
            try
            {
                var message = new PersistentUI.MessageDisplayer.NotificationMessage(
                    "Short toast", PersistentUI.DisplayMessageType.Bottom);
                Assert.That(message.WaitTime, Is.EqualTo(5f));
                message.WaitTime = 1f;
                displayer.DisplayMessage(message);
                yield return null;
                Assert.That(label.text, Is.EqualTo(message.Message));
                Assert.That(background.enabled, Is.True);
                Assert.That(((RectTransform)toast.transform).rect.width, Is.LessThan(300f));
                Assert.That(background.color.a, Is.EqualTo(label.alpha).Within(0.001f));

                yield return new WaitForSeconds(0.45f);
                Assert.That(label.alpha, Is.EqualTo(1f).Within(0.01f),
                    "Toast text and background should reach full opacity quickly.");
                Assert.That(background.color.a, Is.EqualTo(1f).Within(0.01f));

                var timeout = Time.realtimeSinceStartup + 5f;
                while (background.enabled && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(background.enabled, Is.False);
                Assert.That(background.color.a, Is.Zero.Within(0.001f));
            }
            finally
            {
                Object.Destroy(toast);
            }
        }

        private static GameObject FindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child.gameObject;
                }

                var match = FindChild(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
