using System;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSRotationSessionPlaybackTest : TestBase
    {
        private const int DiagnosticGroupId = 1000000;

        private GameObject effectObject;
        private GameObject targetObject;
        private GameObject firstPlacementTargetObject;
        private LightRotationGroupEffect effect;
        private LightRotationGroupEffectManager sceneRotationManager;
        private BaseLightRotationEventBoxGroup actionGroup;

        [SetUp]
        public void CreateRotationPreview()
        {
            effectObject = new GameObject(nameof(GLSRotationSessionPlaybackTest));
            effect = effectObject.AddComponent<LightRotationGroupEffect>();
            effect.Atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            effect.Count = 1;

            targetObject = new GameObject("Observed GLS rotation target");
            targetObject.transform.SetParent(effectObject.transform);
            effect.Register(0, Axis.Y, false, targetObject.transform);

            // Weave's initial outer rotation placement is its automatic X lane; observe that lane independently.
            firstPlacementTargetObject = new GameObject("Observed first-placement GLS rotation target");
            firstPlacementTargetObject.transform.SetParent(effectObject.transform);
            effect.Register(0, Axis.X, false, firstPlacementTargetObject.transform);
            effect.Initialize();
        }

        // Seek past the edited group before placing a node, as in a long mapping session.
        [Test]
        public void PlacingRotationNodeAfterAdvancingPreviewMatchesReload()
        {
            var original = CreateGroup(4f, (0f, 30f));
            effect.InsertData(original);
            effect.Refresh();
            effect.UpdateTime(false, SongTime(40f));

            var edited = CreateGroup(4f, (0f, 30f), (36f, 135f));
            ReplaceGroup(original, edited);
            effect.UpdateTime(false, SongTime(40f));

            Assert.That(CurrentYRotation(), Is.EqualTo(135f).Within(0.001f));
        }

        [Test]
        public void RepeatedRotationNodePlacementKeepsLivePreviewEquivalentToReload()
        {
            var liveGroup = CreateGroup(8f, (0f, 15f));
            effect.InsertData(liveGroup);
            effect.Refresh();

            for (var node = 1; node <= 64; node++)
            {
                effect.UpdateTime(false, SongTime(8f + node));
                var edited = CreateGroupWithRegularNodes(8f, node, 15f);
                ReplaceGroup(liveGroup, edited);
                liveGroup = edited;
            }

            effect.UpdateTime(false, SongTime(72f));

            Assert.That(
                Quaternion.Angle(
                    targetObject.transform.localRotation,
                    Quaternion.Euler(0f, 15f + (64f * 5f), 0f)),
                Is.LessThan(0.001f));
        }

        [Test]
        public void LocalRotationGroupPlacementUpdatesPreviewWithoutReload()
        {
            RegisterEffectWithSceneManager();
            actionGroup = CreateGroup(0f, (0f, 90f));
            actionGroup.ID = DiagnosticGroupId;

            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    actionGroup,
                    Array.Empty<BaseObject>(),
                    "Place local diagnostic GLS rotation group."),
                true);

            Assert.That(
                Quaternion.Angle(targetObject.transform.localRotation, Quaternion.Euler(0f, 90f, 0f)),
                Is.LessThan(0.001f));
        }

        [Test]
        public void FirstRotationGroupPlacedFromAutomaticAxisLaneUpdatesPreviewWithoutReload()
        {
            RegisterEffectWithSceneManager();
            actionGroup = CreateGroup(0f, Axis.X, (0f, 90f));
            actionGroup.ID = DiagnosticGroupId;
            actionGroup.Boxes[0].IsAutomaticAxisLane = true;

            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    actionGroup,
                    Array.Empty<BaseObject>(),
                    "Place the first diagnostic GLS rotation group from its automatic axis lane."),
                true);

            Assert.That(
                Quaternion.Angle(firstPlacementTargetObject.transform.localRotation, Quaternion.Euler(90f, 0f, 0f)),
                Is.LessThan(0.001f),
                "The first placed node remained a display-only lane in the live playback cache.");
        }

        // Remote undo removes the group by GUID; its cached GLS effect must disappear in the same operation.
        [Test]
        public void NetworkedRotationGroupUndoUpdatesPreviewWithoutReload()
        {
            RegisterEffectWithSceneManager();
            actionGroup = CreateGroup(0f, (0f, 90f));
            actionGroup.ID = DiagnosticGroupId;
            var action = new BeatmapObjectPlacementAction(
                actionGroup,
                Array.Empty<BaseObject>(),
                "Place networked diagnostic GLS rotation group.")
            {
                Networked = true
            };

            BeatmapActionContainer.AddAction(action, true);

            // Rebuild playback from the map before remote undo so only the undo notification is under test.
            effect.Initialize();
            effect.InsertData(actionGroup);
            effect.Refresh();
            Assert.That(
                Quaternion.Angle(targetObject.transform.localRotation, Quaternion.Euler(0f, 90f, 0f)),
                Is.LessThan(0.001f));

            BeatmapActionContainer.Undo(action.Guid);

            Assert.That(
                Quaternion.Angle(targetObject.transform.localRotation, Quaternion.identity),
                Is.LessThan(0.001f),
                "The received undo removed the group from the map but not from the live GLS rotation cache.");
        }

        // Check received placement before rebuilding the empty cache to isolate the redo notification.
        [Test]
        public void NetworkedRotationGroupPlacementAndRedoUpdatePreviewWithoutReload()
        {
            RegisterEffectWithSceneManager();
            actionGroup = CreateGroup(0f, (0f, 90f));
            actionGroup.ID = DiagnosticGroupId;
            var action = new BeatmapObjectPlacementAction(
                actionGroup,
                Array.Empty<BaseObject>(),
                "Place networked diagnostic GLS rotation group.")
            {
                Networked = true
            };

            BeatmapActionContainer.AddAction(action, true);

            Assert.That(
                Quaternion.Angle(targetObject.transform.localRotation, Quaternion.Euler(0f, 90f, 0f)),
                Is.LessThan(0.001f),
                "The received group reached the map but not the live GLS rotation cache.");

            BeatmapActionContainer.Undo(action.Guid);

            // Rebuild playback after undo so only the remote redo notification is under test.
            effect.Initialize();
            BeatmapActionContainer.Redo(action.Guid);

            Assert.That(
                Quaternion.Angle(targetObject.transform.localRotation, Quaternion.Euler(0f, 90f, 0f)),
                Is.LessThan(0.001f),
                "The received redo restored the group in the map but not in the live GLS rotation cache.");
        }

        [Test]
        public void GlsManagerUpdateTimeAdvancesRotationEffects()
        {
            RegisterEffectWithSceneManager();
            var glsManager = Object.FindAnyObjectByType<GLSManager>();
            Assert.That(glsManager, Is.Not.Null);
            actionGroup = CreateGroup(0f, (0f, 15f), (1f, 90f));
            actionGroup.ID = DiagnosticGroupId;
            effect.InsertData(actionGroup);
            effect.UpdateTime(false, SongTime(0f));
            Assert.That(CurrentYRotation(), Is.EqualTo(15f).Within(0.001f));

            glsManager.UpdateTime(false, SongTime(1f));

            Assert.That(CurrentYRotation(), Is.EqualTo(90f).Within(0.001f));
        }

        // The fixture owns only its synthetic effect and target; the shared map and audio clock remain unchanged.
        protected override void BeforeCleanup()
        {
            if (actionGroup != null)
            {
                var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSRotation);
                if (collection.ContainsObject(actionGroup))
                {
                    collection.SilentRemoveObject(actionGroup);
                }
                actionGroup = null;
            }
            if (sceneRotationManager != null)
            {
                sceneRotationManager.IdToEffect.Remove(DiagnosticGroupId);
                sceneRotationManager = null;
            }
            Object.DestroyImmediate(effectObject);
        }

        private void RegisterEffectWithSceneManager()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.That(context.Descriptor, Is.Not.Null);
            sceneRotationManager = context.Descriptor.LightRotationGroupEffectManager;
            Assert.That(sceneRotationManager, Is.Not.Null);
            Assert.That(sceneRotationManager.IdToEffect.ContainsKey(DiagnosticGroupId), Is.False);
            sceneRotationManager.IdToEffect.Add(DiagnosticGroupId, effect);
        }

        private void ReplaceGroup(BaseLightRotationEventBoxGroup original, BaseLightRotationEventBoxGroup edited)
        {
            effect.RemoveData(original, original);
            effect.Refresh();
            effect.InsertData(edited);
            effect.Refresh();
        }

        private static BaseLightRotationEventBoxGroup CreateGroupWithRegularNodes(
            float groupBeat,
            int finalRelativeBeat,
            float initialRotation)
        {
            var nodes = new (float RelativeBeat, float Rotation)[finalRelativeBeat + 1];
            for (var node = 0; node <= finalRelativeBeat; node++)
            {
                nodes[node] = (node, initialRotation + (node * 5f));
            }

            return CreateGroup(groupBeat, nodes);
        }

        private static BaseLightRotationEventBoxGroup CreateGroup(
            float groupBeat,
            params (float RelativeBeat, float Rotation)[] nodes) =>
            CreateGroup(groupBeat, Axis.Y, nodes);

        private static BaseLightRotationEventBoxGroup CreateGroup(
            float groupBeat,
            Axis axis,
            params (float RelativeBeat, float Rotation)[] nodes)
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                ID = 0,
                JsonTime = groupBeat
            };
            var box = new BaseLightRotationEventBox
            {
                Axis = (int)axis,
                IndexFilter = new BaseIndexFilter()
            };
            var events = new BaseLightRotationBase[nodes.Length];
            for (var node = 0; node < nodes.Length; node++)
            {
                events[node] = new BaseLightRotationBase
                {
                    RelativeJsonTime = nodes[node].RelativeBeat,
                    Rotation = nodes[node].Rotation,
                    Direction = (int)LightRotationDirection.Automatic,
                Easing = (int)EaseType.Linear
                };
            }

            box.Events = events;
            group.Boxes.Add(box);
            group.NormalizeLoadedEventConflicts();
            group.RecomputeSongBpmTime();
            return group;
        }

        private static float SongTime(float jsonTime) =>
            (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);

        private float CurrentYRotation()
        {
            var angle = targetObject.transform.localEulerAngles.y;
            return angle < 180f
                ? angle
                : angle - 360f;
        }
    }
}
