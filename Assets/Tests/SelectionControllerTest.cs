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
        private SelectionFixture _fixture;

        [SetUp]
        public void PlaceObjects() => _fixture = new SelectionFixture();

        [Test]
        public void SelectBetweenNotes()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Note3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotes());
        }

        [Test]
        public void SelectBetweenEvents()
        {
            SelectionController.SelectBetween(_fixture.Event1, _fixture.Event3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenEvents());
        }

        [Test]
        public void SelectBetweenBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.BpmEvent1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenBpmEvents());
        }

        [Test]
        public void SelectBetweenNotesAndEvents()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.Event3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotesAndEvents());
        }

        [Test]
        public void SelectBetweenNotesAndBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.Note1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenNotesAndBpmEvents());
        }

        [Test]
        public void SelectBetweenEventsAndBpmEvents()
        {
            SelectionController.SelectBetween(_fixture.Event1, _fixture.BpmEvent3);
            AssertSelectedObjects(_fixture.ExpectedSelectBetweenEventsAndBpmEvents());
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

        private class SelectionFixture
        {
            public readonly BaseArc Arc02;
            public readonly BaseArc Arc04;
            public readonly BaseArc Arc24;
            public readonly BaseArc Arc44;
            public readonly BaseBpmEvent BpmEvent1;
            public readonly BaseBpmEvent BpmEvent2;
            public readonly BaseBpmEvent BpmEvent3;
            public readonly BaseEvent Event1;
            public readonly BaseEvent Event2;
            public readonly BaseEvent Event3;
            public readonly BaseEvent Event4;
            public readonly BaseEvent RotationEvent2;
            public readonly BaseNote Note1;
            public readonly BaseNote Note2;
            public readonly BaseNote Note3;
            public readonly BaseNote Note4;

            public SelectionFixture()
            {
                BpmEvent1 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 1, Bpm = 100 });
                BpmEvent2 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 2, Bpm = 100 });
                BpmEvent3 = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 3, Bpm = 100 });

                Note1 = PlaceUtils.Place(new BaseNote { JsonTime = 1 });
                Note2 = PlaceUtils.Place(new BaseNote { JsonTime = 2 });
                Note3 = PlaceUtils.Place(new BaseNote { JsonTime = 3 });
                Note4 = PlaceUtils.Place(new BaseNote { JsonTime = 4 });

                Event1 = PlaceUtils.Place(new BaseEvent { JsonTime = 1 });
                Event2 = PlaceUtils.Place(new BaseEvent { JsonTime = 2 });
                Event3 = PlaceUtils.Place(new BaseEvent { JsonTime = 3 });
                Event4 = PlaceUtils.Place(new BaseEvent { JsonTime = 4 });

                RotationEvent2 = PlaceUtils.Place(
                    new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.EarlyLaneRotation });

                Arc02 = PlaceUtils.Place(new BaseArc { JsonTime = 0, TailJsonTime = 2 });
                Arc04 = PlaceUtils.Place(new BaseArc { JsonTime = 0, TailJsonTime = 4 });
                Arc24 = PlaceUtils.Place(new BaseArc { JsonTime = 2, TailJsonTime = 4 });
                Arc44 = PlaceUtils.Place(new BaseArc { JsonTime = 4, TailJsonTime = 4 });
            }

            public ICollection<BaseObject> ExpectedSelectBetweenNotes() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc02, Arc04, Arc24
            };

            public ICollection<BaseObject> ExpectedSelectBetweenEvents() => new List<BaseObject>
            {
                Event1, Event2, Event3, RotationEvent2
            };

            public ICollection<BaseObject> ExpectedSelectBetweenBpmEvents() => new List<BaseObject>
            {
                BpmEvent1, BpmEvent2, BpmEvent3
            };

            public ICollection<BaseObject> ExpectedSelectBetweenNotesAndEvents() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc02, Arc04, Arc24, Event1, Event2, Event3, RotationEvent2
            };

            public ICollection<BaseObject> ExpectedSelectBetweenNotesAndBpmEvents() => new List<BaseObject>
            {
                Note1, Note2, Note3, Arc02, Arc04, Arc24, BpmEvent1, BpmEvent2, BpmEvent3
            };

            public ICollection<BaseObject> ExpectedSelectBetweenEventsAndBpmEvents() => new List<BaseObject>
            {
                Event1, Event2, Event3, RotationEvent2, BpmEvent1, BpmEvent2, BpmEvent3
            };
        }

        [Test]
        public void ShiftSelectionOutsideVanillaGrid([Values] bool isVanillaOnlyShiftSettingEnabled)
        {
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var note = _fixture.Note1;

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
