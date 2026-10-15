using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Chroma preserves map-authored transforms on a box-light mesh across later light refreshes.
    // The native BTS box is a child renderer, so this fixture exercises the real environment loader.
    public class ParametricBoxEnhancementTransformTest : TestBase
    {
        private const string BoxId =
            "BTSEnvironment.[0]Environment.[22]PillarPair (3).[1]PillarR.[2]LaserR.[1]BoxLight";
        private const string AuthoredBoxId =
            "BTSEnvironment.[0]Environment.[18]PillarPair (1).[0]PillarL.[3]LaserLight0.[1]BoxLight";
        private const string PositionOnlyBoxId =
            "BTSEnvironment.[0]Environment.[17]SmallPillarPair.[1]PillarR.[2]LaserR.[1]BoxLight";
        private const string ScaleOnlyBoxId =
            "BTSEnvironment.[0]Environment.[24]PillarPair (4).[1]PillarR.[1]RotationBaseR.[0]LaserRH.[1]BoxLight";
        private const string BoxTrack = "authoredBoxMesh";

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(3, CreateDifficulty(), environmentName: "BTSEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // AuthoredBoxPositionAndScaleSurviveLaterLightRefresh: a lighting event refreshes
        // ParametricBoxLight after Chroma applies the enhancement's mesh transform.
        [UnityTest]
        public IEnumerator AuthoredBoxPositionAndScaleSurviveLaterLightRefresh()
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(0f);
            yield return null;
            yield return null;

            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var marker = descriptor.ChromaIDMarkers.Single(item => item.ChromaID == AuthoredBoxId);
            var box = marker.GetComponent<ParametricBoxLight>();
            Assert.That(box, Is.Not.Null, "The BTS box-light fixture did not load its native renderer.");

            box.InitIfNeeded();
            box.SetColor(Color.white);

            Assert.That(box.transform.localPosition,
                Is.EqualTo(new Vector3(3f, 4f, 5f)).Within(0.0001f),
                "A light refresh replaced the authored local position of the box mesh.");
            Assert.That(box.transform.localScale,
                Is.EqualTo(new Vector3(4f, 6f, 8f)).Within(0.0001f),
                "A light refresh replaced the authored local scale of the box mesh.");
            yield break;
        }

        // AnimatedBoxPositionAndScaleSurviveLaterLightRefresh: Chroma saves each track update
        // separately, so a later color refresh must not restore the enhancement's old pose.
        [UnityTest]
        public IEnumerator AnimatedBoxPositionAndScaleSurviveLaterLightRefresh()
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(4.1f);
            yield return null;
            yield return null;

            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var box = descriptor.ChromaIDMarkers.Single(item => item.ChromaID == BoxId)
                .GetComponent<ParametricBoxLight>();
            Assert.That(box, Is.Not.Null);
            var animatedPosition = box.transform.localPosition;
            var animatedScale = box.transform.localScale;
            Assert.That(animatedPosition,
                Is.Not.EqualTo(new Vector3(3f, 4f, 5f)),
                "The track did not move the box mesh before its light refresh.");
            Assert.That(animatedScale,
                Is.EqualTo(new Vector3(2f, 3f, 4f)).Within(0.0001f),
                "The track did not scale the box mesh before its light refresh.");
            box.InitIfNeeded();
            box.SetColor(Color.red);

            Assert.That(box.transform.localPosition,
                Is.EqualTo(animatedPosition).Within(0.0001f),
                "A light refresh replaced the track-animated local position.");
            Assert.That(box.transform.localScale,
                Is.EqualTo(animatedScale).Within(0.0001f),
                "A light refresh replaced the track-animated local scale.");
        }

        // A position-only enhancement must not freeze the native parametric dimensions.
        [UnityTest]
        public IEnumerator AuthoredPositionKeepsNativeScaleRefresh()
        {
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var box = descriptor.ChromaIDMarkers.Single(item => item.ChromaID == PositionOnlyBoxId)
                .GetComponent<ParametricBoxLight>();
            Assert.That(box, Is.Not.Null);

            box.InitIfNeeded();
            box.Width = 10f;
            box.Height = 12f;
            box.Length = 14f;
            box.SetColor(Color.white);

            Assert.That(box.transform.localPosition,
                Is.EqualTo(new Vector3(7f, 8f, 9f)).Within(0.0001f));
            Assert.That(box.transform.localScale,
                Is.EqualTo(new Vector3(5f, 6f, 7f)).Within(0.0001f));
            yield break;
        }

        // A scale-only enhancement must not freeze the native height-center position.
        [UnityTest]
        public IEnumerator AuthoredScaleKeepsNativePositionRefresh()
        {
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var box = descriptor.ChromaIDMarkers.Single(item => item.ChromaID == ScaleOnlyBoxId)
                .GetComponent<ParametricBoxLight>();
            Assert.That(box, Is.Not.Null);

            box.InitIfNeeded();
            box.Width = 10f;
            box.Height = 12f;
            box.Length = 14f;
            box.Center = 0.25f;
            box.SetColor(Color.white);

            Assert.That(box.transform.localScale,
                Is.EqualTo(new Vector3(11f, 12f, 13f)).Within(0.0001f));
            Assert.That(box.transform.localPosition,
                Is.EqualTo(new Vector3(0f, 3f, 0f)).Within(0.0001f));
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private static JSONNode CreateDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = BoxId,
                ["lookupMethod"] = "Exact",
                ["localPosition"] = JSON.Parse("[3,4,5]"),
                ["scale"] = JSON.Parse("[4,6,8]"),
                ["track"] = BoxTrack
            });
            environment.Add(new JSONObject
            {
                ["id"] = AuthoredBoxId,
                ["lookupMethod"] = "Exact",
                ["localPosition"] = JSON.Parse("[3,4,5]"),
                ["scale"] = JSON.Parse("[4,6,8]")
            });
            environment.Add(new JSONObject
            {
                ["id"] = PositionOnlyBoxId,
                ["lookupMethod"] = "Exact",
                ["localPosition"] = JSON.Parse("[7,8,9]")
            });
            environment.Add(new JSONObject
            {
                ["id"] = ScaleOnlyBoxId,
                ["lookupMethod"] = "Exact",
                ["scale"] = JSON.Parse("[11,12,13]")
            });

            var events = new JSONArray();
            events.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateTrack",
                ["d"] = new JSONObject
                {
                    ["track"] = BoxTrack,
                    ["duration"] = 0f,
                    ["position"] = JSON.Parse("[[9,10,11,0]]"),
                    ["scale"] = JSON.Parse("[[2,3,4,0]]")
                }
            });

            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = events
                }
            };
        }
    }
}
