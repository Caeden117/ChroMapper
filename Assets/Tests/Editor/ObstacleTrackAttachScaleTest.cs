using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ObstacleTrackAttachScaleTest : TestBase
    {
        private static readonly int handleScaleId = Shader.PropertyToID("_HandleScale");

        [UnityTest]
        public IEnumerator ScaledObstacleSelectionUsesConstantWorldSpaceOutline()
        {
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var wall = PlaceUtils.Place(new BaseObstacle
            {
                JsonTime = 2,
                Duration = 16,
                PosX = 0,
                PosY = 0,
                Width = 2,
                Height = 3
            });

            var container = collection.LoadedContainers[wall] as ObstacleContainer;
            Assert.IsNotNull(container, "Obstacle container missing");

            container.Highlighted = true;
            yield return null;

            var selection = container.SelectionMpbController.Renderers[0];
            Assert.IsNotNull(selection, "Selection renderer missing");
            Assert.IsTrue(selection.enabled, "Hovered obstacle selection renderer was not enabled");
            Assert.Greater(container.ObstacleScale.z, container.ObstacleScale.x * 10f,
                "Test wall must be long enough to expose proportional outline expansion");

            var properties = new MaterialPropertyBlock();
            selection.GetPropertyBlock(properties);
            // The selection shader multiplies its outline by object scale unless _HandleScale is enabled.
            // Long and tall walls therefore receive proportionally larger white boxes; obstacle selections
            // must request the shader's constant-world-space outline mode instead.
            Assert.AreEqual(1f, properties.GetFloat(handleScaleId), 0.001f,
                "Scaled obstacle hover outlines must keep a constant world-space thickness");
        }
    }
}
