using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class SelectionControllerTest : TestBase
    {
        private BaseArc baseArc02, baseArc04, baseArc24, baseArc44;
        private BaseBpmEvent baseBpmEvent1, baseBpmEvent2, baseBpmEvent3;
        private BaseEvent baseEvent1, baseEvent2, baseEvent3, baseEvent4, baseRotationEvent2;
        private BaseNote baseNote1, baseNote2, baseNote3, baseNote4;

        [SetUp]
        public void PlaceObjects()
        {
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var bpmEventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);

            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();
            var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();

            baseBpmEvent1 = new BaseBpmEvent { JsonTime = 1, Bpm = 100 };
            baseBpmEvent2 = new BaseBpmEvent { JsonTime = 2, Bpm = 100 };
            baseBpmEvent3 = new BaseBpmEvent { JsonTime = 3, Bpm = 100 };
            baseBpmEvent1 = PlaceUtils.Place(baseBpmEvent1);
            baseBpmEvent2 = PlaceUtils.Place(baseBpmEvent2);
            baseBpmEvent3 = PlaceUtils.Place(baseBpmEvent3);

            baseNote1 = new BaseNote { JsonTime = 1 };
            baseNote2 = new BaseNote { JsonTime = 2 };
            baseNote3 = new BaseNote { JsonTime = 3 };
            baseNote4 = new BaseNote { JsonTime = 4 };
            baseNote1 = PlaceUtils.Place(baseNote1);
            baseNote2 = PlaceUtils.Place(baseNote2);
            baseNote3 = PlaceUtils.Place(baseNote3);
            baseNote4 = PlaceUtils.Place(baseNote4);

            baseEvent1 = new BaseEvent { JsonTime = 1 };
            baseEvent2 = new BaseEvent { JsonTime = 2 };
            baseEvent3 = new BaseEvent { JsonTime = 3 };
            baseEvent4 = new BaseEvent { JsonTime = 4 };
            baseEvent1 = PlaceUtils.Place(baseEvent1);
            baseEvent2 = PlaceUtils.Place(baseEvent2);
            baseEvent3 = PlaceUtils.Place(baseEvent3);
            baseEvent4 = PlaceUtils.Place(baseEvent4);

            baseRotationEvent2 = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.EarlyLaneRotation };
            baseRotationEvent2 = PlaceUtils.Place(baseRotationEvent2);

            baseArc02 = new BaseArc { JsonTime = 0, TailJsonTime = 2 };
            baseArc04 = new BaseArc { JsonTime = 0, TailJsonTime = 4 };
            baseArc24 = new BaseArc { JsonTime = 2, TailJsonTime = 4 };
            baseArc44 = new BaseArc { JsonTime = 4, TailJsonTime = 4 };
            baseArc02 = PlaceUtils.Place(baseArc02);
            baseArc04 = PlaceUtils.Place(baseArc04);
            baseArc24 = PlaceUtils.Place(baseArc24);
            baseArc44 = PlaceUtils.Place(baseArc44);
        }

        [Test]
        public void SelectBetweenNotes()
        {
            SelectionController.SelectBetween(baseNote1, baseNote3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    baseNote1,
                    baseNote2,
                    baseNote3,
                    baseArc02,
                    baseArc04,
                    baseArc24
                });
        }

        [Test]
        public void SelectBetweenEvents()
        {
            SelectionController.SelectBetween(baseEvent1, baseEvent3);
            AssertSelectedObjects(new List<BaseObject> { baseEvent1, baseEvent2, baseEvent3, baseRotationEvent2 });
        }

        [Test]
        public void SelectBetweenBpmEvents()
        {
            SelectionController.SelectBetween(baseBpmEvent1, baseBpmEvent3);
            AssertSelectedObjects(new List<BaseObject> { baseBpmEvent1, baseBpmEvent2, baseBpmEvent3 });
        }

        [Test]
        public void SelectBetweenNotesAndEvents()
        {
            SelectionController.SelectBetween(baseNote1, baseEvent3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    baseNote1,
                    baseNote2,
                    baseNote3,
                    baseArc02,
                    baseArc04,
                    baseArc24,
                    baseEvent1,
                    baseEvent2,
                    baseEvent3,
                    baseRotationEvent2
                });
        }

        [Test]
        public void SelectBetweenNotesAndBpmEvents()
        {
            SelectionController.SelectBetween(baseNote1, baseBpmEvent3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    baseNote1,
                    baseNote2,
                    baseNote3,
                    baseArc02,
                    baseArc04,
                    baseArc24,
                    baseBpmEvent1,
                    baseBpmEvent2,
                    baseBpmEvent3
                });
        }

        [Test]
        public void SelectBetweenEventsAndBpmEvents()
        {
            SelectionController.SelectBetween(baseEvent1, baseBpmEvent3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    baseEvent1,
                    baseEvent2,
                    baseEvent3,
                    baseRotationEvent2,
                    baseBpmEvent1,
                    baseBpmEvent2,
                    baseBpmEvent3
                });
        }

        private void AssertSelectedObjects(ICollection<BaseObject> objects)
        {
            foreach (var baseObject in objects)
                Assert.True(
                    SelectionController.SelectedObjects.Contains(baseObject),
                    $"{baseObject} should be selected");

            Assert.AreEqual(
                objects.Count,
                SelectionController.SelectedObjects.Count,
                "Selection should be the exact amount");
        }

        [Test]
        public void ShiftSelectionOutsideVanillaGrid([Values] bool isVanillaOnlyShiftSettingEnabled)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var note = noteGridContainer.MapObjects[0];

            SelectionController.Select(note);

            Assert.AreEqual(1, SelectionController.SelectedObjects.Count, "Note should be selected");
            Assert.AreEqual(0, note.PosX);
            Assert.AreEqual(0, note.PosY);


            Settings.Instance.VanillaOnlyShift = isVanillaOnlyShiftSettingEnabled;

            selectionController.ShiftSelection(5, 5);
            note = noteGridContainer.MapObjects[0];

            if (isVanillaOnlyShiftSettingEnabled)
            {
                // Expect clamped values
                Assert.AreEqual((int)GridX.Right, note.PosX);
                Assert.AreEqual((int)GridY.Top, note.PosY);
                Assert.IsNull(note.CustomCoordinate);
            }
            else
            {
                Assert.AreEqual(0, note.PosX);
                Assert.AreEqual(0, note.PosY);
                Assert.NotNull(note.CustomCoordinate);
                Assert.AreEqual(3, note.CustomCoordinate[0].AsInt);
                Assert.AreEqual(5, note.CustomCoordinate[1].AsInt);
            }
        }
    }
}