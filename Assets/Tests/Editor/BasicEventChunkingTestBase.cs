using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    // Shared production visual-pool helpers for node and ribbon chunking tests.
    public abstract class BasicEventChunkingTestBase : TestBase
    {
        // Missing nodes can be active geometry rendered fully transparent, so inspect the shader value applied to them.
        private static readonly int mainAlphaId = Shader.PropertyToID("_MainAlpha");

        // Avoid test-induced GC while inspecting thousands of nodes per scrub matrix.
        private static readonly MaterialPropertyBlock nodeRendererProperties = new();

        // Linux Jenkins has no reliable native audio clock, so playback time is driven directly.
        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        // Virtual devices isolate input tests from editor focus and Jenkins host state.
        private InputTestFixture inputTestFixture;
        private Mouse virtualMouse;
        private Keyboard virtualKeyboard;

        protected override EditingMode InitialEditingMode => EditingMode.BasicEvent;

        protected static void StartDeterministicPlaybackAtSongBpmTime(
            AudioTimeSyncController atsc,
            float songBpmTime)
        {
            Assert.That(atsc.IsPlaying, Is.False, "Deterministic playback did not start from a paused controller.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "AudioTimeSyncController.CurrentSeconds was not found.");

            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBpmTime));

            Assert.That(atsc.IsPlaying, Is.True, "Deterministic playback did not enter the production playing state.");
            Assert.That(
                atsc.SongAudioSource.isPlaying,
                Is.False,
                "Deterministic playback unexpectedly retained a native audio backend.");
        }

        protected static void PauseDeterministicPlayback(AudioTimeSyncController atsc)
        {
            Assert.That(atsc.IsPlaying, Is.True, "Deterministic playback was already paused.");
            atsc.TogglePlaying();
            Assert.That(atsc.IsPlaying, Is.False, "Deterministic playback did not pause.");
            Assert.That(atsc.StopScheduled, Is.False, "Deterministic playback left an automatic stop scheduled.");
        }

        protected void InitializeVirtualInput(bool includeKeyboard)
        {
            Assert.That(inputTestFixture, Is.Null, "Virtual input was initialized twice in one test.");
            inputTestFixture = new InputTestFixture();
            inputTestFixture.Setup();
            virtualMouse = InputSystem.AddDevice<Mouse>();
            if (includeKeyboard)
            {
                virtualKeyboard = InputSystem.AddDevice<Keyboard>();
            }
        }

        protected void SetVirtualMouseState(Vector2 position, Vector2 scroll)
        {
            Assert.That(inputTestFixture, Is.Not.Null, "Virtual input was not initialized before setting mouse state.");
            inputTestFixture.Set(virtualMouse.position, position, queueEventOnly: true);
            inputTestFixture.Set(virtualMouse.scroll, scroll, queueEventOnly: true);
            InputSystem.Update();
        }

        // Apply each key event before constructing the next chord state.
        protected void PressVirtualKeys(params Key[] keys)
        {
            Assert.That(virtualKeyboard, Is.Not.Null, "Virtual keyboard input was not initialized.");
            foreach (var key in keys)
            {
                inputTestFixture.Press(virtualKeyboard[key], queueEventOnly: true);
                InputSystem.Update();
                Assert.That(virtualKeyboard[key].isPressed, Is.True, $"Virtual modifier {key} was not pressed.");
            }
        }

        protected void ReleaseVirtualKeys(params Key[] keys)
        {
            Assert.That(virtualKeyboard, Is.Not.Null, "Virtual keyboard input was not initialized.");
            foreach (var key in keys)
            {
                inputTestFixture.Release(virtualKeyboard[key], queueEventOnly: true);
                InputSystem.Update();
                Assert.That(virtualKeyboard[key].isPressed, Is.False, $"Virtual modifier {key} was not released.");
            }
        }

        protected void TearDownVirtualInput()
        {
            virtualMouse = null;
            virtualKeyboard = null;
            if (inputTestFixture == null)
            {
                return;
            }

            inputTestFixture.TearDown();
            inputTestFixture = null;
        }

        // Chunk regressions must use real EventPlacement-backed placement so insertion callbacks and ribbon indexes run.
        protected static BaseEvent PlaceLightEvent(
            float jsonTime,
            LightValue value,
            EventTypeValue eventType = EventTypeValue.Event2) =>
            PlaceUtils.Place(CreateLightEvent(jsonTime, value, eventType));

        // Light-ID-view chunk regressions must author scoped and All Lights variants through the same placement path.
        protected static BaseEvent PlaceLightEvent(
            float jsonTime,
            LightValue value,
            EventTypeValue eventType,
            int[] lightIds)
        {
            var evt = CreateLightEvent(jsonTime, value, eventType);
            evt.CustomLightID = lightIds;
            return PlaceUtils.Place(evt);
        }

        // Boundary guards and node/ribbon matrices need full-intensity events without any direct collection insertion.
        protected static BaseEvent CreateLightEvent(
            float jsonTime,
            LightValue value,
            EventTypeValue eventType) =>
            new()
            {
                JsonTime = jsonTime,
                Type = (int)eventType,
                Value = (int)value,
                FloatValue = 1f
            };

        // Both fixtures must query the one authoritative Basic Event visual collection used by editor scrubbing.
        protected static EventGridContainer GetEventsContainer() =>
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

        // BasicEventNodeChunkingTest varies jumps and direction through the public playhead while ribbon tests reuse
        // the same frame boundary, ensuring LateUpdate—not a white-box RefreshPool call—owns every tested reload.
        protected static IEnumerator ScrubThroughJsonTimes(params float[] jsonTimes)
        {
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            for (var timeIndex = 0; timeIndex < jsonTimes.Length; timeIndex++)
            {
                atsc.MoveToJsonTime(jsonTimes[timeIndex]);
                yield return null;
            }

            Assert.That(
                Settings.Instance.ChunkDistance,
                Is.EqualTo(2),
                "The test's five-beat visual chunk radius was overwritten while scrubbing.");
        }

        // Preserve the existing two-stop ribbon helper while routing it through the shared production scrub path.
        protected static IEnumerator ScrubAcrossChunkBoundary(float stagingJsonTime, float targetJsonTime) =>
            ScrubThroughJsonTimes(stagingJsonTime, targetJsonTime);

        // Missing-node regressions can leave one of three visual indexes stale, so loaded assertions verify all three
        // plus the active pooled model that a mapper must actually see on the grid.
        protected static void AssertNodeVisualLoaded(BaseEvent evt, string operation)
        {
            var eventsContainer = GetEventsContainer();
            Assert.That(
                eventsContainer.MapObjects.Any(candidate => object.ReferenceEquals(candidate, evt)),
                Is.True,
                $"The authoritative event at beat {evt.JsonTime} disappeared {operation}.");
            Assert.That(
                eventsContainer.LoadedContainers.TryGetValue(evt, out var objectContainer),
                Is.True,
                $"The event at beat {evt.JsonTime} was absent from LoadedContainers {operation}. "
                + DescribeVisualPool(eventsContainer));
            Assert.That(objectContainer, Is.TypeOf<EventContainer>());
            var eventContainer = (EventContainer)objectContainer;
            Assert.That(eventContainer.EventData, Is.SameAs(evt), $"The pooled container was bound to the wrong event {operation}.");
            Assert.That(
                eventsContainer.ObjectsWithContainers.Any(candidate => object.ReferenceEquals(candidate, evt)),
                Is.True,
                $"The event at beat {evt.JsonTime} was missing from ObjectsWithContainers {operation}.");
            Assert.That(evt.HasAttachedContainer, Is.True, $"The loaded event attachment flag was false {operation}.");
            Assert.That(eventContainer.gameObject.activeInHierarchy, Is.True, $"The event container was inactive {operation}.");

            // The reported node can remain dictionary-owned yet be invisible because a reused container is positioned
            // for stale data, so BasicEventNodeChunkingTest must validate the rendered grid position as well as ownership.
            var labels = UnityEngine.Object.FindAnyObjectByType<CreateEventTypeLabels>();
            var expectedGridPosition = evt.GetPosition(
                labels,
                eventsContainer.PropagationEditing,
                eventsContainer.EventTypeToPropagate);
            Assert.That(
                expectedGridPosition.HasValue,
                Is.True,
                $"The event at beat {evt.JsonTime} did not resolve to a visible Basic Event lane {operation}.");
            Assert.That(
                eventContainer.transform.localPosition.x,
                Is.EqualTo(expectedGridPosition.Value.x).Within(0.001f),
                $"The event at beat {evt.JsonTime} was attached to the wrong grid lane {operation}.");
            Assert.That(
                eventContainer.transform.localPosition.z,
                Is.EqualTo(evt.SongBpmTime * EditorScaleController.EditorScale).Within(0.001f),
                $"The event at beat {evt.JsonTime} was attached at the wrong rendered time {operation}.");
            Assert.That(
                eventContainer.transform.lossyScale.sqrMagnitude,
                Is.GreaterThan(0f),
                $"The event at beat {evt.JsonTime} had a zero-sized visual transform {operation}.");
            Assert.That(
                eventContainer.VModelController.Actives.Any(model => model.GameObject.activeInHierarchy),
                Is.True,
                $"The event at beat {evt.JsonTime} had no active visual model {operation}.");
            // Read the actual renderer property block rather than only the controller's desired state; a stale pooled
            // block can make otherwise-active geometry invisible while every collection ownership assertion passes.
            var visibleRenderer = eventContainer.VModelController.Renderers.FirstOrDefault(renderer =>
                renderer != null
                && renderer.enabled
                && renderer.gameObject.activeInHierarchy
                && renderer.bounds.size.sqrMagnitude > 0f);
            Assert.That(
                visibleRenderer,
                Is.Not.Null,
                $"The event at beat {evt.JsonTime} had no enabled, active, non-empty renderer {operation}.");
            nodeRendererProperties.Clear();
            visibleRenderer.GetPropertyBlock(nodeRendererProperties);
            Assert.That(
                nodeRendererProperties.GetFloat(mainAlphaId),
                Is.GreaterThan(0.001f),
                $"The event at beat {evt.JsonTime} was rendered fully transparent {operation}.");
        }

        // Nodes outside the ordinary window and outside every ribbon interval must leave every visual ownership index.
        protected static void AssertNodeVisualUnloaded(BaseEvent evt, string operation)
        {
            var eventsContainer = GetEventsContainer();
            Assert.That(
                eventsContainer.MapObjects.Any(candidate => object.ReferenceEquals(candidate, evt)),
                Is.True,
                $"The authoritative event at beat {evt.JsonTime} disappeared {operation}.");
            Assert.That(
                eventsContainer.LoadedContainers.ContainsKey(evt),
                Is.False,
                $"The event at beat {evt.JsonTime} remained in LoadedContainers {operation}. "
                + DescribeVisualPool(eventsContainer));
            Assert.That(
                eventsContainer.ObjectsWithContainers.Any(candidate => object.ReferenceEquals(candidate, evt)),
                Is.False,
                $"The event at beat {evt.JsonTime} remained in ObjectsWithContainers {operation}.");
            Assert.That(evt.HasAttachedContainer, Is.False, $"The unloaded event attachment flag remained true {operation}.");
        }

        // Node and ribbon chunk tests require the source's retained container and the rendered mesh to describe the
        // exact same transition interval after each forward or backward scrub.
        protected static void AssertVisibleRibbon(BaseEvent source, BaseEvent transition, string operation)
        {
            var eventsContainer = GetEventsContainer();
            Assert.That(
                eventsContainer.LoadedContainers.TryGetValue(source, out var objectContainer),
                Is.True,
                $"The ribbon source at beat {source.JsonTime} was not loaded {operation}. "
                + DescribeVisualPool(eventsContainer));
            Assert.That(objectContainer, Is.TypeOf<EventContainer>());

            var ribbon = objectContainer.GetComponentInChildren<LightGradientController>(true);
            Assert.That(ribbon, Is.Not.Null, $"The loaded source had no ribbon controller {operation}.");
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(renderer, Is.Not.Null, $"The loaded source had no ribbon renderer {operation}.");
            Assert.That(ribbon.gameObject.activeInHierarchy, Is.True, $"The ribbon object was hidden {operation}.");
            Assert.That(renderer.enabled, Is.True, $"The ribbon renderer was disabled {operation}.");
            Assert.That(
                renderer.bounds.size.sqrMagnitude,
                Is.GreaterThan(0f),
                $"The ribbon renderer had empty world bounds {operation}.");

            var expectedLength = (transition.SongBpmTime - source.SongBpmTime)
                * EditorScaleController.EditorScale
                * (4f / 3f);
            Assert.That(
                ribbon.transform.localScale.x,
                Is.EqualTo(expectedLength).Within(0.001f),
                $"The ribbon did not span from beat {source.JsonTime} to beat {transition.JsonTime} {operation}.");
        }

        // Failure diagnostics expose both collection indexes and the private chunk cursor that decides whether a
        // backward playhead move triggers another production RefreshPool.
        protected static string DescribeVisualPool(EventGridContainer eventsContainer)
        {
            var collectionType = typeof(BeatmapObjectContainerCollection);
            var previousAtscBeat = collectionType
                .GetField("previousAtscBeat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(eventsContainer);
            var previousChunk = collectionType
                .GetField("previousChunk", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(eventsContainer);
            var dictionaryBeats = string.Join(
                ", ",
                eventsContainer.LoadedContainers.Keys
                    .OfType<BaseEvent>()
                    .Select(evt => $"{evt.JsonTime}/{evt.SongBpmTime}"));
            var orderedBeats = string.Join(
                ", ",
                eventsContainer.ObjectsWithContainers
                    .OfType<BaseEvent>()
                    .Select(evt => $"{evt.JsonTime}/{evt.SongBpmTime}"));
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            return "enabled/activeSelf/activeInHierarchy/isActiveAndEnabled="
                + $"{eventsContainer.enabled}/{eventsContainer.gameObject.activeSelf}/"
                + $"{eventsContainer.gameObject.activeInHierarchy}/{eventsContainer.isActiveAndEnabled}; "
                + $"visualize={Settings.Instance.VisualizeChromaGradients}; "
                + $"intervals/query={GetPrivateCollectionCount(eventsContainer, "transitionRibbonIntervals", "intervalsBySource")}/"
                + $"{GetPrivateCollectionCount(eventsContainer, "visibleTransitionRibbonSources")}; "
                + $"playhead={atsc.CurrentJsonTime}/{atsc.CurrentSongBpmTime}; "
                + $"previous={previousAtscBeat}/{previousChunk}; "
                + $"dictionary({eventsContainer.LoadedContainers.Count})=[{dictionaryBeats}]; "
                + $"ordered({eventsContainer.ObjectsWithContainers.Count})=[{orderedBeats}]";
        }

        // Reflection remains diagnostic-only and lets failures distinguish an interval-index miss from pool ownership.
        private static int GetPrivateCollectionCount(
            object owner,
            string fieldName,
            string nestedFieldName = null)
        {
            var value = owner.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(owner);
            if (value != null && nestedFieldName != null)
            {
                value = value.GetType()
                    .GetField(
                        nestedFieldName,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(value);
            }

            var count = value?.GetType().GetProperty("Count")?.GetValue(value);
            return count is int collectionCount ? collectionCount : -1;
        }
    }
}
