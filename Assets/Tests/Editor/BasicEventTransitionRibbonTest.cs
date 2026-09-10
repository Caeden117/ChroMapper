using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Share the production scrub and visual assertions with BasicEventNodeChunkingTest so ribbon-only regressions
    // cannot accidentally use different loaded-container semantics from ordinary Basic Event nodes.
    public class BasicEventTransitionRibbonTest : BasicEventChunkingTestBase
    {
        private bool? visualizeGradientsBeforeTest;
        private int? chunkDistanceBeforeTest;
        // Zero-light ribbon shortcut tests isolate the Event Objects action map so one wheel edge reaches production once.
        private CMInput ribbonShortcutInput;
        private bool? sharedEventObjectsInputWasEnabled;
        private Vector2 ribbonShortcutScreenPosition;
        // Light-ID interruption tests temporarily enable Chroma's per-light transition linking mode.
        private bool? emulateChromaAdvancedBeforeTest;
        private bool? lightIdTransitionSupportBeforeTest;

        protected override void AfterCleanup()
        {
            // Zero-light ribbon shortcut tests seed a synthetic hover hit that must not leak into another editor test.
            BeatmapRaycastCache.Invalidate();

            // Restore the application's shared Event Objects bindings after the isolated shortcut map is discarded.
            if (ribbonShortcutInput != null)
            {
                ribbonShortcutInput.EventObjects.Disable();
                ribbonShortcutInput.Dispose();
                ribbonShortcutInput = null;
            }

            TearDownVirtualInput();
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            if (sharedInput != null && sharedEventObjectsInputWasEnabled == true)
                sharedInput.EventObjects.Enable();
            sharedEventObjectsInputWasEnabled = null;

            if (visualizeGradientsBeforeTest.HasValue)
            {
                // Restore the user's ribbon visibility after tests require transition gradients to be rendered.
                Settings.Instance.VisualizeChromaGradients = visualizeGradientsBeforeTest.Value;
                visualizeGradientsBeforeTest = null;
            }

            if (chunkDistanceBeforeTest.HasValue)
            {
                // Restore the normal chunk radius after wide ribbons deliberately unload both endpoint nodes.
                Settings.Instance.ChunkDistance = chunkDistanceBeforeTest.Value;
                chunkDistanceBeforeTest = null;
                GetEventsContainer().RefreshPool(true);
            }

            // Restore Chroma transition-link settings after tests exercise All Lights interruption semantics.
            if (emulateChromaAdvancedBeforeTest.HasValue)
            {
                Settings.Instance.EmulateChromaAdvanced = emulateChromaAdvancedBeforeTest.Value;
                emulateChromaAdvancedBeforeTest = null;
            }

            if (lightIdTransitionSupportBeforeTest.HasValue)
            {
                Settings.Instance.LightIDTransitionSupport = lightIdTransitionSupportBeforeTest.Value;
                lightIdTransitionSupportBeforeTest = null;
            }
        }

        [UnityTest]
        public IEnumerator AltScrollOnTransitionRibbonFromOffLightChangesLerpType()
        {
            // Preserve the passing Off control case while zero-brightness On and Transition nodes reproduce the regression.
            PrepareRibbonAppearance();
            var source = PlaceZeroLightEvent(2f, LightValue.Off);
            var transition = PlaceLightEvent(4f, LightValue.BlueTransition);
            yield return null;

            PrepareRibbonShortcutInput(source, transition);
            ScrollRibbonWithModifiers(UnityEngine.InputSystem.Key.LeftAlt);

            // The edited source is replaced by the action system, so assert against authoritative map data.
            source = RefreshLightEvent(source);
            Assert.That(
                source.CustomLerpType,
                Is.EqualTo("HSV"),
                "Alt+Scroll over a transition ribbon must remain editable when its source is an Off event.");
        }

        // trueHSV remains authored-data-only, so one Alt+Scroll from legacy HSV must return directly to RGB.
        [UnityTest]
        public IEnumerator AltScrollOnTransitionRibbonDoesNotCycleThroughTrueHSV()
        {
            PrepareRibbonAppearance();
            var sourceData = CreateLightEvent(2f, LightValue.RedOn, EventTypeValue.Event2);
            sourceData.CustomLerpType = "HSV";
            var source = PlaceUtils.Place(sourceData);
            var transition = PlaceLightEvent(4f, LightValue.BlueTransition);
            yield return null;

            PrepareRibbonShortcutInput(source, transition);
            ScrollRibbonWithModifiers(UnityEngine.InputSystem.Key.LeftAlt);
            source = RefreshLightEvent(source);
            Assert.That(
                source.CustomLerpType,
                Is.Null,
                "Alt+Scroll from legacy HSV must return to RGB without exposing trueHSV.");
        }

        // Chroma always evaluates legacy lightGradient payloads in RGB, so their ribbon must not accept transition-only lerpType edits.
        [UnityTest]
        public IEnumerator AltScrollOnLegacyGradientRibbonDoesNotChangeLerpType()
        {
            PrepareRibbonAppearance();
            var sourceData = CreateLightEvent(2f, LightValue.RedOn, EventTypeValue.Event2);
            sourceData.CustomLightGradient = new ChromaLightGradient(
                new Color(1f, 0f, 0.6f, 0.4f),
                new Color(0f, 1f, 0.2f, 0.8f),
                2f,
                "easeLinear");
            var source = PlaceUtils.Place(sourceData);
            var endpoint = PlaceLightEvent(4f, LightValue.BlueTransition);
            yield return null;

            PrepareRibbonShortcutInput(source, endpoint);
            ScrollRibbonWithModifiers(UnityEngine.InputSystem.Key.LeftAlt);
            source = RefreshLightEvent(source);

            Assert.That(
                source.CustomLerpType,
                Is.Null,
                "Alt+Scroll over a legacy lightGradient ribbon must not author transition-only lerpType data.");
        }

        [UnityTest]
        public IEnumerator AltScrollOnTransitionRibbonFromZeroBrightnessOnLightChangesLerpType()
        {
            // Reproduce Alt+Scroll when the ribbon source is an On event authored at zero brightness.
            PrepareRibbonAppearance();
            var source = PlaceZeroLightEvent(2f, LightValue.RedOn);
            var transition = PlaceLightEvent(4f, LightValue.BlueTransition);
            yield return null;

            PrepareRibbonShortcutInput(source, transition);
            ScrollRibbonWithModifiers(UnityEngine.InputSystem.Key.LeftAlt);

            // The action-replaced zero-brightness On source must retain the requested HSV interpolation mode.
            source = RefreshLightEvent(source);
            Assert.That(
                source.CustomLerpType,
                Is.EqualTo("HSV"),
                "Alt+Scroll over a transition ribbon must work from a zero-brightness On event.");
        }

        [UnityTest]
        public IEnumerator AltScrollOnTransitionRibbonFromZeroBrightnessTransitionLightChangesLerpType()
        {
            // Reproduce Alt+Scroll when a zero-brightness Transition event itself owns the following transition ribbon.
            PrepareRibbonAppearance();
            var source = PlaceZeroLightEvent(2f, LightValue.RedTransition);
            var transition = PlaceLightEvent(4f, LightValue.BlueTransition);
            yield return null;

            PrepareRibbonShortcutInput(source, transition);
            ScrollRibbonWithModifiers(UnityEngine.InputSystem.Key.LeftAlt);

            // The action-replaced zero-brightness Transition source must retain the requested HSV interpolation mode.
            source = RefreshLightEvent(source);
            Assert.That(
                source.CustomLerpType,
                Is.EqualTo("HSV"),
                "Alt+Scroll over a transition ribbon must work from a zero-brightness Transition event.");
        }

        [UnityTest]
        public IEnumerator CtrlShiftScrollOnTransitionRibbonFromOffLightChangesEasing()
        {
            // Preserve the passing Off control case for the Ctrl+Shift easing shortcut.
            yield return AssertCtrlShiftChangesEasingFromZeroLight(LightValue.Off, "an Off event");
        }

        [UnityTest]
        public IEnumerator CtrlShiftScrollOnTransitionRibbonFromZeroBrightnessOnLightChangesEasing()
        {
            // Reproduce Ctrl+Shift+Scroll when the ribbon source is an On event authored at zero brightness.
            yield return AssertCtrlShiftChangesEasingFromZeroLight(
                LightValue.RedOn,
                "a zero-brightness On event");
        }

        [UnityTest]
        public IEnumerator CtrlShiftScrollOnTransitionRibbonFromZeroBrightnessTransitionLightChangesEasing()
        {
            // Reproduce Ctrl+Shift+Scroll when a zero-brightness Transition event owns the following ribbon.
            yield return AssertCtrlShiftChangesEasingFromZeroLight(
                LightValue.RedTransition,
                "a zero-brightness Transition event");
        }

        [UnityTest]
        public IEnumerator LightIdTransitionRibbonStopsAtAllLightsNonTransitionInterrupt()
        {
            // An intervening All Lights On event must prevent an ID-scoped source from drawing to a later transition.
            PrepareLightIdRibbonAppearance();
            var source = PlaceLightIdEvent(2f, LightValue.RedOn, 1);
            PlaceLightEvent(3f, LightValue.BlueOn);
            PlaceLightIdEvent(4f, LightValue.WhiteTransition, 1);
            yield return null;

            AssertHiddenRibbonIfLoaded(
                source,
                "An ID-scoped ribbon incorrectly crossed an intervening non-transition All Lights event.");
        }

        [UnityTest]
        public IEnumerator LightIdTransitionRibbonEndsAtAllLightsTransitionInterrupt()
        {
            // An intervening All Lights Transition event is the effective target for every matching scoped light.
            PrepareLightIdRibbonAppearance();
            var source = PlaceLightIdEvent(2f, LightValue.RedOn, 1);
            var allLightsTransition = PlaceLightEvent(3f, LightValue.BlueTransition);
            PlaceLightIdEvent(4f, LightValue.WhiteTransition, 1);
            yield return null;

            AssertVisibleRibbon(
                source,
                allLightsTransition,
                "after an All Lights transition interrupted the later ID-scoped target");
        }

        [UnityTest]
        public IEnumerator WideTransitionRibbonRemainsLoadedWhenScrubbingForwardIntoMiddle()
        {
            PrepareRibbonAppearance();
            var source = PlaceLightEvent(2f, LightValue.RedOn);
            var transition = PlaceLightEvent(80f, LightValue.BlueTransition);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Load only the source visual before forward scrubbing tests spanning-ribbon retention.
            yield return SetViewAndRefreshVisualWindow(2f, 1.5f, 2.5f, 2f);
            AssertVisibleRibbon(source, transition, "before scrubbing forward");

            yield return ScrubAcrossChunkBoundary(30f, 40f);

            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(source, "the forward-scrub source");
            AssertOutsideOrdinaryChunkWindow(transition, "the forward-scrub target");
            Assert.That(
                eventsContainer.LoadedContainers.ContainsKey(transition),
                Is.False,
                "The far transition target unexpectedly remained in the ordinary loaded chunk window.");
            AssertVisibleRibbon(source, transition, "after scrubbing forward into the middle");
        }

        [UnityTest]
        public IEnumerator WideTransitionRibbonRemainsLoadedWhenScrubbingBackwardIntoMiddle()
        {
            PrepareRibbonAppearance();
            var source = PlaceLightEvent(2f, LightValue.RedOn);
            var transition = PlaceLightEvent(60f, LightValue.BlueTransition);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Establish a precise unloaded starting state before backward scrubbing tests spanning-ribbon loading.
            yield return SetViewAndRefreshVisualWindow(85f, 84.5f, 85.5f);

            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(source, "the initially unloaded backward-scrub source");
            AssertOutsideOrdinaryChunkWindow(transition, "the initially unloaded backward-scrub target");
            AssertContainerUnloaded(source, "the backward-scrub source");
            AssertContainerUnloaded(transition, "the backward-scrub target");

            yield return ScrubAcrossChunkBoundary(40f, 30f);

            AssertOutsideOrdinaryChunkWindow(source, "the backward-scrub source");
            AssertOutsideOrdinaryChunkWindow(transition, "the backward-scrub target");
            Assert.That(
                eventsContainer.LoadedContainers.ContainsKey(transition),
                Is.False,
                "The far transition target unexpectedly entered the ordinary loaded chunk window.");
            AssertVisibleRibbon(source, transition, "after scrubbing backward into the middle");
        }

        [UnityTest]
        public IEnumerator PlacingTransitionWithUnloadedPreviousNodeCreatesWideRibbon()
        {
            PrepareRibbonAppearance();
            var source = PlaceLightEvent(2f, LightValue.RedOn);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Unload the source deterministically before the real placement path inserts the transition at the current view.
            yield return SetViewAndRefreshVisualWindow(60f, 59.5f, 60.5f);
            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(source, "the previous node before transition placement");
            AssertContainerUnloaded(source, "the previous node before placing the distant transition");

            var transition = PlaceLightEvent(60f, LightValue.BlueTransition);
            yield return null;

            AssertOutsideOrdinaryChunkWindow(source, "the previous node at transition placement");
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(transition), Is.True);
            Assert.That(source.Next, Is.SameAs(transition));
            Assert.That(transition.Prev, Is.SameAs(source));
            AssertVisibleRibbon(source, transition, "after placing a transition ahead of an unloaded source");
        }

        [UnityTest]
        public IEnumerator PlacingOnNodeBeforeUnloadedTransitionReplacesWideRibbonSource()
        {
            PrepareRibbonAppearance();
            var oldSource = PlaceLightEvent(38f, LightValue.RedOn);
            var transition = PlaceLightEvent(80f, LightValue.BlueTransition);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Load only the old source so the test begins with a visible ribbon whose target is outside loaded chunks.
            // The unrelated beat-20 guard is outside the bounded window and must now unload with correct search boundaries.
            yield return SetViewAndRefreshVisualWindow(40f, 37.5f, 40.5f, 38f);
            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(transition, "the transition target before source replacement");
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(transition), Is.False);
            AssertVisibleRibbon(oldSource, transition, "before inserting the replacement source");

            var newSource = PlaceLightEvent(40f, LightValue.BlueOn);
            yield return null;

            Assert.That(oldSource.Next, Is.SameAs(newSource));
            Assert.That(newSource.Prev, Is.SameAs(oldSource));
            Assert.That(newSource.Next, Is.SameAs(transition));
            Assert.That(transition.Prev, Is.SameAs(newSource));
            AssertHiddenRibbonIfLoaded(oldSource, "the superseded source kept its old wide ribbon");
            AssertVisibleRibbon(newSource, transition, "after inserting a new source before the unloaded transition");
        }

        // A legacy gradient owns its duration and end color on the source event, so deleting a coincident alpha-zero
        // event must not erase the still-authored ribbon or reinterpret it as a destination-owned vanilla transition.
        [UnityTest]
        public IEnumerator DeletingCoincidentEventPreservesAuthoredLegacyGradientRibbon()
        {
            PrepareRibbonAppearance();
            var gradientSource = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2.25f,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomLightGradient = new ChromaLightGradient(
                    new Color(0.298f, 1f, 0.584f, 1f),
                    new Color(0.298f, 1f, 0.584f, 0f),
                    0.5f,
                    "easeLinear")
            });
            var destination = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2.75f,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f,
                CustomColor = new Color(0.298f, 1f, 0.584f, 0f)
            });
            yield return null;

            AssertVisibleRibbon(gradientSource, destination, "before deleting the alpha-zero destination");
            PlaceUtils.Delete(destination);
            yield return null;

            Assert.That(
                GetEventsContainer().MapObjects.Contains(destination),
                Is.False,
                "The alpha-zero event remained in authoritative map data after deletion.");
            AssertVisibleRibbon(
                gradientSource,
                destination,
                "after deleting the coincident alpha-zero event while its source gradient remained authored");
        }

        private void PrepareRibbonAppearance()
        {
            // Enable ribbon rendering before placement so the source container builds the same appearance as the editor.
            visualizeGradientsBeforeTest ??= Settings.Instance.VisualizeChromaGradients;
            chunkDistanceBeforeTest ??= Settings.Instance.ChunkDistance;
            Settings.Instance.VisualizeChromaGradients = true;
            // Keep every placed event on the ordinary fixed Basic Event lane used by the ribbon scenarios.
            GetEventsContainer().PropagationEditing = EventGridContainer.PropMode.Off;
        }

        private void PrepareLightIdRibbonAppearance()
        {
            // Enable the production mode whose ID-only successor search currently skips interrupting All Lights nodes.
            PrepareRibbonAppearance();
            emulateChromaAdvancedBeforeTest ??= Settings.Instance.EmulateChromaAdvanced;
            lightIdTransitionSupportBeforeTest ??= Settings.Instance.LightIDTransitionSupport;
            Settings.Instance.EmulateChromaAdvanced = true;
            Settings.Instance.LightIDTransitionSupport = true;
        }

        // Wide-ribbon tests use the same five-beat loading radius as the ordinary-node boundary matrix.
        private static void UseNarrowVisualChunkWindow() => Settings.Instance.ChunkDistance = 2;

        // Author an explicitly scoped Chroma light event while leaving ordinary helper events on All Lights.
        private static BaseEvent PlaceLightIdEvent(float jsonTime, LightValue value, params int[] lightIds) =>
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = jsonTime,
                Type = (int)EventTypeValue.Event2,
                Value = (int)value,
                FloatValue = 1f,
                CustomLightID = lightIds
            });

        // Model each reported zero-light source independently from its Off, On, or Transition node type.
        private static BaseEvent PlaceZeroLightEvent(float jsonTime, LightValue value) =>
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = jsonTime,
                Type = (int)EventTypeValue.Event2,
                Value = (int)value,
                FloatValue = 0f
            });

        private IEnumerator AssertCtrlShiftChangesEasingFromZeroLight(LightValue sourceValue, string sourceDescription)
        {
            // Exercise the same physical easing gesture for all three source-node forms without weakening its assertion.
            PrepareRibbonAppearance();
            var source = PlaceZeroLightEvent(2f, sourceValue);
            var transition = PlaceLightEvent(4f, LightValue.BlueTransition);
            yield return null;

            PrepareRibbonShortcutInput(source, transition);
            ScrollRibbonWithModifiers(
                UnityEngine.InputSystem.Key.LeftCtrl,
                UnityEngine.InputSystem.Key.LeftShift);

            // The first positive easing step must move away from the implicit linear default on the source event.
            source = RefreshLightEvent(source);
            Assert.That(
                source.CustomEasing,
                Is.Not.Null.And.Not.EqualTo("easeLinear"),
                $"Ctrl+Shift+Scroll over a transition ribbon must work from {sourceDescription}.");
        }

        private void PrepareRibbonShortcutInput(BaseEvent source, BaseEvent transition)
        {
            // Prove the zero-light source still owns a visible interactive ribbon before testing shortcut dispatch.
            AssertVisibleRibbon(source, transition, "before zero-light ribbon shortcut input");
            var sourceContainer = (EventContainer)GetEventsContainer().LoadedContainers[source];
            var ribbon = sourceContainer.GetComponentInChildren<LightGradientController>(true);
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(ribbon.IsInteractiveBasicEventRibbon, Is.True, "The visible transition ribbon had no hover collider.");

            // Resolve the actual closest custom collider from the editor camera instead of assuming the ribbon wins hover picking.
            var camera = Object.FindAnyObjectByType<CameraManager>().SelectedCameraController.Camera;
            var ribbonScreenPoint = camera.WorldToScreenPoint(renderer.bounds.center);
            Assert.That(ribbonScreenPoint.z, Is.GreaterThan(0f), "The transition ribbon was behind the editor camera.");
            ribbonShortcutScreenPosition = new Vector2(ribbonScreenPoint.x, ribbonScreenPoint.y);
            var ray = camera.ScreenPointToRay(ribbonShortcutScreenPosition);
            Assert.That(
                Intersections.Raycast(ray, 9, out var hit),
                Is.True,
                "The editor hover ray did not hit any Basic Event collider at the ribbon midpoint.");
            Assert.That(
                hit.GameObject,
                Is.SameAs(renderer.gameObject),
                "The editor hover ray did not resolve the visible transition ribbon as its closest hit.");

            // Reuse the verified production raycast result when the wheel callback performs its same-frame lookup.
            BeatmapRaycastCache.FirstHit = hit.GameObject;
            BeatmapRaycastCache.HasHit = true;
            BeatmapRaycastCache.HasRaycastThisFrame = true;

            // Isolate the production Event Objects callbacks so the synthetic wheel event is processed exactly once.
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.That(sharedInput, Is.Not.Null, "The application's shared input asset was not initialized.");
            sharedEventObjectsInputWasEnabled = sharedInput.EventObjects.enabled;
            sharedInput.EventObjects.Disable();

            InitializeVirtualInput(true);
            ribbonShortcutInput = new CMInput();
            ribbonShortcutInput.EventObjects.SetCallbacks(Object.FindAnyObjectByType<BeatmapEventInputController>());
            ribbonShortcutInput.EventObjects.Enable();
        }

        private void ScrollRibbonWithModifiers(params UnityEngine.InputSystem.Key[] modifiers)
        {
            try
            {
                PressVirtualKeys(modifiers);
                SetVirtualMouseState(ribbonShortcutScreenPosition, new Vector2(0f, 1f));
            }
            finally
            {
                SetVirtualMouseState(ribbonShortcutScreenPosition, Vector2.zero);
                ReleaseVirtualKeys(modifiers);
            }
        }

        // Resolve the action-replaced source without depending on the stale visual-container dictionary key.
        private static BaseEvent RefreshLightEvent(BaseEvent source) =>
            GetEventsContainer().MapObjects.First(evt =>
                evt.Type == source.Type && Mathf.Approximately(evt.JsonTime, source.JsonTime));

        private static void PlaceVisualRangeGuardEvents()
        {
            // Bracket wide ribbon windows around unrelated-lane events so BinarySearchBy cannot return a tested endpoint.
            PlaceUtils.Place(CreateLightEvent(20f, LightValue.RedOn, EventTypeValue.Event3));
            PlaceUtils.Place(CreateLightEvent(95f, LightValue.RedOn, EventTypeValue.Event3));
        }

        private static IEnumerator SetViewAndRefreshVisualWindow(
            float viewJsonTime,
            float lowerJsonTime,
            float upperJsonTime,
            params float[] expectedLoadedJsonTimes)
        {
            // Trace the bounded refresh lifecycle so stale dictionary, ordered-list, and delayed-reload states are distinguishable.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var map = BeatSaberSongContainer.Instance.Map;
            var eventsContainer = GetEventsContainer();
            atsc.MoveToJsonTime(viewJsonTime);
            var lowerBound = (float)map.JsonTimeToSongBpmTime(lowerJsonTime);
            var upperBound = (float)map.JsonTimeToSongBpmTime(upperJsonTime);
            eventsContainer.RefreshPool(lowerBound, upperBound, true);
            var immediatelyLoadedJsonTimes = GetLoadedJsonTimes(eventsContainer);
            yield return null;
            var loadedJsonTimesAfterFrame = GetLoadedJsonTimes(eventsContainer);
            Assert.That(
                immediatelyLoadedJsonTimes,
                Is.EquivalentTo(expectedLoadedJsonTimes),
                "The bounded setup refresh produced the wrong immediate visual pool.");
            Assert.That(
                loadedJsonTimesAfterFrame,
                Is.EquivalentTo(expectedLoadedJsonTimes),
                "The visual pool changed one frame after the bounded setup refresh.");
        }

        private static float[] GetLoadedJsonTimes(EventGridContainer eventsContainer) =>
            eventsContainer.LoadedContainers.Keys.OfType<BaseEvent>().Select(evt => evt.JsonTime).ToArray();

        private static void AssertOutsideOrdinaryChunkWindow(BaseEvent endpoint, string description)
        {
            // Prove endpoint retention comes from the spanning ribbon rather than the ordinary five-beat node window.
            var currentSongBpmTime = Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentSongBpmTime;
            Assert.That(
                Mathf.Abs(endpoint.SongBpmTime - currentSongBpmTime),
                Is.GreaterThan(BeatmapObjectContainerCollection.ChunkSize),
                $"Expected {description} to be outside the ordinary loaded chunk window.");
        }

        private static void AssertContainerUnloaded(BaseEvent endpoint, string description)
        {
            // Report the complete pool and linkage state when an endpoint fails to leave the visual chunk window.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var eventsContainer = GetEventsContainer();
            var loadedBeats = string.Join(
                ", ",
                eventsContainer.LoadedContainers.Keys
                    .OfType<BaseEvent>()
                    .Select(evt => $"{evt.JsonTime}/{evt.SongBpmTime}"));
            Assert.That(
                eventsContainer.LoadedContainers.ContainsKey(endpoint),
                Is.False,
                $"Expected {description} to be visually unloaded. "
                + $"Playhead json/song={atsc.CurrentJsonTime}/{atsc.CurrentSongBpmTime}; "
                + $"endpoint json/song={endpoint.JsonTime}/{endpoint.SongBpmTime}; "
                + $"chunk distance={Settings.Instance.ChunkDistance}; "
                + $"attached={endpoint.HasAttachedContainer}; collection enabled={eventsContainer.enabled}; "
                + $"previous={endpoint.Prev?.JsonTime.ToString() ?? "null"}; "
                + $"next={endpoint.Next?.JsonTime.ToString() ?? "null"}; loaded json/song beats=[{loadedBeats}].");
        }

        private static void AssertHiddenRibbonIfLoaded(BaseEvent source, string message)
        {
            var eventsContainer = GetEventsContainer();
            if (!eventsContainer.LoadedContainers.TryGetValue(source, out var objectContainer))
            {
                return;
            }

            // A retained old source is valid only if its former transition ribbon has been hidden.
            var ribbon = objectContainer.GetComponentInChildren<LightGradientController>(true);
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(ribbon.gameObject.activeInHierarchy && renderer.enabled, Is.False, message);
        }
    }
}
