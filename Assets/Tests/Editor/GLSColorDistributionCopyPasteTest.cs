using System;
using System.Globalization;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Inner GLS edits replace the whole parent group. Verify box and node color distributions, unknown
    // customData, and parsed playback values survive that replacement.
    public class GLSColorDistributionCopyPasteTest : SongBoundaryTestBase
    {
        // Authored display abbreviations must survive every edit operation without being rewritten on load.
        private static readonly string[] BoxColorDistributions = { "h,-0.2,IO^5", "f,0.1,I^2" };
        private static readonly string[] BoxStrobeColorDistributions = { "s,-0.2,L,l" };
        private static readonly string[] SecondBoxStrobeColorDistributions = { "hs,0.4,L" };
        private static readonly string[] NodeColorDistributions = { "v,0.4,L" };
        private static readonly string[] NodeStrobeColorDistributions = { "r,-0.1,L" };

        private BeatmapRuntimeContext runtime;
        private GLSGroupGridProvider groupProvider;
        private GLSEventGridProvider innerProvider;
        private GLSEventGridContainer innerCollection;
        private BeatmapActionContainer actions;
        private TrackDefinitionsSO originalTracks;
        private TrackDefinitionsSO testTracks;
        private string originalPage;
        private int originalMapVersion;

        // Real GetNewObjects rejects unavailable GLS tracks, so install a valid color lane definition.
        [SetUp]
        public void ConfigureGlsTracks()
        {
            runtime = GetField<BeatmapRuntimeContext>(Selection, "beatmapRuntimeContext");
            groupProvider = GetField<GLSGroupGridProvider>(Selection, "glsGroupGridProvider");
            innerProvider = GetField<GLSEventGridProvider>(Selection, "glsEventGridProvider");
            innerCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            originalTracks = runtime.TrackDefinitions;
            originalPage = groupProvider.CurrentGroup;
            testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            testTracks.Copy(originalTracks);
            SetField(testTracks, "glsEntries", Enumerable.Range(1, 3).Select(id => new TrackDefinitionGLS
            {
                ID = id,
                Name = "Color distribution lane " + id,
                Group = "GLS color-distribution tests",
                ColorTrack = true,
                RotationTracks = new[] { false, false, false },
                TranslationTracks = new[] { false, false, false },
                FloatFXTrack = false
            }).ToList());
            testTracks.Initialize();
            runtime.TrackDefinitions = testTracks;
            runtime.NotifyTrackDefinitions();
            groupProvider.SetGroupPage("GLS color-distribution tests");
            // Group and box ToJson select a V3/V4 writer; pin the version these fixtures serialize.
            originalMapVersion = Settings.Instance.MapVersion;
            Settings.Instance.MapVersion = 3;
        }

        // Tracks are scene state, so failed assertions must not leave subsequent fixtures on the synthetic GLS page.
        [TearDown]
        public void RestoreGlsTracks()
        {
            if (runtime != null && originalTracks != null)
            {
                runtime.TrackDefinitions = originalTracks;
                runtime.NotifyTrackDefinitions();
                groupProvider.SetGroupPage(originalPage);
            }
            if (testTracks != null)
            {
                Object.DestroyImmediate(testTracks);
            }
            Settings.Instance.MapVersion = originalMapVersion;
        }

        [Test]
        public void PasteOuterColorGroupPreservesEveryBoxColorDistributionPayload()
        {
            SetMode(EditingMode.GLS);
            var source = PlaceColorDistributedGroup(8f, 1);
            SelectOnly(source);
            CopyWithKeyboard();
            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(1));
            // The clipboard entry is already a clone; its lanes must carry the color-distribution payload too.
            AssertColorDistributionGroupPayload((BaseLightColorEventBoxGroup)SelectionController.CopiedObjects.First());
            Atsc.MoveToJsonTime(24f);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            var pasted = ColorGroups().Single(g => g.ID == 1 && !ReferenceEquals(g, source));
            AssertRoundTrip(
                () =>
                {
                    Assert.That(ColorGroups().Any(g => ReferenceEquals(g, source)), Is.True);
                    AssertColorDistributionGroupPayload(source);
                    AssertColorDistributionGroupPayload(pasted);
                    // Independent ownership: the pasted boxes and node must not share the source payloads.
                    Assert.AreNotSame(source.Boxes[0].CustomData, pasted.Boxes[0].CustomData);
                    Assert.AreNotSame(source.Boxes[0].ColorDistributions, pasted.Boxes[0].ColorDistributions);
                    Assert.AreNotSame(source.Boxes[0].StrobeColorDistributions, pasted.Boxes[0].StrobeColorDistributions);
                    Assert.AreNotSame(source.Boxes[1].CustomData, pasted.Boxes[1].CustomData);
                    Assert.AreNotSame(
                        source.Boxes[0].Events[0].CustomData,
                        pasted.Boxes[0].Events[0].CustomData);
                },
                () =>
                {
                    Assert.That(ColorGroups().Any(g => ReferenceEquals(g, pasted)), Is.False);
                    AssertColorDistributionGroupPayload(source);
                });
        }

        // Pasting a node into a color-distributed box rebuilds the destination parent; both the parent lane payload and
        // the copied node's own event-scope payload must survive the replacement.
        [Test]
        public void PasteInnerNodePreservesBoxColorDistributionsAndPastedNodeColorDistributionPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceColorDistributedGroup(8f, 1);
            OpenGroup(group);
            var sourceNode = group.Boxes[0].Events[0];
            SelectOnly(sourceNode);
            CopyWithKeyboard();
            var clipboardNode = (BaseLightColorBase)SelectionController.CopiedObjects.First();
            CollectionAssert.AreEqual(NodeColorDistributions, clipboardNode.ColorDistributions);
            CollectionAssert.AreEqual(NodeStrobeColorDistributions, clipboardNode.StrobeColorDistributions);
            SelectionController.DeselectAll();
            ConfigureInnerColorPaste(group, 0, 3f);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            AssertRoundTrip(
                () =>
                {
                    var pasted = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(pasted, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(pasted.Boxes[0].Events.Length, Is.EqualTo(3));
                    AssertColorDistributionGroupPayload(pasted);
                    var pastedNode = pasted.Boxes[0].Events
                        .Single(e => Mathf.Abs(e.RelativeJsonTime - 3f) < 0.0001f);
                    CollectionAssert.AreEqual(NodeColorDistributions, pastedNode.ColorDistributions);
                    CollectionAssert.AreEqual(NodeStrobeColorDistributions, pastedNode.StrobeColorDistributions);
                    Assert.That(pastedNode.ParsedColorDistributions.Count, Is.EqualTo(1));
                    Assert.That(pastedNode.ParsedStrobeColorDistributions.Count, Is.EqualTo(1));
                    Assert.AreNotSame(sourceNode.CustomData, pastedNode.CustomData);
                    Assert.AreNotSame(sourceNode.ColorDistributions, pastedNode.ColorDistributions);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertColorDistributionGroupPayload(group);
                });
        }

        [Test]
        public void EditInnerNodePreservesContainingBoxColorDistributionPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceColorDistributedGroup(8f, 1);
            OpenGroup(group);
            var editedNode = group.Boxes[0].Events[1];
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            GLSEventColorCommand.SetBrightness(editedNode, 0.42f);

            AssertRoundTrip(
                () =>
                {
                    var edited = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(edited, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(edited.Boxes[0].Events[1].Brightness, Is.EqualTo(0.42f).Within(0.0001f));
                    AssertColorDistributionGroupPayload(edited);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertColorDistributionGroupPayload(group);
                });
        }

        [Test]
        public void DeleteInnerNodePreservesContainingBoxColorDistributionPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceColorDistributedGroup(8f, 1);
            OpenGroup(group);
            SelectOnly(group.Boxes[0].Events[1]);

            Selection.Delete();

            AssertRoundTrip(
                () =>
                {
                    var edited = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(edited, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(edited.Boxes[0].Events.Length, Is.EqualTo(1));
                    AssertColorDistributionGroupPayload(edited);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertColorDistributionGroupPayload(group);
                });
        }

        [Test]
        public void AddInnerNodePreservesContainingBoxColorDistributionPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceColorDistributedGroup(8f, 1);
            OpenGroup(group);
            var node = new BaseLightColorBase
            {
                RelativeJsonTime = 2.5f,
                Brightness = 1f,
                EventBoxGroupData = group,
                EventBoxData = group.Boxes[0],
                BoxIndex = 0
            };
            node.SetMap(BeatSaberSongContainer.Instance.Map);
            node.RecomputeSongBpmTime();
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            innerCollection.SpawnObject(node, false, true);

            AssertRoundTrip(
                () =>
                {
                    var edited = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(edited, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(edited.Boxes[0].Events.Length, Is.EqualTo(3));
                    AssertColorDistributionGroupPayload(edited);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertColorDistributionGroupPayload(group);
                });
        }

        // A two-lane group whose first lane owns box color distributions, strobe color distributions, an unknown field, and a color-distributed node.
        private static BaseLightColorEventBoxGroup CreateColorDistributedGroup(float beat, int id)
        {
            return BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                "{\"b\":" + beat.ToString(CultureInfo.InvariantCulture) +
                ",\"g\":" + id.ToString(CultureInfo.InvariantCulture) +
                ",\"e\":[" +
                "{\"f\":{\"c\":1,\"f\":0,\"p\":0,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":1,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":0," +
                "\"customData\":{\"" + GLSColorDistribution.ColorDistributionsKey + "\":[\"h,-0.2,IO^5\",\"f,0.1,I^2\"]," +
                "\"" + GLSColorDistribution.StrobeColorDistributionsKey + "\":[\"s,-0.2,L,l\"],\"future\":42}," +
                "\"e\":[" +
                "{\"b\":0.5,\"c\":0,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0," +
                "\"customData\":{\"" + GLSColorDistribution.ColorDistributionsKey + "\":[\"v,0.4,L\"],\"" + GLSColorDistribution.StrobeColorDistributionsKey + "\":[\"r,-0.1,L\"]," +
                "\"eventFuture\":\"kept\"}}," +
                "{\"b\":1.5,\"c\":1,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}," +
                "{\"f\":{\"c\":1,\"f\":1,\"p\":2,\"t\":4,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":1,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":0," +
                "\"customData\":{\"" + GLSColorDistribution.StrobeColorDistributionsKey + "\":[\"hs,0.4,L\"]}," +
                "\"e\":[{\"b\":0.75,\"c\":1,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}]}"));
        }

        private BaseLightColorEventBoxGroup PlaceColorDistributedGroup(float beat, int id) =>
            Spawn(CreateColorDistributedGroup(beat, id));

        // Check parsed playback values as well as saved JSON; preserving strings alone is not enough.
        private static void AssertColorDistributionGroupPayload(BaseLightColorEventBoxGroup group)
        {
            var first = group.Boxes[0];
            CollectionAssert.AreEqual(BoxColorDistributions, first.ColorDistributions);
            CollectionAssert.AreEqual(BoxStrobeColorDistributions, first.StrobeColorDistributions);
            Assert.That(first.ParsedColorDistributions.Count, Is.EqualTo(2));
            Assert.That(first.ParsedStrobeColorDistributions.Count, Is.EqualTo(1));
            Assert.That(first.ParsedStrobeColorDistributions[0].UsesAffectedLightProgress, Is.True);
            Assert.That(first.CustomData["future"].AsInt, Is.EqualTo(42));

            var colorDistributedNode = first.Events[0];
            CollectionAssert.AreEqual(NodeColorDistributions, colorDistributedNode.ColorDistributions);
            CollectionAssert.AreEqual(NodeStrobeColorDistributions, colorDistributedNode.StrobeColorDistributions);
            Assert.That(colorDistributedNode.ParsedColorDistributions.Count, Is.EqualTo(1));
            Assert.That(colorDistributedNode.ParsedStrobeColorDistributions.Count, Is.EqualTo(1));
            Assert.That(colorDistributedNode.CustomData["eventFuture"].Value, Is.EqualTo("kept"));

            var second = group.Boxes[1];
            CollectionAssert.AreEqual(SecondBoxStrobeColorDistributions, second.StrobeColorDistributions);
            Assert.That(second.ParsedStrobeColorDistributions.Count, Is.EqualTo(1));

            // SaveCustom rewrites both owned arrays during serialization; assert the keys remain authored.
            var output = group.ToJson();
            Assert.That(output["e"][0]["customData"]["colorDistributions"][0].Value, Is.EqualTo("h,-0.2,IO^5"));
            Assert.That(output["e"][0]["customData"]["colorDistributions"][1].Value, Is.EqualTo("f,0.1,I^2"));
            Assert.That(output["e"][0]["customData"]["strobeColorDistributions"][0].Value, Is.EqualTo("s,-0.2,L,l"));
            Assert.That(output["e"][0]["customData"]["future"].AsInt, Is.EqualTo(42));
            Assert.That(output["e"][0]["e"][0]["customData"]["colorDistributions"][0].Value, Is.EqualTo("v,0.4,L"));
            Assert.That(
                output["e"][0]["e"][0]["customData"]["strobeColorDistributions"][0].Value,
                Is.EqualTo("r,-0.1,L"));
            Assert.That(output["e"][1]["customData"]["strobeColorDistributions"][0].Value, Is.EqualTo("hs,0.4,L"));
        }

        // Keep selection gestures out of the action stack so one undo reverses exactly the tested operation.
        private static void SelectOnly(params BaseObject[] objects)
        {
            SelectionController.DeselectAll();
            foreach (var obj in objects)
            {
                SelectionController.Select(obj, true, false, false);
            }
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
        }

        // Clear pending replacements before opening the next group.
        private void OpenGroup(BaseEventBoxGroup group)
        {
            innerProvider.LastContext = null;
            innerProvider.GroupContext = group;
        }

        // Inner paste uses a relative hover offset; the queued color placement supplies the destination lane.
        private void ConfigureInnerColorPaste(BaseEventBoxGroup group, int boxIndex, float relativeBeat)
        {
            var placement = GetField<BasePlacement>(Selection, "glsEventColorPlacement");
            var queued = GetField<BaseGLSEvent>(placement, "QueuedData");
            queued.EventBoxGroupData = group;
            queued.EventBoxData = group.ReadOnlyBoxes[boxIndex];
            queued.BoxIndex = boxIndex;
            queued.RelativeJsonTime = relativeBeat;
            queued.SetMap(BeatSaberSongContainer.Instance.Map);
            queued.RecomputeSongBpmTime();
            placement.State = PlacementState.Active;
        }

        private static BaseLightColorEventBoxGroup[] ColorGroups() =>
            BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSColor)
                .LoadedObjects.Cast<BaseLightColorEventBoxGroup>().ToArray();

        private void AssertRoundTrip(Action assertApplied, Action assertOriginal)
        {
            assertApplied();
            var action = actions.Undo();
            Assert.That(action, Is.Not.Null);
            assertOriginal();
            Assert.That(actions.Undo(), Is.Null, "The edit must be one atomic action.");
            Assert.That(actions.Redo(), Is.SameAs(action));
            assertApplied();
            Assert.That(actions.Redo(), Is.Null, "No extra edits may remain on the redo stack.");
        }
    }
}
