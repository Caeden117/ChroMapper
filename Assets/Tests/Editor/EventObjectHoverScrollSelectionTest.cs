using System;
using System.Reflection;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class EventObjectHoverScrollSelectionTest : InputTestBase
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void RingRotationModifierScrollChangesAllSelectedRotationLanes(int property)
        {
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.BasicEvent;
            var first = PlaceUtils.Place(new BaseEvent { JsonTime = 2, Type = 0 });
            var second = PlaceUtils.Place(new BaseEvent { JsonTime = 100, Type = 1 });
            var zoom = PlaceUtils.Place(new BaseEvent { JsonTime = 3, Type = 9 });
            foreach (var evt in new[] { first, second })
            {
                evt.CustomRingRotation = 90;
                evt.CustomSpeed = 1;
                evt.CustomProp = 1;
                evt.CustomStep = 5;
                evt.WriteCustom();
            }

            var definitions = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            definitions.Basic = new Dictionary<int, TrackDefinitionBasic>
            {
                [0] = new() { Type = 0, Components = BasicEventComponent.RingRotation },
                [1] = new() { Type = 1, Components = BasicEventComponent.RingRotation },
                [9] = new() { Type = 9, Components = BasicEventComponent.RingZoom }
            };
            var container = (EventContainer)BeatmapObjectContainerCollection
                .GetCollectionForType(ObjectType.Event).LoadedContainers[first];
            var previousDefinitions = container.TrackDefinitions;
            var previousController = BeatmapEventInputController.ActiveInstance;
            var controllerObject = new GameObject("Basic Event modifier selection test");
            try
            {
                var controller = controllerObject.AddComponent<EventController>();
                controller.Configure(previousController);
                controller.Target = container;
                container.TrackDefinitions = definitions;
                Select(first, second, zoom);
                Action<InputAction.CallbackContext> callback = property switch
                {
                    0 => controller.OnTweakEventMain,
                    1 => controller.OnTweakEventCtrlAlt,
                    2 => controller.OnTweakEventCtrlShift,
                    _ => controller.OnTweakEventCtrlShiftAlt
                };
                Scroll(callback);
                var replacement = GetSelected<BaseEvent>(100);
                switch (property)
                {
                    case 0:
                        Assert.AreNotEqual(90, replacement.CustomRingRotation);
                        break;
                    case 1:
                        Assert.AreNotEqual(1, replacement.CustomSpeed);
                        break;
                    case 2:
                        Assert.AreNotEqual(1, replacement.CustomProp);
                        break;
                    default:
                        Assert.AreNotEqual(5, replacement.CustomStep);
                        break;
                }

                Assert.IsNull(zoom.CustomRingRotation);
                Assert.IsNull(zoom.CustomSpeed);
                Assert.IsNull(zoom.CustomProp);
                Assert.IsNull(zoom.CustomStep);
                Assert.IsTrue(SelectionController.IsObjectSelected(zoom));
            }
            finally
            {
                container.TrackDefinitions = previousDefinitions;
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(definitions);
                if (previousController != null && previousController.enabled)
                {
                    previousController.enabled = false;
                    previousController.enabled = true;
                }
            }
        }

        [Test]
        public void BpmScrollChangesOnlySelectedBpmEvents()
        {
            var first = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 2, Bpm = 100 });
            var second = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 100, Bpm = 200 });
            var njs = PlaceUtils.Place(new BaseNJSEvent { JsonTime = 3, RelativeNJS = 5 });
            Select(first, second, njs);
            var controllerObject = new GameObject("BPM selection scroll test");
            try
            {
                var controller = controllerObject.AddComponent<BpmController>();
                controller.Target = (BpmEventContainer)BeatmapObjectContainerCollection
                    .GetCollectionForType(ObjectType.BpmChange).LoadedContainers[first];
                Scroll(controller.OnTweakBPMValue);
                Assert.AreEqual(201, GetSelected<BaseBpmEvent>(100).Bpm);
                Assert.AreEqual(5, njs.RelativeNJS);
                Assert.IsTrue(SelectionController.IsObjectSelected(njs));
                PlaceUtils.Undo();
                Assert.IsTrue(SelectionController.IsObjectSelected(first));
                Assert.IsTrue(SelectionController.IsObjectSelected(second));
                Assert.AreEqual(100, first.Bpm);
                Assert.AreEqual(200, second.Bpm);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NjsScrollChangesOnlySelectedNjsEvents(bool easing)
        {
            var first = PlaceUtils.Place(new BaseNJSEvent { JsonTime = 2, RelativeNJS = 5, Easing = 0 });
            var second = PlaceUtils.Place(new BaseNJSEvent { JsonTime = 100, RelativeNJS = 6, Easing = 1 });
            var bpm = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 3, Bpm = 100 });
            Select(first, second, bpm);
            var previousController = Object.FindAnyObjectByType<BeatmapNJSEventInputController>();
            var controllerObject = new GameObject("NJS selection scroll test");
            try
            {
                var controller = controllerObject.AddComponent<NjsController>();
                controller.Target = (NJSEventContainer)BeatmapObjectContainerCollection
                    .GetCollectionForType(ObjectType.NJSEvent).LoadedContainers[first];
                var precision = Object.FindAnyObjectByType<ScrollPrecisionController>();
                typeof(BeatmapNJSEventInputController)
                    .GetField("scrollPrecisionController", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(controller, precision);
                if (easing)
                {
                    Scroll(controller.OnTweakVNJSEasing);
                    Assert.AreNotEqual(1, GetSelected<BaseNJSEvent>(100).Easing);
                }
                else
                {
                    Scroll(controller.OnTweakNJSValue);
                    var sign = Settings.Instance.InvertScrollEventValue
                        ? -1
                        : 1;
                    Assert.That(
                        GetSelected<BaseNJSEvent>(100).RelativeNJS,
                        Is.EqualTo(6 + (sign * precision.GetCurrentTimePrecision())).Within(0.001f));
                }

                Assert.AreEqual(100, bpm.Bpm);
                Assert.IsTrue(SelectionController.IsObjectSelected(bpm));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                if (previousController != null && previousController.enabled)
                {
                    previousController.enabled = false;
                    previousController.enabled = true;
                }
            }
        }

        private static void Select(params BaseObject[] objects)
        {
            foreach (var obj in objects)
                SelectionController.Select(obj, true, false, false);
        }

        private static T GetSelected<T>(float beat) where T : BaseObject
        {
            foreach (var selected in SelectionController.SelectedObjects)
            {
                if (selected is T result && result.JsonTime == beat)
                    return result;
            }

            Assert.Fail($"No selected {typeof(T).Name} at beat {beat}.");
            return null;
        }

        private void Scroll(Action<InputAction.CallbackContext> callback)
        {
            InitializeVirtualInput(includeKeyboard: false);
            try
            {
                using (var action = new InputAction("Event selection scroll", InputActionType.Value, "<Mouse>/scroll/y"))
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

        private class BpmController : BeatmapBPMChangeInputController
        {
            public BpmEventContainer Target;

            protected override bool RaycastFirstObject(out BpmEventContainer firstObject)
            {
                firstObject = Target;
                return true;
            }
        }

        private class EventController : BeatmapEventInputController
        {
            public EventContainer Target;

            public void Configure(BeatmapEventInputController original)
            {
                CustomStandaloneInputModule = Object.FindAnyObjectByType<CustomStandaloneInputModule>();
                foreach (var name in new[] { "eventAppearance", "beatmapRuntimeContext", "scrollPrecisionController", "selectionController" })
                {
                    var field = typeof(BeatmapEventInputController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                    field.SetValue(this, field.GetValue(original));
                }
            }

            protected override bool RaycastFirstObject(out EventContainer firstObject)
            {
                firstObject = Target;
                BeatmapRaycastCache.FirstHit = Target.gameObject;
                BeatmapRaycastCache.HasHit = true;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return true;
            }
        }

        private class NjsController : BeatmapNJSEventInputController
        {
            public NJSEventContainer Target;

            protected override bool RaycastFirstObject(out NJSEventContainer firstObject)
            {
                firstObject = Target;
                return true;
            }
        }
    }
}
