using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class BPMTest : TestBase
    {
        private static void CheckBPM(
            string msg,
            BaseBpmEvent bpmEvent,
            float jsonTime,
            float bpm,
            float? songBpmTime = null)
        {
            var decimalPrecision = Settings.Instance.TimeValueDecimalPrecision;
            var delta = 1.5 * Mathf.Pow(10, -decimalPrecision);

            Assert.AreEqual(jsonTime, bpmEvent.JsonTime, delta, $"{msg}: Mismatched JsonTime");
            Assert.AreEqual(bpm, bpmEvent.Bpm, delta, $"{msg}: Mismatched BPM");
            if (songBpmTime != null)
                Assert.AreEqual(songBpmTime.Value, bpmEvent.SongBpmTime, delta, $"{msg}: Mismatched SongBpmTime");
        }

        [Test]
        public void SongBpmTimes()
        {
            var songBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            var bpmEvent0 = PlaceUtils.Place(new BaseBpmEvent(0, 111));
            var bpmEvent1 = PlaceUtils.Place(new BaseBpmEvent(1, 222));
            var bpmEvent2 = PlaceUtils.Place(new BaseBpmEvent(2, 333));
            var bpmEvent3 = PlaceUtils.Place(new BaseBpmEvent(3, 444));

            BeatmapAssertion.CollectionCount<BaseBpmEvent>(4);
            CheckBPM("1st BPM values", bpmEvent0, 0, 111, 0);
            CheckBPM("2nd BPM values", bpmEvent1, 1, 222, songBpm / 111);
            CheckBPM("3rd BPM values", bpmEvent2, 2, 333, songBpm / 111 + songBpm / 222);
            CheckBPM("4th BPM values", bpmEvent3, 3, 444, songBpm / 111 + songBpm / 222 + songBpm / 333);

            var replacementBpmEvent0 = PlaceUtils.Place(new BaseBpmEvent(0, 1));
            BeatmapAssertion.CollectionCount<BaseBpmEvent>(4);
            CheckBPM("1st BPM values after modified", replacementBpmEvent0, 0, 1, 0);
            CheckBPM("2nd BPM values after modified", bpmEvent1, 1, 222, songBpm / 1);
            CheckBPM("3rd BPM values after modified", bpmEvent2, 2, 333, songBpm / 1 + songBpm / 222);
            CheckBPM(
                "4th BPM values after modified",
                bpmEvent3,
                3,
                444,
                songBpm / 1 + songBpm / 222 + songBpm / 333);

            PlaceUtils.Delete(replacementBpmEvent0);
            BeatmapAssertion.CollectionCount<BaseBpmEvent>(3);
            CheckBPM("1st BPM values after delete", bpmEvent1, 1, 222, 1);
            CheckBPM("2nd BPM values after delete", bpmEvent2, 2, 333, 1 + songBpm / 222);
            CheckBPM("3rd BPM values after delete", bpmEvent3, 3, 444, 1 + songBpm / 222 + songBpm / 333);
        }

        [Test]
        public void ModifyEvent()
        {
            var bpmCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);

            var futureBpmEvent = PlaceUtils.Place(new BaseBpmEvent(20, 20));

            var modifiedBpmEvent = PlaceUtils.Place(new BaseBpmEvent(10, 10));

            if (bpmCollection.LoadedContainers[modifiedBpmEvent] is BpmEventContainer container)
                BeatmapBPMChangeInputController.ChangeBpm(container, "60");

            BeatmapAssertion.CollectionCount<BaseBpmEvent>(2);
            CheckBPM("Update BPM event", modifiedBpmEvent, 10, 60);
            CheckBPM("Update future BPM event SongTime", futureBpmEvent, 20, 20, 10 + 10 * (100f / 60));

            var undone = PlaceUtils.Undo<BaseBpmEvent>().ToList();
            modifiedBpmEvent = undone[0];

            BeatmapAssertion.CollectionCount<BaseBpmEvent>(2);
            CheckBPM("Undo BPM event", modifiedBpmEvent, 10, 10);
            CheckBPM("Undo future BPM event SongTime", futureBpmEvent, 20, 20, 10 + 10 * (100f / 10));

            var redone = PlaceUtils.Redo<BaseBpmEvent>().ToList();
            modifiedBpmEvent = redone[0];

            BeatmapAssertion.CollectionCount<BaseBpmEvent>(2);
            CheckBPM("Redo BPM event", modifiedBpmEvent, 10, 60);
            CheckBPM("Redo future BPM event SongTime", futureBpmEvent, 20, 20, 10 + 10 * (100f / 60));
        }

        [Test]
        public void GoToBeat()
        {
            var songBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            var bpmEvent = new BaseBpmEvent(0, 111);
            bpmEvent = PlaceUtils.Place(bpmEvent);

            bpmEvent = new BaseBpmEvent(1, 222);
            bpmEvent = PlaceUtils.Place(bpmEvent);

            BeatmapAssertion.CollectionCount<BaseBpmEvent>(2);

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            atsc.GoToBeat("0");
            Assert.AreEqual(0, atsc.CurrentJsonTime, 0.001f);
            Assert.AreEqual(0, atsc.CurrentSongBpmTime, 0.001f);

            atsc.GoToBeat("0.5");
            Assert.AreEqual(0.5, atsc.CurrentJsonTime, 0.001f);
            Assert.AreEqual(0.5 * (songBpm / 111), atsc.CurrentSongBpmTime, 0.001f);

            atsc.GoToBeat("1.0");
            Assert.AreEqual(1.0, atsc.CurrentJsonTime, 0.001f);
            Assert.AreEqual(1.0 * (songBpm / 111), atsc.CurrentSongBpmTime, 0.001f);

            atsc.GoToBeat("1.5");
            Assert.AreEqual(1.5, atsc.CurrentJsonTime, 0.001f);
            Assert.AreEqual(1.0 * (songBpm / 111) + 0.5 * (songBpm / 222), atsc.CurrentSongBpmTime, 0.001f);

            atsc.GoToBeat("Invalid number");
            Assert.AreEqual(1.5, atsc.CurrentJsonTime, 0.001f);
            Assert.AreEqual(1.0 * (songBpm / 111) + 0.5 * (songBpm / 222), atsc.CurrentSongBpmTime, 0.001f);
        }

        [Test]
        public void UndoActionCollection()
        {
            var songBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            var bpmEvent0 = new BaseBpmEvent(0, 111);
            bpmEvent0 = PlaceUtils.Place(bpmEvent0);

            var bpmEvent1 = new BaseBpmEvent(1, 222);
            bpmEvent1 = PlaceUtils.Place(bpmEvent1);

            var bpmEvent2 = new BaseBpmEvent(2, 333);
            bpmEvent2 = PlaceUtils.Place(bpmEvent2);

            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(
                    new List<BeatmapAction>
                    {
                        new BeatmapObjectPlacementAction(bpmEvent0, new List<BaseObject>(), ""),
                        new BeatmapObjectPlacementAction(bpmEvent1, new List<BaseObject>(), ""),
                        new BeatmapObjectPlacementAction(bpmEvent2, new List<BaseObject>(), "")
                    }));

            // Check songBpm after placing
            BeatmapAssertion.CollectionCount<BaseBpmEvent>(3);

            Assert.AreEqual(0, bpmEvent0.JsonTime, 0.001f);
            Assert.AreEqual(1, bpmEvent1.JsonTime, 0.001f);
            Assert.AreEqual(2, bpmEvent2.JsonTime, 0.001f);

            Assert.AreEqual(0, bpmEvent0.SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111, bpmEvent1.SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111 + songBpm / 222, bpmEvent2.SongBpmTime, 0.001f);

            // Undo should remove everything
            PlaceUtils.Undo();
            BeatmapAssertion.CollectionCount<BaseBpmEvent>(0);

            // Redo should replace objects in the same positions
            var redone = PlaceUtils.Redo<BaseBpmEvent>().ToList();
            BeatmapAssertion.CollectionCount<BaseBpmEvent>(3);

            Assert.AreEqual(0, redone[0].JsonTime, 0.001f);
            Assert.AreEqual(1, redone[1].JsonTime, 0.001f);
            Assert.AreEqual(2, redone[2].JsonTime, 0.001f);

            Assert.AreEqual(0, redone[0].SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111, redone[1].SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111 + songBpm / 222, redone[2].SongBpmTime, 0.001f);
        }
    }
}