using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    // Per-light ownership of the shared wave fixture: serialized box order, index-filter claims,
    // and OEM's nodeBeat < nextElementStart cross-group interruption semantics.
    public class GLSColorOwnershipTest : GLSColorPlaybackTestBase
    {
        // The second all-light box cannot steal the first box's even lights, even though its node is later in time.
        [Test]
        public void SerializedFirstBoxWinsAndDistributionKeepsPhysicalOrder()
        {
            Assert.AreEqual(3, map.LightColorEventBoxGroups.Count);
            Assert.AreEqual(2, map.LightColorEventBoxGroups[1].Boxes.Count);
            Assert.AreEqual(29.75f, Node(1, 0, 2).JsonTime);
            Assert.AreEqual(39f, Node(1, 0, 3).JsonTime);
            Assert.AreEqual(22f, Node(1, 1).JsonTime);
            Assert.AreEqual(4, Node(1, 1).ChromaStrobeColorEasing);
            Assert.AreEqual(19, Node(1, 1).ChromaStrobeEasing);
            Assert.AreEqual(2f, Node(1, 1).ChromaStrobeInterval);

            for (var light = 0; light < LightCount; light++)
            {
                var first = StateAt(light, 5f);
                Assert.AreSame(Node(0), first.Base);
                Assert.AreSame(light % 2 == 0 ? Node(1) : Node(1, 1), first.Next.Base);
                Assert.That(first.EndTime, Is.EqualTo(SongTime(light % 2 == 0 ? 11f : 22f + (0.4f * light))).Within(0.0001f));
                var later = StateAt(light, 40f);
                Assert.AreSame(light % 2 == 0 ? Node(1, 0, 3) : Node(1, 1), later.Base);
                Assert.AreSame(Node(2), later.Next.Base);
                // First-wins ownership must also materialize as per-light event states: every filtered node only
                // exists on the even lights its box claimed, while the same-group all-light node exists only on odd.
                Assert.That(HasState(light, Node(1, 0, 0)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 0 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 0, 1)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 1 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 0, 2)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 2 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 0, 3)), Is.EqualTo(light % 2 == 0), $"light {light}: filtered node 3 exists iff the light is even");
                Assert.That(HasState(light, Node(1, 1)), Is.EqualTo(light % 2 == 1), $"light {light}: the all-light node exists iff the light is odd");
            }

            for (var light = 0; light < LightCount; light++)
            {
                var before = StateAt(light, 45f);
                Assert.AreSame(light % 2 == 0 ? Node(1, 0, 3) : Node(1, 1), before.Base);
                Assert.AreSame(Node(2), before.Next.Base);
                Assert.That(before.EndTime, Is.EqualTo(SongTime(46f)).Within(0.0001f));
                Assert.AreSame(Node(2), StateAt(light, 46f).Base);
                Assert.IsTrue(HasState(light, Node(1, 0, 3)) == (light % 2 == 0));
            }
        }

        // LightColorBeatmapEventDataBox.Unpack uses nodeBeat < nextElementStart: the beat-15 boundary child and
        // every later child are cancelled, while all eight lights transition to the interrupter's actual beat-17 node.
        [Test]
        public void DifferentAllLightsGroupInterruptsAndCancelsFilteredEventsAtOrAfterGroupStart()
        {
            LoadPlayback(InterruptedMapJson);
            var baseline = Node(0);
            var filteredSource = Node(1, 0, 0);
            var boundaryChild = Node(1, 0, 1);
            var laterChild = Node(1, 0, 2);
            var finalChild = Node(1, 0, 3);
            var interrupter = Node(2);

            for (var light = 0; light < LightCount; light++)
            {
                var even = light % 2 == 0;
                var before = StateAt(light, 14f);
                Assert.AreSame(even ? filteredSource : baseline, before.Base);
                Assert.AreSame(interrupter, before.Next.Base);
                Assert.That(before.EndTime, Is.EqualTo(SongTime(17f)).Within(0.0001f));
                Assert.That(HasState(light, filteredSource), Is.EqualTo(even));
                Assert.IsFalse(HasState(light, boundaryChild), $"light {light}: a prior child at the exact beat-15 ownership boundary must be omitted");
                Assert.IsFalse(HasState(light, laterChild), $"light {light}: a prior child after takeover must be omitted");
                Assert.IsFalse(HasState(light, finalChild), $"light {light}: the prior box must never resume after takeover");

                var sourceColor = even ? Color.red : Color.white;
                var progress = even ? (13.5f - 10f) / (17f - 10f) : 13.5f / 17f;
                AssertColor(
                    ColorAt(light, 13.5f),
                    Color.LerpUnclamped(sourceColor, Color.blue, progress),
                    0.002f,
                    $"light {light}: transition before the beat-17 all-light node");
                AssertColor(ColorAt(light, 17f), Color.blue, 0.002f, $"light {light}: all-light takeover");
                Assert.AreSame(interrupter, StateAt(light, 20f).Base);
            }
        }
    }
}
