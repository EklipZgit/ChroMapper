using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    // Same-type/filter ring or laser events within one 50 Hz fixed tick (0.02 s) can anchor on a
    // stale rotation destination in game; EventGridContainer flags both endpoints of each pair.
    public class EventDesyncRiskTest : TestBase
    {
        // The shared test map runs at 100 BPM, so the 0.02 s window is 1/30 of a beat; these deltas
        // keep a clear float margin on each side of the boundary.
        private const float InsideWindowBeatDelta = 0.02f;
        private const float OutsideWindowBeatDelta = 0.05f;

        private static EventGridContainer GetEventsContainer() =>
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

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

            Assert.That(first.DesyncRisk, Is.True);
            Assert.That(second.DesyncRisk, Is.True);
            Assert.That(distant.DesyncRisk, Is.False);
        }

        [Test]
        public void RingRotationOutsideFixedTickIsNotFlagged()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var second = PlaceEvent(2f + OutsideWindowBeatDelta, (int)EventTypeValue.Event8);

            Assert.That(first.DesyncRisk, Is.False);
            Assert.That(second.DesyncRisk, Is.False);
        }

        [Test]
        public void RingRotationAndZoomWithinFixedTickDoNotFlag()
        {
            var rotation = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var zoom = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event9);

            Assert.That(rotation.DesyncRisk, Is.False);
            Assert.That(zoom.DesyncRisk, Is.False);
        }

        [Test]
        public void RingZoomWithinFixedTickFlagsBothEndpoints()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event9);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event9);

            Assert.That(first.DesyncRisk, Is.True);
            Assert.That(second.DesyncRisk, Is.True);
        }

        [Test]
        public void LaserSpeedWithinFixedTickFlagsBothEndpoints()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event12);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event12);

            Assert.That(first.DesyncRisk, Is.True);
            Assert.That(second.DesyncRisk, Is.True);
        }

        [Test]
        public void OppositeLaserSpeedSidesWithinFixedTickDoNotFlag()
        {
            var left = PlaceEvent(2f, (int)EventTypeValue.Event12);
            var right = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event13);

            Assert.That(left.DesyncRisk, Is.False);
            Assert.That(right.DesyncRisk, Is.False);
        }

        [Test]
        public void SameNameFilterWithinFixedTickFlags()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8, "Rings");
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8, "Rings");

            Assert.That(first.DesyncRisk, Is.True);
            Assert.That(second.DesyncRisk, Is.True);
        }

        [Test]
        public void DifferentNameFiltersWithinFixedTickDoNotFlag()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8, "DistantRings");
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8, "SmallRings");

            Assert.That(first.DesyncRisk, Is.False);
            Assert.That(second.DesyncRisk, Is.False);
        }

        // An unfiltered event reaches every same-type effect, so it still races filtered neighbors.
        [Test]
        public void UnfilteredEventWithinFixedTickFlagsFilteredNeighbor()
        {
            var unfiltered = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var filtered = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8, "Rings");

            Assert.That(unfiltered.DesyncRisk, Is.True);
            Assert.That(filtered.DesyncRisk, Is.True);
        }

        [Test]
        public void DeletingNeighborClearsDesyncFlag()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event8);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event8);

            Assert.That(first.DesyncRisk, Is.True);

            PlaceUtils.Delete(second);

            Assert.That(first.DesyncRisk, Is.False);
        }

        [Test]
        public void LightEventsWithinFixedTickAreNotFlagged()
        {
            var first = PlaceEvent(2f, (int)EventTypeValue.Event2);
            var second = PlaceEvent(2f + InsideWindowBeatDelta, (int)EventTypeValue.Event2);

            Assert.That(first.DesyncRisk, Is.False);
            Assert.That(second.DesyncRisk, Is.False);
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

            Assert.That(loaded[0].DesyncRisk, Is.True);
            Assert.That(loaded[1].DesyncRisk, Is.True);

            // LoadObjects replaces the shared collection's list, so put the baseline map's events back.
            loader.LoadObjects(BeatSaberSongContainer.Instance.Map.Events);
        }
    }
}
