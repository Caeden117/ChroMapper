using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Info;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChromaObjectColorRoutingTest : TestBase
    {
        private const string FamilyKey = "routingFamily";
        private bool previousAnimations;
        private UIMode uiMode;
        private CameraManager cameras;
        private bool expectsMissingPointDuringTeardown;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator StaticCustomColorsReachEveryObjectFamily()
        {
            yield return Load(ColorMap(false, 0));
            yield return CheckFamilies(new Color(0.9f, 0.8f, 0.7f, 1f));
        }

        [UnityTest]
        public IEnumerator ObjectPathColorReplacesTrackPathBeforeMultiplyingGlobalColor()
        {
            yield return Load(ColorMap(true, 1));
            yield return CheckFamilies(new Color(0.48f, 0.49f, 0.48f, 1f));
        }

        [UnityTest]
        public IEnumerator FiveGlobalColorTracksReachEveryObjectFamily()
        {
            yield return Load(ColorMap(false, 5));
            yield return CheckFamilies(new Color(0.32768f, 0.16807f, 0.07776f, 1f));
        }

        [UnityTest]
        public IEnumerator FiveTrackPathColorsMultiplyWithoutAnObjectPath()
        {
            var data = ColorMap(false, 5);
            foreach (var ev in data["customData"]["customEvents"].Children)
                ev["t"] = "AssignPathAnimation";

            yield return Load(data);
            yield return CheckFamilies(new Color(0.32768f, 0.16807f, 0.07776f, 1f));
        }

        [UnityTest]
        public IEnumerator MissingObjectColorPointDefinitionKeepsTrackPathColor()
        {
            var data = ColorMap(false, 1);
            data["customData"]["customEvents"][0]["t"] = "AssignPathAnimation";
            foreach (var arrayKey in new[] { "colorNotes", "bombNotes", "obstacles", "sliders", "burstSliders" })
            {
                foreach (var item in data[arrayKey].Children)
                    item["customData"]["animation"] = new JSONObject { ["color"] = "missingObjectColor" };

            }

            // Loading the editing grid samples the wall color before gameplay objects are spawned.
            ExpectMissingPoint("Obstacle", 1);
            foreach (var sample in new[]
                     {
                         (Family: "Note", X: 0), (Family: "Note", X: 2), (Family: "Note", X: 3),
                         (Family: "Arc", X: 0), (Family: "Chain", X: 2),
                         (Family: "Obstacle", X: 1)
                     })
            {
                ExpectMissingPoint(sample.Family, sample.X);
            }

            yield return Load(data);
            expectsMissingPointDuringTeardown = true;
            yield return CheckFamilies(new Color(0.8f, 0.7f, 0.6f, 1f));
        }

        private static void ExpectMissingPoint(string family, int x) =>
            LogAssert.Expect(LogType.Error, $"Could not find point definition [missingObjectColor]. {family} at beat 10, position ({x}, 0), track [\"objectColor0\"]. Property [color], point definition [missingObjectColor]");

        [UnityTest]
        public IEnumerator ObjectColorPathsUseNativeFamilyAndSliceClocks()
        {
            var data = ColorMap(false, 0);
            foreach (var arrayKey in new[] { "colorNotes", "bombNotes", "obstacles", "sliders", "burstSliders" })
            {
                foreach (var item in data[arrayKey].Children)
                    item["customData"]["animation"] = new JSONObject { ["color"] = "linearObjectColor" };

            }

            data["customData"]["pointDefinitions"] = new JSONObject
            {
                ["linearObjectColor"] = JSON.Parse("[[0,0,0,1,0],[1,1,1,1,1]]")
            };
            yield return Load(data);
            var failures = new List<string>();
            foreach (var beat in new[] { 9f, 9.75f, 9f })
            {
                Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
                yield return null;
                yield return null;
                foreach (var sample in new[]
                         {
                             (Kind: ObjectType.Note, Family: "note"), (Kind: ObjectType.Note, Family: "bomb"),
                             (Kind: ObjectType.Obstacle, Family: "wall"), (Kind: ObjectType.Arc, Family: "arc"),
                             (Kind: ObjectType.Chain, Family: "chain")
                         })
                {
                    var container = BeatmapObjectContainerCollection.GetCollectionForType(sample.Kind).LoadedContainers.Values
                        .Single(item => item.ObjectData.CustomData.GetValueOrDefault(FamilyKey, null)?.Value == sample.Family);
                    var grid = (BaseGrid)container.ObjectData;
                    Assert.That(grid.HalfJumpDuration, Is.EqualTo(2f), "The independent native oracle assumes a four-beat jump at120BPM.");
                    if (container is ChainContainer chain)
                    {
                        var nodes = chain.Nodes.Where(node => node.gameObject.activeSelf).ToArray();
                        Assert.That(nodes.Length, Is.EqualTo(2), "Native sliceCount3 creates slices at11 and12, separately from the head note.");
                        for (var i = 0; i < nodes.Length; i++)
                        {
                            var cutBeat = 10f + ((i + 1f) / 2f) * 2f;
                            var normalized = Mathf.Clamp01((beat - (cutBeat - 2f)) / 4f);
                            RecordClockColor(
                                failures,
                                nodes[i].ModelController.MpbController.Renderers,
                                normalized,
                                $"b{beat} chain slice{i + 1}");
                        }
                    }

                    else
                    {
                        var denominator = sample.Family switch
                        {
                            "wall" => 8f,
                            "arc" => 9f,
                            _ => 4f
                        };
                        var normalized = Mathf.Clamp01((beat - 8f) / denominator);
                        RecordClockColor(failures, BodyRenderers(container), normalized, $"b{beat} {sample.Family}");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void RecordClockColor(
            List<string> failures,
            IEnumerable<Renderer> renderers,
            float normalized,
            string sample)
        {
            var block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(block);
                var actual = block.GetColor("_Color");
                if (Vector3.Distance(new Vector3(actual.r, actual.g, actual.b), Vector3.one * normalized) > 0.0001f)
                    failures.Add($"{sample}: rendered RGB={actual}, native color clock={normalized}.");

            }
        }

        private static readonly string[] ColorNullEventTypes = { "AnimateTrack", "AssignPathAnimation" };

        [UnityTest]
        public IEnumerator TrackColorNullPreservesChronologyAndLaterWrites(
            [ValueSource(nameof(ColorNullEventTypes))] string eventType)
        {
            yield return CheckNullHistory(eventType, ColorNullMap(eventType));
        }

        private IEnumerator CheckNullHistory(string eventType, JSONNode data)
        {
            yield return Load(data);
            var failures = new List<string>();
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousClockEnabled = clock.enabled;
            clock.enabled = false;
            try
            {
                var stages = new[]
                {
                    (Beat: 6.5f, Group: "Early", Color: new Color(0.64f, 0.49f, 0.36f, 1f)),
                    (Beat: 7.5f, Group: "Early", Color: new Color(0.8f, 0.7f, 0.6f, 1f)),
                    (Beat: 9.5f, Group: "Late", Color: new Color(0.8f, 0.7f, 0.6f, 1f)),
                    (Beat: 10.5f, Group: "Late", Color: new Color(0.16f, 0.21f, 0.24f, 1f))
                };
                foreach (var stage in stages)
                {
                    clock.MoveToJsonTime(stage.Beat);
                    yield return null;
                    yield return null;
                    RecordNullColor(failures, stage.Beat, stage.Group, stage.Color, "cold " + eventType);
                }

                clock.MoveToJsonTime(0f);
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 0f);
                var seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
                foreach (var stage in stages)
                {
                    seconds.SetValue(clock, clock.GetSecondsFromBeat(stage.Beat));
                    yield return null;
                    yield return null;
                    RecordNullColor(failures, stage.Beat, stage.Group, stage.Color, "playing " + eventType);
                }

                TestUtils.PauseDeterministicPlayback(clock);
                for (var i = stages.Length - 1; i >= 0; i--)
                {
                    var stage = stages[i];
                    clock.MoveToJsonTime(stage.Beat);
                    yield return null;
                    yield return null;
                    RecordNullColor(failures, stage.Beat, stage.Group, stage.Color, "reverse " + eventType);
                }
            }

            finally
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator LastColorTrackNullRetainsBornObjectsButNotFutureObjects(
            [ValueSource(nameof(ColorNullEventTypes))] string eventType)
        {
            var data = ColorNullMap(eventType);
            foreach (var arrayKey in new[] { "colorNotes", "bombNotes", "obstacles", "sliders", "burstSliders" })
            {
                foreach (var item in data[arrayKey].Children)
                    item["customData"]["track"] = "objectColor0";

            }

            foreach (var ev in data["customData"]["customEvents"].Children)
            {
                if (ev["b"].AsFloat == 7f)
                {
                    // Both early slices are born before cancellation. Later slices are born afterwards.
                    ev["b"] = 7.25f;
                }
            }

            yield return Load(data);
            var failures = new List<string>();
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousClockEnabled = clock.enabled;
            clock.enabled = false;
            try
            {
                var seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
                foreach (var pass in new[] { 0, 1 })
                {
                    TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 6.5f);
                    foreach (var stage in new[]
                             {
                                 (Beat: 6.5f, Group: "Early", Color: new Color(0.8f, 0.7f, 0.6f, 1f)),
                                 (Beat: 7.1f, Group: "Early", Color: new Color(0.8f, 0.7f, 0.6f, 1f)),
                                 (Beat: 7.5f, Group: "Early", Color: new Color(0.8f, 0.7f, 0.6f, 1f)),
                                 (Beat: 9.5f, Group: "Late", Color: new Color(0.9f, 0.8f, 0.7f, 1f)),
                                 (Beat: 10.5f, Group: "Late", Color: new Color(0.2f, 0.3f, 0.4f, 1f))
                             })
                    {
                        seconds.SetValue(clock, clock.GetSecondsFromBeat(stage.Beat));
                        yield return null;
                        yield return null;
                        RecordNullColor(
                            failures,
                            stage.Beat,
                            stage.Group,
                            stage.Color,
                            $"single-track playing/replay{pass} {eventType}");
                        if (stage.Beat == 7.1f)
                            AssertFutureObjectsHaveNotReachedNativeBirth();
                    }

                    TestUtils.PauseDeterministicPlayback(clock);
                    clock.MoveToJsonTime(6.5f);
                    yield return null;
                    yield return null;
                    RecordNullColor(
                        failures,
                        6.5f,
                        "Early",
                        new Color(0.8f, 0.7f, 0.6f, 1f),
                        $"single-track rewind{pass} {eventType}");
                }
            }

            finally
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator NullObjectColorPathKeepsTrackPathColor()
        {
            var data = ColorMap(false, 1);
            data["customData"]["customEvents"].Add(
                new JSONObject
                {
                    ["b"] = 0f, ["t"] = "AssignPathAnimation",
                    ["d"] = new JSONObject
                    {
                        ["track"] = "objectColor0", ["color"] = JSON.Parse("[0.6,0.7,0.8,1]")
                    }
                });
            foreach (var arrayKey in new[] { "colorNotes", "bombNotes", "obstacles", "sliders", "burstSliders" })
            {
                foreach (var item in data[arrayKey].Children)
                    item["customData"]["animation"] = new JSONObject { ["color"] = JSON.Parse("null") };

            }

            yield return Load(data);
            yield return CheckFamilies(new Color(0.48f, 0.49f, 0.48f, 1f));
        }

        [UnityTest]
        public IEnumerator FirstColorPathUsesItsCurveImmediatelyWhileExistingPathTransitions()
        {
            var failures = new List<string>();
            foreach (var hasPreviousPath in new[] { false, true })
            {
                var data = ColorNullMap("AssignPathAnimation");
                var events = new JSONArray();
                events.Add(
                    new JSONObject
                    {
                        ["b"] = 0f, ["t"] = "AnimateTrack",
                        ["d"] = new JSONObject { ["track"] = "objectColor1", ["color"] = JSON.Parse("[0.8,0.7,0.6,1]") }
                    });
                if (hasPreviousPath)
                    events.Add(
                        new JSONObject
                        {
                            ["b"] = 0f, ["t"] = "AssignPathAnimation",
                            ["d"] = new JSONObject { ["track"] = "objectColor0", ["color"] = JSON.Parse("[0.8,0.7,0.6,1]") }
                        });

                events.Add(
                    new JSONObject
                    {
                        ["b"] = 6f, ["t"] = "AssignPathAnimation",
                        ["d"] = new JSONObject
                        {
                            ["track"] = "objectColor0", ["duration"] = 2f, ["color"] = JSON.Parse("[0.6,0.7,0.8,1]")
                        }
                    });
                data["customData"]["customEvents"] = events;
                yield return Load(data);
                foreach (var beat in new[] { 6.5f, 7.5f, 6.5f })
                {
                    Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
                    yield return null;
                    yield return null;
                    var target = new Color(0.6f, 0.7f, 0.8f, 1f);
                    var path = hasPreviousPath
                        ? Color.Lerp(new Color(0.8f, 0.7f, 0.6f, 1f), target, (beat - 6f) / 2f)
                        : target;
                    RecordNullColor(
                        failures,
                        beat,
                        "Early",
                        path * new Color(0.8f, 0.7f, 0.6f, 1f),
                        hasPreviousPath
                            ? "existing path transition control"
                            : "first path native base");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator ColorPathAfterNullUsesNewCurveImmediatelyDespiteDuration()
        {
            var data = ColorNullMap("AssignPathAnimation");
            foreach (var ev in data["customData"]["customEvents"].Children)
            {
                if (ev["b"].AsFloat == 10f)
                    ev["d"]["duration"] = 2f;

            }

            yield return CheckNullHistory("AssignPathAnimation duration2", data);
        }

        private static void AssertFutureObjectsHaveNotReachedNativeBirth()
        {
            foreach (var kind in new[] { ObjectType.Obstacle, ObjectType.Arc })
            {
                var target = BeatmapObjectContainerCollection.GetCollectionForType(kind).LoadedContainers.Values
                    .Single(
                        container => container.ObjectData.CustomData.GetValueOrDefault(FamilyKey, null)?.Value
                        == (kind == ObjectType.Obstacle
                            ? "wallLate"
                            : "arcLate"));
                var grid = (BaseGrid)target.ObjectData;
                Assert.That(grid.HalfJumpDuration, Is.EqualTo(2f));
                Assert.That(
                    grid.SongBpmTime - 3f,
                    Is.GreaterThan(7.25f),
                    "Native HJD2 plus the one-beat floor lead creates these objects after color cancellation.");
            }
        }

        private static JSONNode ColorNullMap(string eventType)
        {
            var data = ColorMap(false, 2);
            foreach (var arrayKey in new[] { "colorNotes", "bombNotes", "obstacles", "sliders", "burstSliders" })
            {
                var array = data[arrayKey].AsArray;
                var originals = array.Children.ToArray();
                foreach (var item in originals)
                {
                    var family = item["customData"][FamilyKey].Value;
                    item["b"] = 8f;
                    if (item.HasKey("tb"))
                        item["tb"] = 10f;

                    item["customData"][FamilyKey] = family + "Early";
                    var late = item.Clone();
                    late["b"] = 12f;
                    if (late.HasKey("tb"))
                        late["tb"] = 14f;

                    late["customData"][FamilyKey] = family + "Late";
                    if (arrayKey == "obstacles")
                        late["b"] = 11f;

                    if (arrayKey == "burstSliders")
                    {
                        late["b"] = 10f;
                        late["tb"] = 12f;
                    }

                    if (family == "chainHead")
                        late["b"] = 10f;

                    array.Add(late);
                }
            }

            var events = data["customData"]["customEvents"].AsArray;
            foreach (var ev in events.Children)
                ev["t"] = eventType;

            events.Add(
                new JSONObject
                {
                    ["b"] = 7f, ["t"] = eventType,
                    ["d"] = new JSONObject { ["track"] = "objectColor0", ["color"] = JSON.Parse("null") }
                });
            events.Add(
                new JSONObject
                {
                    ["b"] = 10f, ["t"] = eventType,
                    ["d"] = new JSONObject { ["track"] = "objectColor0", ["color"] = JSON.Parse("[0.2,0.3,0.4,1]") }
                });
            return data;
        }

        private static void RecordNullColor(List<string> failures, float beat, string group, Color expected, string route)
        {
            foreach (var sample in new[]
                     {
                         (Kind: ObjectType.Note, Family: "note"), (Kind: ObjectType.Note, Family: "bomb"),
                         (Kind: ObjectType.Obstacle, Family: "wall"), (Kind: ObjectType.Arc, Family: "arc"),
                         (Kind: ObjectType.Chain, Family: "chain")
                     })
            {
                var matches = BeatmapObjectContainerCollection.GetCollectionForType(sample.Kind).LoadedContainers.Values
                    .Where(item => item.ObjectData.CustomData.GetValueOrDefault(FamilyKey, null)?.Value == sample.Family + group)
                    .ToArray();
                Assert.That(matches.Length, Is.EqualTo(1), $"{route} b{beat}: source {sample.Family}{group} must be loaded.");
                var block = new MaterialPropertyBlock();
                var container = matches[0];
                IEnumerable<Renderer> renderers = BodyRenderers(container);
                if (container is ChainContainer chain)
                {
                    // Native slices spawn separately. Compare only nodes whose half-second floor phase has begun.
                    var source = chain.ChainData;
                    renderers = chain.Nodes.Take(source.SliceCount - 1)
                        .Where(
                            (node, index) => Mathf.Lerp(
                                source.JsonTime,
                                source.TailJsonTime,
                                (index + 1f) / (source.SliceCount - 1f)) - 3f <= beat)
                        .SelectMany(node => node.ModelController.MpbController.Renderers);
                }

                var observed = renderers.ToArray();
                Assert.That(observed, Is.Not.Empty, "The native observation window must contain a born slice or family renderer.");
                foreach (var renderer in observed)
                {
                    renderer.GetPropertyBlock(block);
                    var actual = block.GetColor("_Color");
                    if (Vector3.Distance(new Vector3(actual.r, actual.g, actual.b), new Vector3(expected.r, expected.g, expected.b)) > 0.0001f)
                        failures.Add($"{route} b{beat} {sample.Family}{group}: RGB={actual}, native surviving-track product={expected}.");

                }
            }
        }

        [UnityTest]
        public IEnumerator SuggestedNoodleDoesNotChangeNativeChromaColorBirth()
        {
            var data = ColorMap(false, 1);
            foreach (var key in new[] { "colorNotes", "bombNotes", "sliders", "burstSliders" })
                data[key] = new JSONArray();

            data["obstacles"][0]["b"] = 11f;
            data["obstacles"][0]["customData"]["noteJumpMovementSpeed"] = 32f;
            data["customData"]["customEvents"].Add(
                new JSONObject
                {
                    ["b"] = 8.5f,
                    ["t"] = "AnimateTrack",
                    ["d"] = new JSONObject
                    {
                        ["track"] = "objectColor0",
                        ["color"] = JSON.Parse("null")
                    }
                });
            yield return Load(data, new List<string> { "Noodle Extensions" });
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousClockEnabled = clock.enabled;
            clock.enabled = false;
            try
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 8.1f);
                var seconds = typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
                foreach (var beat in new[] { 8.1f, 9.5f })
                {
                    seconds.SetValue(clock, clock.GetSecondsFromBeat(beat));
                    yield return null;
                    yield return null;
                    var wall = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle)
                        .LoadedContainers.Values.OfType<ObstacleContainer>().Single();
                    Assert.That(
                        wall.ObstacleData.HalfJumpDuration,
                        Is.EqualTo(1f),
                        "CM's custom movement preview remains available without a required Noodle capability.");
                    var block = new MaterialPropertyBlock();
                    foreach (var renderer in BodyRenderers(wall))
                    {
                        renderer.GetPropertyBlock(block);
                        var color = block.GetColor("_Color");
                        Assert.That(
                            Vector3.Distance(new Vector3(color.r, color.g, color.b), new Vector3(0.8f, 0.7f, 0.6f)),
                            Is.LessThan(0.0001f),
                            $"b{beat}: suggested Noodle does not replace Chroma's native callback birth8; the born wall retains its color after null8.5.");
                    }
                }
            }

            finally
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }
        }

        [UnityTest]
        public IEnumerator SameNjsReloadRefreshesCurrentJumpParametersForTheNewDifficulty()
        {
            yield return Load(
                ColorMap(false, 0),
                beatsPerMinute: 150f,
                startBeatOffset: 4f,
                noteJumpSpeed: 17f);
            var provider = Object.FindAnyObjectByType<VariableNJSProvider>();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(9f);
            yield return null;
            yield return null;
            Assert.That(provider.NoteJumpSpeed, Is.EqualTo(17f));
            Assert.That(provider.HalfJumpDurationInBeats, Is.EqualTo(6f), "The first difficulty establishes a different jump duration at the same NJS.");
            yield return Load(ColorMap(false, 0), noteJumpSpeed: 17f);
            provider = Object.FindAnyObjectByType<VariableNJSProvider>();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(9f);
            yield return null;
            yield return null;
            Assert.That(provider.BaseHalfJumpDurationInBeats, Is.EqualTo(2f));
            Assert.That(provider.SpawnAheadTime, Is.EqualTo(1.5f).Within(0.00001f));
            Assert.That(
                provider.HalfJumpDurationInBeats,
                Is.EqualTo(2f),
                "Native movement Init resets current half-jump when BPM/offset change, even when NJS remains17.");
            Assert.That(provider.HalfJumpDuration, Is.EqualTo(1f).Within(0.00001f));
            Assert.That(provider.JumpDistance, Is.EqualTo(34f).Within(0.00001f));
        }

        private IEnumerator Load(
            JSONNode data,
            List<string> suggestions = null,
            float beatsPerMinute = 120f,
            float startBeatOffset = 0f,
            float noteJumpSpeed = 16f)
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = true;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = noteJumpSpeed,
                NoteStartBeatOffset = startBeatOffset,
                CustomRequirements = new List<string> { "Chroma" },
                CustomSuggestions = suggestions ?? new List<string>()
            };
            yield return TestUtils.ReloadMap(
                3,
                data,
                beatsPerMinute: beatsPerMinute,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
        }

        private static IEnumerator CheckFamilies(Color expected)
        {
            var failures = new List<string>();
            foreach (var beat in new[] { 9f, 10f, 9f })
            {
                Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
                yield return null;
                yield return null;
                foreach (var sample in new[]
                         {
                             (Kind: ObjectType.Note, Family: "note"), (Kind: ObjectType.Note, Family: "bomb"),
                             (Kind: ObjectType.Obstacle, Family: "wall"), (Kind: ObjectType.Arc, Family: "arc"),
                             (Kind: ObjectType.Chain, Family: "chain")
                         })
                {
                    var collection = BeatmapObjectContainerCollection.GetCollectionForType(sample.Kind);
                    var matches = collection.LoadedContainers.Values
                        .Where(container => container.ObjectData.CustomData.GetValueOrDefault(FamilyKey, null)?.Value == sample.Family)
                        .ToArray();
                    Assert.That(matches.Length, Is.EqualTo(1), $"Source {sample.Family} must be loaded at b{beat}.");
                    var container = matches[0];
                    var renderers = BodyRenderers(container).ToArray();
                    Assert.That(renderers, Is.Not.Empty, $"Source {sample.Family} has no renderer.");
                    var block = new MaterialPropertyBlock();
                    foreach (var renderer in renderers)
                    {
                        renderer.GetPropertyBlock(block);
                        var actual = block.GetColor("_Color");
                        var actualRgb = new Vector3(actual.r, actual.g, actual.b);
                        var expectedRgb = new Vector3(expected.r, expected.g, expected.b);
                        if (Vector3.Distance(actualRgb, expectedRgb) > 0.0001f)
                            failures.Add($"b{beat} {sample.Family}: renderer '{renderer.name}' RGB={actualRgb}, native={expectedRgb}.");

                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static JSONNode ColorMap(bool localPath, int trackCount)
        {
            var map = JSON.Parse(
                @"{
                ""version"":""3.2.0"",
                ""colorNotes"":[{""b"":10,""x"":0,""y"":0,""c"":0,""d"":1},{""b"":10,""x"":2,""y"":0,""c"":1,""d"":1}],
                ""bombNotes"":[{""b"":10,""x"":3,""y"":0}],
                ""obstacles"":[{""b"":10,""x"":1,""y"":0,""d"":4,""w"":1,""h"":2}],
                ""sliders"":[{""b"":10,""c"":0,""x"":0,""y"":0,""d"":1,""mu"":1,""tb"":12,""tx"":0,""ty"":1,""tc"":1,""tmu"":1,""m"":0}],
                ""burstSliders"":[{""b"":10,""c"":1,""x"":2,""y"":0,""d"":1,""tb"":12,""tx"":2,""ty"":1,""sc"":3,""s"":1}]
            }");
            var tracks = new JSONArray();
            var events = new JSONArray();
            for (var i = 0; i < trackCount; i++)
            {
                var name = "objectColor" + i;
                tracks.Add(name);
                events.Add(
                    new JSONObject
                    {
                        ["b"] = 0f, ["t"] = "AnimateTrack",
                        ["d"] = new JSONObject { ["track"] = name, ["color"] = JSON.Parse("[0.8,0.7,0.6,1]") }
                    });
            }

            if (localPath)
                events.Add(
                    new JSONObject
                    {
                        ["b"] = 0f, ["t"] = "AssignPathAnimation",
                        ["d"] = new JSONObject { ["track"] = "objectColor0", ["color"] = JSON.Parse("[0.2,0.3,0.4,1]") }
                    });

            foreach (var sample in new[]
                     {
                         (ArrayKey: "colorNotes", Family: "note"), (ArrayKey: "bombNotes", Family: "bomb"),
                         (ArrayKey: "obstacles", Family: "wall"), (ArrayKey: "sliders", Family: "arc"),
                         (ArrayKey: "burstSliders", Family: "chain")
                     })
            {
                var data = new JSONObject
                {
                    [FamilyKey] = sample.Family,
                    ["color"] = JSON.Parse("[0.9,0.8,0.7,1]")
                };
                if (trackCount > 0)
                    data["track"] = tracks.Clone();

                if (localPath)
                    data["animation"] = new JSONObject { ["color"] = JSON.Parse("[0.6,0.7,0.8,1]") };

                map[sample.ArrayKey][0]["customData"] = data;
            }

            var headData = map["burstSliders"][0]["customData"].Clone();
            headData[FamilyKey] = "chainHead";
            map["colorNotes"][1]["customData"] = headData;
            map["customData"] = new JSONObject { ["customEvents"] = events };
            return map;
        }

        private static IEnumerable<Renderer> BodyRenderers(ObjectContainer container)
        {
            if (container is NoteContainer note)
                return note.ModelController.MpbController.Renderers;

            if (container is ChainContainer chain)
            {
                var authoredCount = chain.ChainData.SliceCount - 1;
                Assert.That(chain.Nodes.Count, Is.GreaterThanOrEqualTo(authoredCount));
                for (var i = 0; i < authoredCount; i++)
                    Assert.That(chain.Nodes[i].ModelController.MpbController.Renderers, Is.Not.Empty);

                // Surplus cached nodes belong to earlier chains. Check every body and LOD authored by this source chain.
                return chain.Nodes.Take(authoredCount).SelectMany(node => node.ModelController.MpbController.Renderers);
            }

            return container.MpbController.Renderers;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);

            if (uiMode != null)
            {
                if (expectsMissingPointDuringTeardown)
                {
                    ExpectMissingPoint("Obstacle", 1);
                    expectsMissingPointDuringTeardown = false;
                }

                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
