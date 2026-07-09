using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class SelectionControllerTest : TestBase
    {
        private BaseArc arc02, arc04, arc24, arc44;
        private BaseBpmEvent bpmEvent1, bpmEvent2, bpmEvent3;
        private BaseEvent event1, event2, event3, event4, rotationEvent2;
        private BaseNote note1, note2, note3, note4;

        [SetUp]
        public void PlaceObjects()
        {
            bpmEvent1 = new BaseBpmEvent { JsonTime = 1, Bpm = 100 };
            bpmEvent2 = new BaseBpmEvent { JsonTime = 2, Bpm = 100 };
            bpmEvent3 = new BaseBpmEvent { JsonTime = 3, Bpm = 100 };
            bpmEvent1 = PlaceUtils.Place(bpmEvent1);
            bpmEvent2 = PlaceUtils.Place(bpmEvent2);
            bpmEvent3 = PlaceUtils.Place(bpmEvent3);

            note1 = new BaseNote { JsonTime = 1 };
            note2 = new BaseNote { JsonTime = 2 };
            note3 = new BaseNote { JsonTime = 3 };
            note4 = new BaseNote { JsonTime = 4 };
            note1 = PlaceUtils.Place(note1);
            note2 = PlaceUtils.Place(note2);
            note3 = PlaceUtils.Place(note3);
            note4 = PlaceUtils.Place(note4);

            event1 = new BaseEvent { JsonTime = 1 };
            event2 = new BaseEvent { JsonTime = 2 };
            event3 = new BaseEvent { JsonTime = 3 };
            event4 = new BaseEvent { JsonTime = 4 };
            event1 = PlaceUtils.Place(event1);
            event2 = PlaceUtils.Place(event2);
            event3 = PlaceUtils.Place(event3);
            event4 = PlaceUtils.Place(event4);

            rotationEvent2 = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.EarlyLaneRotation };
            rotationEvent2 = PlaceUtils.Place(rotationEvent2);

            arc02 = new BaseArc { JsonTime = 0, TailJsonTime = 2 };
            arc04 = new BaseArc { JsonTime = 0, TailJsonTime = 4 };
            arc24 = new BaseArc { JsonTime = 2, TailJsonTime = 4 };
            arc44 = new BaseArc { JsonTime = 4, TailJsonTime = 4 };
            arc02 = PlaceUtils.Place(arc02);
            arc04 = PlaceUtils.Place(arc04);
            arc24 = PlaceUtils.Place(arc24);
            arc44 = PlaceUtils.Place(arc44);
        }

        [Test]
        public void SelectBetweenNotes()
        {
            SelectionController.SelectBetween(note1, note3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    note1,
                    note2,
                    note3,
                    arc02,
                    arc04,
                    arc24
                });
        }

        [Test]
        public void SelectBetweenEvents()
        {
            SelectionController.SelectBetween(event1, event3);
            AssertSelectedObjects(new List<BaseObject> { event1, event2, event3, rotationEvent2 });
        }

        [Test]
        public void SelectBetweenBpmEvents()
        {
            SelectionController.SelectBetween(bpmEvent1, bpmEvent3);
            AssertSelectedObjects(new List<BaseObject> { bpmEvent1, bpmEvent2, bpmEvent3 });
        }

        [Test]
        public void SelectBetweenNotesAndEvents()
        {
            SelectionController.SelectBetween(note1, event3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    note1,
                    note2,
                    note3,
                    arc02,
                    arc04,
                    arc24,
                    event1,
                    event2,
                    event3,
                    rotationEvent2
                });
        }

        [Test]
        public void SelectBetweenNotesAndBpmEvents()
        {
            SelectionController.SelectBetween(note1, bpmEvent3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    note1,
                    note2,
                    note3,
                    arc02,
                    arc04,
                    arc24,
                    bpmEvent1,
                    bpmEvent2,
                    bpmEvent3
                });
        }

        [Test]
        public void SelectBetweenEventsAndBpmEvents()
        {
            SelectionController.SelectBetween(event1, bpmEvent3);
            AssertSelectedObjects(
                new List<BaseObject>
                {
                    event1,
                    event2,
                    event3,
                    rotationEvent2,
                    bpmEvent1,
                    bpmEvent2,
                    bpmEvent3
                });
        }

        private void AssertSelectedObjects(ICollection<BaseObject> objects)
        {
            foreach (var selectedObject in objects)
                Assert.True(
                    SelectionController.SelectedObjects.Contains(selectedObject),
                    $"{selectedObject} should be selected");

            Assert.AreEqual(
                objects.Count,
                SelectionController.SelectedObjects.Count,
                "Selection should be the exact amount");
        }

        [Test]
        public void ShiftSelectionOutsideVanillaGrid([Values] bool isVanillaOnlyShiftSettingEnabled)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = note1;

            SelectionController.Select(note);

            Assert.AreEqual(1, SelectionController.SelectedObjects.Count, "Note should be selected");
            Assert.AreEqual(0, note.PosX);
            Assert.AreEqual(0, note.PosY);


            Settings.Instance.VanillaOnlyShift = isVanillaOnlyShiftSettingEnabled;

            selectionController.ShiftSelection(5, 5);
            note = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

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