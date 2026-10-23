using System.Collections.Generic;
using Beatmap.Animations;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class AnimationAggregatorCompositionTest
    {
        [Test]
        public void FiveContributorsComposePositionScaleRotationColorAndOpacity()
        {
            var root = new GameObject("Five animation contributors");
            root.SetActive(false);
            try
            {
                var animator = root.AddComponent<ObjectAnimator>();
                for (var i = 0; i < 5; i++)
                {
                    animator.WorldPosition.Add(new Vector3(1f, 2f, 3f));
                    animator.Scale.Add(Vector3.one * 2f);
                    animator.WorldRotation.Add(Quaternion.Euler(0f, 0f, 15f));
                    animator.Colors.Add(new Color(0.8f, 0.7f, 0.6f, 0.5f));
                    animator.Opacity.Add(0.5f);
                }

                var failures = new List<string>();
                var position = animator.WorldPosition.Get();
                var scale = animator.Scale.Get();
                var rotation = animator.WorldRotation.Get();
                var color = animator.Colors.Get();
                var opacity = animator.Opacity.Get();
                if (Vector3.Distance(position, new Vector3(5f, 10f, 15f)) > 0.0001f)
                    failures.Add($"Five position contributors produced {position}, expected (5,10,15).");

                if (Vector3.Distance(scale, Vector3.one * 32f) > 0.0001f)
                    failures.Add($"Five scale contributors produced {scale}, expected (32,32,32).");

                if (Quaternion.Angle(rotation, Quaternion.Euler(0f, 0f, 75f)) > 0.01f)
                    failures.Add($"Five rotations produced {rotation.eulerAngles}, expected Z=75 degrees.");

                if (Vector4.Distance(color, new Color(0.32768f, 0.16807f, 0.07776f, 0.03125f)) > 0.0001f)
                    failures.Add($"Five color contributors produced {color}, expected (.32768,.16807,.07776,.03125).");

                if (Mathf.Abs(opacity - 0.03125f) > 0.0001f)
                    failures.Add($"Five opacity contributors produced {opacity}, expected .03125.");

                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }

            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PersistentContributorsSurviveFlushAndRecomposition()
        {
            var root = new GameObject("Persistent animation contributors");
            root.SetActive(false);
            try
            {
                var scale = root.AddComponent<ObjectAnimator>().Scale;
                scale.Preload(new Vector3(2f, 3f, 4f));
                scale.HoldUntilFlush = true;
                for (var i = 0; i < 5; i++)
                    scale.Add(Vector3.one * 2f);

                var expected = new Vector3(64f, 96f, 128f);
                Assert.That(Vector3.Distance(scale.Get(), expected), Is.LessThan(0.0001f));
                Assert.That(Vector3.Distance(scale.Get(), expected), Is.LessThan(0.0001f));
                scale.Flush();
                Assert.That(Vector3.Distance(scale.Get(), new Vector3(2f, 3f, 4f)), Is.LessThan(0.0001f));
                for (var i = 0; i < 5; i++)
                    scale.Add(Vector3.one * 3f);

                Assert.That(Vector3.Distance(scale.Get(), new Vector3(486f, 729f, 972f)), Is.LessThan(0.0001f));
            }

            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
