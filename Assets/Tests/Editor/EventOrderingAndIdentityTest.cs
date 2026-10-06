using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Event ordering must retain authored ties without making clones unequal or confusing stacked objects during lookup.
    public class EventOrderingAndIdentityTest : TestBase
    {
        // Parsing must not regroup simultaneous callbacks by their event type before playback or export sees them.
        [Test]
        public void ParsingBasicEventsKeepsAuthoredOrderAcrossTypes()
        {
            var map = ParseBasicEvents();
            Assert.That(map.Events.Select(evt => evt.Type), Is.EqualTo(new[] { 8, 1, 8 }));
        }

        // Loading must preserve the same equal-beat sequence that the exporter writes, even across custom-event types.
        [Test]
        public void LoadingCustomEventsKeepsAuthoredOrderAcrossTypes()
        {
            var loader = Object.FindAnyObjectByType<MapLoader>();
            var collection = Object.FindAnyObjectByType<CustomEventGridContainer>();
            var previous = collection.MapObjects;
            var events = new List<BaseCustomEvent> { CustomEvent("Zebra", 1), CustomEvent("Alpha", 2) };
            try
            {
                loader.LoadObjects(events);
                Assert.That(events.Select(evt => evt.Type), Is.EqualTo(new[] { "Zebra", "Alpha" }));
            }
            finally
            {
                loader.LoadObjects(previous);
            }
        }

        // New equal-beat custom events belong after existing events rather than at an arbitrary BinarySearch match.
        [Test]
        public void InsertingCustomEventsAppendsEqualBeatEntries()
        {
            var collection = Object.FindAnyObjectByType<CustomEventGridContainer>();
            var previous = collection.MapObjects;
            var first = CustomEvent("Unknown", 1);
            var second = CustomEvent("Unknown", 2);
            try
            {
                collection.MapObjects = new List<BaseCustomEvent> { first };
                collection.SpawnObject(second, removeConflicting: false, refreshesPool: false);
                Assert.That(collection.MapObjects[0], Is.SameAs(first));
                Assert.That(collection.MapObjects[1], Is.SameAs(second));
            }
            finally
            {
                collection.MapObjects = previous;
            }
        }

        // United Mapping sends event content without the local file ordinal, so matching must accept an unchanged clone.
        [Test]
        public void LookupFindsLoadedBasicEventCloneWithoutFileOrdinal()
        {
            var collection = Object.FindAnyObjectByType<EventGridContainer>();
            var previous = collection.MapObjects;
            try
            {
                collection.MapObjects = ParseBasicEvents().Events;
                var clone = BeatmapFactory.Clone(collection.MapObjects[0]);
                Assert.That(collection.ContainsObject(clone), Is.True);
                collection.SilentRemoveObject(clone);
                Assert.That(collection.MapObjects.Count, Is.EqualTo(2));
                Assert.That(collection.MapObjects.Any(evt => evt.Type == 8 && evt.Value == 2), Is.False);
            }
            finally
            {
                collection.MapObjects = previous;
            }
        }

        // A time/type match cannot identify a custom event whose payload differs from every stored event.
        [Test]
        public void LookupRejectsCustomEventWithDifferentData()
        {
            var collection = Object.FindAnyObjectByType<CustomEventGridContainer>();
            var previous = collection.MapObjects;
            try
            {
                collection.MapObjects = new List<BaseCustomEvent> { CustomEvent("Unknown", 1) };
                Assert.That(collection.ContainsObject(CustomEvent("Unknown", 2)), Is.False);
            }
            finally
            {
                collection.MapObjects = previous;
            }
        }

        // Identical stacked events still have distinct undo identities; a binary-search neighbor must not replace the live reference.
        [Test]
        public void SilentRemovalPrefersExactReferenceAmongIdenticalCustomEvents()
        {
            var collection = Object.FindAnyObjectByType<CustomEventGridContainer>();
            var previous = collection.MapObjects;
            var first = CustomEvent("Unknown", 1);
            var second = CustomEvent("Unknown", 1);
            var third = CustomEvent("Unknown", 1);
            try
            {
                collection.MapObjects = new List<BaseCustomEvent> { first, second, third };
                collection.SilentRemoveObject(first);
                Assert.That(collection.MapObjects[0], Is.SameAs(second));
                Assert.That(collection.MapObjects[1], Is.SameAs(third));
            }
            finally
            {
                collection.MapObjects = previous;
            }
        }

        // Returning an edited clone to the loaded event's original values must stop action merging despite a missing file ordinal.
        [Test]
        public void BasicEventToggleBackDoesNotMergeThroughFileOrdinal()
        {
            var original = ParseBasicEvents().Events.First(evt => evt.Type == 8 && evt.Value == 2);
            var edited = BeatmapFactory.Clone(original);
            edited.Value = 7;
            var restored = BeatmapFactory.Clone(original);
            var previous = new BeatmapObjectUpdatedAction(edited, original, mergeType: ActionMergeType.EventMainTweak);
            var current = new BeatmapObjectUpdatedAction(restored, edited, mergeType: ActionMergeType.EventMainTweak);
            Assert.That(current.CanMerge(previous), Is.False);
        }

        // Editing custom-event data must remain mergeable even when the beat and type do not change.
        [Test]
        public void CustomEventPayloadEditIsDetectedForActionMerging()
        {
            var original = CustomEvent("Unknown", 1);
            var edited = CustomEvent("Unknown", 2);
            var next = CustomEvent("Unknown", 3);
            var previous = new BeatmapObjectUpdatedAction(edited, original, mergeType: ActionMergeType.EventMainTweak);
            var current = new BeatmapObjectUpdatedAction(next, edited, mergeType: ActionMergeType.EventMainTweak);
            Assert.That(current.CanMerge(previous), Is.True);
        }

        // Real V2 deserialization supplies file ordinals so these tests exercise the difference between loaded and cloned events.
        private static BaseDifficulty ParseBasicEvents() => V2Difficulty.GetFromJson(JSON.Parse(@"{
            ""_version"": ""2.6.0"",
            ""_events"": [
                { ""_time"": 5, ""_type"": 8, ""_value"": 2, ""_floatValue"": 1 },
                { ""_time"": 5, ""_type"": 1, ""_value"": 1, ""_floatValue"": 1 },
                { ""_time"": 5, ""_type"": 8, ""_value"": 7, ""_floatValue"": 1 }
            ],
            ""_notes"": [],
            ""_obstacles"": []
        }"), "event-order-test");

        // Unknown custom-event types isolate collection ordering and equality from animation or scene side effects.
        private static BaseCustomEvent CustomEvent(string type, int value) => new()
        {
            JsonTime = 5,
            Type = type,
            Data = new JSONObject { ["value"] = value }
        };
    }
}

