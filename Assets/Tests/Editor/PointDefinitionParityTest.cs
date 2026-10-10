using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    // PointDefinitionParityTest pins CM's point-definition evaluation to Heck's documented semantics
    // (https://heck.aeroluna.dev/animation/tracks-and-points/): modifiers (opAdd/opSub/opMul/opDiv, nested and
    // chained), base properties, swizzling, and smoothing must evaluate through the production PointDefinition
    // pipeline exactly like Heck's Modifier/Values/BaseProviderManager stack instead of being silently dropped.
    public class PointDefinitionParityTest
    {
        // FloatModifierChainMatchesHeckDocs evaluates the docs' arithmetic example as a point row:
        // [1.5, [2, "opMul", [1, "opSub"]], [4, "opAdd"]] // 5.5
        [Test]
        public void FloatModifierChainMatchesHeckDocs()
        {
            var definition = CreateFloatDefinition("[1.5, [2, \"opMul\", [1, \"opSub\"]], [4, \"opAdd\"]]");
            Assert.That(definition.Interpolate(0f), Is.EqualTo(5.5f).Within(0.0001f),
                "The chained modifier [2 opMul [1 opSub]] then [4 opAdd] must evaluate 1.5*2-1+4 = 5.5.");
        }

        // FloatPointSupportsEveryOperation covers opSub/opDiv alongside opAdd/opMul on float points.
        [TestCase("opAdd", 3f)]
        [TestCase("opSub", -1f)]
        [TestCase("opMul", 2f)]
        [TestCase("opDiv", 0.5f)]
        public void FloatPointSupportsEveryOperation(string operation, float expectedResult)
        {
            var definition = CreateFloatDefinition($"[[1, [2, \"{operation}\"], 0]]");
            Assert.That(definition.Interpolate(0f), Is.EqualTo(expectedResult).Within(0.0001f),
                $"The [2, {operation}] modifier did not apply to the float point.");
        }

        // Vector3ModifiersApplyComponentwise mirrors Heck's Vector3PointDefinition op table
        // (opAdd/opSub add, opMul scales, opDiv divides per component).
        [Test]
        public void Vector3ModifiersApplyComponentwise()
        {
            var definition = CreateVector3Definition("[[1, 2, 3, [4, 8, 2, \"opMul\"], 0]]");
            Assert.That(
                definition.Interpolate(0f),
                Is.EqualTo(new Vector3(4f, 16f, 6f)).Within(0.0001f),
                "Vector3 modifiers must apply componentwise in declaration order.");
        }

        // QuaternionModifiersApplyOnEulerComponents matches Heck's QuaternionPointDefinition, whose operations
        // run on the euler representation rather than the quaternion. Component-wise tolerances because
        // Quaternion euler round-trips carry float noise.
        [Test]
        public void QuaternionModifiersApplyOnEulerComponents()
        {
            var value = CreateQuaternionDefinition("[[10, 20, 30, 0, [20, 30, 40, \"opAdd\"]]]").Interpolate(0f);
            var euler = value.eulerAngles;
            Assert.That(euler.x, Is.EqualTo(30f).Within(0.001f));
            Assert.That(euler.y, Is.EqualTo(50f).Within(0.001f));
            Assert.That(euler.z, Is.EqualTo(70f).Within(0.001f));
        }

        // ColorModifiersRemainComponentwise pins the color op path that already worked so the engine rewrite
        // cannot regress it.
        [Test]
        public void ColorModifiersRemainComponentwise()
        {
            PointDataParsers.ColorScheme = null;
            var definition = CreateColorDefinition("[[1, 0.5, 0.25, 1, 0, [0.5, 0.5, 0.5, 0.5, \"opMul\"]]]");
            var value = definition.Interpolate(0f);
            Assert.That(value.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(value.g, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(value.b, Is.EqualTo(0.125f).Within(0.0001f));
            Assert.That(value.a, Is.EqualTo(0.5f).Within(0.0001f));
        }

        // The first two point rows below come from "CENSORED!!", mapped by Saltyfish (BeatSaver ID: 4b3da).
        // Vector3NullComponentParsesAsNumericZero pins Heck's DeserializeValues behavior:
        // Convert.ToSingle(null) is 0, so a JSON null inside a numeric point row is a zero
        // component, not a dropped point. CENSORED!! authors its censson flash as
        // [[null,1.25,4.9,0]] / [[null,1.25,-6969,0]] — dropping the point zeroed the whole
        // pushed position and the flash never opened.
        [Test]
        public void Vector3NullComponentParsesAsNumericZero()
        {
            Assert.That(
                CreateVector3Definition("[[null, 1.25, 4.9, 0]]").Interpolate(0f),
                Is.EqualTo(new Vector3(0f, 1.25f, 4.9f)).Within(0.0001f),
                "A null leading component must evaluate as x=0, not drop the point.");
            Assert.That(
                CreateVector3Definition("[[null, 1.25, -6969, 0]]").Interpolate(0f),
                Is.EqualTo(new Vector3(0f, 1.25f, -6969f)).Within(0.0001f),
                "A null leading component must evaluate as x=0, not drop the point.");
            Assert.That(
                CreateVector3Definition("[[1, null, -3, 0]]").Interpolate(0f),
                Is.EqualTo(new Vector3(1f, 0f, -3f)).Within(0.0001f),
                "A null middle component must evaluate as y=0, not drop the point.");
        }

        private static PointDefinition<float> CreateFloatDefinition(string points) =>
            new(PointDataParsers.ParseFloat, BuildParams(points), null);

        private static PointDefinition<Vector3> CreateVector3Definition(string points) =>
            new(PointDataParsers.ParseVector3, BuildParams(points), null);

        private static PointDefinition<Quaternion> CreateQuaternionDefinition(string points) =>
            new(PointDataParsers.ParseQuaternion, BuildParams(points), null);

        private static PointDefinition<Color> CreateColorDefinition(string points) =>
            new(PointDataParsers.ParseColor, BuildParams(points), null);

        private static IPointDefinition.UntypedParams BuildParams(string points) => new()
        {
            Points = JSON.Parse(points),
            Time = 0f,
            TimeBegin = 0f,
            TimeEnd = 0f
        };
    }
}
