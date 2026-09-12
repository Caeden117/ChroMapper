using System;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class BasicMovementLifecycleTest : TestBase
    {
        [Test]
        public void LightPairRotationLateWiringIsFinalizedDuringEffectInitialization()
        {
            var root = new GameObject(nameof(LightPairRotationLateWiringIsFinalizedDuringEffectInitialization));
            try
            {
                var visual = root.AddComponent<LightPairRotation>();
                var effect = root.AddComponent<LightPairRotationEffect>();
                var left = CreateChild(root.transform, "Left", new Vector3(10f, 20f, 30f), Vector3.zero);
                var right = CreateChild(root.transform, "Right", new Vector3(-15f, 25f, 35f), Vector3.zero);
                visual.Transforms = new[]
                {
                    new LightPairRotation.TransformContainer { Transform = left },
                    new LightPairRotation.TransformContainer { Transform = right },
                };
                effect.Visual = visual;
                effect.Atsc = GetAudioTimeSyncController();
                var expectedLeftRotation = left.rotation;
                var expectedRightRotation = right.rotation;

                effect.Initialize();

                // Unity normalizes applied quaternions, so compare their angle rather than raw components.
                Assert.That(Quaternion.Angle(visual.Transforms[0].Start, expectedLeftRotation), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(visual.Transforms[1].Start, expectedRightRotation), Is.LessThan(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LightPairSinMoveLateWiringIsFinalizedDuringEffectInitialization()
        {
            var root = new GameObject(nameof(LightPairSinMoveLateWiringIsFinalizedDuringEffectInitialization));
            try
            {
                var visual = root.AddComponent<LightPairSinMove>();
                var effect = root.AddComponent<LightPairSinMoveEffect>();
                var left = CreateChild(root.transform, "Left", Vector3.zero, new Vector3(1f, 2f, 3f));
                var right = CreateChild(root.transform, "Right", Vector3.zero, new Vector3(4f, 5f, 6f));
                visual.Transforms = new[]
                {
                    new LightPairSinMove.TransformContainer { Transform = left },
                    new LightPairSinMove.TransformContainer { Transform = right },
                };
                effect.Visual = visual;
                effect.Atsc = GetAudioTimeSyncController();

                effect.Initialize();

                Assert.That(visual.Transforms[0].StartPosition, Is.EqualTo(left.localPosition));
                Assert.That(visual.Transforms[1].StartPosition, Is.EqualTo(right.localPosition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LightRotationLateWiringIsFinalizedDuringEffectInitialization()
        {
            var root = new GameObject(nameof(LightRotationLateWiringIsFinalizedDuringEffectInitialization));
            try
            {
                var visual = root.AddComponent<LightRotation>();
                var effect = root.AddComponent<LightRotationEffect>();
                var target = CreateChild(root.transform, "Target", new Vector3(12f, 34f, 56f), Vector3.zero);
                visual.Transform = target;
                effect.Visual = visual;
                effect.Atsc = GetAudioTimeSyncController();

                effect.Initialize();

                Assert.That(visual.StartRotation, Is.EqualTo(target.rotation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MovementLateWiringIsFinalizedDuringEffectInitialization()
        {
            var root = new GameObject(nameof(MovementLateWiringIsFinalizedDuringEffectInitialization));
            try
            {
                var visual = root.AddComponent<Movement>();
                var effect = root.AddComponent<MovementEffect>();
                var target = CreateChild(root.transform, "Target", Vector3.zero, new Vector3(2f, 3f, 4f));
                visual.Transforms = new[] { target };
                visual.MovementData = new[] { Vector3.zero, Vector3.forward };
                effect.Visual = visual;
                effect.Atsc = GetAudioTimeSyncController();

                effect.Initialize();
                visual.Apply(Vector3.right);

                Assert.That(target.localPosition, Is.EqualTo(new Vector3(3f, 3f, 4f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(typeof(LightPairRotationEffect))]
        [TestCase(typeof(LightPairSinMoveEffect))]
        [TestCase(typeof(LightRotationEffect))]
        [TestCase(typeof(MovementEffect))]
        public void MissingVisualIsRejectedDuringEffectInitialization(Type effectType)
        {
            var root = new GameObject($"{nameof(MissingVisualIsRejectedDuringEffectInitialization)}_{effectType.Name}");
            var ignoreFailingMessages = LogAssert.ignoreFailingMessages;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                var effect = (StateManager)root.AddComponent(effectType);
                effect.Atsc = GetAudioTimeSyncController();

                Assert.Throws<InvalidOperationException>(() => effect.Initialize());
            }
            finally
            {
                LogAssert.ignoreFailingMessages = ignoreFailingMessages;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform CreateChild(
            Transform parent,
            string name,
            Vector3 eulerAngles,
            Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent);
            child.localPosition = localPosition;
            child.rotation = Quaternion.Euler(eulerAngles);
            return child;
        }

        private static AudioTimeSyncController GetAudioTimeSyncController()
        {
            var atsc = UnityEngine.Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(atsc, Is.Not.Null, "The shared editor test scene has no AudioTimeSyncController.");
            return atsc;
        }
    }
}
