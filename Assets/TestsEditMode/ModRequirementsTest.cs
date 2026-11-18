using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Info;
using Beatmap.V3;
using Beatmap.V3.Customs;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    // The fixture covers requirements across multiple mods, so its name must not imply Heck-only coverage.
    public class ModRequirementsTest
    {
        // For use in PlayMode
        public void TestEverything()
        {
        }

        private BaseDifficulty _difficulty;
        private InfoDifficulty _infoDifficulty;

        private HeckRequirementCheck _chromaReq, _noodleReq;
        private RequirementCheck _beatToTheFutureReq, _chromaGLSReq;

        [OneTimeSetUp]
        public void SetupReqs()
        {
            _chromaReq = new ChromaReq();
            _noodleReq = new NoodleExtensionsReq();
            _beatToTheFutureReq = new BeatToTheFutureReq();
            _chromaGLSReq = new ChromaGLSReq();
        }

        [SetUp]
        public void SetupMop()
        {
            Settings.Instance.MapVersion = 3;
            _difficulty = new BaseDifficulty();
            _infoDifficulty = new InfoDifficulty(new InfoDifficultySet());
        }

        [Test]
        public void UnusedTracksDoNotRequireMods()
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "I am unused"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "1",
                        ["color"] = 0,
                        ["dissolve"] = 0,
                    }
                },
                new BaseCustomEvent
                {
                    Type = "AssignPathAnimation",
                    Data = new JSONObject
                    {
                        ["track"] = "2",
                        ["color"] = 0,
                        ["dissolve"] = 0,
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void GLSCustomColorsSuggestChromaGLSInsteadOfChroma()
        {
            _difficulty.LightColorEventBoxGroups = new List<BaseLightColorEventBoxGroup>
            {
                new()
                {
                    Boxes = new List<BaseLightColorEventBox>
                    {
                        new()
                        {
                            Events = new[]
                            {
                                new BaseLightColorBase
                                {
                                    CustomData = new JSONObject
                                    {
                                        ["color"] = CreateColorArray()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void GLSBoxDistributionsSuggestChromaGLSInsteadOfChroma(
            [Values(3, 4)] int version,
            [Values("colorDistributions", "strobeColorDistributions")] string key)
        {
            Settings.Instance.MapVersion = version;
            var box = (BaseLightColorEventBox)AddGLSRequirementBox("Color");
            box.SetCustomData(new JSONObject { [key] = JSON.Parse(@"[""h,0.5,L""]") });
            Assert.That(box.Events[0].IsChroma(), Is.False);

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void GLSBoxDistributionSuggestionTracksRemoval(
            [Values(3, 4)] int version,
            [Values("colorDistributions", "strobeColorDistributions")] string key)
        {
            Settings.Instance.MapVersion = version;
            var box = (BaseLightColorEventBox)AddGLSRequirementBox("Color");
            box.SetCustomData(new JSONObject { [key] = JSON.Parse(@"[""h,0.5,L""]") });

            RefreshChromaGLSRequirements();
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));

            box.ColorDistributions = System.Array.Empty<string>();
            box.StrobeColorDistributions = System.Array.Empty<string>();
            RefreshChromaGLSRequirements();

            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));
        }

        [Test]
        public void GLSBoxMetadataAndEmptyDistributionsDoNotSuggestChromaGLS(
            [Values(3, 4)] int version,
            [Values(
                @"{""unrelated"":true}",
                @"{""colorDistributions"":[]}",
                @"{""strobeColorDistributions"":[]}")]
            string customData)
        {
            Settings.Instance.MapVersion = version;
            var box = (BaseLightColorEventBox)AddGLSRequirementBox("Color");
            box.SetCustomData(JSON.Parse(customData));

            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // Extended easings suggest ChromaGLS for all four GLS node types. Only BeatToTheFuture's V4 upper
        // walls may add a requirement. Sample both ends and the middle of the extended-easing range.
        [Test]
        public void ExtendedGLSNodeEasingsRequireChromaGLSWhenSerialized(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Values(4, 11, 18)] int easing)
        {
            Settings.Instance.MapVersion = version;
            AddGLSRequirementBox(kind, easing);

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // ExtendedGLSDistributionEasingsRequireChromaGLS protects the box-only path: the game consumes
        // distribution easing through the same converter even when every child node uses vanilla easing.
        [Test]
        public void ExtendedGLSDistributionEasingsRequireChromaGLS(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Values(4, 11, 18)] int easing)
        {
            Settings.Instance.MapVersion = version;
            AddGLSRequirementBox(kind, distributionEasing: easing);

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // Native easings need no mod suggestion. V3 color stores known non-native curves in ChromaGLS
        // customData; sample the serialized range boundaries, including native IDs 3 and 19 on either side of
        // IDs 4-18.
        [Test]
        public void VanillaGLSEasingRequirementsRespectV3ColorSchema(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Values(-1, 0, 1, 3, 19, 30, 31, 99, 100, 102, 103)] int easing)
        {
            Settings.Instance.MapVersion = version;
            AddGLSRequirementBox(kind, easing, easing);

            var usesV3ColorExtension = version == 3 && kind == "Color"
                && (easing >= 1 && easing <= 30 || easing >= 100 && easing <= 102);
            Assert.AreEqual(usesV3ColorExtension
                    ? RequirementCheck.RequirementType.Suggestion
                    : RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // ColorEasingSerializationDeterminesChromaGLSRequirement suggests the plugin for both the V3
        // custom extension and V4's native numeric field, not merely for selecting a curve in the UI.
        [Test]
        public void ColorEasingSerializationDeterminesChromaGLSRequirement([Values(4, 11, 18)] int easing)
        {
            var box = (BaseLightColorEventBox)AddGLSRequirementBox("Color", easing);
            var color = box.Events[0];
            var v3Color = V3LightColorBase.GetFromJson(V3LightColorBase.ToJson(color));
            Assert.AreEqual(easing, v3Color.Easing);
            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));

            Settings.Instance.MapVersion = 4;
            var serialized = Beatmap.V4.V4CommonData.LightColorEvent.FromBaseLightColorEvent(color).ToJson();
            var v4Color = Beatmap.V4.V4CommonData.LightColorEvent.GetFromJson(serialized);
            Assert.AreEqual(easing, v4Color.Easing);
            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // An Extension node still suggests ChromaGLS when its selected easing is not native.
        [Test]
        public void V3ColorUsePreviousCustomEasingRequiresChromaGLS([Values(0, 1)] int usePrevious)
        {
            var box = (BaseLightColorEventBox)AddGLSRequirementBox("Color", (int)EaseType.InCircular);
            box.Events[0].UsePrevious = usePrevious;
            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));

            box.Events[0].Easing = (int)EaseType.Linear;
            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // Recalculate suggestions on save after edits or deletion. Extended easings must demote a stale
        // ChromaGLS requirement to a suggestion.
        [Test]
        public void ExtendedGLSEasingRequirementTracksEditsAndRemoval(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Values(false, true)] bool distribution)
        {
            Settings.Instance.MapVersion = version;
            _infoDifficulty.CustomRequirements.Add("ChromaGLS");
            _infoDifficulty.CustomRequirements.Add("ChromaGLS");
            _infoDifficulty.CustomSuggestions.Add("ChromaGLS");
            _infoDifficulty.CustomRequirements.Add("UnrelatedPlugin");
            RefreshChromaGLSRequirements();
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));

            var box = AddGLSRequirementBox(kind, distribution ? 0 : 4, distribution ? 4 : 0);
            RefreshChromaGLSRequirements();
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));

            if (distribution)
                box.Easing = (int)EaseType.Linear;
            else
                box.ClearEvents();

            RefreshChromaGLSRequirements();
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomRequirements, Does.Contain("UnrelatedPlugin"));
        }

        // ExtendedGLSEasingRequirementOverridesSuggestions verifies that an extended easing no longer
        // promotes the plugin to a requirement and that removing that easing leaves the cosmetic suggestion untouched.
        [Test]
        public void ExtendedGLSEasingRequirementOverridesSuggestions([Values(false, true)] bool ringZoom)
        {
            TrackDefinitionsSO tracksDefinition = null;
            try
            {
                if (ringZoom)
                {
                    tracksDefinition = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
                    tracksDefinition.Register(new TrackDefinitionBasic
                    {
                        Type = 9,
                        Components = BasicEventComponent.SmoothStepRingZoom
                    });
                    _difficulty.RuntimeTrackDefinitions = tracksDefinition;
                    _difficulty.Events.Add(new BaseEvent { Type = 9, CustomStep = 1.5f });
                }
                else
                {
                    var colorBox = (BaseLightColorEventBox)AddGLSRequirementBox("Color");
                    colorBox.Events[0].CustomData = new JSONObject { ["color"] = CreateColorArray() };
                }

                RefreshChromaGLSRequirements();
                Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));
                var box = AddGLSRequirementBox("Translation", (int)EaseType.InOutExponential);
                RefreshChromaGLSRequirements();
                Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
                Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));

                box.ClearEvents();
                RefreshChromaGLSRequirements();
                Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
                Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));
            }
            finally
            {
                _difficulty.RuntimeTrackDefinitions = null;
                if (tracksDefinition != null)
                    Object.DestroyImmediate(tracksDefinition);
            }
        }

        // Inspect serialized groups only. Placement defaults and unused FloatFX pool entries do not need mod
        // suggestions.
        [Test]
        public void UnreferencedExtendedEasingDoesNotRequireChromaGLS()
        {
            Settings.Instance.MapVersion = 4;
            var box = AddGLSRequirementBox("FloatFX", (int)EaseType.InCubic);
            _difficulty.FxEventsCollection.FloatFxEvents = ((BaseVfxEventEventBox)box).Events;
            _difficulty.VfxEventBoxGroups.Clear();
            _difficulty.NJSEvents.Add(new BaseNJSEvent { Easing = (int)EaseType.InCubic });

            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // V2OmittedGLSEasingsDoNotRequireChromaGLS prevents unsaved GLS data from advertising the mod
        // after switching to the older format, which has no GLS event-box schema at all.
        [Test]
        public void V2OmittedGLSEasingsDoNotRequireChromaGLS()
        {
            Settings.Instance.MapVersion = 2;
            AddGLSRequirementBox("Color", 4, 4);
            AddGLSRequirementBox("Rotation", 7, 7);
            AddGLSRequirementBox("Translation", 10, 10);
            AddGLSRequirementBox("FloatFX", 18, 18);

            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // AddGLSRequirementBox attaches only authoritative typed beatmap data so the regression suite
        // never depends on loaded visuals, the selected easing menu, or an event-order cache refresh.
        private BaseEventBox AddGLSRequirementBox(string kind, int nodeEasing = 0, int distributionEasing = 0)
        {
            BaseEventBox box;
            switch (kind)
            {
                case "Color":
                    var colorBox = new BaseLightColorEventBox
                    {
                        Events = new[] { new BaseLightColorBase { Easing = nodeEasing } }
                    };
                    _difficulty.LightColorEventBoxGroups.Add(new BaseLightColorEventBoxGroup
                    {
                        Boxes = new List<BaseLightColorEventBox> { colorBox }
                    });
                    box = colorBox;
                    break;
                case "Rotation":
                    var rotationBox = new BaseLightRotationEventBox
                    {
                        Events = new[] { new BaseLightRotationBase { Easing = nodeEasing } }
                    };
                    _difficulty.LightRotationEventBoxGroups.Add(new BaseLightRotationEventBoxGroup
                    {
                        Boxes = new List<BaseLightRotationEventBox> { rotationBox }
                    });
                    box = rotationBox;
                    break;
                case "Translation":
                    var translationBox = new BaseLightTranslationEventBox
                    {
                        Events = new[] { new BaseLightTranslationBase { Easing = nodeEasing } }
                    };
                    _difficulty.LightTranslationEventBoxGroups.Add(new BaseLightTranslationEventBoxGroup
                    {
                        Boxes = new List<BaseLightTranslationEventBox> { translationBox }
                    });
                    box = translationBox;
                    break;
                default:
                    Assert.AreEqual("FloatFX", kind);
                    var fxBox = new BaseVfxEventEventBox
                    {
                        Events = new[] { new BaseFxEventFloat { Easing = nodeEasing } }
                    };
                    _difficulty.VfxEventBoxGroups.Add(new BaseVfxEventEventBoxGroup
                    {
                        Boxes = new List<BaseVfxEventEventBox> { fxBox }
                    });
                    box = fxBox;
                    break;
            }

            box.Easing = distributionEasing;
            return box;
        }

        // RefreshChromaGLSRequirements isolates only the check under test while exercising the production
        // save-list update; restoring the shared registry/settings avoids leaking state into other fixtures.
        private void RefreshChromaGLSRequirements() => RefreshRequirements(_chromaGLSReq);

        // TrueHSVSuggestionUsesFinalSaveRequirements reuses the same isolated save lifecycle with either registry order.
        private void RefreshRequirements(params RequirementCheck[] checks)
        {
            var originalAutomatic = Settings.Instance.AutomaticModRequirements;
            var originalChecks = new HashSet<RequirementCheck>(RequirementCheck.requirementsAndSuggestions);
            try
            {
                Settings.Instance.AutomaticModRequirements = true;
                RequirementCheck.requirementsAndSuggestions.Clear();
                // Run the requested production checks without duplicating fixture setup and shared-state restoration.
                foreach (var check in checks)
                    RequirementCheck.RegisterRequirement(check);
                _infoDifficulty.RefreshRequirementsAndWarnings(_difficulty);
            }
            finally
            {
                Settings.Instance.AutomaticModRequirements = originalAutomatic;
                RequirementCheck.requirementsAndSuggestions.Clear();
                RequirementCheck.requirementsAndSuggestions.UnionWith(originalChecks);
            }
        }

        [Test]
        public void TrueHSVBasicEventsSuggestBeatToTheFutureOnSave([Values(2, 3, 4)] int version)
        {
            Settings.Instance.MapVersion = version;
            _difficulty.Events.Add(new BaseEvent { Type = 2, Value = 1, CustomLerpType = "TrueHSV" });
            RefreshTrueHSVRequirements();

            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("BeatToTheFuture"));
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("BeatToTheFuture"));
        }

        // OtherBasicLerpModesDoNotSuggestBeatToTheFuture prevents ordinary Chroma HSV/RGB, miscased
        // spellings, and unknown values from advertising the extension.
        [TestCase(null)]
        [TestCase("RGB")]
        [TestCase("HSV")]
        [TestCase("unknown")]
        [TestCase("trueHSV")]
        public void OtherBasicLerpModesDoNotSuggestBeatToTheFuture(string lerpType)
        {
            _difficulty.Events.Add(new BaseEvent { Type = 2, Value = 1, CustomLerpType = lerpType });
            RefreshTrueHSVRequirements();
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("BeatToTheFuture"));
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("BeatToTheFuture"));
        }

        // TrueHSVSuggestionTracksEditsAndRemoval exercises the save refresh rather than a parallel detector, including stale duplicates.
        [Test]
        public void TrueHSVSuggestionTracksEditsAndRemoval()
        {
            var evt = new BaseEvent { Type = 2, Value = 1, CustomLerpType = "TrueHSV" };
            _difficulty.Events.Add(evt);
            _infoDifficulty.CustomSuggestions.Add("UnrelatedPlugin");
            _infoDifficulty.CustomSuggestions.Add("BeatToTheFuture");
            _infoDifficulty.CustomSuggestions.Add("BeatToTheFuture");
            RefreshTrueHSVRequirements();
            Assert.That(_infoDifficulty.CustomSuggestions.FindAll(x => x == "BeatToTheFuture"), Has.Count.EqualTo(1));

            evt.CustomLerpType = "HSV";
            RefreshTrueHSVRequirements();
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("BeatToTheFuture"));
            evt.CustomLerpType = "TrueHSV";
            RefreshTrueHSVRequirements();
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("BeatToTheFuture"));
            _difficulty.Events.Clear();
            RefreshTrueHSVRequirements();
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("BeatToTheFuture"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("UnrelatedPlugin"));
        }

        // TrueHSV suggests BeatToTheFuture even if the map previously listed it as required. ChromaGLS is
        // also only a suggestion and cannot suppress it.
        [TestCase("BeatToTheFuture")]
        [TestCase("ChromaGLS")]
        public void TrueHSVSuggestionIsCoveredByExistingRequirements(string requirement)
        {
            _difficulty.Events.Add(new BaseEvent { Type = 2, Value = 1, CustomLerpType = "TrueHSV" });
            _infoDifficulty.CustomRequirements.Add(requirement);
            _infoDifficulty.CustomSuggestions.Add("BeatToTheFuture");
            RefreshTrueHSVRequirements();
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("BeatToTheFuture"));
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("BeatToTheFuture"));
            if (requirement == "ChromaGLS")
                Assert.That(_infoDifficulty.CustomRequirements, Does.Contain("ChromaGLS"));
        }

        // Keep both mod suggestions regardless of detection order.
        [TestCase(false)]
        [TestCase(true)]
        public void TrueHSVSuggestionUsesFinalSaveRequirements(bool chromaFirst)
        {
            _difficulty.Events.Add(new BaseEvent { Type = 2, Value = 1, CustomLerpType = "TrueHSV" });
            var box = AddGLSRequirementBox("Translation", (int)EaseType.InCubic);
            RefreshTrueHSVRequirements(true, chromaFirst);
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("BeatToTheFuture"));
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));

            box.ClearEvents();
            RefreshTrueHSVRequirements(true, chromaFirst);
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("BeatToTheFuture"));

            _difficulty.NJSEvents.Add(new BaseNJSEvent());
            RefreshTrueHSVRequirements(true, chromaFirst);
            Assert.That(_infoDifficulty.CustomRequirements, Does.Contain("BeatToTheFuture"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("BeatToTheFuture"));
        }

        // Restrict the save registry to the participating checks, preserving the actual refresh order and restoring shared editor state.
        private void RefreshTrueHSVRequirements(bool includeChroma = false, bool chromaFirst = false)
        {
            if (!includeChroma)
                RefreshRequirements(_beatToTheFutureReq);
            else if (chromaFirst)
                RefreshRequirements(_chromaGLSReq, _beatToTheFutureReq);
            else
                RefreshRequirements(_beatToTheFutureReq, _chromaGLSReq);
        }

        [Test]
        public void PreservedV3VNJSRequiresBeatToTheFuture()
        {
            // Flat VNJS has no OEM V3 runtime path, so preserving even one event must require BeatToTheFuture.
            _difficulty.NJSEvents = new List<BaseNJSEvent> { new() };

            Assert.AreEqual(
                RequirementCheck.RequirementType.Requirement,
                _beatToTheFutureReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            // Moving the runtime owner must not leave the same VNJS data attached to ChromaGLS as well.
            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void OmittedV3VNJSDoesNotRequireBeatToTheFuture()
        {
            // The converter's No path does not write VNJS, so those in-memory events alone must not declare an unnecessary requirement.
            _difficulty.SaveVNJSEventsInV3 = false;
            _difficulty.NJSEvents = new List<BaseNJSEvent> { new() };

            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _beatToTheFutureReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // V3 saves containing raw V4 y=3/4 walls need BeatToTheFuture even without VNJS, so automatic requirement
        // refresh must tag BeatToTheFuture and must not fall back to ChromaGLS or Mapping Extensions.
        [TestCase(3)]
        [TestCase(4)]
        public void V3UpperLaneWallsRequireBeatToTheFutureOnSave(int posY)
        {
            _difficulty.Obstacles = new List<BaseObstacle>
            {
                new()
                {
                    PosY = posY,
                    Width = 1,
                    Height = 1
                }
            };

            Assert.AreEqual(
                RequirementCheck.RequirementType.Requirement,
                _beatToTheFutureReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                new MappingExtensionsReq().IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void SmoothStepRingZoomCustomStepSuggestsChromaGLS()
        {
            // Requirement detection uses the same component metadata as Basic Event editing.
            var trackDefinitions = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            trackDefinitions.Register(
                new TrackDefinitionBasic
                {
                    Type = (int)EventTypeValue.Event9,
                    Components = BasicEventComponent.SmoothStepRingZoom
                });
            _difficulty.RuntimeTrackDefinitions = trackDefinitions;
            _difficulty.Events = new List<BaseEvent>
            {
                new()
                {
                    Type = (int)EventTypeValue.Event9,
                    CustomStep = 1.5f
                }
            };

            Assert.AreEqual(
                RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase(BasicEventComponent.SmoothStepRingZoom, false)]
        [TestCase(BasicEventComponent.RingZoom, true)]
        public void OtherRingZoomDataDoesNotSuggestChromaGLS(BasicEventComponent component, bool hasCustomStep)
        {
            // Only a custom step on the distinct smooth-step component belongs to ChromaGLS.
            var trackDefinitions = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            trackDefinitions.Register(new TrackDefinitionBasic { Type = 9, Components = component });
            _difficulty.RuntimeTrackDefinitions = trackDefinitions;
            _difficulty.Events = new List<BaseEvent>
            {
                new()
                {
                    Type = 9,
                    CustomStep = hasCustomStep ? 1.5f : null
                }
            };

            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void GLSAndBasicEventCustomColorsDeclareBothChromaMods()
        {
            _difficulty.Events = new List<BaseEvent>
            {
                new()
                {
                    Type = (int)EventTypeValue.Event0,
                    CustomData = new JSONObject
                    {
                        ["color"] = CreateColorArray()
                    }
                }
            };
            _difficulty.LightColorEventBoxGroups = new List<BaseLightColorEventBoxGroup>
            {
                new()
                {
                    Boxes = new List<BaseLightColorEventBox>
                    {
                        new()
                        {
                            Events = new[]
                            {
                                new BaseLightColorBase
                                {
                                    CustomData = new JSONObject
                                    {
                                        ["color"] = CreateColorArray()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }


        [TestCase("AnimateComponent")]
        public void TrackTypeAlwaysRequiresOnlyChroma(string trackType)
        {
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = trackType,
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        ["dissolve"] = 0
                    }
                }
            };

            Assert.AreNotEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("AssignTrackParent")]
        [TestCase("AssignPlayerToTrack")]
        public void TrackTypeAlwaysRequiresOnlyNoodle(string trackType)
        {
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = trackType,
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        ["color"] = 0
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }
        
        [Test]
        public void AssignTrackParentAlwaysRequiresNoodle()
        {
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AssignTrackParent",
                    Data = new JSONObject
                    {
                        ["parentTrack"] = "parent",
                        ["childrenTracks"] = new JSONArray
                        {
                            [0] = "child"
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("position", 0)]
        [TestCase("dissolve", 1)]
        [TestCase("interactable", 1)]
        public void TrackWithUsedNoodlePropertyRequiresNoodle(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "3"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        [property] = value
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("color", 0)]
        public void TrackWithUsedChromaPropertySuggestsChroma(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "3"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        [property] = value
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("hafsdhklsdf", 0)]
        public void TrackWithGarbagePropertyRequiresNothing(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "3"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        [property] = value
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void TrackWithArrayWorks()
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = new JSONArray { [0] = "2", [1] = "3" }
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        ["color"] = 0,
                        ["dissolve"] = 0
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void NoteWithColorAnimationSuggestsChroma()
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["animation"] = new JSONObject
                        {
                            ["color"] = 0
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("position", 0)]
        [TestCase("dissolve", 1)]
        [TestCase("interactable", 1)]
        public void NoteWithGameplayAnimationRequiresNoodle(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["animation"] = new JSONObject
                        {
                            [property] = value
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // SimpleJSON JSONArray requires values to be appended through Add rather than a collection initializer.
        private static JSONArray CreateColorArray()
        {
            var color = new JSONArray();
            color.Add(1f);
            color.Add(0f);
            color.Add(0f);
            return color;
        }
    }
}
