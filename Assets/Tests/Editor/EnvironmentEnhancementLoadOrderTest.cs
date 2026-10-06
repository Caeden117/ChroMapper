using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // A standalone empty map keeps this loader regression independent of fixture geometry:
    // reloading a cloned-ring map's enhancements without reloading its scene duplicates its markers.
    public class EnvironmentEnhancementLoadOrderTest : TestBase
    {
        // The comparison counter detects an inherited beat sort even when a stable sort
        // happens to retain the order of two equal-time enhancement instructions.
        private sealed class ComparisonCounterEnhancement : BaseEnvironmentEnhancement
        {
            public static int Comparisons;

            public override int CompareTo(BaseObject other)
            {
                Comparisons++;
                return base.CompareTo(other);
            }
        }

        // Vagueness & JOURNEY's left source scale moved behind 27 clones when the generic
        // loader compared beat-zero enhancements. Chroma runs them in source array order.
        [Test]
        public void LoadingEnvironmentEnhancementsNeverSortsTheirInstructionList()
        {
            var loader = Object.FindAnyObjectByType<MapLoader>();
            var authored = BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements;
            var first = new ComparisonCounterEnhancement
            {
                ID = "unmatched-first-instruction",
                LookupMethod = EnvironmentLookupMethod.Exact
            };
            var second = new ComparisonCounterEnhancement
            {
                ID = "unmatched-second-instruction",
                LookupMethod = EnvironmentLookupMethod.Exact
            };
            var instructions = new List<BaseEnvironmentEnhancement> { first, second };
            ComparisonCounterEnhancement.Comparisons = 0;

            try
            {
                loader.LoadEnvironmentEnhancements(instructions);
                Assert.That(ComparisonCounterEnhancement.Comparisons, Is.Zero,
                    "Environment enhancements must never reach a beat sort.");
                Assert.That(instructions[0], Is.SameAs(first));
                Assert.That(instructions[1], Is.SameAs(second));
            }
            finally
            {
                loader.LoadEnvironmentEnhancements(authored);
            }
        }
    }
}
