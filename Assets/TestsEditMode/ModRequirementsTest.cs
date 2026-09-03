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
            var tracksDefinition = ScriptableObject.CreateInstance<TracksDefinitionSO>();
            tracksDefinition.Register(
                new TrackDefinitionBasic
                {
                    Type = (int)EventTypeValue.Event9,
                    Components = BasicEventComponent.SmoothStepRingZoom
                });
            _difficulty.RuntimeTracksDefinition = tracksDefinition;
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
            var tracksDefinition = ScriptableObject.CreateInstance<TracksDefinitionSO>();
            tracksDefinition.Register(new TrackDefinitionBasic { Type = 9, Components = component });
            _difficulty.RuntimeTracksDefinition = tracksDefinition;
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
