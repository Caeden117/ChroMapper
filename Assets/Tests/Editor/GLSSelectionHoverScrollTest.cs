using System;
using Beatmap.Base;
using Beatmap.Containers;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GLSSelectionHoverScrollTest : InputTestBase
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void OuterControllerRequiresExplicitGroupSelection(bool previewNode, bool selectedGroup)
        {
            var group = CreateGroup(0, 20);
            OpenGroup(group);
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.GLS;
            if (selectedGroup)
                SelectionController.Select(group, true, false, false);

            var controllerObject = new GameObject("Outer selection scroll controller");
            var containerObject = new GameObject("Outer selection scroll target");
            var previousPreview = Settings.Instance.EnableGLSGhostPreview;
            try
            {
                Settings.Instance.EnableGLSGhostPreview = true;
                var container = containerObject.AddComponent<GLSGroupContainer>();
                foreach (var initialized in Object.FindObjectsByType<ObjectContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
                {
                    if (initialized.VisualSettings != null)
                    {
                        container.VisualSettings = initialized.VisualSettings;
                        break;
                    }
                }

                var controller = controllerObject.AddComponent<OuterColorController>();
                controller.Target = container;
                controller.IsHovering = true;
                controller.HoveredObject = container;
                var nodeIndex = previewNode
                    ? 2
                    : 0;
                for (var tick = 0; tick < 2; tick++)
                {
                    container.EventBoxGroupData = GetOpenGroup();
                    container.PreviewEventData = GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[nodeIndex];
                    Scroll(controller.OnTweakBrightnessHover);
                    Assert.AreEqual(selectedGroup, SelectionController.IsObjectSelected(GetOpenGroup()));
                }

                for (var i = 0; i < 3; i++)
                {
                    var changed = selectedGroup || i == nodeIndex;
                    var delta = changed
                        ? 2 * Delta(0)
                        : 0;
                    Assert.That(
                        Value(GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[i]),
                        Is.EqualTo(i + 1 + delta).Within(0.001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
                Settings.Instance.EnableGLSGhostPreview = previousPreview;
            }
        }

        [Test]
        public void SelectedAxisMovesRetainNodesWhenDestinationsOverlap()
        {
            var group = (BaseLightRotationEventBoxGroup)CreateGroup(1, 20);
            group.Boxes.Add(new BaseLightRotationEventBox
            {
                Axis = 1,
                Events = new[] { new BaseLightRotationBase { Rotation = 20 } }
            });
            GLSCommonCommand.RebindGroup(group);
            group.RecomputeSongBpmTime();
            OpenGroup(group);
            var first = group.Boxes[0].Events[0];
            var second = group.Boxes[1].Events[0];
            SelectionController.Select(first, true, false, false);
            SelectionController.Select(second, true, false, false);
            Scroll(context => GLSCommonCommand.CycleEventAxis(context, first));
            var count = 0;
            foreach (var box in GetOpenGroup().ReadOnlyBoxes)
                count += box.ReadOnlyEvents.Count;

            Assert.AreEqual(4, count);
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count);
            var axes = new System.Collections.Generic.HashSet<int>();
            foreach (var selected in SelectionController.SelectedObjects)
                axes.Add(((BaseLightTransformEventBox)((BaseGLSEvent)selected).EventBoxData).Axis);

            Assert.AreEqual(2, axes.Count);
        }

        [Test]
        public void SelectedRotationNodesCycleAxesTogether()
        {
            var group = CreateGroup(1, 20);
            OpenGroup(group);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0], true, false, false);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[2], true, false, false);
            Scroll(context => GLSCommonCommand.CycleEventAxis(context, group.ReadOnlyBoxes[0].ReadOnlyEvents[0]));
            var targetAxis = Settings.Instance.InvertScrollEventValue
                ? 2
                : 1;
            var movedCount = 0;
            foreach (var box in GetOpenGroup().ReadOnlyBoxes)
            {
                if (((BaseLightTransformEventBox)box).Axis == targetAxis)
                    movedCount += box.ReadOnlyEvents.Count;
            }

            Assert.AreEqual(2, movedCount);
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count);
            foreach (var selected in SelectionController.SelectedObjects)
                Assert.AreEqual(targetAxis, ((BaseLightTransformEventBox)((BaseGLSEvent)selected).EventBoxData).Axis);
        }

        [Test]
        public void MultipleSelectedGroupsChangeInOneUndoStep()
        {
            var first = CreateGroup(0, 20);
            var second = CreateGroup(0, 30);
            OpenGroup(first);
            SelectionController.Select(first, true, false, false);
            SelectionController.Select(second, true, false, false);
            BeatmapAction action = null;
            var actionCount = 0;
            void Record(BeatmapAction created)
            {
                action = created;
                actionCount++;
            }

            BeatmapActionContainer.OnActionCreated += Record;
            try
            {
                Scroll(context => Adjust(context, first.ReadOnlyBoxes[0].ReadOnlyEvents[0]));
            }
            finally
            {
                BeatmapActionContainer.OnActionCreated -= Record;
            }

            Assert.AreEqual(1, actionCount);
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count);
            foreach (var selected in SelectionController.SelectedObjects)
                Assert.That(
                    Value(((BaseEventBoxGroup)selected).ReadOnlyBoxes[0].ReadOnlyEvents[2]),
                    Is.EqualTo(3 + Delta(0)).Within(0.001f));

            BeatmapActionContainer.Undo(action.Guid);
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count);
            foreach (var selected in SelectionController.SelectedObjects)
                Assert.AreEqual(3, Value(((BaseEventBoxGroup)selected).ReadOnlyBoxes[0].ReadOnlyEvents[2]));

            BeatmapActionContainer.Redo(action.Guid);
            foreach (var selected in SelectionController.SelectedObjects)
                Assert.That(
                    Value(((BaseEventBoxGroup)selected).ReadOnlyBoxes[0].ReadOnlyEvents[2]),
                    Is.EqualTo(3 + Delta(0)).Within(0.001f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void ColorModifiersApplyToSelectedNodes(int property)
        {
            var group = CreateGroup(0, 20);
            OpenGroup(group);
            var first = (BaseLightColorBase)group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            var last = (BaseLightColorBase)group.ReadOnlyBoxes[0].ReadOnlyEvents[2];
            first.Frequency = last.Frequency = 3;
            first.StrobeBrightness = last.StrobeBrightness = 0.5f;
            first.ChromaStrobePulseWidth = last.ChromaStrobePulseWidth = 0.5f;
            first.WriteCustom();
            last.WriteCustom();
            SelectionController.Select(first, true, false, false);
            SelectionController.Select(last, true, false, false);
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            Scroll(
                context =>
                {
                    switch (property)
                    {
                        case 0:
                            GLSEventHoverMutation.AdjustColorFrequency(context, first, precision);
                            break;
                        case 1:
                            GLSEventHoverMutation.AdjustColorStrobeBrightness(context, first, precision);
                            break;
                        case 2:
                            GLSEventHoverMutation.AdjustColorStrobePulseWidth(context, first, precision);
                            break;
                        case 3:
                            GLSEventHoverMutation.CycleColorStrobeFade(context, first);
                            break;
                        case 4:
                            GLSEventHoverMutation.AdjustColorEasing(context, first);
                            break;
                        case 5:
                            GLSEventHoverMutation.AdjustStrobeColorEasing(context, first);
                            break;
                        default:
                            GLSEventHoverMutation.CycleColorLerpType(context, first);
                            break;
                    }
                });
            foreach (var evt in new[]
                {
                    (BaseLightColorBase)GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[0],
                    (BaseLightColorBase)GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[2]
                })
            {
                switch (property)
                {
                    case 0:
                        Assert.AreNotEqual(3, evt.Frequency);
                        break;
                    case 1:
                        Assert.AreNotEqual(0.5f, evt.StrobeBrightness);
                        break;
                    case 2:
                        Assert.AreNotEqual(0.5f, evt.ChromaStrobePulseWidth);
                        break;
                    case 3:
                        Assert.AreEqual(1, evt.StrobeFade);
                        break;
                    case 4:
                        Assert.AreNotEqual(0, evt.ChromaColorEasing ?? evt.Easing);
                        break;
                    case 5:
                        Assert.NotNull(evt.ChromaStrobeColorEasing);
                        break;
                    default:
                        Assert.AreEqual(Beatmap.Shared.BasicEventColorLerpType.TrueHSV, evt.CustomLerpType);
                        break;
                }
            }

            Assert.AreEqual(2, Value(GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[1]));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void HoveringSelectedNodeAdjustsOnlySelectedNodes(int type)
        {
            var group = CreateGroup(type, 20);
            OpenGroup(group);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0], true, false, false);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[2], true, false, false);
            for (var tick = 0; tick < 2; tick++)
            {
                var target = GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[0];
                Scroll(context => Adjust(context, target));
            }

            var edited = GetOpenGroup();
            Assert.That(Value(edited.ReadOnlyBoxes[0].ReadOnlyEvents[0]), Is.EqualTo(1 + (2 * Delta(type))).Within(0.001f));
            Assert.AreEqual(2, Value(edited.ReadOnlyBoxes[0].ReadOnlyEvents[1]));
            Assert.That(Value(edited.ReadOnlyBoxes[0].ReadOnlyEvents[2]), Is.EqualTo(3 + (2 * Delta(type))).Within(0.001f));
            Assert.IsFalse(SelectionController.IsObjectSelected(edited));
            Assert.IsTrue(SelectionController.IsObjectSelected(edited.ReadOnlyBoxes[0].ReadOnlyEvents[0]));
            Assert.IsTrue(SelectionController.IsObjectSelected(edited.ReadOnlyBoxes[0].ReadOnlyEvents[2]));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitlySelectedOuterGroupAdjustsEveryNode(bool previewNode)
        {
            var group = CreateGroup(0, 20);
            OpenGroup(group);
            SelectionController.Select(group, true, false, false);
            var nodeIndex = previewNode
                ? 2
                : 0;
            Scroll(context => Adjust(context, group.ReadOnlyBoxes[0].ReadOnlyEvents[nodeIndex]));
            var edited = GetOpenGroup();
            for (var i = 0; i < 3; i++)
                Assert.That(Value(edited.ReadOnlyBoxes[0].ReadOnlyEvents[i]), Is.EqualTo(i + 1 + Delta(0)).Within(0.001f));

            Assert.IsTrue(SelectionController.IsObjectSelected(edited));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnselectedOuterNodeStaysSingleAcrossRepeatedScrolls(bool previewNode)
        {
            var group = CreateGroup(0, 20);
            OpenGroup(group);
            var nodeIndex = previewNode
                ? 2
                : 0;
            for (var tick = 0; tick < 2; tick++)
            {
                var target = GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[nodeIndex];
                Scroll(context => Adjust(context, target));
                Assert.IsEmpty(SelectionController.SelectedObjects, "Scrolling must not implicitly select the outer group.");
            }

            var edited = GetOpenGroup();
            for (var i = 0; i < 3; i++)
            {
                var delta = i == nodeIndex
                    ? 2 * Delta(0)
                    : 0;
                Assert.That(
                    Value(edited.ReadOnlyBoxes[0].ReadOnlyEvents[i]),
                    Is.EqualTo(i + 1 + delta).Within(0.001f));
            }
        }

        [Test]
        public void HoveringUnselectedNodeDoesNotEditTheSelection()
        {
            var group = CreateGroup(0, 20);
            OpenGroup(group);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[2], true, false, false);
            Scroll(context => Adjust(context, group.ReadOnlyBoxes[0].ReadOnlyEvents[0]));
            Assert.That(Value(GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[0]), Is.EqualTo(1 + Delta(0)).Within(0.001f));
            Assert.AreEqual(3, Value(GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[2]));
            Assert.IsFalse(SelectionController.IsObjectSelected(GetOpenGroup()));
        }

        [Test]
        public void HoveringUnselectedOuterGroupDoesNotChangeAnotherSelectedGroup()
        {
            var selectedGroup = CreateGroup(0, 20);
            var hoveredGroup = CreateGroup(0, 30);
            OpenGroup(hoveredGroup);
            SelectionController.Select(selectedGroup, true, false, false);
            for (var tick = 0; tick < 2; tick++)
            {
                var target = GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[2];
                Scroll(context => Adjust(context, target));
                Assert.IsFalse(SelectionController.IsObjectSelected(GetOpenGroup()));
                Assert.IsTrue(SelectionController.IsObjectSelected(selectedGroup));
                Assert.AreEqual(1, SelectionController.SelectedObjects.Count);
            }

            Assert.AreEqual(3, Value(selectedGroup.ReadOnlyBoxes[0].ReadOnlyEvents[2]));
            Assert.AreEqual(1, Value(GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[0]));
            Assert.That(
                Value(GetOpenGroup().ReadOnlyBoxes[0].ReadOnlyEvents[2]),
                Is.EqualTo(3 + (2 * Delta(0))).Within(0.001f));
        }

        private static BaseEventBoxGroup GetOpenGroup() => Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext;

        private static BaseEventBoxGroup CreateGroup(int type, float beat)
        {
            BaseEventBoxGroup group = type switch
            {
                0 => new BaseLightColorEventBoxGroup { Boxes = { new BaseLightColorEventBox() } },
                1 => new BaseLightRotationEventBoxGroup { Boxes = { new BaseLightRotationEventBox() } },
                2 => new BaseLightTranslationEventBoxGroup { Boxes = { new BaseLightTranslationEventBox() } },
                _ => new BaseVfxEventEventBoxGroup { Boxes = { new BaseVfxEventEventBox() } }
            };
            var events = new BaseGLSEvent[3];
            for (var i = 0; i < events.Length; i++)
            {
                events[i] = type switch
                {
                    0 => new BaseLightColorBase { Brightness = i + 1 },
                    1 => new BaseLightRotationBase { Rotation = i + 1 },
                    2 => new BaseLightTranslationBase { Translation = i + 1 },
                    _ => new BaseFxEventFloat { Value = i + 1 }
                };
                events[i].RelativeJsonTime = i == 2
                    ? 100
                    : i;
            }

            group.ReadOnlyBoxes[0].SetEvents(events);
            group.JsonTime = beat;
            group.ID = 1;
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            GLSCommonCommand.RebindGroup(group);
            group.RecomputeSongBpmTime();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            return group;
        }

        private static void OpenGroup(BaseEventBoxGroup group)
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.EventBox;
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
        }

        private static float Value(BaseGLSEvent evt) => evt switch
        {
            BaseLightColorBase color => color.Brightness,
            BaseLightRotationBase rotation => rotation.Rotation,
            BaseLightTranslationBase translation => translation.Translation,
            BaseFxEventFloat fx => fx.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(evt))
        };

        private static float Delta(int type)
        {
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            var value = type switch
            {
                0 => precision.GetCurrentBrightnessPrecision() / 100f,
                1 => precision.GetCurrentRotationPrecision(),
                2 => precision.GetCurrentTranslationPrecision() / 100f,
                _ => precision.GetCurrentFloatFXPrecision() / 100f
            };
            return Settings.Instance.InvertScrollEventValue
                ? -value
                : value;
        }

        private static void Adjust(InputAction.CallbackContext context, BaseGLSEvent evt)
        {
            var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
            switch (evt)
            {
                case BaseLightColorBase color:
                    GLSEventHoverMutation.AdjustColorBrightness(context, color, precision);
                    break;
                case BaseLightRotationBase rotation:
                    GLSEventHoverMutation.AdjustRotation(context, rotation, precision);
                    break;
                case BaseLightTranslationBase translation:
                    GLSEventHoverMutation.AdjustTranslation(context, translation, precision);
                    break;
                case BaseFxEventFloat fx:
                    GLSEventHoverMutation.AdjustFloatFx(context, fx, precision);
                    break;
            }
        }

        private void Scroll(Action<InputAction.CallbackContext> callback)
        {
            InitializeVirtualInput(includeKeyboard: false);
            try
            {
                using (var action = new InputAction("Selection scroll", InputActionType.Value, "<Mouse>/scroll/y"))
                {
                    action.performed += callback;
                    action.Enable();
                    SetVirtualMouseState(Vector2.zero, Vector2.up);
                }
            }
            finally
            {
                TearDownVirtualInput();
            }
        }

        private class OuterColorController : BeatmapGLSGroupColorInputController
        {
            public GLSGroupContainer Target;

            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = Target;
                BeatmapRaycastCache.FirstHit = Target.gameObject;
                BeatmapRaycastCache.HasHit = true;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return true;
            }
        }
    }
}
