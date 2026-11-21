using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    // An empty event box still claims its selected lights. Later boxes can affect only unclaimed lights.
    public class GLSEmptyLaneOwnershipTest
    {
        private const int GroupElementCount = 4;
        private const float GroupBeat = 4f;
        private const float ContainerMaxBeat = 128f;

        [Test]
        public void EmptySpecificLaneClaimsOverlapBeforeLaterAllLightsLane()
        {
            var effectGameObject = new GameObject(nameof(EmptySpecificLaneClaimsOverlapBeforeLaterAllLightsLane));
            try
            {
                var effect = CreateInitializedEffect(effectGameObject);
                var emptyBox = new FakeBox { IndexFilter = StepAndOffsetFilter(0, 2) };
                var populatedBox = new FakeBox
                {
                    IndexFilter = new BaseIndexFilter(),
                    Events = new[] { new FakeEvent { RelativeJsonTime = 0.5f } }
                };
                var group = CreateGroup(emptyBox, populatedBox);

                effect.InsertData(group);

                Assert.AreSame(
                    emptyBox,
                    OwningBox(effect, group, 0),
                    "The earlier empty lane must claim element 0 before the later overlapping box.");
                Assert.AreSame(
                    populatedBox,
                    OwningBox(effect, group, 1),
                    "The later box keeps element 1 because the empty lane did not select it.");
                Assert.AreSame(
                    emptyBox,
                    OwningBox(effect, group, 2),
                    "The earlier empty lane must claim element 2 before the later overlapping box.");
                Assert.AreSame(
                    populatedBox,
                    OwningBox(effect, group, 3),
                    "The later box keeps element 3 because the empty lane did not select it.");
                Assert.AreEqual(
                    0,
                    GeneratedEventCount(effect, populatedBox, 0),
                    "Element 0 belongs to the empty lane, so the later box generates no event state there.");
                Assert.AreEqual(
                    1,
                    GeneratedEventCount(effect, populatedBox, 1),
                    "Element 1 is unclaimed, so the later box generates its event state there.");
                Assert.AreEqual(
                    0,
                    GeneratedEventCount(effect, populatedBox, 2),
                    "Element 2 belongs to the empty lane, so the later box generates no event state there.");
                Assert.AreEqual(
                    1,
                    GeneratedEventCount(effect, populatedBox, 3),
                    "Element 3 is unclaimed, so the later box generates its event state there.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(effectGameObject);
            }
        }

        [Test]
        public void EmptyAllLightsLaneSuppressesEveryLaterOverlappingEvent()
        {
            var effectGameObject = new GameObject(nameof(EmptyAllLightsLaneSuppressesEveryLaterOverlappingEvent));
            try
            {
                var effect = CreateInitializedEffect(effectGameObject);
                var emptyBox = new FakeBox { IndexFilter = new BaseIndexFilter() };
                var populatedBox = new FakeBox
                {
                    IndexFilter = StepAndOffsetFilter(0, 2),
                    Events = new[] { new FakeEvent { RelativeJsonTime = 0.5f } }
                };
                var group = CreateGroup(emptyBox, populatedBox);

                effect.InsertData(group);

                for (var element = 0; element < GroupElementCount; element++)
                {
                    Assert.AreSame(
                        emptyBox,
                        OwningBox(effect, group, element),
                        $"The earlier empty all-lights lane must claim element {element}.");
                    Assert.AreEqual(
                        0,
                        GeneratedEventCount(effect, populatedBox, element),
                        $"The suppressed later box must generate no event state on element {element}.");
                }

                // Eventless claimants must also be traversed on removal, otherwise elements owned only by the
                // empty lane would keep stale group states after the authored group is removed.
                effect.RemoveData(group, group);

                for (var element = 0; element < GroupElementCount; element++)
                {
                    Assert.That(
                        effect.GroupStatesFor(element).Any(state => ReferenceEquals(state.Base, group)),
                        Is.False,
                        $"Removing the group must clear the empty lane's ownership state for element {element}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(effectGameObject);
            }
        }

        // Editor-only automatic axis lanes are not serialized OEM boxes, so an empty ghost lane must not reserve
        // elements or suppress the authored lane that follows it.
        [Test]
        public void EmptyAutomaticAxisLaneDoesNotClaimAuthoredLights()
        {
            var effectGameObject = new GameObject(nameof(EmptyAutomaticAxisLaneDoesNotClaimAuthoredLights));
            try
            {
                var effect = CreateInitializedEffect(effectGameObject);
                var automaticBox = new FakeBox
                {
                    IndexFilter = new BaseIndexFilter(),
                    IsAutomaticAxisLane = true
                };
                var populatedBox = new FakeBox
                {
                    IndexFilter = new BaseIndexFilter(),
                    Events = new[] { new FakeEvent { RelativeJsonTime = 0.5f } }
                };
                var group = CreateGroup(automaticBox, populatedBox);

                effect.InsertData(group);

                for (var element = 0; element < GroupElementCount; element++)
                {
                    Assert.AreSame(
                        populatedBox,
                        OwningBox(effect, group, element),
                        $"The editor-only automatic lane must not claim authored element {element}.");
                    Assert.AreEqual(
                        1,
                        GeneratedEventCount(effect, populatedBox, element),
                        $"The authored lane must generate its event state on element {element}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(effectGameObject);
            }
        }

        private static FakeBox OwningBox(FakeGroupEffect effect, FakeGroup group, int element) =>
            effect.GroupStatesFor(element).Single(state => ReferenceEquals(state.Base, group)).Box;

        private static int GeneratedEventCount(FakeGroupEffect effect, FakeBox box, int element) =>
            effect.EventStatesFor(element).Count(state => box.Events.Contains(state.Base));

        private static FakeGroupEffect CreateInitializedEffect(GameObject host)
        {
            var effect = host.AddComponent<FakeGroupEffect>();
            effect.Count = GroupElementCount;
            effect.Initialize();
            return effect;
        }

        private static FakeGroup CreateGroup(params FakeBox[] boxes)
        {
            // JsonTime assigns before songBpmTime because its setter recomputes songBpmTime from a Map that
            // does not exist in this fixture.
            var group = new FakeGroup { JsonTime = GroupBeat, songBpmTime = GroupBeat };
            group.Boxes.AddRange(boxes);
            return group;
        }

        private static BaseIndexFilter StepAndOffsetFilter(int offset, int step) =>
            new()
            {
                Type = (int)IndexFilterType.StepAndOffset,
                Param0 = offset,
                Param1 = step
            };

        private class FakeGroup : BaseEventBoxGroup<FakeBox>
        {
            public override ObjectType ObjectType { get; set; } = ObjectType.GLSFloatFx;
            public override string CustomKeyColor => "unusedKeyColor";
            public override string CustomKeyTrack => "unusedKeyTrack";
            public override JSONNode ToJson() => null;
            public override BaseItem Clone() => new FakeGroup();
        }

        private class FakeBox : BaseEventBox
        {
            public FakeEvent[] Events { get; set; } = Array.Empty<FakeEvent>();
            public override IReadOnlyList<BaseGLSEvent> ReadOnlyEvents => Events;
            public override void ClearEvents() => Events = Array.Empty<FakeEvent>();
            public override void SetEvents(BaseGLSEvent[] data) => Events = data.OfType<FakeEvent>().ToArray();
            public override JSONNode ToJson() => null;
            public override BaseItem Clone() => new FakeBox { IndexFilter = IndexFilter, Events = Events };
        }

        private class FakeEvent : BaseGLSEvent
        {
            public int Easing { get; set; }
            public int UsePrevious { get; set; }
            public float Value { get; set; }

            protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) =>
                other is FakeEvent;

            public override JSONNode ToJson() => null;
            public override BaseItem Clone() => new FakeEvent { RelativeJsonTime = RelativeJsonTime };
        }

        private class FakeGroupStateData : EventGroupStateData<FakeGroup, FakeBox, FakeEvent>
        {
            public FakeGroupStateData(FakeGroup data) : base(data)
            {
            }
        }

        private class FakeEventStateData : EventGroupEventStateData<FakeEvent>
        {
            public readonly float Value;

            public FakeEventStateData(FakeEvent data, float startTime, float offset = 0f)
                : base(data, startTime, data.Easing, data.UsePrevious) =>
                Value = data.Value + offset;
        }

        private class FakeGroupEffect : EventGroupEffect<
            FakeGroupStateData,
            FakeEventStateData,
            FakeGroup,
            FakeBox,
            FakeEvent>
        {
            private StateChunksContainer<FakeGroupStateData, FakeGroup>[] groupContainers =
                Array.Empty<StateChunksContainer<FakeGroupStateData, FakeGroup>>();
            private StateChunksContainer<FakeEventStateData, FakeEvent>[] eventContainers =
                Array.Empty<StateChunksContainer<FakeEventStateData, FakeEvent>>();

            public override void Initialize()
            {
                groupContainers = new StateChunksContainer<FakeGroupStateData, FakeGroup>[Count];
                eventContainers = new StateChunksContainer<FakeEventStateData, FakeEvent>[Count];
                for (var element = 0; element < Count; element++)
                {
                    var groupContainer = new StateChunksContainer<FakeGroupStateData, FakeGroup>();
                    var eventContainer = new StateChunksContainer<FakeEventStateData, FakeEvent>();
                    groupContainers[element] = groupContainer;
                    eventContainers[element] = eventContainer;
                    SeedSentinels(groupContainer, eventContainer);
                }
            }

            public IEnumerable<FakeGroupStateData> GroupStatesFor(int element) =>
                groupContainers[element].Collection;

            public IEnumerable<FakeEventStateData> EventStatesFor(int element) =>
                eventContainers[element].Collection;

            public override void Refresh()
            {
            }

            public override void UpdateTime(bool isPlaying, float time)
            {
            }

            protected override FakeGroupStateData CreateState(FakeGroup data) => new(data);

            protected override StateChunksContainer<FakeGroupStateData, FakeGroup> GetGroupContainer(
                (Axis axis, int element) key) =>
                key.element >= 0 && key.element < groupContainers.Length
                    ? groupContainers[key.element]
                    : null;

            protected override StateChunksContainer<FakeEventStateData, FakeEvent> GetEventContainer(
                (Axis axis, int element) key) =>
                key.element >= 0 && key.element < eventContainers.Length
                    ? eventContainers[key.element]
                    : null;

            protected override IEnumerable<(
                StateChunksContainer<FakeGroupStateData, FakeGroup> groupContainer,
                StateChunksContainer<FakeEventStateData, FakeEvent> eventContainer)> GetContainers() =>
                Enumerable.Range(0, groupContainers.Length).Select(element =>
                    (groupContainer: groupContainers[element], eventContainer: eventContainers[element]));

            protected override int GetEventCount(FakeBox box) => box.Events.Length;

            protected override float GetLastEventTime(FakeBox box) => box.Events[^1].RelativeJsonTime;

            protected override float GetDistribution(
                IndexFilterHelper.IndexFilter indexFilter,
                FakeBox box,
                int order) =>
                DistributionHelper.GetValueStep(
                    order,
                    DistributionHelper.GetDistributionCount(indexFilter),
                    (DistributionType)box.BeatDistributionType,
                    box.BeatDistribution,
                    (EaseType)box.Easing);

            protected override FakeEventStateData[] GenerateEvents(
                FakeGroupStateData state,
                float distributionOffset,
                float maxRelativeJsonTime) =>
                state
                    .Box
                    .Events
                    .Select(x => new FakeEventStateData(
                        x,
                        state.Base.JsonTime + x.RelativeJsonTime + (state.DurationOrder * state.BeatStep),
                        distributionOffset))
                    .Where(x =>
                        state.Base.JsonTime + x.Base.RelativeJsonTime + (state.DurationOrder * state.BeatStep)
                        <= maxRelativeJsonTime)
                    .ToArray();

            // Both chunk containers need the same start/end sentinels the concrete effects add during
            // Initialize so overlapping/next-state lookups always resolve while InsertData runs.
            private static void SeedSentinels(
                StateChunksContainer<FakeGroupStateData, FakeGroup> groupContainer,
                StateChunksContainer<FakeEventStateData, FakeEvent> eventContainer)
            {
                var startEvent = new FakeEventStateData(new FakeEvent(), short.MinValue);
                var endEvent = new FakeEventStateData(new FakeEvent { UsePrevious = 1 }, float.MaxValue);
                eventContainer.Resize(ContainerMaxBeat);
                startEvent.EndTime = endEvent.StartTime;
                startEvent.Next = endEvent;
                endEvent.Previous = startEvent;
                eventContainer.AddState(startEvent);
                eventContainer.AddState(endEvent);
                eventContainer.SetStateAt(0);

                var start = new FakeGroupStateData(
                    new FakeGroup { JsonTime = short.MinValue, songBpmTime = short.MinValue })
                {
                    Box = new FakeBox(),
                    LocalJsonTime = short.MinValue
                };
                var end = new FakeGroupStateData(
                    new FakeGroup { JsonTime = float.MaxValue, songBpmTime = float.MaxValue })
                {
                    Box = new FakeBox(),
                    LocalJsonTime = float.MaxValue,
                    StartTime = float.MaxValue
                };
                groupContainer.Resize(ContainerMaxBeat);
                groupContainer.AddState(start);
                groupContainer.AddState(end);
                groupContainer.SetStateAt(0);
            }
        }
    }
}
