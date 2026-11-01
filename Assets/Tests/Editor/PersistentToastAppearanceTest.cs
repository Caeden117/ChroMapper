using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
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
            Assert.That(background.color.a, Is.GreaterThan(0.5f));
            Assert.That(((RectTransform)bottomToast.transform).anchoredPosition.y, Is.GreaterThan(40f));
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
