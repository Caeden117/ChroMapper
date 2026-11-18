using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public abstract class GLSExtensionTestBase : TestBase
    {
        // The fixture temporarily supplies a rotation track and restores the scene's original definitions after each case.
        private TrackDefinitionsSO originalTracks;
        private TrackDefinitionsSO testTracks;
        private BeatmapRuntimeContext runtime;
        protected GLSGroupGridProvider groupProvider;
        private string originalPage;

        protected override EditingMode InitialEditingMode => EditingMode.GLS;

        // The shared test map has no rotation tracks; provide a real rotation lane so placement reaches its map action.
        [SetUp]
        public void ConfigureRotationTrack()
        {
            runtime = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            groupProvider = Object.FindAnyObjectByType<GLSGroupGridProvider>();
            originalTracks = runtime.TrackDefinitions;
            originalPage = groupProvider.CurrentGroup;
            testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            testTracks.Copy(originalTracks);
            typeof(TrackDefinitionsSO).GetField("glsEntries", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(testTracks, new List<TrackDefinitionGLS>
                {
                    new TrackDefinitionGLS
                    {
                        ID = 0,
                        Name = "Extension rotation",
                        Group = "Extension tests",
                        ColorTrack = true,
                        FloatFXTrack = true,
                        RotationTracks = new[] { true, true, true },
                        TranslationTracks = new[] { true, true, true }
                    }
                });
            testTracks.Initialize();
            runtime.TrackDefinitions = testTracks;
            runtime.NotifyTrackDefinitions();
            groupProvider.SetGroupPage("Extension tests");
        }

        // Restore scene definitions even after a failing placement assertion so later fixtures keep their environment tracks.
        [TearDown]
        public void RestoreRotationTrack()
        {
            runtime.TrackDefinitions = originalTracks;
            runtime.NotifyTrackDefinitions();
            groupProvider.SetGroupPage(originalPage);
            Object.DestroyImmediate(testTracks);
        }

        // Reflection reaches serialized scene dependencies, while all assertions exercise runtime behavior.
        protected static T Field<T>(object owner, string name)
        {
            for (var type = owner.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                    return (T)field.GetValue(owner);
            }
            Assert.Fail("Missing serialized dependency " + name);
            return default;
        }
    }

    public class GLSRotationExtensionTest : GLSExtensionTestBase
    {
        // Both placement queues can retain a normal node's loop count after switching to Extension. Use their
        // input notifications and insert the resulting nodes into the map.
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void NewExtensionPlacementClearsPreviouslySelectedLoops(bool outer, bool copiedFromAuthoredNode)
        {
            var track = groupProvider.IdToTracks.Values.First(t => t.TrackDefinition.RotationTracks.Any(x => x));
            var innerProvider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var group = CreateGroup(track.TrackDefinition.ID);
            var collection = (GLSGroupRotationGridContainer)BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSRotation);
            GLSEventRotationPlacement innerPlacement = null;
            GLSGroupRotationPlacement outerPlacement = null;
            BaseLightRotationBase queued;
            BeatmapGLSEventRotationInputController input;
            BeatmapEasingsSelectionInputController easing;
            if (outer)
            {
                var provider = track.GetComponent<PlacementProvider>();
                outerPlacement = provider.Placements.OfType<GLSGroupRotationPlacement>().Single();
                outerPlacement.Initialize(provider);
                outerPlacement.QueuedData.JsonTime = 8f;
                queued = outerPlacement.QueuedData.Boxes[0].Events[0];
                input = Field<BeatmapGLSEventRotationInputController>(outerPlacement, "eventInputController");
                easing = Field<BeatmapEasingsSelectionInputController>(outerPlacement, "EasingInputController");
            }
            else
            {
                collection.SpawnObject(group, out _);
                innerProvider.GroupContext = group;
                innerPlacement = Object.FindAnyObjectByType<GLSEventRotationPlacement>(FindObjectsInactive.Include);
                innerPlacement.Initialize(null);
                queued = innerPlacement.QueuedData;
                queued.EventBoxGroupData = group;
                queued.EventBoxData = group.Boxes[0];
                queued.BoxIndex = 0;
                queued.RelativeJsonTime = 4f;
                queued.RecomputeSongBpmTime();
                input = Field<BeatmapGLSEventRotationInputController>(innerPlacement, "inputController");
                easing = Field<BeatmapEasingsSelectionInputController>(innerPlacement, "EasingInputController");
            }

            // Copied or restored queue data can bypass the Extension button; actual placement must enforce zero too.
            if (copiedFromAuthoredNode)
            {
                queued.Loop = 3;
                queued.UsePrevious = 1;
            }
            else
            {
                input.NotifyLoopChanged(3);
                Assert.That(queued.Loop, Is.EqualTo(3), "Normal rotation placements must still accept loops.");
                easing.NotifyExtensionChanged(1);
                Assert.That(queued.UsePrevious, Is.EqualTo(1));
                Assert.That(queued.Loop, Is.Zero, "Selecting Extension must clear the normal node's remembered loops.");
            }

            if (outer)
                outerPlacement.HandleApply();
            else
                innerPlacement.HandleApply();

            var placed = collection.MapObjects.Single(g => g.ID == group.ID);
            var evt = outer
                ? placed.Boxes[0].Events[0]
                : placed.Boxes[0].Events.Last();
            Assert.That(evt.UsePrevious, Is.EqualTo(1));
            Assert.That(evt.Loop, Is.Zero);
        }

        // Editing an existing extension is an authored choice and must survive the group replacement action.
        [TestCase(false)]
        [TestCase(true)]
        public void ExistingExtensionAllowsExplicitLoopEdit(bool outer)
        {
            var group = CreateGroup(0);
            group.Boxes[0].Events[0].UsePrevious = 1;
            var collection = (GLSGroupRotationGridContainer)BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSRotation);
            collection.SpawnObject(group, out _);
            if (!outer)
                Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;

            var edited = GLSEventRotationCommand.SetLoop(group.Boxes[0].Events[0], 2);

            Assert.That(edited, Is.Not.Null);
            Assert.That(edited.UsePrevious, Is.EqualTo(1));
            Assert.That(edited.Loop, Is.EqualTo(2));
            Assert.That(collection.MapObjects.Single(g => g.ID == group.ID).Boxes[0].Events[0].Loop, Is.EqualTo(2));
        }

        // Render both shipped prefabs: extension labels show only authored nonzero loops and retain
        // the normal rotation loop's character position instead of recentering it on the face.
        [TestCase(false, 0)]
        [TestCase(false, 2)]
        [TestCase(false, -1)]
        [TestCase(true, 0)]
        [TestCase(true, 2)]
        [TestCase(true, -1)]
        // Easing has visible work only when an extension adds positive loops; zero and negative loops hide it.
        [TestCase(false, 0, EaseType.InQuadratic)]
        [TestCase(true, 0, EaseType.InQuadratic)]
        [TestCase(false, 2, EaseType.InQuadratic)]
        [TestCase(true, 2, EaseType.InQuadratic)]
        [TestCase(false, 0, EaseType.None)]
        [TestCase(true, 0, EaseType.None)]
        [TestCase(false, -1, EaseType.InQuadratic)]
        [TestCase(true, -1, EaseType.InQuadratic)]
        [TestCase(false, 2, EaseType.None)]
        [TestCase(true, 2, EaseType.None)]
        public void ExtensionLabelShowsOnlyNonzeroLoopInNormalSlot(bool outer, int loops, EaseType easing = EaseType.Linear)
        {
            var path = outer
                ? "Assets/_Prefabs/MapEditor/Beatmap/GLS Group.prefab"
                : "Assets/_Prefabs/MapEditor/Beatmap/GLS Event.prefab";
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var group = CreateGroup(0);
            var evt = group.Boxes[0].Events[0];
            evt.Loop = loops;
            evt.Easing = (int)easing;
            try
            {
                if (outer)
                    instance.GetComponent<GLSGroupContainer>().EventBoxGroupData = group;
                else
                    instance.GetComponent<GLSEventContainer>().EventData = evt;
                ApplyAppearance(instance, outer);
                var displays = new[]
                {
                    instance.transform.Find("TextTop").GetComponent<TextMeshPro>(),
                    instance.transform.Find("TextSide").GetComponent<TextMeshPro>()
                };
                var normalPositions = displays.Select(display =>
                {
                    display.ForceMeshUpdate();
                    return display.textInfo.characterInfo[0].bottomLeft;
                }).ToArray();
                var easingLabel = Easing.IDToShortName[(int)easing];
                var normalEasingPositions = displays.Select(display =>
                    display.textInfo.characterInfo[display.GetParsedText().IndexOf(easingLabel)].bottomLeft).ToArray();
                var easingIcon = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var normalEasingSprite = easingIcon.sprite;

                evt.UsePrevious = 1;
                ApplyAppearance(instance, outer);
                Assert.That(easingIcon.enabled, Is.EqualTo(loops > 0));
                if (loops > 0 && easing == EaseType.None)
                    Assert.That(easingIcon.sprite.name, Is.EqualTo("NoEasingStep"));
                else if (loops > 0)
                    Assert.That(easingIcon.sprite, Is.SameAs(normalEasingSprite));
                for (var i = 0; i < displays.Length; i++)
                {
                    var display = displays[i];
                    Assert.That(display.enabled, Is.EqualTo(loops != 0));
                    if (!display.enabled)
                        continue;
                    display.ForceMeshUpdate();
                    var labels = display.GetParsedText().Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);
                    var expectedLabels = new List<string>();
                    if (loops != 0)
                        expectedLabels.Add(loops.ToString());
                    if (loops > 0)
                        expectedLabels.Add(easingLabel);
                    CollectionAssert.AreEqual(expectedLabels, labels);
                    if (loops != 0)
                        Assert.That(Vector3.Distance(display.textInfo.characterInfo[0].bottomLeft, normalPositions[i]), Is.LessThan(0.01f));
                    if (loops > 0)
                    {
                        var position = display.textInfo.characterInfo[display.GetParsedText().IndexOf(easingLabel)].bottomLeft;
                        Assert.That(Vector3.Distance(position, normalEasingPositions[i]), Is.LessThan(0.01f));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // Sample inside a full-turn tween; endpoint orientations alone cannot distinguish an ignored loop.
        [TestCase(0, LightRotationDirection.Clockwise, 30f)]
        [TestCase(1, LightRotationDirection.Clockwise, 120f)]
        [TestCase(2, LightRotationDirection.CounterClockwise, -150f)]
        [TestCase(1, LightRotationDirection.Automatic, 120f)]
        public void AuthoredExtensionLoopsRotateUsingOwnDirection(int loops, LightRotationDirection direction, float expected)
        {
            var instance = new GameObject("Extension rotation preview");
            try
            {
                var effect = instance.AddComponent<LightRotationGroupEffect>();
                effect.Atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
                effect.Count = 1;
                effect.Register(0, Axis.Y, false, instance.transform);
                effect.Initialize();
                var group = CreateGroup(0);
                group.Boxes[0].Events = new[]
                {
                    new BaseLightRotationBase { Rotation = 30f, Easing = (int)EaseType.None },
                    new BaseLightRotationBase
                    {
                        RelativeJsonTime = 4f,
                        Rotation = 230f,
                        UsePrevious = 1,
                        Loop = loops,
                        Direction = (int)direction,
                        Easing = (int)EaseType.Linear
                    }
                };
                group.RecomputeSongBpmTime();
                effect.InsertData(group);
                effect.Refresh();
                effect.UpdateTime(false, (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(5f));

                Assert.That(Quaternion.Angle(instance.transform.localRotation, Quaternion.Euler(0f, expected, 0f)),
                    Is.LessThan(0.01f), "Extension inherits the angle but retains its own loop and direction.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // The real appearance assets own the text layout and colors for both editor views.
        private static void ApplyAppearance(GameObject instance, bool outer)
        {
            if (outer)
            {
                AssetDatabase.LoadAssetAtPath<GLSGroupAppearanceSO>("Assets/__Scripts/Beatmap/Appearances/GLSGroupAppearanceSO.asset")
                    .SetAppearance(instance.GetComponent<GLSGroupContainer>());
            }
            else
            {
                AssetDatabase.LoadAssetAtPath<GLSEventAppearanceSO>("Assets/__Scripts/Beatmap/Appearances/GLSEventAppearanceSO.asset")
                    .SetAppearance(instance.GetComponent<GLSEventContainer>());
            }
        }

        // A single Y lane isolates extension behavior from filters and rotation distribution.
        private static BaseLightRotationEventBoxGroup CreateGroup(int id)
        {
            var group = BeatmapFactory.Clone(new BaseLightRotationEventBoxGroup
            {
                ID = id,
                JsonTime = 4f,
                Boxes = new()
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = (int)Axis.Y,
                Events = new[] { new BaseLightRotationBase { Rotation = 30f, Easing = (int)EaseType.Linear } }
                    }
                }
            });
            group.RecomputeSongBpmTime();
            return group;
        }
    }
}
