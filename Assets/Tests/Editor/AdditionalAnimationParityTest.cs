using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // AdditionalAnimationParityTest covers the remaining Heck animation parity items from
    // HECK-ANIMATION-PARITY-PLAN.md: animated interactable (P3), AssignPlayerToTrack's target field (P4),
    // null property erasing (P5), and strict point-value-count validation (P6). Each case evaluates through
    // the production map-load pipeline; each test loads its own fixture because the per-test teardown
    // restores the empty shared map.
    public class AdditionalAnimationParityTest : TestBase
    {
        private const string InteractableTrackName = "interactableNotes";
        private const string EraseTrackName = "eraseTrack";
        private const string MalformedTrackName = "malformedTrack";
        private const string PlayerTrackName = "playerTrack";

        private static IEnumerator LoadMapWithEvents(params JSONObject[] events)
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");

            var eventArray = new JSONArray();
            foreach (var customEvent in events)
            {
                eventArray.Add(customEvent);
            }

            yield return TestUtils.ReloadMap(3, CreateDifficulty(eventArray));
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private static JSONNode CreateDifficulty(JSONArray customEvents) => new JSONObject
        {
            ["version"] = "3.3.0",
            ["customData"] = new JSONObject
            {
                ["customEvents"] = customEvents
            }
        };

        // AnimatedInteractableParsesAndEvaluates: Heck registers interactable as a track property
        // (NoodleExtensions/Plugin.cs); CM must parse it instead of silently dropping the property.
        [UnityTest]
        public IEnumerator AnimatedInteractableParsesAndEvaluates()
        {
            yield return LoadMapWithEvents(InteractableEvent());

            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(InteractableTrackName);
            Assert.That(
                track.AnimatedProperties.ContainsKey("interactable"),
                Is.True,
                "The animated interactable property was silently dropped.");

            // The event animates [1 @ 0, 0 @ 1] over beats 4..6, so beat 5 is halfway and beat 7 holds 0.
            var property = (AnimateProperty<float>)track.AnimatedProperties["interactable"];
            Assert.That(property.GetLerpedValue(5f), Is.EqualTo(0.5f).Within(0.0001f),
                "The interactable property did not interpolate over its duration.");
            Assert.That(property.GetLerpedValue(7f), Is.EqualTo(0f).Within(0.0001f),
                "The interactable property did not hold its post-event value.");

            yield break;
        }

        // NullPropertyErasesTheTrackProperty: Heck allows "property": null to erase a track property ("as if
        // it was never set"); CM must drop the property instead of animating it to default(T).
        [UnityTest]
        public IEnumerator NullPropertyErasesTheTrackProperty()
        {
            yield return LoadMapWithEvents(
                DissolveEvent(4f, 2f, JSON.Parse("[[1, 0], [0, 1]]")),
                DissolveEvent(12f, 0f, JSON.Parse("null")));

            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(EraseTrackName);
            Assert.That(
                track.AnimatedProperties.ContainsKey("dissolve"),
                Is.False,
                "A null property must erase the track's property instead of animating it to default.");

            yield break;
        }

        // MalformedPointValueCountIsLoggedAndSkipped: Heck errors on wrong component counts (the docs' color
        // example needs exactly 5 numbers); CM must log and skip the point without wedging the load
        // (the failure shape SpellsLaserWallTest proved for missing point definitions). The skipped point
        // leaves the property with no usable definitions, so its evaluation stays at the default.
        [UnityTest]
        public IEnumerator MalformedPointValueCountIsLoggedAndSkipped()
        {
            LogAssert.Expect(LogType.Error, "Point for [Color] must have 4 numbers plus an optional time; got 4 and it was skipped.");
            yield return LoadMapWithEvents(MalformedColorEvent());

            var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(MalformedTrackName);
            var propertyUsable = track.AnimatedProperties.TryGetValue("color", out var animateProperty)
                && ((AnimateProperty<Color>)animateProperty).PointDefinitions.Any(definition => definition.Points.Length > 0);
            Assert.That(
                propertyUsable,
                Is.False,
                "A malformed color point must be logged and skipped instead of animating garbage.");

            yield break;
        }

        // AssignPlayerToTrackTargetFollowsHeckSemantics: target defaults to Root; the editor preview has no
        // hand rigs, so LeftHand/RightHand targets must be logged and skipped instead of silently binding the
        // whole player. The player camera controller (not the editor camera) owns the player-track bindings.
        [UnityTest]
        public IEnumerator AssignPlayerToTrackTargetDefaultsToRootAndSkipsHands()
        {
            yield return LoadMapWithEvents(
                AssignPlayerToTrackEvent(2f, "LeftHand"),
                AssignPlayerToTrackEvent(3f, null));

            var playerCameraController = Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(controller => ReadPrivateField<bool>(controller, "playerCamera"));
            var playerTracks = ReadPrivateField<List<TrackAnimator>>(playerCameraController, "playerTracks");
            Assert.That(
                playerTracks.Count,
                Is.EqualTo(1),
                "The fixture binds one Root player track; the LeftHand-targeted event must be skipped because the editor has no hands.");

            yield break;
        }

        private static JSONObject InteractableEvent() => new()
        {
            ["b"] = 4f,
            ["t"] = "AnimateTrack",
            ["d"] = new JSONObject
            {
                ["track"] = InteractableTrackName,
                ["duration"] = 2f,
                ["interactable"] = JSON.Parse("[[1, 0], [0, 1]]")
            }
        };

        private static JSONObject DissolveEvent(float beat, float duration, JSONNode points) => new()
        {
            ["b"] = beat,
            ["t"] = "AnimateTrack",
            ["d"] = new JSONObject
            {
                ["track"] = EraseTrackName,
                ["duration"] = duration,
                ["dissolve"] = points
            }
        };

        private static JSONObject MalformedColorEvent() => new()
        {
            ["b"] = 0f,
            ["t"] = "AnimateTrack",
            ["d"] = new JSONObject
            {
                ["track"] = MalformedTrackName,
                // The docs' malformed example: a color point with 4 numbers where 5 are required.
                ["color"] = JSON.Parse("[[1, 5, 3, 0.5]]")
            }
        };

        private static JSONObject AssignPlayerToTrackEvent(float beat, string target)
        {
            var data = new JSONObject
            {
                ["track"] = PlayerTrackName
            };
            if (target != null)
            {
                data["target"] = target;
            }

            return new()
            {
                ["b"] = beat,
                ["t"] = "AssignPlayerToTrack",
                ["d"] = data
            };
        }

        private static T ReadPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Private field '{fieldName}' was not found.");
            return (T)field.GetValue(instance);
        }

        // Restore the canonical empty shared map so later fixtures do not inherit this fixture. Once per
        // class is enough: every test loads its own map anyway, so a per-test empty reload was a pure extra
        // scene transition.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
