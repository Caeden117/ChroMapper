using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    // Same-type/filter ring-rotation events within one 50 Hz fixed tick (0.02 s) can anchor on a
    // stale cumulative destination in game; EventGridContainer flags both endpoints of each pair.
    // Ring zoom writes each event's destination immediately and laser speed replaces rather than
    // accumulates rotation state, so only ring rotations retain the desync risk.
    public class EventDesyncRiskTest : TestBase
    {
        // The shared test map runs at 100 BPM, so the 0.02 s window is 1/30 of a beat; these deltas
        // keep a clear float margin on each side of the boundary.
        private const float InsideWindowBeatDelta = 0.02f;
        private const float OutsideWindowBeatDelta = 0.05f;

        private static EventGridContainer GetEventsContainer() =>
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

        // The desync flag is collection-side state, not beatmap data — query the grid container.
        private static bool IsFlagged(BaseEvent evt) => GetEventsContainer().IsDesyncRisk(evt);

        private static BaseEvent PlaceEvent(float jsonTime, int type, string nameFilter = null)
        {
            return PlaceUtils.Place(new BaseEvent
            {
                JsonTime = jsonTime,
                Type = type,
                Value = 0,
                CustomNameFilter = nameFilter
            });
        }

        [Test]
        public void RingRotationWithinFixedTickFlagsBothEndpoints()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8);
            var distant = PlaceEvent(2f + InsideWindowBeatDelta + OutsideWindowBeatDelta, (int)EventTypeValue.Event8);

            Assert.That(IsFlagged(first), Is.True);
            Assert.That(IsFlagged(second), Is.True);
            Assert.That(IsFlagged(distant), Is.False);
        }

        [Test]
        public void RingRotationOutsideFixedTickIsNotFlagged()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var second = PlaceEvent(2f + OutsideWindowBeatDelta, (int)EventTypeValue.Event8);

            Assert.That(IsFlagged(first), Is.False);
            Assert.That(IsFlagged(second), Is.False);
        }

        [Test]
        public void RingRotationAndZoomWithinFixedTickDoNotFlag()
        {
            var rotation = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var zoom = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event9);

            Assert.That(IsFlagged(rotation), Is.False);
            Assert.That(IsFlagged(zoom), Is.False);
        }

        // In-game zoom applies each event's destination immediately, so a within-tick neighbor
        // never anchors on a stale destination; the desync warning must not flag ring zooms.
        [Test]
        public void RingZoomWithinFixedTickIsNotFlagged()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event9);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event9);

            Assert.That(IsFlagged(first), Is.False);
            Assert.That(IsFlagged(second), Is.False);
        }

        // A laser speed event sets rotation speed rather than accumulating a destination, so
        // within-tick neighbors stay in sync; the desync warning must not flag laser speeds.
        [Test]
        public void LaserSpeedWithinFixedTickIsNotFlagged()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event12);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event12);

            Assert.That(IsFlagged(first), Is.False);
            Assert.That(IsFlagged(second), Is.False);
        }

        // Regression coverage for the reported map: Chroma laser speed events (value 1 with
        // speed 0, direction 0, lockRotation on the second event) on both left and right lasers
        // were falsely flagged; every delivered callback applies its own speed, so none desync.
        [Test]
        public void ChromaLockedLaserSpeedsWithinFixedTickAreNotFlagged()
        {
            var leftFirst = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = (int)EventTypeValue.Event12,
                Value = 1
            });
            var leftSecond = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2.016f,
                Type = (int)EventTypeValue.Event12,
                Value = 1,
                CustomSpeed = 0f,
                CustomDirection = 0,
                CustomLockRotation = true
            });
            var rightFirst = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2f,
                Type = (int)EventTypeValue.Event13,
                Value = 1
            });
            var rightSecond = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2.016f,
                Type = (int)EventTypeValue.Event13,
                Value = 1,
                CustomSpeed = 0f,
                CustomDirection = 0,
                CustomLockRotation = true
            });

            Assert.That(IsFlagged(leftFirst), Is.False);
            Assert.That(IsFlagged(leftSecond), Is.False);
            Assert.That(IsFlagged(rightFirst), Is.False);
            Assert.That(IsFlagged(rightSecond), Is.False);
        }

        // Same-beat placement collapses to one event through PlaceUtils.Place, so exercise the
        // index directly: two ring rotations at an identical SongBpmTime have a zero gap and must
        // flag both endpoints just like the smallest positive within-tick gap.
        [Test]
        public void RingRotationsAtSameBeatIndexFlagsBothEndpoints()
        {
            var index = new EventDesyncRiskIndex(
                _ => BasicEventComponent.RingRotation,
                _ => { });
            var first = new BaseEvent { JsonTime = 2f, Type = (int)EventTypeValue.Event8 };
            var second = new BaseEvent { JsonTime = 2f, Type = (int)EventTypeValue.Event8 };
            first.SetMap();
            second.SetMap();

            index.BeginScan();
            index.Observe(first);
            index.Observe(second);
            index.FinishScan();

            Assert.That(index.IsFlagged(first), Is.True);
            Assert.That(index.IsFlagged(second), Is.True);
        }

        // The smooth-step ring zoom tween replaces the ring's active event and targets an
        // event-time position rather than reading a queued rotation destination, so a components
        // mask containing only SmoothStepRingZoom must not participate in desync flagging at all.
        [Test]
        public void SmoothStepRingZoomOnlyIndexNeverFlags()
        {
            var index = new EventDesyncRiskIndex(
                _ => BasicEventComponent.SmoothStepRingZoom,
                _ => { });
            var first = new BaseEvent { JsonTime = 2f, Type = (int)EventTypeValue.Event9 };
            var second = new BaseEvent { JsonTime = 2f + InsideWindowBeatDelta, Type = (int)EventTypeValue.Event9 };
            first.SetMap();
            second.SetMap();

            index.BeginScan();
            index.Observe(first);
            index.Observe(second);
            index.FinishScan();

            Assert.That(index.IsFlagged(first), Is.False);
            Assert.That(index.IsFlagged(second), Is.False);
        }

        [Test]
        public void OppositeLaserSpeedSidesWithinFixedTickDoNotFlag()
        {
            var left = PlaceEvent(2f, (int)EventTypeValue.Event12);
            var right = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event13);

            Assert.That(IsFlagged(left), Is.False);
            Assert.That(IsFlagged(right), Is.False);
        }

        [Test]
        public void SameNameFilterWithinFixedTickFlags()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8, "Rings");
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8, "Rings");

            Assert.That(IsFlagged(first), Is.True);
            Assert.That(IsFlagged(second), Is.True);
        }

        [Test]
        public void DifferentNameFiltersWithinFixedTickDoNotFlag()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8, "DistantRings");
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8, "SmallRings");

            Assert.That(IsFlagged(first), Is.False);
            Assert.That(IsFlagged(second), Is.False);
        }

        // An unfiltered event reaches every same-type effect, so it still races filtered neighbors.
        [Test]
        public void UnfilteredEventWithinFixedTickFlagsFilteredNeighbor()
        {
            var unfiltered = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var filtered = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8, "Rings");

            Assert.That(IsFlagged(unfiltered), Is.True);
            Assert.That(IsFlagged(filtered), Is.True);
        }

        [Test]
        public void DeletingNeighborClearsDesyncFlag()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8);

            Assert.That(IsFlagged(first), Is.True);

            PlaceUtils.Delete(second);

            Assert.That(IsFlagged(first), Is.False);
        }

        [Test]
        public void LightEventsWithinFixedTickAreNotFlagged()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event2);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event2);

            Assert.That(IsFlagged(first), Is.False);
            Assert.That(IsFlagged(second), Is.False);
        }

        // Map load assigns MapObjects directly without SpawnObject/HandleObjectSpawned, so
        // LinkRingEvents must run from MapLoader.LoadObjects or loaded events never get flagged.
        [Test]
        public void LoadedMapRingPairsWithinFixedTickAreFlagged()
        {
            var loaded = new List<BaseEvent>
            {
                new() { JsonTime = 2f, Type = (int)EventTypeValue.Event8, Value = 0 },
                new() { JsonTime = 2f + InsideWindowBeatDelta, Type = (int)EventTypeValue.Event8, Value = 0 }
            };

            var loader = Object.FindAnyObjectByType<MapLoader>();
            loader.LoadObjects(loaded);

            Assert.That(IsFlagged(loaded[0]), Is.True);
            Assert.That(IsFlagged(loaded[1]), Is.True);

            // LoadObjects replaces the shared collection's list, so put the baseline map's events back.
            loader.LoadObjects(BeatSaberSongContainer.Instance.Map.Events);
        }
    }
}
