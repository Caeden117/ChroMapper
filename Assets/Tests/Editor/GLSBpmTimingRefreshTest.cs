using System;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSBpmTimingRefreshTest : TestBase
    {
        private const int DiagnosticColorGroupId = 9000001;
        private const int DiagnosticRotationGroupId = 9000002;
        private const int DiagnosticTranslationGroupId = 9000003;
        private const int DiagnosticFloatFxGroupId = 9000004;
        private const int DiagnosticLightCount = 1;
        private const float BpmChangeJsonTime = 4f;
        private const string ChangedBpmText = "200";

        public enum GlsNodeKind
        {
            Color,
            Rotation,
            Translation,
            FloatFx
        }

        public enum GroupLayout
        {
            FutureGroup,
            GroupSpansBpmChange
        }

        private GameObject effectRoot;
        private ColorSchemeSO fixtureColorScheme;
        private LightColorGroupEffect colorEffect;
        private RecordingLight recordingLight;
        private LightRotationGroupEffect rotationEffect;
        private Transform rotationTarget;
        private LightTranslationGroupEffect translationEffect;
        private Transform translationTarget;
        private FloatFxGroupEffect floatFxEffect;
        private RecordingFxTarget recordingFxTarget;
        private GLSManager glsManager;
        private BeatmapRuntimeContext context;
        private BaseBpmEvent bpmEventAtChange;

        [SetUp]
        public void CreateDiagnosticEffects()
        {
            context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.That(context.Descriptor, Is.Not.Null);
            glsManager = Object.FindAnyObjectByType<GLSManager>();
            Assert.That(glsManager, Is.Not.Null);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(atsc, Is.Not.Null);

            effectRoot = new GameObject(nameof(GLSBpmTimingRefreshTest));

            fixtureColorScheme = ScriptableObject.CreateInstance<ColorSchemeSO>();
            colorEffect = effectRoot.AddComponent<LightColorGroupEffect>();
            colorEffect.ColorBoostEffect = effectRoot.AddComponent<ColorBoostEffect>();
            colorEffect.ColorSchemeProvider = effectRoot.AddComponent<ColorSchemeProvider>();
            colorEffect.ColorSchemeProvider.ColorScheme = fixtureColorScheme;
            colorEffect.ColorBoostEffect.ColorSchemeProvider = colorEffect.ColorSchemeProvider;
            colorEffect.Atsc = atsc;
            colorEffect.ID = DiagnosticColorGroupId;
            colorEffect.Count = DiagnosticLightCount;
            recordingLight = effectRoot.AddComponent<RecordingLight>();
            recordingLight.ID = 0;
            colorEffect.Register(recordingLight);
            colorEffect.Initialize();

            rotationEffect = effectRoot.AddComponent<LightRotationGroupEffect>();
            rotationEffect.Atsc = atsc;
            rotationEffect.ID = DiagnosticRotationGroupId;
            rotationEffect.Count = DiagnosticLightCount;
            rotationTarget = CreateTarget("Rotation X");
            rotationEffect.Register(0, Axis.X, false, rotationTarget);
            rotationEffect.Initialize();

            translationEffect = effectRoot.AddComponent<LightTranslationGroupEffect>();
            translationEffect.Atsc = atsc;
            translationEffect.ID = DiagnosticTranslationGroupId;
            translationEffect.Count = DiagnosticLightCount;
            translationEffect.TranslationLimits = new[]
            {
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f),
                new Vector2(-1f, 1f)
            };
            translationEffect.DistributionLimits = new[]
            {
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            };
            translationTarget = CreateTarget("Translation X");
            translationEffect.Register(0, Axis.X, false, translationTarget);
            translationEffect.Initialize();

            floatFxEffect = effectRoot.AddComponent<FloatFxGroupEffect>();
            floatFxEffect.Atsc = atsc;
            floatFxEffect.ID = DiagnosticFloatFxGroupId;
            floatFxEffect.Count = DiagnosticLightCount;
            floatFxEffect.Trigger = false;
            recordingFxTarget = effectRoot.AddComponent<RecordingFxTarget>();
            floatFxEffect.Register(0, recordingFxTarget);
            floatFxEffect.Initialize();

            var descriptor = context.Descriptor;
            Assert.That(
                descriptor.LightColorGroupEffectManager.IdToEffect.ContainsKey(DiagnosticColorGroupId),
                Is.False);
            Assert.That(
                descriptor.LightRotationGroupEffectManager.IdToEffect.ContainsKey(DiagnosticRotationGroupId),
                Is.False);
            Assert.That(
                descriptor.LightTranslationGroupEffectManager.IdToEffect.ContainsKey(DiagnosticTranslationGroupId),
                Is.False);
            Assert.That(
                descriptor.FloatFxGroupEffectManager.IdToEffect.ContainsKey(DiagnosticFloatFxGroupId),
                Is.False);
            descriptor.LightColorGroupEffectManager.IdToEffect.Add(DiagnosticColorGroupId, colorEffect);
            descriptor.LightRotationGroupEffectManager.IdToEffect.Add(DiagnosticRotationGroupId, rotationEffect);
            descriptor.LightTranslationGroupEffectManager.IdToEffect.Add(
                DiagnosticTranslationGroupId,
                translationEffect);
            descriptor.FloatFxGroupEffectManager.IdToEffect.Add(DiagnosticFloatFxGroupId, floatFxEffect);
        }

        [Test]
        public void BpmChangeRetainsColorTimelineAndUnaffectedPrefix()
        {
            PlaceBpmEvents();
            var group = (BaseLightColorEventBoxGroup)CreateDiagnosticGroup(
                GlsNodeKind.Color,
                GroupLayout.GroupSpansBpmChange);
            group.Boxes[0].Events = new[]
            {
                ColorNode(0f, Color.white),
                ColorNode(2f, Color.red),
                ColorNode(12f, Color.green),
                ColorNode(16f, Color.blue)
            };
            group.NormalizeLoadedEventConflicts();
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(group, Array.Empty<BaseObject>(), "Placed diagnostic prefix."),
                true);
            var nodes = group.Boxes[0].Events;
            var timeline = GLSEventCommon.GetColorTimeline(nodes[0], DiagnosticLightCount);
            Assert.True(timeline.TryGetOutgoing(nodes[0], 0, out var prefix));
            Assert.True(timeline.TryGetOutgoing(nodes[1], 0, out var crossing));
            Assert.True(timeline.TryGetOutgoing(nodes[2], 0, out var future));

            ChangeAuthoredBpmTo200();

            var refreshed = GLSEventCommon.GetColorTimeline(nodes[0], DiagnosticLightCount);
            Assert.AreSame(timeline, refreshed, "A BPM edit must retime the existing per-light schedule.");
            Assert.True(refreshed.TryGetOutgoing(nodes[0], 0, out var refreshedPrefix));
            Assert.AreSame(prefix, refreshedPrefix);
            Assert.That(prefix.StartTime, Is.EqualTo(0f).Within(0.001f));
            Assert.That(prefix.EndTime, Is.EqualTo(2f).Within(0.001f));
            Assert.True(refreshed.TryGetOutgoing(nodes[1], 0, out var refreshedCrossing));
            Assert.AreSame(crossing, refreshedCrossing);
            Assert.That(crossing.StartTime, Is.EqualTo(2f).Within(0.001f));
            Assert.That(crossing.EndTime, Is.EqualTo(8f).Within(0.001f));
            Assert.True(refreshed.TryGetOutgoing(nodes[2], 0, out var refreshedFuture));
            Assert.AreSame(future, refreshedFuture);
            Assert.That(future.StartTime, Is.EqualTo(8f).Within(0.001f));
            Assert.That(future.EndTime, Is.EqualTo(10f).Within(0.001f));
            Assert.AreSame(future, crossing.Next);
        }

        [TestCase(DistributionType.Step)]
        [TestCase(DistributionType.Wave)]
        public void BpmChangeRetimesDistributedLightsWithoutRebuildingEarlierLight(DistributionType distribution)
        {
            PlaceBpmEvents();
            var group = (BaseLightColorEventBoxGroup)CreateDiagnosticGroup(
                GlsNodeKind.Color,
                GroupLayout.GroupSpansBpmChange);
            var box = group.Boxes[0];
            box.Events = new[] { ColorNode(0f, Color.red), ColorNode(2f, Color.green) };
            box.BeatDistribution = distribution == DistributionType.Step ? 6f : 14f;
            box.BeatDistributionType = (int)distribution;
            group.NormalizeLoadedEventConflicts();
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(group, Array.Empty<BaseObject>(), "Placed distributed timing fixture."),
                true);
            var timeline = GLSEventCommon.GetColorTimeline(box.Events[0], 3);
            var before = new LightColorEventStateData[3];
            for (var light = 0; light < 3; light++)
            {
                Assert.True(timeline.TryGetOutgoing(box.Events[0], light, out before[light]));
                Assert.That(before[light].StartTime, Is.EqualTo(light * 6f).Within(0.001f));
            }

            ChangeAuthoredBpmTo200();

            var refreshed = GLSEventCommon.GetColorTimeline(box.Events[0], 3);
            Assert.AreSame(timeline, refreshed);
            for (var light = 0; light < 3; light++)
            {
                Assert.True(refreshed.TryGetOutgoing(box.Events[0], light, out var state));
                Assert.AreSame(before[light], state);
                var expectedStart = light == 0 ? 0f : 4f + ((light * 6f - 4f) / 2f);
                var expectedEnd = light == 0 ? 2f : expectedStart + 1f;
                Assert.That(state.StartTime, Is.EqualTo(expectedStart).Within(0.001f));
                Assert.That(state.EndTime, Is.EqualTo(expectedEnd).Within(0.001f));
            }

            PlaceUtils.Undo();
            for (var light = 0; light < 3; light++)
            {
                Assert.That(before[light].StartTime, Is.EqualTo(light * 6f).Within(0.001f));
                Assert.That(before[light].EndTime, Is.EqualTo((light * 6f) + 2f).Within(0.001f));
            }

        }

        [TestCase(GlsNodeKind.Color)]
        [TestCase(GlsNodeKind.Rotation)]
        [TestCase(GlsNodeKind.Translation)]
        [TestCase(GlsNodeKind.FloatFx)]
        public void BpmChangeRetimesGeneratedDistributionOffset(GlsNodeKind kind)
        {
            PlaceBpmEvents();
            switch (kind)
            {
                case GlsNodeKind.Color:
                    colorEffect.Count = 2;
                    recordingLight = effectRoot.AddComponent<RecordingLight>();
                    recordingLight.ID = 1;
                    colorEffect.Register(recordingLight);
                    colorEffect.Initialize();
                    break;
                case GlsNodeKind.Rotation:
                    rotationEffect.Count = 2;
                    rotationTarget = CreateTarget("Distributed rotation X");
                    rotationEffect.Register(1, Axis.X, false, rotationTarget);
                    rotationEffect.Initialize();
                    break;
                case GlsNodeKind.Translation:
                    translationEffect.Count = 2;
                    translationTarget = CreateTarget("Distributed translation X");
                    translationEffect.Register(1, Axis.X, false, translationTarget);
                    translationEffect.Initialize();
                    break;
                default:
                    floatFxEffect.Count = 2;
                    recordingFxTarget = effectRoot.AddComponent<RecordingFxTarget>();
                    floatFxEffect.Register(1, recordingFxTarget);
                    floatFxEffect.Initialize();
                    break;
            }

            var group = CreateDiagnosticGroup(kind, GroupLayout.GroupSpansBpmChange);
            group.ReadOnlyBoxes[0].BeatDistribution = 6f;
            group.ReadOnlyBoxes[0].BeatDistributionType = (int)DistributionType.Step;
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(group, Array.Empty<BaseObject>(), "Placed distributed preview."),
                true);
            AssertPreview(
                kind,
                13f,
                kind == GlsNodeKind.Rotation ? 45f : 0.2f,
                new Color(0.5f, 0.5f, 0f, 1f),
                "before BPM edit");

            ChangeAuthoredBpmTo200();

            AssertPreview(
                kind,
                8.5f,
                kind == GlsNodeKind.Rotation ? 45f : 0.2f,
                new Color(0.5f, 0.5f, 0f, 1f),
                "after BPM edit");
            PlaceUtils.Undo();
            AssertPreview(
                kind,
                13f,
                kind == GlsNodeKind.Rotation ? 45f : 0.2f,
                new Color(0.5f, 0.5f, 0f, 1f),
                "after undo");
        }

        [TestCase(GroupLayout.FutureGroup)]
        [TestCase(GroupLayout.GroupSpansBpmChange)]
        public void BpmChangeRetimesWarmedColorTransitionTimeline(GroupLayout layout)
        {
            PlaceBpmEvents();
            var group = (BaseLightColorEventBoxGroup)PlaceDiagnosticGroup(GlsNodeKind.Color, layout);
            var spanning = layout == GroupLayout.GroupSpansBpmChange;
            var source = group.Boxes[0].Events[0];
            var expectedEndBefore = spanning ? 12f : 28f;

            var warmedTimeline = GLSEventCommon.GetColorTimeline(source, DiagnosticLightCount);
            Assert.That(warmedTimeline, Is.Not.Null);
            Assert.That(warmedTimeline.TryGetOutgoing(source, 0, out var warmedState), Is.True);
            Assert.That(
                warmedState.StartTime,
                Is.EqualTo(spanning ? 2f : 12f).Within(0.001f),
                "warmed transition segment start before the BPM edit");
            Assert.That(
                warmedState.EndTime,
                Is.EqualTo(expectedEndBefore).Within(0.001f),
                "warmed transition segment end before the BPM edit");
            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var warmedEnd), Is.True);
            Assert.That(
                warmedEnd,
                Is.EqualTo(expectedEndBefore).Within(0.001f),
                "warmed transition end bound before the BPM edit");

            ChangeAuthoredBpmTo200();

            var timeline = GLSEventCommon.GetColorTimeline(source, DiagnosticLightCount);
            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.TryGetOutgoing(source, 0, out var state), Is.True);
            var expectedStartAfter = spanning ? 2f : 8f;
            var expectedEndAfter = spanning ? 8f : 16f;
            Assert.That(
                state.StartTime,
                Is.EqualTo(expectedStartAfter).Within(0.001f),
                "transition segment start after the BPM edit");
            Assert.That(
                state.EndTime,
                Is.EqualTo(expectedEndAfter).Within(0.001f),
                "transition segment end after the BPM edit");
            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var endAfter), Is.True);
            Assert.That(
                endAfter,
                Is.EqualTo(expectedEndAfter).Within(0.001f),
                "transition end bound after the BPM edit");
            Assert.That(
                GLSEventCommon.TryGetColorRibbonBounds(
                    source,
                    DiagnosticLightCount,
                    out var boundStart,
                    out var boundEnd),
                Is.True);
            Assert.That(
                boundStart,
                Is.EqualTo(expectedStartAfter).Within(0.001f),
                "ribbon inner bound start after the BPM edit");
            Assert.That(
                boundEnd,
                Is.EqualTo(expectedEndAfter).Within(0.001f),
                "ribbon inner bound end after the BPM edit");
        }

        [TestCase(GlsNodeKind.Color, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.Color, GroupLayout.GroupSpansBpmChange)]
        [TestCase(GlsNodeKind.Rotation, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.Rotation, GroupLayout.GroupSpansBpmChange)]
        [TestCase(GlsNodeKind.Translation, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.Translation, GroupLayout.GroupSpansBpmChange)]
        [TestCase(GlsNodeKind.FloatFx, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.FloatFx, GroupLayout.GroupSpansBpmChange)]
        public void BpmChangeUndoRedoRestoresGlsTiming(GlsNodeKind kind, GroupLayout layout)
        {
            PlaceBpmEvents();
            var group = PlaceDiagnosticGroup(kind, layout);
            var spanning = layout == GroupLayout.GroupSpansBpmChange;
            var nodes = group.ReadOnlyBoxes[0].ReadOnlyEvents;

            AssertSongBpmTime(group, spanning ? 0f : 12f, "group before the BPM edit");
            AssertSongBpmTime(nodes[0], spanning ? 2f : 12f, "first node before the BPM edit");
            AssertSongBpmTime(nodes[^1], spanning ? 16f : 28f, "last node before the BPM edit");

            AssertBaselinePreview(kind, spanning);
            ChangeAuthoredBpmTo200();

            AssertSongBpmTime(group, spanning ? 0f : 8f, "group after the BPM edit");
            AssertSongBpmTime(nodes[0], spanning ? 2f : 8f, "first node after the BPM edit");
            AssertSongBpmTime(nodes[^1], spanning ? 10f : 16f, "last node after the BPM edit");
            if (spanning)
            {
                AssertSongBpmTime(nodes[1], 8f, "node past the edited beat after the BPM edit");
            }

            if (spanning)
            {
                AssertPreview(
                    kind,
                    5f,
                    kind == GlsNodeKind.Rotation ? 45f : 0.2f,
                    new Color(0.5f, 0.5f, 0f, 1f),
                    "preview at remapped song time 5");
                AssertPreview(
                    kind,
                    9f,
                    kind == GlsNodeKind.Rotation ? 180f : 0.7f,
                    new Color(0f, 0.5f, 0.5f, 1f),
                    "preview at remapped song time 9");
                AssertPreview(
                    kind,
                    12f,
                    kind == GlsNodeKind.Rotation ? 270f : 1f,
                    new Color(0f, 0f, 1f, 1f),
                    "preview at remapped song time 12");
            }
            else
            {
                AssertPreview(
                    kind,
                    8f,
                    kind == GlsNodeKind.Rotation ? 30f : 0.2f,
                    new Color(0f, 1f, 0f, 1f),
                    "preview at remapped song time 8");
                AssertPreview(
                    kind,
                    12f,
                    kind == GlsNodeKind.Rotation ? 105f : 0.6f,
                    new Color(0f, 0.5f, 0.5f, 1f),
                    "preview at remapped song time 12");
                AssertPreview(
                    kind,
                    20f,
                    kind == GlsNodeKind.Rotation ? 180f : 1f,
                    new Color(0f, 0f, 1f, 1f),
                    "preview at remapped song time 20");
            }

            PlaceUtils.Undo();
            AssertSongBpmTime(group, spanning ? 0f : 12f, "group after undoing the BPM edit");
            AssertSongBpmTime(nodes[^1], spanning ? 16f : 28f, "last node after undoing the BPM edit");
            AssertRestoredTimingPreview(kind, spanning, "preview after undoing the BPM edit");

            PlaceUtils.Redo();
            AssertSongBpmTime(group, spanning ? 0f : 8f, "group after redoing the BPM edit");
            AssertSongBpmTime(nodes[^1], spanning ? 10f : 16f, "last node after redoing the BPM edit");
            AssertChangedTimingPreview(kind, spanning, "preview after redoing the BPM edit");
        }

        [TestCase(GlsNodeKind.Color, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.Color, GroupLayout.GroupSpansBpmChange)]
        [TestCase(GlsNodeKind.Rotation, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.Rotation, GroupLayout.GroupSpansBpmChange)]
        [TestCase(GlsNodeKind.Translation, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.Translation, GroupLayout.GroupSpansBpmChange)]
        [TestCase(GlsNodeKind.FloatFx, GroupLayout.FutureGroup)]
        [TestCase(GlsNodeKind.FloatFx, GroupLayout.GroupSpansBpmChange)]
        public void BpmEventPlacementDeletionRetimesGlsTiming(GlsNodeKind kind, GroupLayout layout)
        {
            PlaceUtils.Place(new BaseBpmEvent(0f, 100f));
            var group = PlaceDiagnosticGroup(kind, layout);
            var spanning = layout == GroupLayout.GroupSpansBpmChange;
            var nodes = group.ReadOnlyBoxes[0].ReadOnlyEvents;

            AssertBaselinePreview(kind, spanning);

            var placedChange = PlaceUtils.Place(new BaseBpmEvent(BpmChangeJsonTime, 200f));
            AssertSongBpmTime(nodes[^1], spanning ? 10f : 16f, "last node after placing the beat-4 BPM event");
            AssertChangedTimingPreview(kind, spanning, "preview after placing the beat-4 BPM event");

            PlaceUtils.Delete(placedChange);
            AssertSongBpmTime(nodes[^1], spanning ? 16f : 28f, "last node after deleting the beat-4 BPM event");
            AssertRestoredTimingPreview(kind, spanning, "preview after deleting the beat-4 BPM event");

            PlaceUtils.Undo();
            AssertSongBpmTime(nodes[^1], spanning ? 10f : 16f, "last node after undoing the BPM deletion");
            AssertChangedTimingPreview(kind, spanning, "preview after undoing the BPM deletion");

            PlaceUtils.Redo();
            AssertSongBpmTime(nodes[^1], spanning ? 16f : 28f, "last node after redoing the BPM deletion");
            AssertRestoredTimingPreview(kind, spanning, "preview after redoing the BPM deletion");
        }

        [TestCase(GroupLayout.FutureGroup)]
        [TestCase(GroupLayout.GroupSpansBpmChange)]
        public void BpmChangeRetimesLoadedColorTransitionRibbon(GroupLayout layout)
        {
            PlaceBpmEvents();
            var group = (BaseLightColorEventBoxGroup)PlaceDiagnosticGroup(GlsNodeKind.Color, layout);
            var spanning = layout == GroupLayout.GroupSpansBpmChange;
            var source = group.Boxes[0].Events[0];
            var editContext = Object.FindAnyObjectByType<EditModeContext>();
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var innerCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            var previousMode = editContext.EditingMode;
            var previousContext = provider.GroupContext;
            try
            {
                editContext.EditingMode = EditingMode.EventBox;
                provider.LastContext = null;
                provider.GroupContext = group;
                innerCollection.RefreshPool(-1f, 60f, true);

                var ribbon = GetLoadedSourceRibbon(innerCollection, source);
                var expectedDurationBefore = spanning ? 10f : 16f;
                Assert.That(
                    ribbon.ColorTimelineDuration,
                    Is.EqualTo(expectedDurationBefore).Within(0.001f),
                    "The loaded source ribbon duration before the BPM edit");
                Assert.That(
                    ribbon.transform.localScale.x,
                    Is.EqualTo(expectedDurationBefore * EditorScaleController.EditorScale * (4f / 3f))
                        .Within(0.001f),
                    "The loaded source ribbon scale before the BPM edit");

                ChangeAuthoredBpmTo200();

                ribbon = GetLoadedSourceRibbon(innerCollection, source);
                var expectedDurationAfter = spanning ? 6f : 8f;
                Assert.That(
                    ribbon.ColorTimelineDuration,
                    Is.EqualTo(expectedDurationAfter).Within(0.001f),
                    "The loaded source ribbon duration after the BPM edit");
                Assert.That(
                    ribbon.transform.localScale.x,
                    Is.EqualTo(expectedDurationAfter * EditorScaleController.EditorScale * (4f / 3f))
                        .Within(0.001f),
                    "The loaded source ribbon scale after the BPM edit");
            }
            finally
            {
                provider.GroupContext = previousContext;
                editContext.EditingMode = previousMode;
            }
        }

        [TestCase(GlsNodeKind.Color)]
        [TestCase(GlsNodeKind.Rotation)]
        [TestCase(GlsNodeKind.Translation)]
        [TestCase(GlsNodeKind.FloatFx)]
        public void BpmChangeOnEmptyGroupKindClearsPreviewOutput(GlsNodeKind kind)
        {
            PlaceBpmEvents();
            var group = CreateDiagnosticGroup(kind, GroupLayout.FutureGroup);
            try
            {
                AddGroupToMapKind(kind, group);
                glsManager.Refresh();
                AssertBaselinePreview(kind, false);

                RemoveGroupFromMapKind(kind, group);
                ChangeAuthoredBpmTo200();

                AssertClearedPreview(kind);
            }
            finally
            {
                RemoveGroupFromMapKind(kind, group);
            }
        }

        private static void AddGroupToMapKind(GlsNodeKind kind, BaseEventBoxGroup group)
        {
            var map = BeatSaberSongContainer.Instance.Map;
            switch (kind)
            {
                case GlsNodeKind.Color:
                    map.LightColorEventBoxGroups.Add((BaseLightColorEventBoxGroup)group);
                    break;
                case GlsNodeKind.Rotation:
                    map.LightRotationEventBoxGroups.Add((BaseLightRotationEventBoxGroup)group);
                    break;
                case GlsNodeKind.Translation:
                    map.LightTranslationEventBoxGroups.Add((BaseLightTranslationEventBoxGroup)group);
                    break;
                default:
                    map.VfxEventBoxGroups.Add((BaseVfxEventEventBoxGroup)group);
                    break;
            }
        }

        private static void RemoveGroupFromMapKind(GlsNodeKind kind, BaseEventBoxGroup group)
        {
            var map = BeatSaberSongContainer.Instance.Map;
            switch (kind)
            {
                case GlsNodeKind.Color:
                    map.LightColorEventBoxGroups.Remove((BaseLightColorEventBoxGroup)group);
                    break;
                case GlsNodeKind.Rotation:
                    map.LightRotationEventBoxGroups.Remove((BaseLightRotationEventBoxGroup)group);
                    break;
                case GlsNodeKind.Translation:
                    map.LightTranslationEventBoxGroups.Remove((BaseLightTranslationEventBoxGroup)group);
                    break;
                default:
                    map.VfxEventBoxGroups.Remove((BaseVfxEventEventBoxGroup)group);
                    break;
            }
        }

        private void AssertClearedPreview(GlsNodeKind kind)
        {
            glsManager.UpdateTime(false, 20f);
            switch (kind)
            {
                case GlsNodeKind.Color:
                    Assert.That(
                        recordingLight.Color.a,
                        Is.EqualTo(0f).Within(0.01f),
                        "GLS color output must clear after a timing refresh leaves the kind without authored groups.");
                    break;
                case GlsNodeKind.Rotation:
                    Assert.That(
                        Quaternion.Angle(rotationTarget.localRotation, Quaternion.identity),
                        Is.LessThan(0.01f),
                        "GLS rotation output must clear after a timing refresh leaves the kind without authored groups.");
                    break;
                case GlsNodeKind.Translation:
                    Assert.That(
                        translationTarget.localPosition.x,
                        Is.EqualTo(0f).Within(0.001f),
                        "GLS translation output must clear after a timing refresh leaves the kind without authored groups.");
                    break;
                case GlsNodeKind.FloatFx:
                    Assert.That(
                        recordingFxTarget.Value,
                        Is.EqualTo(0f).Within(0.001f),
                        "GLS float FX output must clear after a timing refresh leaves the kind without authored groups.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        protected override void BeforeCleanup()
        {
            var descriptor = context.Descriptor;
            descriptor.LightColorGroupEffectManager.IdToEffect.Remove(DiagnosticColorGroupId);
            descriptor.LightRotationGroupEffectManager.IdToEffect.Remove(DiagnosticRotationGroupId);
            descriptor.LightTranslationGroupEffectManager.IdToEffect.Remove(DiagnosticTranslationGroupId);
            descriptor.FloatFxGroupEffectManager.IdToEffect.Remove(DiagnosticFloatFxGroupId);
            RemoveDiagnosticGroups();
            Object.DestroyImmediate(effectRoot);
            Object.DestroyImmediate(fixtureColorScheme);
        }

        private Transform CreateTarget(string targetName)
        {
            var target = new GameObject(targetName);
            target.transform.SetParent(effectRoot.transform);
            return target.transform;
        }

        private void PlaceBpmEvents()
        {
            PlaceUtils.Place(new BaseBpmEvent(0f, 100f));
            bpmEventAtChange = PlaceUtils.Place(new BaseBpmEvent(BpmChangeJsonTime, 100f));
        }

        private BaseEventBoxGroup PlaceDiagnosticGroup(GlsNodeKind kind, GroupLayout layout)
        {
            var group = CreateDiagnosticGroup(kind, layout);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    group,
                    Array.Empty<BaseObject>(),
                    "Placed a diagnostic GLS group."),
                true);
            return group;
        }

        private void ChangeAuthoredBpmTo200()
        {
            var bpmCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);

            // These synchronous cases do not advance collection LateUpdate, so load the BPM marker before its UI edit.
            bpmCollection.RefreshPool(bpmEventAtChange.SongBpmTime, bpmEventAtChange.SongBpmTime, true);
            Assert.That(
                bpmCollection.LoadedContainers.TryGetValue(bpmEventAtChange, out var container),
                Is.True,
                "The placed beat-4 BPM event must expose a loaded container for ChangeBpm.");
            BeatmapBPMChangeInputController.ChangeBpm((BpmEventContainer)container, ChangedBpmText);
            Assert.That(bpmEventAtChange.Bpm, Is.EqualTo(200f).Within(0.001f));
        }

        private static BaseEventBoxGroup CreateDiagnosticGroup(GlsNodeKind kind, GroupLayout layout)
        {
            var spanning = layout == GroupLayout.GroupSpansBpmChange;
            var groupJsonTime = spanning ? 0f : 12f;
            BaseEventBoxGroup group;
            switch (kind)
            {
                case GlsNodeKind.Color:
                    var colorGroup = new BaseLightColorEventBoxGroup
                    {
                        ID = DiagnosticColorGroupId,
                        JsonTime = groupJsonTime,
                        Boxes =
                        {
                            new BaseLightColorEventBox
                            {
                                IndexFilter = SingleElementFilter(),
                                Events = spanning
                                    ? new[]
                                    {
                                        ColorNode(2f, new Color(1f, 0f, 0f, 1f)),
                                        ColorNode(12f, new Color(0f, 1f, 0f, 1f)),
                                        ColorNode(16f, new Color(0f, 0f, 1f, 1f))
                                    }
                                    : new[]
                                    {
                                        ColorNode(0f, new Color(0f, 1f, 0f, 1f)),
                                        ColorNode(16f, new Color(0f, 0f, 1f, 1f))
                                    }
                            }
                        }
                    };
                    colorGroup.NormalizeLoadedEventConflicts();
                    group = colorGroup;
                    break;
                case GlsNodeKind.Rotation:
                    var rotationGroup = new BaseLightRotationEventBoxGroup
                    {
                        ID = DiagnosticRotationGroupId,
                        JsonTime = groupJsonTime,
                        Boxes =
                        {
                            new BaseLightRotationEventBox
                            {
                                Axis = (int)Axis.X,
                                IndexFilter = SingleElementFilter(),
                                Events = spanning
                                    ? new[]
                                    {
                                        RotationNode(2f, 0f),
                                        RotationNode(12f, 90f),
                                        RotationNode(16f, 270f)
                                    }
                                    : new[]
                                    {
                                        RotationNode(0f, 30f),
                                        RotationNode(16f, 180f)
                                    }
                            }
                        }
                    };
                    rotationGroup.NormalizeLoadedEventConflicts();
                    group = rotationGroup;
                    break;
                case GlsNodeKind.Translation:
                    var translationGroup = new BaseLightTranslationEventBoxGroup
                    {
                        ID = DiagnosticTranslationGroupId,
                        JsonTime = groupJsonTime,
                        Boxes =
                        {
                            new BaseLightTranslationEventBox
                            {
                                Axis = (int)Axis.X,
                                IndexFilter = SingleElementFilter(),
                                Events = spanning
                                    ? new[]
                                    {
                                        TranslationNode(2f, 0f),
                                        TranslationNode(12f, 0.4f),
                                        TranslationNode(16f, 1f)
                                    }
                                    : new[]
                                    {
                                        TranslationNode(0f, 0.2f),
                                        TranslationNode(16f, 1f)
                                    }
                            }
                        }
                    };
                    translationGroup.NormalizeLoadedEventConflicts();
                    group = translationGroup;
                    break;
                default:
                    var floatFxGroup = new BaseVfxEventEventBoxGroup
                    {
                        ID = DiagnosticFloatFxGroupId,
                        JsonTime = groupJsonTime,
                        Boxes =
                        {
                            new BaseVfxEventEventBox
                            {
                                IndexFilter = SingleElementFilter(),
                                Events = spanning
                                    ? new[]
                                    {
                                        FloatFxNode(2f, 0f),
                                        FloatFxNode(12f, 0.4f),
                                        FloatFxNode(16f, 1f)
                                    }
                                    : new[]
                                    {
                                        FloatFxNode(0f, 0.2f),
                                        FloatFxNode(16f, 1f)
                                    }
                            }
                        }
                    };
                    floatFxGroup.NormalizeLoadedEventConflicts();
                    group = floatFxGroup;
                    break;
            }

            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            return group;
        }

        private static BaseIndexFilter SingleElementFilter() => new()
        {
            Type = (int)IndexFilterType.Division,
            Param0 = 1,
            Param1 = 0
        };

        private static BaseLightColorBase ColorNode(float relativeJsonTime, Color color) => new()
        {
            RelativeJsonTime = relativeJsonTime,
            Brightness = 1f,
            Easing = (int)EaseType.Linear,
            CustomColor = color
        };

        private static BaseLightRotationBase RotationNode(float relativeJsonTime, float rotation) => new()
        {
            RelativeJsonTime = relativeJsonTime,
            Rotation = rotation,
            Direction = (int)LightRotationDirection.Automatic,
                        Easing = (int)EaseType.Linear
        };

        private static BaseLightTranslationBase TranslationNode(float relativeJsonTime, float translation) =>
            new()
            {
                RelativeJsonTime = relativeJsonTime,
                Translation = translation,
                        Easing = (int)EaseType.Linear
            };

        private static BaseFxEventFloat FloatFxNode(float relativeJsonTime, float value) => new()
        {
            RelativeJsonTime = relativeJsonTime,
            Value = value,
            Easing = (int)EaseType.Linear
        };

        private void AssertBaselinePreview(GlsNodeKind kind, bool spanning)
        {
            AssertPreview(
                kind,
                spanning ? 7f : 20f,
                kind == GlsNodeKind.Rotation ? (spanning ? 45f : 105f) : (spanning ? 0.2f : 0.6f),
                spanning ? new Color(0.5f, 0.5f, 0f, 1f) : new Color(0f, 0.5f, 0.5f, 1f),
                "preview before the BPM edit");
        }

        private void AssertRestoredTimingPreview(GlsNodeKind kind, bool spanning, string context)
        {
            AssertPreview(
                kind,
                spanning ? 9f : 20f,
                kind == GlsNodeKind.Rotation ? (spanning ? 63f : 105f) : (spanning ? 0.28f : 0.6f),
                spanning ? new Color(0.3f, 0.7f, 0f, 1f) : new Color(0f, 0.5f, 0.5f, 1f),
                context);
        }

        private void AssertChangedTimingPreview(GlsNodeKind kind, bool spanning, string context)
        {
            AssertPreview(
                kind,
                spanning ? 9f : 12f,
                kind == GlsNodeKind.Rotation ? (spanning ? 180f : 105f) : (spanning ? 0.7f : 0.6f),
                new Color(0f, 0.5f, 0.5f, 1f),
                context);
        }

        private void AssertPreview(
            GlsNodeKind kind,
            float songBpmTime,
            float expectedScalar,
            Color expectedColor,
            string message)
        {
            if (kind == GlsNodeKind.Color)
            {
                AssertColorPreview(songBpmTime, expectedColor, message);
            }
            else
            {
                AssertScalarPreview(kind, songBpmTime, expectedScalar, message);
            }
        }

        private void AssertScalarPreview(GlsNodeKind kind, float songBpmTime, float expected, string message)
        {
            glsManager.UpdateTime(false, songBpmTime);
            switch (kind)
            {
                case GlsNodeKind.Rotation:
                    Assert.That(
                        Quaternion.Angle(
                            rotationTarget.localRotation,
                            Quaternion.AngleAxis(expected, Vector3.right)),
                        Is.LessThan(0.01f),
                        $"{message}: X rotation at song time {songBpmTime} must be {expected}.");
                    break;
                case GlsNodeKind.Translation:
                    Assert.That(
                        translationTarget.localPosition.x,
                        Is.EqualTo(expected).Within(0.001f),
                        $"{message}: X translation at song time {songBpmTime} must be {expected}.");
                    break;
                case GlsNodeKind.FloatFx:
                    Assert.That(
                        recordingFxTarget.Value,
                        Is.EqualTo(expected).Within(0.001f),
                        $"{message}: FX value at song time {songBpmTime} must be {expected}.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private void AssertColorPreview(float songBpmTime, Color expected, string message)
        {
            glsManager.UpdateTime(false, songBpmTime);
            var actual = recordingLight.Color;
            Assert.That(
                actual.r,
                Is.EqualTo(expected.r).Within(0.01f),
                $"{message}: red at song time {songBpmTime}.");
            Assert.That(
                actual.g,
                Is.EqualTo(expected.g).Within(0.01f),
                $"{message}: green at song time {songBpmTime}.");
            Assert.That(
                actual.b,
                Is.EqualTo(expected.b).Within(0.01f),
                $"{message}: blue at song time {songBpmTime}.");
            Assert.That(
                actual.a,
                Is.EqualTo(expected.a).Within(0.01f),
                $"{message}: alpha at song time {songBpmTime}.");
        }

        private static void AssertSongBpmTime(BaseObject obj, float expected, string context)
        {
            Assert.That(
                obj.SongBpmTime,
                Is.EqualTo(expected).Within(0.001f),
                $"{context}: {obj.GetType().Name} at JsonTime {obj.JsonTime} must render at song time {expected}.");
        }

        private static bool IsDiagnosticGroupId(int id) =>
            id is >= DiagnosticColorGroupId and <= DiagnosticFloatFxGroupId;

        private void RemoveDiagnosticGroups()
        {
            RemoveDiagnosticGroupsFrom(
                BeatmapObjectContainerCollection.GetCollectionForType<GLSGroupColorGridContainer>(
                    ObjectType.GLSColor));
            RemoveDiagnosticGroupsFrom(
                BeatmapObjectContainerCollection.GetCollectionForType<GLSGroupRotationGridContainer>(
                    ObjectType.GLSRotation));
            RemoveDiagnosticGroupsFrom(
                BeatmapObjectContainerCollection.GetCollectionForType<GLSGroupTranslationGridContainer>(
                    ObjectType.GLSTranslation));
            RemoveDiagnosticGroupsFrom(
                BeatmapObjectContainerCollection.GetCollectionForType<GLSGroupFloatFXGridContainer>(
                    ObjectType.GLSFloatFx));
        }

        private static void RemoveDiagnosticGroupsFrom<TGroup>(
            BeatmapObjectContainerCollection<TGroup> collection)
            where TGroup : BaseEventBoxGroup
        {
            for (var index = collection.MapObjects.Count - 1; index >= 0; index--)
            {
                var group = collection.MapObjects[index];
                if (IsDiagnosticGroupId(group.ID))
                {
                    collection.DeleteObject(
                        group,
                        triggersAction: false,
                        refreshesPool: false,
                        comment: "Remove diagnostic GLS group.",
                        inCollectionOfDeletes: false,
                        deselect: false);
                }
            }
        }

        private static LightGradientController GetLoadedSourceRibbon(
            GLSEventGridContainer innerCollection,
            BaseLightColorBase source)
        {
            Assert.That(
                innerCollection.LoadedContainers.TryGetValue(source, out var loaded),
                Is.True,
                "The diagnostic color source node must keep a pooled inner container.");
            var ribbon = ((GLSEventContainer)loaded).LightGradientController;
            Assert.That(ribbon, Is.Not.Null, "The pooled color node has no outgoing transition ribbon.");
            return ribbon;
        }

        private class RecordingLight : LightController
        {
            protected override bool Initialize() => true;
            public override void SetColor(Color color) => Color = color;
        }

        private class RecordingFxTarget : FxTarget
        {
            public float Value;
            public override void SetValue(int group, int id, float value) => Value = value;
            public override void TriggerValue(int group, int id, float value)
            {
            }
        }
    }
}
