using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSEventAxisLaneTest : TestBase
    {
        // Rotation groups must expose missing Y/Z axes as translucent placement lanes without adding authored boxes.
        [Test]
        public void RotationGroupAlwaysDisplaysAllAxisLanes()
        {
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var group = new BaseLightRotationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightRotationBase() }
                    }
                }
            };

            provider.GroupContext = group;

            Assert.AreEqual(3, GetDisplayedLabels(provider).Length);
            Assert.That(GetDisplayedLabels(provider).Single(label => label.text.EndsWith("Y")).color.a, Is.EqualTo(0.5f));
            Assert.That(GetDisplayedLabels(provider).Single(label => label.text.EndsWith("Z")).color.a, Is.EqualTo(0.5f));
            Assert.AreEqual(1, group.Boxes.Count, "View-only axis lanes must not mutate authored rotation boxes.");
        }

        // Translation groups need the same fixed XYZ affordance and half-opacity empty-lane treatment as rotation groups.
        [Test]
        public void TranslationGroupAlwaysDisplaysAllAxisLanes()
        {
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var group = new BaseLightTranslationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightTranslationBase() }
                    }
                }
            };

            provider.GroupContext = group;

            Assert.AreEqual(3, GetDisplayedLabels(provider).Length);
            Assert.That(GetDisplayedLabels(provider).Single(label => label.text.EndsWith("X")).color.a, Is.EqualTo(0.5f));
            Assert.That(GetDisplayedLabels(provider).Single(label => label.text.EndsWith("Y")).color.a, Is.EqualTo(0.5f));
            Assert.AreEqual(1, group.Boxes.Count, "View-only axis lanes must not mutate authored translation boxes.");
        }

        // SyntheticAxisLanesAreReusedAcrossTrackRefreshes prevents accepted wheel edits from allocating replacement XYZ boxes twice per group refresh.
        [Test]
        public void SyntheticAxisLanesAreReusedAcrossTrackRefreshes()
        {
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = new BaseLightRotationEventBoxGroup();
            Assert.True(provider.TryGetDisplayedBox(0, out var firstRotationX));
            Assert.True(provider.TryGetDisplayedBox(1, out var firstRotationY));
            Assert.True(provider.TryGetDisplayedBox(2, out var firstRotationZ));

            provider.GroupContext = new BaseLightTranslationEventBoxGroup();
            Assert.True(provider.TryGetDisplayedBox(0, out var firstTranslationX));
            Assert.True(provider.TryGetDisplayedBox(1, out var firstTranslationY));
            Assert.True(provider.TryGetDisplayedBox(2, out var firstTranslationZ));

            provider.GroupContext = new BaseLightRotationEventBoxGroup();
            Assert.True(provider.TryGetDisplayedBox(0, out var secondRotationX));
            Assert.True(provider.TryGetDisplayedBox(1, out var secondRotationY));
            Assert.True(provider.TryGetDisplayedBox(2, out var secondRotationZ));

            provider.GroupContext = new BaseLightTranslationEventBoxGroup();
            Assert.True(provider.TryGetDisplayedBox(0, out var secondTranslationX));
            Assert.True(provider.TryGetDisplayedBox(1, out var secondTranslationY));
            Assert.True(provider.TryGetDisplayedBox(2, out var secondTranslationZ));

            Assert.AreSame(firstRotationX, secondRotationX);
            Assert.AreSame(firstRotationY, secondRotationY);
            Assert.AreSame(firstRotationZ, secondRotationZ);
            Assert.AreSame(firstTranslationX, secondTranslationX);
            Assert.AreSame(firstTranslationY, secondTranslationY);
            Assert.AreSame(firstTranslationZ, secondTranslationZ);
        }

        // ID-filter lanes already define the intended layout, so an X-only ID set must not gain trailing Y/Z ghosts.
        [Test]
        public void TranslationIdLanesDoNotDisplayMissingAxisGhosts()
        {
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var group = new BaseLightTranslationEventBoxGroup
            {
                Boxes =
                {
                    CreateTranslationIdLane(0),
                    CreateTranslationIdLane(1),
                    CreateTranslationIdLane(2)
                }
            };

            provider.GroupContext = group;

            var labels = GetDisplayedLabels(provider);
            Assert.AreEqual(3, labels.Length);
            Assert.True(labels.All(label => label.text.EndsWith("X")));
            Assert.False(labels.Any(label => label.text.EndsWith("Y")));
            Assert.False(labels.Any(label => label.text.EndsWith("Z")));
            Assert.AreEqual(3, group.Boxes.Count, "Suppressing ghosts must not mutate authored ID lanes.");
        }

        // Saving a partially populated rotation group must discard view-equivalent empty axis/filter boxes.
        [Test]
        public void RotationSerializationOmitsEmptyAxisLanesWhenAContainerLaneExists()
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightRotationBase() }
                    },
                    new BaseLightRotationEventBox { Axis = 1, IsAutomaticAxisLane = true },
                    new BaseLightRotationEventBox { Axis = 2, IsAutomaticAxisLane = true }
                }
            };

            // Authoritative mutation cleanup, rather than V3 serialization, removes disposable empty axes.
            group.PruneEmptyAutomaticAxisLanes();
            var serializedBoxes = V3LightRotationEventBoxGroup.ToJson(group)["e"].AsArray;

            Assert.AreEqual(1, group.Boxes.Count);
            Assert.AreEqual(1, serializedBoxes.Count);
        }

        // Saving a partially populated translation group must not persist empty axis/filter placeholders either.
        [Test]
        public void TranslationSerializationOmitsEmptyAxisLanesWhenAContainerLaneExists()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase() }
                    },
                    new BaseLightTranslationEventBox { Axis = 1, IsAutomaticAxisLane = true },
                    new BaseLightTranslationEventBox { Axis = 2, IsAutomaticAxisLane = true }
                }
            };

            // Authoritative mutation cleanup, rather than V3 serialization, removes disposable empty axes.
            group.PruneEmptyAutomaticAxisLanes();
            var serializedBoxes = V3LightTranslationEventBoxGroup.ToJson(group)["e"].AsArray;

            Assert.AreEqual(1, group.Boxes.Count);
            Assert.AreEqual(1, serializedBoxes.Count);
        }

        // The empty-group exception keeps authored lanes when no node exists anywhere, so a lane-less group is not created by saving.
        [Test]
        public void EntirelyEmptyTransformGroupsRetainTheirAuthoredLanes()
        {
            var rotationGroup = new BaseLightRotationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightRotationEventBox { Axis = 0 },
                    new BaseLightRotationEventBox { Axis = 1 },
                    new BaseLightRotationEventBox { Axis = 2 }
                }
            };
            var translationGroup = new BaseLightTranslationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightTranslationEventBox { Axis = 0 },
                    new BaseLightTranslationEventBox { Axis = 1 },
                    new BaseLightTranslationEventBox { Axis = 2 }
                }
            };

            Assert.AreEqual(3, V3LightRotationEventBoxGroup.ToJson(rotationGroup)["e"].AsArray.Count);
            Assert.AreEqual(3, V3LightTranslationEventBoxGroup.ToJson(translationGroup)["e"].AsArray.Count);
        }

        // Explicitly authored empty lanes, including configured filters, must remain opaque and survive beside populated lanes.
        [Test]
        public void ExplicitEmptyFilterLaneRemainsOpaqueAndSerializable()
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightRotationBase() }
                    },
                    new BaseLightRotationEventBox
                    {
                        Axis = 1,
                        IndexFilter = new BaseIndexFilter
                        {
                            Type = (int)IndexFilterType.StepAndOffset,
                            Param0 = 4,
                            Param1 = 2
                        }
                    }
                }
            };
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = group;
            var labels = GetDisplayedLabels(provider);
            var populatedAlpha = labels.Single(label => label.text.EndsWith("X")).color.a;

            Assert.That(labels.Single(label => label.text.EndsWith("Y")).color.a, Is.EqualTo(populatedAlpha));
            Assert.That(labels.Single(label => label.text.EndsWith("Z")).color.a, Is.EqualTo(populatedAlpha * 0.5f));
            var serializedBoxes = V3LightRotationEventBoxGroup.ToJson(group)["e"].AsArray;
            Assert.AreEqual(2, serializedBoxes.Count);
            Assert.AreEqual(4, serializedBoxes[1]["f"]["p"].AsInt);
            Assert.AreEqual(2, serializedBoxes[1]["f"]["t"].AsInt);
        }

        // Explicit Add-style default lanes are authored too; only automatic XYZ lanes may disappear when empty.
        [Test]
        public void ExplicitDefaultEmptyLaneSurvivesTranslationSerialization()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase() }
                    },
                    new BaseLightTranslationEventBox { Axis = 1 }
                }
            };

            Assert.AreEqual(2, V3LightTranslationEventBoxGroup.ToJson(group)["e"].AsArray.Count);
        }

        // Add Axes must preserve every existing rotation box and node, including multiple authored filters on one axis.
        [Test]
        public void AddAxesPreservesMultipleExistingRotationAxisNodes()
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = 126,
                ID = 126,
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightRotationBase { Rotation = 10 } }
                    },
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        IndexFilter = new BaseIndexFilter
                        {
                            Type = (int)IndexFilterType.StepAndOffset,
                            Param0 = 1
                        },
                        Events = new[] { new BaseLightRotationBase { Rotation = 20 } }
                    },
                    new BaseLightRotationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightRotationBase { Rotation = 30 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);
            var track = new TrackDefinitionGLS { RotationTracks = new[] { true, true, true } };

            var editedGroup = GLSEventBoxCommand.AddAllAxesEventBox(group, track) as BaseLightRotationEventBoxGroup;

            Assert.AreEqual(4, editedGroup.Boxes.Count);
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
            AssertValidEventOwnership(editedGroup);
            Assert.AreEqual(2, editedGroup.Boxes.Count(box => box.Axis == 0));
            Assert.AreEqual(1, editedGroup.Boxes.Count(box => box.Axis == 1));
            Assert.AreEqual(1, editedGroup.Boxes.Count(box => box.Axis == 2));
            CollectionAssert.AreEquivalent(
                new[] { 10f, 20f, 30f },
                editedGroup.Boxes.SelectMany(box => box.Events).Select(evt => evt.Rotation));
            Assert.AreEqual(
                1,
                editedGroup.Boxes.Single(
                    box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset
                           && box.IndexFilter.Param0 == 1).Events.Length);
        }

        // Translation Add Axes must use translation track availability while retaining the node on an existing axis.
        [Test]
        public void AddAxesPreservesTranslationNodeAndUsesTranslationTracks()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 127,
                ID = 127,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase { Translation = 40 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);
            var track = new TrackDefinitionGLS
            {
                RotationTracks = new[] { false, false, false },
                TranslationTracks = new[] { true, false, true }
            };

            var editedGroup = GLSEventBoxCommand.AddAllAxesEventBox(group, track) as BaseLightTranslationEventBoxGroup;

            Assert.AreEqual(2, editedGroup.Boxes.Count);
            Assert.AreEqual(40, editedGroup.Boxes.Single(box => box.Axis == 0).Events.Single().Translation);
            Assert.AreEqual(0, editedGroup.Boxes.Single(box => box.Axis == 2).Events.Length);
        }

        // +Ids must generate the full ID filter set for every unique rotation axis already represented by a lane.
        [Test]
        public void AddIdsUsesEveryExistingRotationAxis()
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = 128,
                ID = 128,
                Boxes =
                {
                    new BaseLightRotationEventBox { Axis = 0 },
                    new BaseLightRotationEventBox { Axis = 1 }
                }
            };
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 2)
                as BaseLightRotationEventBoxGroup;

            Assert.AreEqual(4, editedGroup.Boxes.Count);
            Assert.AreEqual(2, editedGroup.Boxes.Count(box => box.Axis == 0));
            Assert.AreEqual(2, editedGroup.Boxes.Count(box => box.Axis == 1));
            Assert.AreEqual(0, editedGroup.Boxes.Count(box => box.Axis == 2));
            CollectionAssert.AreEquivalent(
                new[] { 0, 1 },
                editedGroup.Boxes.Where(box => box.Axis == 0).Select(box => box.IndexFilter.Param0));
            CollectionAssert.AreEquivalent(
                new[] { 0, 1 },
                editedGroup.Boxes.Where(box => box.Axis == 1).Select(box => box.IndexFilter.Param0));
        }

        // A lone translation Y lane must produce only Y ID filters rather than falling back to X.
        [Test]
        public void AddIdsWithOnlyTranslationYAxisCreatesOnlyYFilters()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 129,
                ID = 129,
                Boxes =
                {
                    new BaseLightTranslationEventBox { Axis = 1 }
                }
            };
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 3)
                as BaseLightTranslationEventBoxGroup;

            Assert.AreEqual(3, editedGroup.Boxes.Count);
            Assert.True(editedGroup.Boxes.All(box => box.Axis == 1));
            CollectionAssert.AreEquivalent(
                new[] { 0, 1, 2 },
                editedGroup.Boxes.Select(box => box.IndexFilter.Param0));
        }

        // A copied Y node must be rebound to every generated ID box or all copies visually overlap the first lane.
        [Test]
        public void AddIdsRebindsLoneTranslationYNodeToEveryGeneratedLane()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 146,
                ID = 146,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 1,
                        Events = new[] { new BaseLightTranslationBase { Translation = 25 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 3)
                as BaseLightTranslationEventBoxGroup;

            Assert.AreEqual(3, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            CollectionAssert.AreEqual(
                new[] { 0, 1, 2 },
                editedGroup.Boxes.Select(box => box.Events.Single().BoxIndex));
        }

        // +Ids converts one populated X source lane into N X ID lanes, cloning every source node into every lane.
        [Test]
        public void AddIdsCopiesAllXAxisNodesIntoEveryGeneratedIdLane()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 135,
                ID = 135,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[]
                        {
                            new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 10 },
                            new BaseLightTranslationBase { RelativeJsonTime = 1, Translation = 20 }
                        }
                    }
                }
            };
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 3)
                as BaseLightTranslationEventBoxGroup;

            Assert.AreEqual(3, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            Assert.True(editedGroup.Boxes.All(box => box.Axis == 0));
            Assert.True(editedGroup.Boxes.All(box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.IndexFilter.Param0));
            foreach (var box in editedGroup.Boxes)
            {
                CollectionAssert.AreEqual(new[] { 10f, 20f }, box.Events.Select(evt => evt.Translation));
            }
            Assert.False(ReferenceEquals(editedGroup.Boxes[0].Events[0], editedGroup.Boxes[1].Events[0]));

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = editedGroup;
            Assert.AreEqual(3, GetDisplayedLabels(provider).Length);
            Assert.True(GetDisplayedLabels(provider).All(label => label.text.EndsWith("X")));
        }

        // +Ids duplicates each axis's own source nodes across its ID lanes and never creates an absent Z layout.
        [Test]
        public void AddIdsCopiesXAxisAndYAxisNodesWithoutCreatingZLanes()
        {
            var group = CreatePopulatedXYTranslationGroup(136, 136);
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 2)
                as BaseLightTranslationEventBoxGroup;

            Assert.AreEqual(4, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            Assert.AreEqual(2, editedGroup.Boxes.Count(box => box.Axis == 0));
            Assert.AreEqual(2, editedGroup.Boxes.Count(box => box.Axis == 1));
            Assert.AreEqual(0, editedGroup.Boxes.Count(box => box.Axis == 2));
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 0))
            {
                CollectionAssert.AreEqual(new[] { 10f }, box.Events.Select(evt => evt.Translation));
            }
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 1))
            {
                CollectionAssert.AreEqual(new[] { 20f, 30f }, box.Events.Select(evt => evt.Translation));
            }
            Assert.True(editedGroup.Boxes.All(box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset));

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = editedGroup;
            Assert.False(GetDisplayedLabels(provider).Any(label => label.text.EndsWith("Z")));
        }

        // +Axes IDs creates ID layouts for enabled empty axes too while copying populated-axis nodes into every matching lane.
        [Test]
        public void AddAxesAndIdsCopiesNodesAndCreatesEmptyIdLanesForMissingAxes()
        {
            var group = CreatePopulatedXYTranslationGroup(137, 137);
            PrepareAxisScrollGroup(group);
            var track = new TrackDefinitionGLS { TranslationTracks = new[] { true, true, true } };

            var editedGroup = GLSEventBoxCommand.AddAllAxesAndIdsEventBox(group, track, 2)
                as BaseLightTranslationEventBoxGroup;

            Assert.AreEqual(6, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 0))
            {
                CollectionAssert.AreEqual(new[] { 10f }, box.Events.Select(evt => evt.Translation));
            }
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 1))
            {
                CollectionAssert.AreEqual(new[] { 20f, 30f }, box.Events.Select(evt => evt.Translation));
            }
            Assert.True(editedGroup.Boxes.Where(box => box.Axis == 2).All(box => box.Events.Length == 0));
            Assert.True(editedGroup.Boxes.All(box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset));

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = editedGroup;
            Assert.AreEqual(6, GetDisplayedLabels(provider).Length);
        }

        // Rotation +Ids shares Translation's authored-axis copy behavior and suppresses an absent Z ghost.
        [Test]
        public void AddIdsCopiesRotationNodesAcrossAuthoredAxesWithoutGhosts()
        {
            var group = CreatePopulatedXYRotationGroup(141, 141);
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 2)
                as BaseLightRotationEventBoxGroup;

            Assert.AreEqual(4, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 0))
            {
                CollectionAssert.AreEqual(new[] { 10f }, box.Events.Select(evt => evt.Rotation));
            }
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 1))
            {
                CollectionAssert.AreEqual(new[] { 20f, 30f }, box.Events.Select(evt => evt.Rotation));
            }
            Assert.AreEqual(0, editedGroup.Boxes.Count(box => box.Axis == 2));
            Assert.True(editedGroup.Boxes.All(box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset));

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = editedGroup;
            Assert.False(GetDisplayedLabels(provider).Any(label => label.text.EndsWith("Z")));
        }

        // Rotation +Axes IDs also creates empty ID lanes for the enabled missing Z axis through the shared axis path.
        [Test]
        public void AddAxesAndIdsCopiesRotationNodesAndCreatesEmptyMissingAxisIds()
        {
            var group = CreatePopulatedXYRotationGroup(142, 142);
            PrepareAxisScrollGroup(group);
            var track = new TrackDefinitionGLS { RotationTracks = new[] { true, true, true } };

            var editedGroup = GLSEventBoxCommand.AddAllAxesAndIdsEventBox(group, track, 2)
                as BaseLightRotationEventBoxGroup;

            Assert.AreEqual(6, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 0))
            {
                CollectionAssert.AreEqual(new[] { 10f }, box.Events.Select(evt => evt.Rotation));
            }
            foreach (var box in editedGroup.Boxes.Where(box => box.Axis == 1))
            {
                CollectionAssert.AreEqual(new[] { 20f, 30f }, box.Events.Select(evt => evt.Rotation));
            }
            Assert.True(editedGroup.Boxes.Where(box => box.Axis == 2).All(box => box.Events.Length == 0));
            Assert.True(editedGroup.Boxes.All(box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset));
        }

        // +Axes materializes only the missing Y lane; existing X/Z nodes and boxes remain otherwise unchanged.
        [Test]
        public void AddAxesMakesMissingLanePermanentWithoutMovingExistingNodes()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 138,
                ID = 138,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase { Translation = 10 } }
                    },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[]
                        {
                            new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 20 },
                            new BaseLightTranslationBase { RelativeJsonTime = 1, Translation = 30 }
                        }
                    }
                }
            };
            PrepareAxisScrollGroup(group);
            var track = new TrackDefinitionGLS { TranslationTracks = new[] { true, true, true } };

            var editedGroup = GLSEventBoxCommand.AddAllAxesEventBox(group, track) as BaseLightTranslationEventBoxGroup;

            Assert.AreEqual(3, editedGroup.Boxes.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
            AssertValidEventOwnership(editedGroup);
            CollectionAssert.AreEqual(
                new[] { 10f },
                editedGroup.Boxes.Single(box => box.Axis == 0).Events.Select(evt => evt.Translation));
            Assert.AreEqual(0, editedGroup.Boxes.Single(box => box.Axis == 1).Events.Length);
            Assert.False(editedGroup.Boxes.Single(box => box.Axis == 1).IsAutomaticAxisLane);
            CollectionAssert.AreEqual(
                new[] { 20f, 30f },
                editedGroup.Boxes.Single(box => box.Axis == 2).Events.Select(evt => evt.Translation));
        }

        // Color +Ids removes the source box and independently copies every color node into each generated ID box.
        [Test]
        public void AddIdsCopiesColorNodesIntoEveryGeneratedIdLane()
        {
            var group = new BaseLightColorEventBoxGroup
            {
                JsonTime = 139,
                ID = 139,
                Boxes =
                {
                    new BaseLightColorEventBox
                    {
                        Events = new[]
                        {
                            new BaseLightColorBase { RelativeJsonTime = 0, Brightness = 0.5f },
                            new BaseLightColorBase { RelativeJsonTime = 1, Brightness = 1f }
                        }
                    }
                }
            };
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 2)
                as BaseLightColorEventBoxGroup;

            Assert.AreEqual(2, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, editedGroup.Boxes.Select(box => box.IndexFilter.Param0));
            Assert.True(editedGroup.Boxes.All(box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset));
            foreach (var box in editedGroup.Boxes)
            {
                CollectionAssert.AreEqual(new[] { 0.5f, 1f }, box.Events.Select(evt => evt.Brightness));
            }
            Assert.False(ReferenceEquals(editedGroup.Boxes[0].Events[0], editedGroup.Boxes[1].Events[0]));
        }

        // FloatFX +Ids uses the same node-copy contract as Color and transform GLS groups.
        [Test]
        public void AddIdsCopiesFloatFxNodesIntoEveryGeneratedIdLane()
        {
            var group = new BaseVfxEventEventBoxGroup
            {
                JsonTime = 140,
                ID = 140,
                Boxes =
                {
                    new BaseVfxEventEventBox
                    {
                        Events = new[]
                        {
                            new BaseFxEventFloat { RelativeJsonTime = 0, Value = 2f },
                            new BaseFxEventFloat { RelativeJsonTime = 1, Value = 3f }
                        }
                    }
                }
            };
            PrepareAxisScrollGroup(group);

            var editedGroup = GLSEventBoxCommand.AddAllIdsEventBox(group, 3)
                as BaseVfxEventEventBoxGroup;

            Assert.AreEqual(3, editedGroup.Boxes.Count);
            AssertValidEventOwnership(editedGroup);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.IndexFilter.Param0));
            Assert.True(editedGroup.Boxes.All(box => box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset));
            foreach (var box in editedGroup.Boxes)
            {
                CollectionAssert.AreEqual(new[] { 2f, 3f }, box.Events.Select(evt => evt.Value));
            }
            Assert.False(ReferenceEquals(editedGroup.Boxes[0].Events[0], editedGroup.Boxes[1].Events[0]));
        }

        // V4 group references and common-data tables must use the same filtered rotation box set.
        [Test]
        public void V4RotationSerializationOmitsEmptyAxisLanes()
        {
            var populatedBox = new BaseLightRotationEventBox
            {
                Axis = 0,
                Events = new[] { new BaseLightRotationBase() }
            };
            var group = new BaseLightRotationEventBoxGroup
            {
                Boxes =
                {
                    populatedBox,
                    new BaseLightRotationEventBox { Axis = 1, IsAutomaticAxisLane = true },
                    new BaseLightRotationEventBox { Axis = 2, IsAutomaticAxisLane = true }
                }
            };
            var filters = new List<V4CommonData.IndexFilter>
            {
                V4CommonData.IndexFilter.FromBaseIndexFilter(populatedBox.IndexFilter)
            };
            var boxes = new List<V4CommonData.LightRotationEventBox>
            {
                V4CommonData.LightRotationEventBox.FromBaseLightRotationEventBox(populatedBox)
            };
            var events = new List<V4CommonData.LightRotationEvent>
            {
                V4CommonData.LightRotationEvent.FromBaseLightRotationEvent(populatedBox.Events[0])
            };

            // V4 receives the same already-pruned authoritative rotation boxes as V3.
            group.PruneEmptyAutomaticAxisLanes();
            var serialized = V4LightRotationEventBoxGroup.ToJson(group, filters, boxes, events);

            Assert.AreEqual(1, group.Boxes.Count);
            Assert.AreEqual(1, serialized["e"].AsArray.Count);
        }

        // V4 translation serialization needs the same empty-lane filtering as V3 and rotation groups.
        [Test]
        public void V4TranslationSerializationOmitsEmptyAxisLanes()
        {
            var populatedBox = new BaseLightTranslationEventBox
            {
                Axis = 0,
                Events = new[] { new BaseLightTranslationBase() }
            };
            var group = new BaseLightTranslationEventBoxGroup
            {
                Boxes =
                {
                    populatedBox,
                    new BaseLightTranslationEventBox { Axis = 1, IsAutomaticAxisLane = true },
                    new BaseLightTranslationEventBox { Axis = 2, IsAutomaticAxisLane = true }
                }
            };
            var filters = new List<V4CommonData.IndexFilter>
            {
                V4CommonData.IndexFilter.FromBaseIndexFilter(populatedBox.IndexFilter)
            };
            var boxes = new List<V4CommonData.LightTranslationEventBox>
            {
                V4CommonData.LightTranslationEventBox.FromBaseLightTranslationEventBox(populatedBox)
            };
            var events = new List<V4CommonData.LightTranslationEvent>
            {
                V4CommonData.LightTranslationEvent.FromBaseLightTranslationEvent(populatedBox.Events[0])
            };

            // V4 receives the same already-pruned authoritative translation boxes as V3.
            group.PruneEmptyAutomaticAxisLanes();
            var serialized = V4LightTranslationEventBoxGroup.ToJson(group, filters, boxes, events);

            Assert.AreEqual(1, group.Boxes.Count);
            Assert.AreEqual(1, serialized["e"].AsArray.Count);
        }

        // Placing into a display-only axis must create exactly one authored box and make undo restore the prior group.
        [Test]
        public void FirstNodeInEmptyRotationAxisMaterializesOnlyThatLane()
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = 120,
                ID = 120,
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightRotationBase { RelativeJsonTime = 0.5f } }
                    }
                }
            };
            group.NormalizeLoadedEventConflicts();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            // Direct test cleanup does not run the provider's next LateUpdate, so clear its prior retirement marker explicitly.
            provider.LastContext = null;
            provider.GroupContext = group;
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);

            eventCollection.PlaceInDisplayOnlyLane(
                new BaseLightRotationBase { JsonTime = 121, RelativeJsonTime = 1 },
                emptyYAxis);

            var editedGroup = provider.GroupContext as BaseLightRotationEventBoxGroup;
            Assert.AreEqual(2, editedGroup.Boxes.Count);
            Assert.AreEqual(1, editedGroup.Boxes[1].Axis);
            Assert.AreEqual(1, editedGroup.Boxes[1].Events.Length);
            Assert.AreEqual(2, V3LightRotationEventBoxGroup.ToJson(editedGroup)["e"].AsArray.Count);

            Object.FindAnyObjectByType<BeatmapActionContainer>().Undo();
            Assert.AreEqual(1, provider.GroupContext.ReadOnlyBoxes.Count);
        }

        // Clicking the ghost Y lane between populated X/Z must materialize it in XYZ order without disturbing either existing node.
        [Test]
        public void PlacingNodeInMiddleTranslationGhostKeepsXyzLaneOrder()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 148,
                ID = 148,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase { Translation = 10 } }
                    },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightTranslationBase { Translation = 30 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);

            eventCollection.PlaceInDisplayOnlyLane(
                new BaseLightTranslationBase { JsonTime = 149, RelativeJsonTime = 1, Translation = 20 },
                emptyYAxis);

            var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
            CollectionAssert.AreEqual(
                new[] { 10f, 20f, 30f },
                editedGroup.Boxes.SelectMany(box => box.Events).Select(evt => evt.Translation));
            AssertValidEventOwnership(editedGroup);
        }

        // Rotation uses the same ghost-to-real insertion boundary and must also place a new Y lane between populated X/Z.
        [Test]
        public void PlacingNodeInMiddleRotationGhostKeepsXyzLaneOrder()
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = 149,
                ID = 149,
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightRotationBase { Rotation = 10 } }
                    },
                    new BaseLightRotationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightRotationBase { Rotation = 30 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);

            eventCollection.PlaceInDisplayOnlyLane(
                new BaseLightRotationBase { JsonTime = 150, RelativeJsonTime = 1, Rotation = 20 },
                emptyYAxis);

            var editedGroup = provider.GroupContext as BaseLightRotationEventBoxGroup;
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
            CollectionAssert.AreEqual(
                new[] { 10f, 20f, 30f },
                editedGroup.Boxes.SelectMany(box => box.Events).Select(evt => evt.Rotation));
            AssertValidEventOwnership(editedGroup);
        }

        // Alt-dragging into the ghost Y lane must use the same XYZ insertion boundary as direct placement.
        [Test]
        public void DraggingNodeIntoMiddleTranslationGhostKeepsXyzLaneOrder()
        {
            var movedEvent = new BaseLightTranslationBase { RelativeJsonTime = 1, Translation = 20 };
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 150,
                ID = 150,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[]
                        {
                            new BaseLightTranslationBase { Translation = 10 },
                            movedEvent
                        }
                    },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightTranslationBase { Translation = 30 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            var originalGroup = BeatmapFactory.Clone(group);
            eventCollection.SilentRemoveObject(movedEvent);
            movedEvent.EventBoxData = emptyYAxis;
            movedEvent.BoxIndex = -1;

            eventCollection.MoveToDisplayOnlyLane(
                movedEvent,
                originalGroup.Boxes[0].Events[1],
                emptyYAxis,
                originalGroup);

            var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
            CollectionAssert.AreEqual(
                new[] { 10f, 20f, 30f },
                editedGroup.Boxes.SelectMany(box => box.Events).Select(evt => evt.Translation));
            AssertValidEventOwnership(editedGroup);
        }

        // If every remaining lane was automatic and becomes empty, retain only the most recently materialized axis.
        [Test]
        public void FullyEmptyAutomaticGroupRetainsOneMostRecentLane()
        {
            var group = new BaseLightTranslationEventBoxGroup { JsonTime = 125, ID = 125 };
            group.NormalizeLoadedEventConflicts();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            Assert.True(provider.TryGetDisplayedBox(0, out var emptyXAxis));
            eventCollection.PlaceInDisplayOnlyLane(
                new BaseLightTranslationBase { JsonTime = 125, RelativeJsonTime = 0 },
                emptyXAxis);
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            eventCollection.PlaceInDisplayOnlyLane(
                new BaseLightTranslationBase { JsonTime = 126, RelativeJsonTime = 1 },
                emptyYAxis);
            var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            eventCollection.DeleteObject(editedGroup.Boxes.Single(box => box.Axis == 0).Events.Single(), true, false);
            editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            eventCollection.DeleteObject(editedGroup.Boxes.Single(box => box.Axis == 1).Events.Single(), true, false);
            editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;

            var serializedBoxes = V3LightTranslationEventBoxGroup.ToJson(editedGroup)["e"].AsArray;

            Assert.AreEqual(1, editedGroup.Boxes.Count);
            Assert.AreEqual(1, editedGroup.Boxes[0].Axis);
            Assert.AreEqual(1, serializedBoxes.Count);
            Assert.AreEqual(1, serializedBoxes[0]["a"].AsInt);
        }

        // Deleting a disposable axis's last node must remove every empty automatic box before the group reaches serializers.
        [Test]
        public void DeletingAutomaticAxisNodePrunesEmptyAutomaticBoxesFromGroup()
        {
            var removableEvent = new BaseLightRotationBase { RelativeJsonTime = 1, Rotation = 20 };
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = 143,
                ID = 143,
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightRotationBase { Rotation = 10 } }
                    },
                    new BaseLightRotationEventBox
                    {
                        Axis = 1,
                        IsAutomaticAxisLane = true,
                        Events = new[] { removableEvent }
                    },
                    new BaseLightRotationEventBox { Axis = 2, IsAutomaticAxisLane = true }
                }
            };
            PrepareAxisScrollGroup(group);
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);

            eventCollection.DeleteObject(removableEvent, true, false);

            var editedGroup = Object.FindAnyObjectByType<GLSEventGridProvider>()
                .GroupContext as BaseLightRotationEventBoxGroup;
            Assert.AreEqual(1, editedGroup.Boxes.Count);
            Assert.AreEqual(0, editedGroup.Boxes[0].Axis);
            Assert.AreEqual(1, editedGroup.Boxes[0].Events.Length);
        }

        // Alt-dragging to a display-only axis must remove the source node, materialize one destination, and remain undoable.
        [Test]
        public void DragToEmptyTranslationAxisMovesNodeWithoutLeavingSourceDuplicate()
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 121,
                ID = 121,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = 0,
                        Events = new[] { new BaseLightTranslationBase { RelativeJsonTime = 0.5f } }
                    }
                }
            };
            group.NormalizeLoadedEventConflicts();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            // Isolate this direct replacement from a previous test's deferred provider retirement marker.
            provider.LastContext = null;
            provider.GroupContext = group;
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            var movedEvent = group.Boxes[0].Events[0];
            var originalGroup = BeatmapFactory.Clone(group);
            eventCollection.SilentRemoveObject(movedEvent);
            movedEvent.EventBoxData = emptyYAxis;
            movedEvent.BoxIndex = 1;

            // Supply the immutable source child so the optimized move replaces only its cloned source-array entry.
            eventCollection.MoveToDisplayOnlyLane(
                movedEvent,
                originalGroup.Boxes[0].Events[0],
                emptyYAxis,
                originalGroup);

            var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            Assert.AreEqual(2, editedGroup.Boxes.Count);
            Assert.AreEqual(0, editedGroup.Boxes[0].Events.Length);
            Assert.AreEqual(1, editedGroup.Boxes[1].Events.Length);
            Assert.AreEqual(1, editedGroup.Boxes[1].Axis);

            Object.FindAnyObjectByType<BeatmapActionContainer>().Undo();
            var restoredGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            Assert.AreEqual(1, restoredGroup.Boxes.Count);
            Assert.AreEqual(1, restoredGroup.Boxes[0].Events.Length);
        }

        // The initial transform X lane is disposable, so Alt-dragging its only node to Y must leave X as a translucent view-only ghost.
        [Test]
        public void InitialTranslationXAxisBecomesGhostAfterAltDragToY()
        {
            var placement = Object.FindAnyObjectByType<GLSGroupTranslationPlacement>();
            var generateMethod = typeof(GLSGroupTranslationPlacement).GetMethod(
                "GenerateOriginalData",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var group = generateMethod.Invoke(placement, null) as BaseLightTranslationEventBoxGroup;
            group.JsonTime = 147;
            group.ID = 147;
            group.Boxes[0].Events[0].Translation = 25;
            group.NormalizeLoadedEventConflicts();

            Assert.AreEqual(0, group.Boxes[0].Axis);
            Assert.True(group.Boxes[0].IsAutomaticAxisLane);
            PrepareAxisScrollGroup(group);

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            Assert.True(provider.TryGetDisplayedBox(1, out var emptyYAxis));
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            var movedEvent = group.Boxes[0].Events[0];
            var originalGroup = BeatmapFactory.Clone(group);
            eventCollection.SilentRemoveObject(movedEvent);
            movedEvent.EventBoxData = emptyYAxis;
            movedEvent.BoxIndex = -1;

            eventCollection.MoveToDisplayOnlyLane(
                movedEvent,
                originalGroup.Boxes[0].Events[0],
                emptyYAxis,
                originalGroup);

            var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            Assert.AreEqual(1, editedGroup.Boxes.Count);
            Assert.AreEqual(1, editedGroup.Boxes[0].Axis);
            Assert.True(editedGroup.Boxes[0].IsAutomaticAxisLane);
            Assert.AreEqual(1, editedGroup.Boxes[0].Events.Length);
            Assert.AreEqual(25, editedGroup.Boxes[0].Events[0].Translation);
            AssertValidEventOwnership(editedGroup);
            Assert.True(provider.TryGetDisplayedBox(0, out var emptyXAxis));
            Assert.AreEqual(0, (int)emptyXAxis.GetAxis());
            Assert.True(emptyXAxis.IsAutomaticAxisLane);
            Assert.AreEqual(0, emptyXAxis.ReadOnlyEvents.Count);
            Assert.False(editedGroup.ReadOnlyBoxes.Contains(emptyXAxis));
            Assert.That(
                GetDisplayedLabels(provider).Single(label => label.text.EndsWith("X")).color.a,
                Is.EqualTo(0.5f));
            Assert.AreEqual(0, provider.GetAuthoredBoxIndex(1));
        }

        // Axis scrolling runs on every wheel pulse, so scanning a large destination lane linearly causes repeated input stalls.
        // The sorted event list permits logarithmic indexed access while preserving the occupied-axis result.
        [Test]
        public void AxisScrollSearchesSortedDestinationEventsWithoutLinearEnumeration()
        {
            const int eventCount = 1024;
            var destinationEvents = new BaseGLSEvent[eventCount];
            for (var index = 0; index < destinationEvents.Length; index++)
                destinationEvents[index] = new BaseLightTranslationBase { RelativeJsonTime = index };
            var countedDestinationEvents = new CountingEventList(destinationEvents);
            var boxes = new List<CountingEventBox>
            {
                new(0, new CountingEventList(System.Array.Empty<BaseGLSEvent>())),
                new(1, countedDestinationEvents),
                new(2, new CountingEventList(System.Array.Empty<BaseGLSEvent>()))
            };
            var method = typeof(GLSCommonCommand).GetMethod(
                "TryFindOpenAxis",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var closedMethod = method.MakeGenericMethod(typeof(CountingEventBox));
            var arguments = new object[]
            {
                boxes,
                0,
                (float)(eventCount - 1),
                1,
                new System.Func<CountingEventBox, int>(box => box.Axis),
                0
            };

            var foundOpenAxis = (bool)closedMethod.Invoke(null, arguments);

            Assert.True(foundOpenAxis);
            Assert.AreEqual(2, arguments[5]);
            Assert.AreEqual(0, countedDestinationEvents.EnumerationCount);
            Assert.LessOrEqual(countedDestinationEvents.IndexAccessCount, 11);
        }

        // Axis scrolling must skip an occupied Y beat and preserve that node while moving the translation into open Z.
        [Test]
        public void TranslationAxisScrollSkipsOccupiedDestinationAtSameBeat()
        {
            var source = new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 10 };
            var occupied = new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 20 };
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 122,
                ID = 122,
                Boxes =
                {
                    new BaseLightTranslationEventBox { Axis = 0, Events = new[] { source } },
                    new BaseLightTranslationEventBox { Axis = 1, Events = new[] { occupied } },
                    new BaseLightTranslationEventBox { Axis = 2 }
                }
            };
            PrepareAxisScrollGroup(group);

            InvokeAxisCycle("CycleTranslationEventAxis", source, 1);

            var editedGroup = Object.FindAnyObjectByType<GLSEventGridProvider>()
                .GroupContext as BaseLightTranslationEventBoxGroup;
            Assert.AreEqual(20, editedGroup.Boxes.Single(box => box.Axis == 1).Events.Single().Translation);
            Assert.AreEqual(10, editedGroup.Boxes.Single(box => box.Axis == 2).Events.Single().Translation);
        }

        // Reverse rotation scrolling must wrap past occupied Z and use open Y without deleting either authored node.
        [Test]
        public void RotationAxisScrollSkipsOccupiedDestinationInReverse()
        {
            var source = new BaseLightRotationBase { RelativeJsonTime = 0, Rotation = 10 };
            var occupied = new BaseLightRotationBase { RelativeJsonTime = 0, Rotation = 30 };
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = 123,
                ID = 123,
                Boxes =
                {
                    new BaseLightRotationEventBox { Axis = 0, Events = new[] { source } },
                    new BaseLightRotationEventBox { Axis = 1 },
                    new BaseLightRotationEventBox { Axis = 2, Events = new[] { occupied } }
                }
            };
            PrepareAxisScrollGroup(group);

            InvokeAxisCycle("CycleRotationEventAxis", source, -1);

            var editedGroup = Object.FindAnyObjectByType<GLSEventGridProvider>()
                .GroupContext as BaseLightRotationEventBoxGroup;
            Assert.AreEqual(10, editedGroup.Boxes.Single(box => box.Axis == 1).Events.Single().Rotation);
            Assert.AreEqual(30, editedGroup.Boxes.Single(box => box.Axis == 2).Events.Single().Rotation);
        }

        // Ctrl+Alt movement must merge an unused X lane before authored Y/Z lanes in visible XYZ order.
        [Test]
        public void TranslationAxisScrollKeepsAuthoredAndUnusedLanesInXyzDisplayOrder()
        {
            var source = new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 10 };
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 144,
                ID = 144,
                Boxes =
                {
                    new BaseLightTranslationEventBox { Axis = 0, Events = new[] { source } },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightTranslationBase { RelativeJsonTime = 1, Translation = 30 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);

            InvokeAxisCycle("CycleTranslationEventAxis", source, 1);

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var editedGroup = provider.GroupContext as BaseLightTranslationEventBoxGroup;
            CollectionAssert.AreEqual(new[] { 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
            CollectionAssert.AreEqual(new[] { "X", "Y", "Z" }, GetDisplayedAxisOrder(provider));
            // Both cached directions must distinguish the view-only X lane from authoritative Y/Z ownership.
            Assert.AreEqual(-1, provider.GetAuthoredBoxIndex(0));
            Assert.AreEqual(0, provider.GetAuthoredBoxIndex(1));
            Assert.AreEqual(1, provider.GetAuthoredBoxIndex(2));
            Assert.AreEqual(1, provider.GetDisplayedLaneIndex(0));
            Assert.AreEqual(2, provider.GetDisplayedLaneIndex(1));
        }

        // Rotation uses the same merged authored/unused XYZ presentation after Ctrl+Alt movement.
        [Test]
        public void RotationAxisScrollKeepsAuthoredAndUnusedLanesInXyzDisplayOrder()
        {
            var source = new BaseLightRotationBase { RelativeJsonTime = 0, Rotation = 10 };
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = 145,
                ID = 145,
                Boxes =
                {
                    new BaseLightRotationEventBox { Axis = 0, Events = new[] { source } },
                    new BaseLightRotationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightRotationBase { RelativeJsonTime = 1, Rotation = 30 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);

            InvokeAxisCycle("CycleRotationEventAxis", source, 1);

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var editedGroup = provider.GroupContext as BaseLightRotationEventBoxGroup;
            CollectionAssert.AreEqual(new[] { 1, 2 }, editedGroup.Boxes.Select(box => box.Axis));
            CollectionAssert.AreEqual(new[] { "X", "Y", "Z" }, GetDisplayedAxisOrder(provider));
            // Rotation must use the same cached display/ownership mapping as Translation.
            Assert.AreEqual(-1, provider.GetAuthoredBoxIndex(0));
            Assert.AreEqual(0, provider.GetAuthoredBoxIndex(1));
            Assert.AreEqual(1, provider.GetAuthoredBoxIndex(2));
            Assert.AreEqual(1, provider.GetDisplayedLaneIndex(0));
            Assert.AreEqual(2, provider.GetDisplayedLaneIndex(1));
        }

        // When XYZ all contain a node at this beat, axis scrolling must keep every node and render the requested bottom warning.
        [Test]
        public void AxisScrollWithNoOpenAxisLeavesGroupUnchangedAndWarns()
        {
            var source = new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 10 };
            var group = new BaseLightTranslationEventBoxGroup
            {
                JsonTime = 124,
                ID = 124,
                Boxes =
                {
                    new BaseLightTranslationEventBox { Axis = 0, Events = new[] { source } },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 1,
                        Events = new[] { new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 20 } }
                    },
                    new BaseLightTranslationEventBox
                    {
                        Axis = 2,
                        Events = new[] { new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 30 } }
                    }
                }
            };
            PrepareAxisScrollGroup(group);

            InvokeAxisCycle("CycleTranslationEventAxis", source, 1);

            var editedGroup = Object.FindAnyObjectByType<GLSEventGridProvider>()
                .GroupContext as BaseLightTranslationEventBoxGroup;
            Assert.AreEqual(10, editedGroup.Boxes.Single(box => box.Axis == 0).Events.Single().Translation);
            Assert.AreEqual(20, editedGroup.Boxes.Single(box => box.Axis == 1).Events.Single().Translation);
            Assert.AreEqual(30, editedGroup.Boxes.Single(box => box.Axis == 2).Events.Single().Translation);
            Assert.True(HasBottomMessage("No open axis to shift to on this beat."));
        }

        // Axis-scroll integration requires an authoritative manager-owned group and valid child ownership indexes.
        private static void PrepareAxisScrollGroup(BaseEventBoxGroup group)
        {
            // Restore concrete generic group access before normalizing child ownership for the shared base-typed helper.
            switch (group)
            {
                case BaseLightRotationEventBoxGroup rotationGroup:
                    rotationGroup.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightTranslationEventBoxGroup translationGroup:
                    translationGroup.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightColorEventBoxGroup colorGroup:
                    colorGroup.NormalizeLoadedEventConflicts();
                    break;
                case BaseVfxEventEventBoxGroup floatFxGroup:
                    floatFxGroup.NormalizeLoadedEventConflicts();
                    break;
            }

            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
        }

        // Invoke the type-specific command directly so these tests isolate axis selection from Input System chord delivery.
        private static void InvokeAxisCycle(string methodName, BaseGLSEvent evt, int direction)
        {
            var method = typeof(GLSCommonCommand).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, new object[] { evt, direction });
        }

        // Generated GLS lanes are valid only when every cloned child points back to its actual parent lane and group.
        private static void AssertValidEventOwnership(BaseEventBoxGroup group)
        {
            for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
            {
                var box = group.ReadOnlyBoxes[boxIndex];
                foreach (var evt in box.ReadOnlyEvents)
                {
                    Assert.AreSame(group, evt.EventBoxGroupData);
                    Assert.AreSame(box, evt.EventBoxData);
                    Assert.AreEqual(boxIndex, evt.BoxIndex);
                }
            }
        }

        // Shared +Ids fixtures represent one X node, two Y nodes, and no authored Z lane.
        private static BaseLightTranslationEventBoxGroup CreatePopulatedXYTranslationGroup(float jsonTime, int id) => new()
        {
            JsonTime = jsonTime,
            ID = id,
            Boxes =
            {
                new BaseLightTranslationEventBox
                {
                    Axis = 0,
                    Events = new[] { new BaseLightTranslationBase { Translation = 10 } }
                },
                new BaseLightTranslationEventBox
                {
                    Axis = 1,
                    Events = new[]
                    {
                        new BaseLightTranslationBase { RelativeJsonTime = 0, Translation = 20 },
                        new BaseLightTranslationBase { RelativeJsonTime = 1, Translation = 30 }
                    }
                }
            }
        };

        // Rotation parity fixtures mirror the Translation X/Y node layout for shared ID-conversion assertions.
        private static BaseLightRotationEventBoxGroup CreatePopulatedXYRotationGroup(float jsonTime, int id) => new()
        {
            JsonTime = jsonTime,
            ID = id,
            Boxes =
            {
                new BaseLightRotationEventBox
                {
                    Axis = 0,
                    Events = new[] { new BaseLightRotationBase { Rotation = 10 } }
                },
                new BaseLightRotationEventBox
                {
                    Axis = 1,
                    Events = new[]
                    {
                        new BaseLightRotationBase { RelativeJsonTime = 0, Rotation = 20 },
                        new BaseLightRotationBase { RelativeJsonTime = 1, Rotation = 30 }
                    }
                }
            }
        };

        // The existing fading warning surface stores the currently rendered bottom text in its serialized TMP label.
        private static bool HasBottomMessage(string expectedMessage)
        {
            var bottomDisplayField = typeof(PersistentUI).GetField(
                "bottomDisplay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var bottomDisplay = bottomDisplayField.GetValue(PersistentUI.Instance);
            var messageTextField = bottomDisplay.GetType().GetField(
                "messageText",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if ((messageTextField.GetValue(bottomDisplay) as TMP_Text).text == expectedMessage)
            {
                return true;
            }

            // Another fading notification may own the label, so inspect messages waiting behind it as well.
            var queueField = bottomDisplay.GetType().GetField(
                "messagesQueue",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var queuedMessages = queueField.GetValue(bottomDisplay) as System.Collections.IEnumerable;
            foreach (var queuedMessage in queuedMessages)
            {
                var messageField = queuedMessage.GetType().GetField("Message", BindingFlags.Instance | BindingFlags.Public);
                if ((messageField.GetValue(queuedMessage) as string) == expectedMessage)
                {
                    return true;
                }
            }

            return false;
        }

        // AxisScrollSearchesSortedDestinationEventsWithoutLinearEnumeration needs observable indexed and enumerated access
        // so it can distinguish a logarithmic binary search from the previous full event walk.
        private sealed class CountingEventList : IReadOnlyList<BaseGLSEvent>
        {
            private readonly BaseGLSEvent[] events;

            public CountingEventList(BaseGLSEvent[] events) => this.events = events;

            public int EnumerationCount { get; private set; }

            public int IndexAccessCount { get; private set; }

            public int Count => events.Length;

            public BaseGLSEvent this[int index]
            {
                get
                {
                    IndexAccessCount++;
                    return events[index];
                }
            }

            public IEnumerator<BaseGLSEvent> GetEnumerator()
            {
                EnumerationCount++;
                return ((IEnumerable<BaseGLSEvent>)events).GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        // AxisScrollSearchesSortedDestinationEventsWithoutLinearEnumeration supplies BaseEventBox's production-facing
        // ReadOnlyEvents contract while exposing a counted list to the private generic axis-selection helper.
        private sealed class CountingEventBox : BaseLightTranslationEventBox
        {
            private readonly IReadOnlyList<BaseGLSEvent> events;

            public CountingEventBox(int axis, IReadOnlyList<BaseGLSEvent> events)
            {
                Axis = axis;
                this.events = events;
            }

            public override IReadOnlyList<BaseGLSEvent> ReadOnlyEvents => events;
        }

        // Build one explicit X StepAndOffset lane matching the +Ids command output.
        private static BaseLightTranslationEventBox CreateTranslationIdLane(int id) => new()
        {
            Axis = 0,
            IndexFilter = new BaseIndexFilter
            {
                Type = (int)IndexFilterType.StepAndOffset,
                Param0 = id
            }
        };

        // Inspect this provider's generated label pool so unrelated scene labels cannot affect lane assertions.
        private static TextMeshProUGUI[] GetDisplayedLabels(GLSEventGridProvider provider)
        {
            var labelsField = typeof(GLSEventGridProvider).GetField("usedLabels", BindingFlags.Instance | BindingFlags.NonPublic);
            return (labelsField.GetValue(provider) as IEnumerable<TextMeshProUGUI>)
                .Where(label => label.enabled)
                .ToArray();
        }

        // Compare lane order by rendered X position because the provider stores active labels in a stack.
        private static string[] GetDisplayedAxisOrder(GLSEventGridProvider provider) => GetDisplayedLabels(provider)
            .OrderBy(label => label.rectTransform.localPosition.x)
            .Select(label =>
            {
                var text = label.text.TrimEnd();
                return text.Substring(text.Length - 1);
            })
            .ToArray();
    }
}
