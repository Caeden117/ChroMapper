using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class ScrollPrecisionEditorStateTest
    {
        // ScrollPrecisionController previously never registered as an EditorData provider;
        // this round trip prevents map-local precision from silently reverting to its prefab default.
        [Test]
        public void CaptureAndLoadRestoresMapScrollPrecision()
        {
            var gameObject = new GameObject(nameof(ScrollPrecisionEditorStateTest));
            try
            {
                var controller = gameObject.AddComponent<ScrollPrecisionController>();
                controller.CurrentPrecision = ScrollPrecision.Low;
                var data = new JSONObject();

                controller.CaptureEditorState(data);
                controller.CurrentPrecision = ScrollPrecision.Ultra;
                controller.LoadEditorState(data);

                Assert.AreEqual(ScrollPrecision.Low, controller.CurrentPrecision);
                Assert.AreEqual((int)ScrollPrecision.Low, data["value"].AsInt);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
