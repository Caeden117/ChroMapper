using System.Reflection;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tests.Editor
{
    public class BeatSaberEasingParityTest : InputTestFixture
    {
        // These samples encode Beat Saber 1.44.1 Tweening.Easing so editor previews cannot drift from runtime curves.
        [TestCase(ElasticCurve.In, 0.25f)]
        [TestCase(ElasticCurve.In, 0.6f)]
        [TestCase(ElasticCurve.Out, 0.25f)]
        [TestCase(ElasticCurve.Out, 0.6f)]
        [TestCase(ElasticCurve.InOut, 0.25f)]
        [TestCase(ElasticCurve.InOut, 0.6f)]
        public void StandardElasticMatchesBeatSaber1441(ElasticCurve curve, float time)
        {
            var expected = EvaluateBeatSaberElastic(curve, time);
            var actual = EvaluateChroMapperElastic(curve, time);

            Assert.AreEqual(expected, actual, 0.000001f);
        }

        // CyclingAlternativeCurveVisitsBothRuntimeVariants proves CM can author every InOut alternative accepted by Beat Saber.
        [TestCase(EaseType.OutBack, EaseType.InOutBack)]
        [TestCase(EaseType.InOutBack, EaseType.BeatSaberInOutBack)]
        [TestCase(EaseType.BeatSaberInOutBack, EaseType.InBack)]
        [TestCase(EaseType.OutElastic, EaseType.InOutElastic)]
        [TestCase(EaseType.InOutElastic, EaseType.BeatSaberInOutElastic)]
        [TestCase(EaseType.BeatSaberInOutElastic, EaseType.InElastic)]
        [TestCase(EaseType.OutBounce, EaseType.InOutBounce)]
        [TestCase(EaseType.InOutBounce, EaseType.BeatSaberInOutBounce)]
        [TestCase(EaseType.BeatSaberInOutBounce, EaseType.InBounce)]
        public void CyclingAlternativeCurveVisitsBothRuntimeVariants(EaseType start, EaseType expected)
        {
            var selected = PerformEasingAction(
                start,
                (controller, action) => action.performed += controller.OnEasingCurve);

            Assert.AreEqual(expected, selected);
        }

        // CyclingAlternativeFamilyPreservesInOutVariant prevents either valid runtime curve from being silently converted.
        [TestCase(EaseType.InOutBounce, EaseType.InOutBack)]
        [TestCase(EaseType.InOutBack, EaseType.InOutElastic)]
        [TestCase(EaseType.InOutElastic, EaseType.InOutBounce)]
        [TestCase(EaseType.BeatSaberInOutBounce, EaseType.BeatSaberInOutBack)]
        [TestCase(EaseType.BeatSaberInOutBack, EaseType.BeatSaberInOutElastic)]
        [TestCase(EaseType.BeatSaberInOutElastic, EaseType.BeatSaberInOutBounce)]
        public void CyclingAlternativeFamilyPreservesInOutVariant(EaseType start, EaseType expected)
        {
            var selected = PerformEasingAction(
                start,
                (controller, action) => action.performed += controller.OnEasingAlternative);

            Assert.AreEqual(expected, selected);
        }

        // A dedicated virtual keyboard drives production callbacks while the fixture owns the isolated Input System runtime.
        private EaseType? PerformEasingAction(
            EaseType start,
            System.Action<BeatmapEasingsSelectionInputController, InputAction> bindAction)
        {
            var controllerObject = new GameObject("Beat Saber easing selection test controller");
            var controller = controllerObject.AddComponent<BeatmapEasingsSelectionInputController>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var cycleAction = new InputAction(
                "Cycle easing curve",
                InputActionType.Button,
                "<Keyboard>/e");
            EaseType? selected = null;
            try
            {
                controller.NotifyEasingChanged(start);
                controller.OnEasingChanged += value => selected = (EaseType)value;
                bindAction(controller, cycleAction);
                cycleAction.Enable();

                Press(keyboard.eKey, queueEventOnly: true);
                InputSystem.Update();

                return selected;
            }
            finally
            {
                // Dispose isolated actions before InputTestFixture restores the original Input System runtime.
                cycleAction.Disable();
                cycleAction.Dispose();
                Object.DestroyImmediate(controllerObject);
            }
        }

        // ExtendedGlsEasingMenuSupportsAllLeads exercises the real menu callbacks and restored state for every ChromaGLS-only family.
        [TestCase("easeSineToggle", EaseType.InSinusoidal)]
        [TestCase("easeCubicToggle", EaseType.InCubic)]
        [TestCase("easeQuarticToggle", EaseType.InQuartic)]
        [TestCase("easeQuinticToggle", EaseType.InQuintic)]
        [TestCase("easeExponentialToggle", EaseType.InExponential)]
        public void ExtendedGlsEasingMenuSupportsAllLeads(string fieldName, EaseType family)
        {
            Assert.IsNotNull(typeof(InputEasingViewController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic),
                "The extended easing family needs a serialized menu control.");
            var menu = new MenuFixture();
            try
            {
                var leadHandlers = new[] { "HandleCurveInInputChanged", "HandleCurveOutInputChanged", "HandleCurveInOutInputChanged" };
                for (var lead = 0; lead < leadHandlers.Length; lead++)
                {
                    var easing = family + lead;
                    menu.Controller.RestoreMenuState((int)easing, 0);
                    menu.View.ApplyEditorState((int)easing, 0);
                    Assert.IsTrue(menu.Toggles.GetFamily(fieldName).Value, $"Restored {easing} must select its family.");
                    Assert.AreEqual(lead == 0, menu.Toggles.CurveIn.Value);
                    Assert.AreEqual(lead == 1, menu.Toggles.CurveOut.Value);
                    Assert.AreEqual(lead == 2, menu.Toggles.CurveInOut.Value);

                    for (var targetLead = 0; targetLead < leadHandlers.Length; targetLead++)
                    {
                        InvokePrivateMethod(menu.View, leadHandlers[targetLead], true);
                        Assert.AreEqual((int)(family + targetLead), menu.Controller.CurrentEasing);
                    }

                    menu.Controller.RestoreMenuState((int)(EaseType.InQuadratic + lead), 0);
                    menu.View.ApplyEditorState(menu.Controller.CurrentEasing, 0);
                    Assert.IsFalse(menu.Toggles.GetFamily(fieldName).Value, "Selecting a native family clears the extended family.");
                    InvokePrivateMethod(menu.View, "HandleEaseInputChanged", family);
                    Assert.AreEqual((int)easing, menu.Controller.CurrentEasing, "Changing family preserves the selected lead.");
                }

                menu.Controller.RestoreMenuState((int)EaseType.BeatSaberInOutBack, 0);
                menu.View.ApplyEditorState(menu.Controller.CurrentEasing, 0);
                InvokePrivateMethod(menu.View, "HandleEaseInputChanged", family);
                Assert.AreEqual((int)(family + 2), menu.Controller.CurrentEasing,
                    "Families without a Beat Saber-specific variant use their ordinary InOut curve.");
            }
            finally
            {
                menu.Dispose();
            }
        }

        // EasingMenuDisplaysEitherInOutVariant keeps imported standard and Beat Saber curves visible without rewriting them.
        [TestCase(EaseType.InOutBack, "easeBackToggle")]
        [TestCase(EaseType.InOutElastic, "easeElasticToggle")]
        [TestCase(EaseType.InOutBounce, "easeBounceToggle")]
        [TestCase(EaseType.BeatSaberInOutBack, "easeBackToggle")]
        [TestCase(EaseType.BeatSaberInOutElastic, "easeElasticToggle")]
        [TestCase(EaseType.BeatSaberInOutBounce, "easeBounceToggle")]
        public void EasingMenuDisplaysEitherInOutVariant(EaseType easing, string familyField)
        {
            var menu = new MenuFixture();
            try
            {
                menu.Controller.RestoreMenuState((int)easing, 0);
                menu.View.ApplyEditorState((int)easing, 0);

                Assert.IsTrue(menu.Toggles.CurveInOut.Value);
                Assert.IsFalse(menu.Toggles.CurveIn.Value);
                Assert.IsFalse(menu.Toggles.CurveOut.Value);
                Assert.IsTrue(menu.Toggles.GetFamily(familyField).Value);
                Assert.AreEqual((int)easing, menu.Controller.CurrentEasing);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // SelectingInOutLeadEmitsStandardValue keeps the visible three-state menu faithful to its standard InOut label.
        [TestCase("easeBackToggle", EaseType.InOutBack)]
        [TestCase("easeElasticToggle", EaseType.InOutElastic)]
        [TestCase("easeBounceToggle", EaseType.InOutBounce)]
        public void SelectingInOutLeadEmitsStandardValue(string familyField, EaseType expected)
        {
            var menu = new MenuFixture();
            EaseType? selected = null;
            try
            {
                menu.Toggles.GetFamily(familyField).SetValueWithoutNotify(true);
                menu.Controller.OnEasingChanged += value => selected = (EaseType)value;

                InvokePrivateMethod(menu.View, "HandleCurveInOutInputChanged", false);

                Assert.AreEqual(expected, selected);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // SelectingAlternativeFamilyWithInOutLeadEmitsStandardValue guards the second menu path against OE coercion.
        [TestCase("HandleEaseBackInputChanged", EaseType.InOutBack)]
        [TestCase("HandleEaseElasticInputChanged", EaseType.InOutElastic)]
        [TestCase("HandleEaseBounceInputChanged", EaseType.InOutBounce)]
        public void SelectingAlternativeFamilyWithInOutLeadEmitsStandardValue(string handler, EaseType expected)
        {
            var menu = new MenuFixture();
            EaseType? selected = null;
            try
            {
                menu.Toggles.CurveInOut.SetValueWithoutNotify(true);
                menu.Controller.OnEasingChanged += value => selected = (EaseType)value;

                InvokePrivateMethod(menu.View, handler, false);

                Assert.AreEqual(expected, selected);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // SelectingAlternativeFamilyPreservesBeatSaberInOutValue keeps imported custom curves intact during family edits.
        [TestCase(EaseType.BeatSaberInOutBounce, "HandleEaseBackInputChanged", EaseType.BeatSaberInOutBack)]
        [TestCase(EaseType.BeatSaberInOutBack, "HandleEaseElasticInputChanged", EaseType.BeatSaberInOutElastic)]
        [TestCase(EaseType.BeatSaberInOutElastic, "HandleEaseBounceInputChanged", EaseType.BeatSaberInOutBounce)]
        public void SelectingAlternativeFamilyPreservesBeatSaberInOutValue(
            EaseType start,
            string handler,
            EaseType expected)
        {
            var menu = new MenuFixture();
            EaseType? selected = null;
            try
            {
                menu.Controller.RestoreMenuState((int)start, 0);
                menu.Toggles.CurveInOut.SetValueWithoutNotify(true);
                menu.Controller.OnEasingChanged += value => selected = (EaseType)value;

                InvokePrivateMethod(menu.View, handler, false);

                Assert.AreEqual(expected, selected);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // ChroMapper's public easing delegates are the production preview path exercised by every affected GLS type.
        private static float EvaluateChroMapperElastic(ElasticCurve curve, float time)
        {
            return curve switch
            {
                ElasticCurve.In => Easing.Elastic.In(time),
                ElasticCurve.Out => Easing.Elastic.Out(time),
                _ => Easing.Elastic.InOut(time)
            };
        }

        // This is a direct transcription of Beat Saber 1.44.1 Tweening.Easing's three standard Elastic equations.
        private static float EvaluateBeatSaberElastic(ElasticCurve curve, float time)
        {
            if (time == 0f || time == 1f)
            {
                return time;
            }

            return curve switch
            {
                ElasticCurve.In => -Mathf.Pow(2f, (10f * time) - 10f)
                    * Mathf.Sin(((10f * time) - 10.75f) * (Mathf.PI * 2f / 3f)),
                ElasticCurve.Out => (Mathf.Pow(2f, -10f * time)
                    * Mathf.Sin(((10f * time) - 0.75f) * (Mathf.PI * 2f / 3f))) + 1f,
                _ when time < 0.5f => -(Mathf.Pow(2f, (20f * time) - 10f)
                    * Mathf.Sin(((20f * time) - 11.125f) * (Mathf.PI * 4f / 9f))) / 2f,
                _ => (Mathf.Pow(2f, (-20f * time) + 10f)
                    * Mathf.Sin(((20f * time) - 11.125f) * (Mathf.PI * 4f / 9f)) / 2f) + 1f
            };
        }

        // Reflection sets only serialized test dependencies; production state still flows through ApplyEditorState and ToggleComponent.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Could not find test field {fieldName}.");
            field.SetValue(target, value);
        }

        // Invoke the real private toggle callbacks so the regression covers serialized UI behavior instead of duplicating it.
        private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Could not find test method {methodName}.");
            method.Invoke(target, arguments);
        }

        public enum ElasticCurve
        {
            In,
            Out,
            InOut
        }

        // A reusable menu fixture ensures every UI regression receives the same complete serialized dependency graph.
        private sealed class MenuFixture
        {
            private readonly GameObject root;

            public MenuFixture()
            {
                root = new GameObject("Beat Saber easing menu test");
                Controller = root.AddComponent<BeatmapEasingsSelectionInputController>();
                View = root.AddComponent<InputEasingViewController>();
                Toggles = new ToggleFixture(root.transform);
                SetPrivateField(View, "inputController", Controller);
                Toggles.AssignTo(View);
            }

            public BeatmapEasingsSelectionInputController Controller { get; }
            public InputEasingViewController View { get; }
            public ToggleFixture Toggles { get; }

            // Destroy the complete hierarchy together so ToggleComponent and view lifecycle callbacks retain production ordering.
            public void Dispose()
            {
                Object.DestroyImmediate(root);
            }
        }

        // A complete lightweight toggle set lets the real menu method run without loading the mapper scene or its unrelated services.
        private sealed class ToggleFixture
        {
            private readonly ToggleComponent easeNone;
            private readonly ToggleComponent easeLinear;
            private readonly ToggleComponent easeQuadratic;
            private readonly ToggleComponent easeCircular;
            private readonly ToggleComponent easeBounce;
            private readonly ToggleComponent easeBack;
            private readonly ToggleComponent easeElastic;
            // ExtendedGlsEasingMenuSupportsAllLeads supplies the same five serialized dependencies as the mapper scene.
            private readonly ToggleComponent easeSine;
            private readonly ToggleComponent easeCubic;
            private readonly ToggleComponent easeQuartic;
            private readonly ToggleComponent easeQuintic;
            private readonly ToggleComponent easeExponential;

            public ToggleFixture(Transform parent)
            {
                Extension = CreateToggle(parent, "Extension");
                CurveIn = CreateToggle(parent, "In");
                CurveOut = CreateToggle(parent, "Out");
                CurveInOut = CreateToggle(parent, "InOut");
                easeNone = CreateToggle(parent, "None");
                easeLinear = CreateToggle(parent, "Linear");
                easeQuadratic = CreateToggle(parent, "Quadratic");
                easeCircular = CreateToggle(parent, "Circular");
                easeBounce = CreateToggle(parent, "Bounce");
                easeBack = CreateToggle(parent, "Back");
                easeElastic = CreateToggle(parent, "Elastic");
                // ExtendedGlsEasingMenuSupportsAllLeads exercises real toggles instead of substituting the selected easing value.
                easeSine = CreateToggle(parent, "Sine");
                easeCubic = CreateToggle(parent, "Cubic");
                easeQuartic = CreateToggle(parent, "Quartic");
                easeQuintic = CreateToggle(parent, "Quintic");
                easeExponential = CreateToggle(parent, "Exponential");
            }

            public ToggleComponent Extension { get; }
            public ToggleComponent CurveIn { get; }
            public ToggleComponent CurveOut { get; }
            public ToggleComponent CurveInOut { get; }

            // Assign every serialized toggle because ApplyEditorState clears the entire real view before selecting the requested value.
            public void AssignTo(InputEasingViewController view)
            {
                SetPrivateField(view, "extensionToggle", Extension);
                SetPrivateField(view, "curveInToggle", CurveIn);
                SetPrivateField(view, "curveOutToggle", CurveOut);
                SetPrivateField(view, "curveInOutToggle", CurveInOut);
                SetPrivateField(view, "easeNoneToggle", easeNone);
                SetPrivateField(view, "easeLinearToggle", easeLinear);
                SetPrivateField(view, "easeQuadToggle", easeQuadratic);
                SetPrivateField(view, "easeCircularToggle", easeCircular);
                SetPrivateField(view, "easeBounceToggle", easeBounce);
                SetPrivateField(view, "easeBackToggle", easeBack);
                SetPrivateField(view, "easeElasticToggle", easeElastic);
                // ExtendedGlsEasingMenuSupportsAllLeads requires full dependency wiring before menu callbacks can inspect extended families.
                SetPrivateField(view, "easeSineToggle", easeSine);
                SetPrivateField(view, "easeCubicToggle", easeCubic);
                SetPrivateField(view, "easeQuarticToggle", easeQuartic);
                SetPrivateField(view, "easeQuinticToggle", easeQuintic);
                SetPrivateField(view, "easeExponentialToggle", easeExponential);
            }

            // Tests select the family by serialized field name to keep each TestCase compact and readable.
            public ToggleComponent GetFamily(string fieldName)
            {
                // ExtendedGlsEasingMenuSupportsAllLeads observes each dedicated toggle after restore and native-family switches.
                return fieldName switch
                {
                    "easeBackToggle" => easeBack,
                    "easeElasticToggle" => easeElastic,
                    "easeSineToggle" => easeSine,
                    "easeCubicToggle" => easeCubic,
                    "easeQuarticToggle" => easeQuartic,
                    "easeQuinticToggle" => easeQuintic,
                    "easeExponentialToggle" => easeExponential,
                    _ => easeBounce
                };
            }

            // Each ToggleComponent receives the sole dependency its value-update hot path requires.
            private static ToggleComponent CreateToggle(Transform parent, string name)
            {
                var toggleObject = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(ToggleComponent));
                toggleObject.transform.SetParent(parent, false);
                var toggleComponent = toggleObject.GetComponent<ToggleComponent>();
                SetPrivateField(toggleComponent, "toggle", toggleObject.GetComponent<Toggle>());
                return toggleComponent;
            }
        }
    }
}
