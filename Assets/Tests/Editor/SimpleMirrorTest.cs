using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Visual
{
    public class SimpleMirrorTest : TestBase
    {
        private MirrorSelection _mirror;
        private PlacementLaneController _laneController;
        private int _originalLaneCount;

        protected override IEnumerator OnMapLoaded()
        {
            _mirror = Object.FindAnyObjectByType<MirrorSelection>();
            _laneController = Object.FindAnyObjectByType<PlacementLaneController>();
            Assert.NotNull(_laneController, "Mirror lane tests require the gameplay placement lane controller.");
            _originalLaneCount = _laneController.LaneCount;
            yield break;
        }

        protected override void AfterCleanup()
        {
            // Restore the shared editor grid after each parameterized case so lane-count tests cannot affect later tests.
            if (_laneController != null)
                _laneController.LaneCount = _originalLaneCount;
        }

        [SetUp]
        public void SetUp()
        {
            Settings.Instance.MapVersion = 3;
        }

        [Test]
        public void MirrorNoteDouble()
        {
            var noteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.MiddleLeft,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            };
            var noteB = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.MiddleRight,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Blue,
                CutDirection = (int)NoteCutDirection.Down
            };

            var originalNoteA = BeatmapFactory.Clone(noteA);
            noteA = PlaceUtils.Place(noteA);
            var originalNoteB = BeatmapFactory.Clone(noteB);
            noteB = PlaceUtils.Place(noteB);

            var expectedA = BeatmapFactory.Clone(originalNoteA);
            var expectedB = BeatmapFactory.Clone(originalNoteB);

            SelectionController.Select(noteA);
            SelectionController.Select(noteB, true);

            _mirror.Mirror();
            AssertNoteDoubleState(
                SelectionController.SelectedObjects.OfType<BaseNote>().ToList(),
                expectedA,
                expectedB);

            _mirror.Mirror();
            AssertNoteDoubleState(
                SelectionController.SelectedObjects.OfType<BaseNote>().ToList(),
                expectedA,
                expectedB);

            var undoSecondMirrorObjects = PlaceUtils.Undo<BaseNote>().ToList();
            AssertNoteDoubleState(undoSecondMirrorObjects, expectedA, expectedB);

            var undoFirstMirrorObjects = PlaceUtils.Undo<BaseNote>().ToList();
            AssertNoteDoubleState(undoFirstMirrorObjects, expectedA, expectedB);
        }

        [Test]
        public void MirrorNotesAcrossFullGrid()
        {
            var noteA = new BaseNote
            {
                JsonTime = 2,
                PosX = 0,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            };
            var noteB = new BaseNote
            {
                JsonTime = 2,
                PosX = 1,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Blue,
                CutDirection = (int)NoteCutDirection.Down
            };
            var noteD = new BaseNote
            {
                JsonTime = 2,
                PosX = 3,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Blue,
                CutDirection = (int)NoteCutDirection.Down
            };

            noteA = PlaceUtils.Place(noteA);
            noteB = PlaceUtils.Place(noteB);
            // Leave the unselected destination lane empty so this full-grid test does not create an intentional collision.
            noteD = PlaceUtils.Place(noteD);

            // Select sparse lanes; standard notes still mirror across the physical four-lane grid.
            SelectionController.Select(noteA);
            SelectionController.Select(noteB, true);
            SelectionController.Select(noteD, true);

            _mirror.Mirror();

            var mirroredNotes = SelectionController.SelectedObjects
                .OfType<BaseNote>()
                .OrderBy(note => note.PosX)
                .ToList();

            Assert.AreEqual(3, mirroredNotes.Count, "Mirrored sparse selection should keep the same note count");
            CollectionAssert.AreEqual(
                new[] { 0, 2, 3 },
                mirroredNotes.Select(note => note.PosX).ToArray(),
                "Note mirroring should use the full physical lane grid");

            var laneZeroNote = mirroredNotes.Single(note => note.PosX == 0);
            var laneTwoNote = mirroredNotes.Single(note => note.PosX == 2);
            var laneThreeNote = mirroredNotes.Single(note => note.PosX == 3);
            Assert.AreEqual((int)NoteType.Red, laneZeroNote.Type, "Lane 3 note should mirror into lane 0");
            Assert.AreEqual((int)NoteType.Red, laneTwoNote.Type, "Lane 1 note should mirror into lane 2");
            Assert.AreEqual((int)NoteType.Blue, laneThreeNote.Type, "Lane 0 note should mirror into lane 3");
        }

        [Test]
        [TestCase(1, 0, 0)]
        [TestCase(4, 0, 3)]
        [TestCase(6, 1, 4)]
        [TestCase(6, 0, 5)]
        [TestCase(6, 2, 3)]
        [TestCase(6, 7, -2)] // wall 2 outside should mirror 2 outside the other direction
        [TestCase(6, 4, 1)]
        public void MirrorNoteUsesLoadedLaneCount(int laneCount, int originalLane, int mirroredLane)
        {
            // Standard note lanes reflect arithmetically across the currently configured physical grid.
            _laneController.LaneCount = laneCount;
            var note = PlaceUtils.Place(new BaseNote
            {
                JsonTime = 2,
                PosX = originalLane,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            });

            SelectionController.Select(note);
            _mirror.Mirror();

            var mirrored = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            Assert.AreEqual(mirroredLane, mirrored.PosX, "The note should mirror using the loaded lane count.");
        }

        [Test]
        [TestCase(1, 0, 1, 0)]
        [TestCase(4, 0, 1, 3)]
        [TestCase(6, 1, 2, 3)]
        [TestCase(6, 7, 1, -2)]
        public void MirrorWallUsesLoadedLaneCount(int laneCount, int originalLane, int width, int mirroredLane)
        {
            // Wall reflection uses the lane count and width directly, including walls starting outside the visible grid.
            _laneController.LaneCount = laneCount;
            var wall = PlaceUtils.Place(new BaseObstacle
            {
                JsonTime = 2,
                PosX = originalLane,
                PosY = (int)GridY.Base,
                Duration = 1,
                Width = width,
                Height = 5
            });

            SelectionController.Select(wall);
            _mirror.Mirror();

            var mirrored = SelectionController.SelectedObjects.OfType<BaseObstacle>().Single();
            Assert.AreEqual(mirroredLane, mirrored.PosX, "The wall should mirror using lane count minus position minus width.");
        }

        [Test]
        public void MirrorNotesAcrossFullGridPreservesOccupiedDestinations()
        {
            var noteA = new BaseNote
            {
                JsonTime = 2,
                PosX = 0,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            };
            var noteB = new BaseNote
            {
                JsonTime = 2,
                PosX = 1,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Blue,
                CutDirection = (int)NoteCutDirection.Down
            };
            var occupiedDestination = new BaseNote
            {
                JsonTime = 2,
                PosX = 2,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Bomb,
                CutDirection = (int)NoteCutDirection.Down
            };
            var noteD = new BaseNote
            {
                JsonTime = 2,
                PosX = 3,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Blue,
                CutDirection = (int)NoteCutDirection.Down
            };

            noteA = PlaceUtils.Place(noteA);
            noteB = PlaceUtils.Place(noteB);
            // Keep the destination occupied by an unselected note to define collision behavior explicitly.
            occupiedDestination = PlaceUtils.Place(occupiedDestination);
            noteD = PlaceUtils.Place(noteD);

            SelectionController.Select(noteA);
            SelectionController.Select(noteB, true);
            SelectionController.Select(noteD, true);

            _mirror.Mirror();

            // Use the typed note collection because the base collection does not expose map objects.
            var noteCollection = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var laneTwoNotes = noteCollection.MapObjects
                .OfType<BaseNote>()
                .Where(note => note.PosX == 2)
                .ToList();
            Assert.AreEqual(4, noteCollection.MapObjects.Count, "Mirroring should retain the unselected destination note");
            Assert.AreEqual(2, laneTwoNotes.Count, "Mirroring should preserve both notes at the occupied destination");
            CollectionAssert.AreEquivalent(
                new[] { (int)NoteType.Red, (int)NoteType.Bomb },
                laneTwoNotes.Select(note => note.Type).ToArray(),
                "The mirrored note and unselected destination note should coexist");

            var undoNotes = PlaceUtils.Undo<BaseNote>().ToList();
            Assert.AreEqual(4, noteCollection.MapObjects.Count, "Undo should restore the complete original selection and destination");
            Assert.AreEqual(1, noteCollection.MapObjects.Count(note => note.PosX == 2), "Undo should remove the mirrored collision");
            Assert.AreEqual((int)NoteType.Bomb, noteCollection.MapObjects.Single(note => note.PosX == 2).Type);
            Assert.AreEqual(3, undoNotes.Count, "Undo should return only the selected mirrored notes");
        }

        private void AssertNoteDoubleState(IReadOnlyList<BaseNote> notes, BaseNote expectedA, BaseNote expectedB)
        {
            Assert.AreEqual(2, notes.Count, "Notes should not be deleted");
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count, "Mirrored notes should be selected");
            var sortedNotes = notes
                .OrderBy(note => note.JsonTime)
                .ThenBy(note => note.PosX)
                .ThenBy(note => note.PosY)
                .ToList();
            BeatmapAssertion.IsEqual(expectedA, sortedNotes[0], "Left note after mirror");
            BeatmapAssertion.IsEqual(expectedB, sortedNotes[1], "Right note after mirror");
        }

        [Test]
        public void MirrorNoteMappingExtensionsPrecision()
        {
            var noteA =
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = -2345,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };

            var originalNoteA = BeatmapFactory.Clone(noteA);
            noteA = PlaceUtils.Place(noteA);

            var expectedMirrored = BeatmapFactory.Clone(originalNoteA);
            expectedMirrored.PosX = 5345;
            expectedMirrored.Type = (int)NoteType.Blue;
            expectedMirrored.CutDirection = (int)NoteCutDirection.Right;
            expectedMirrored.AngleOffset = 0;

            var expectedOriginal = BeatmapFactory.Clone(originalNoteA);
            expectedOriginal.AngleOffset = 0;

            SelectionController.Select(noteA);

            _mirror.Mirror();
            noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            BeatmapAssertion.IsEqual(expectedMirrored, noteA, "Perform note mirror");

            // Undo mirror
            var undoObjects = PlaceUtils.Undo<BaseNote>().ToList();
            BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo note mirror");
        }

        [Test]
        public void MirrorNoteNoodleExtensionsCoordinates()
        {
            var noteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left,
                CustomData = JSON.Parse("{\"coordinates\": [-1, 0]}")
            };

            var originalNoteA = BeatmapFactory.Clone(noteA);
            noteA = PlaceUtils.Place(noteA);

            var expectedMirrored = BeatmapFactory.Clone(originalNoteA);
            expectedMirrored.PosX = (int)GridX.Right;
            expectedMirrored.Type = (int)NoteType.Blue;
            expectedMirrored.CutDirection = (int)NoteCutDirection.Right;
            expectedMirrored.AngleOffset = 0;
            expectedMirrored.CustomData = JSON.Parse($"{{\"{noteA.CustomKeyCoordinate}\": [0, 0]}}");

            var expectedOriginal = BeatmapFactory.Clone(originalNoteA);
            expectedOriginal.AngleOffset = 0;
            expectedOriginal.CustomData = JSON.Parse($"{{\"{noteA.CustomKeyCoordinate}\": [-1, 0]}}");

            SelectionController.Select(noteA);

            _mirror.Mirror();
            noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            BeatmapAssertion.IsEqual(expectedMirrored, noteA, "Perform NE note mirror");

            // Undo mirror
            var undoObjects = PlaceUtils.Undo<BaseNote>().ToList();
            BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo NE note inversion");
        }

        [Test]
        [TestCase(null, null, EventGridContainer.PropMode.Off)]
        [TestCase(null, null, EventGridContainer.PropMode.Light)]
        [TestCase(null, null, EventGridContainer.PropMode.Prop)]

        // Should not affect lightID if off
        [TestCase("[1]", "[1]", EventGridContainer.PropMode.Off)]
        [TestCase("[2]", "[2]", EventGridContainer.PropMode.Off)]
        [TestCase("[1,2]", "[1,2]", EventGridContainer.PropMode.Off)]

        // A single selected lane is its own mirror in light-ID mode.
        [TestCase("[1]", "[1]", EventGridContainer.PropMode.Light)]
        [TestCase("[2]", "[2]", EventGridContainer.PropMode.Light)]
        [TestCase("[1,2]", "[1,2]", EventGridContainer.PropMode.Light)]

        // A single selected propagation group is its own mirror.
        [TestCase("[1]", "[1]", EventGridContainer.PropMode.Prop)]
        [TestCase("[2]", "[2]", EventGridContainer.PropMode.Prop)]
        [TestCase("[1,2]", "[1,2]", EventGridContainer.PropMode.Prop)]
        public void MirrorEventLightID(string original, string mirror, EventGridContainer.PropMode propMode)
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f,
                CustomData = JSON.Parse($"{{\"lightID\": {original}}}")
            };

            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);

            var expectedMirrored = BeatmapFactory.Clone(originalEventA);
            expectedMirrored.Value = (int)LightValue.BlueFade;
            expectedMirrored.CustomData = JSON.Parse($"{{\"lightID\": {mirror}}}");

            var expectedOriginal = BeatmapFactory.Clone(originalEventA);

            SelectionController.Select(eventA);

            eventsContainer.EventTypeToPropagate = eventA.Type;
            eventsContainer.PropagationEditing = propMode;

            _mirror.Mirror();
            // I'm sorry if you're here after changing the lightID mapping for default env
            eventA = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            BeatmapAssertion.IsEqual(expectedMirrored, eventA, "Perform mirror lightID event");

            // Undo mirror
            var undoObjects = PlaceUtils.Undo<BaseEvent>().ToList();
            BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo mirror lightID event");

            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
        }

        [Test]
        public void MirrorSelectedLightIdLanesOnly()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var events = new[]
            {
                PlaceUtils.Place(CreateLightIdEvent(1)),
                PlaceUtils.Place(CreateLightIdEvent(2)),
                PlaceUtils.Place(CreateLightIdEvent(3))
            };

            // Select three adjacent physical lanes so their mirror remains entirely within that set.
            SelectionController.Select(events[0]);
            SelectionController.Select(events[1], true);
            SelectionController.Select(events[2], true);
            eventsContainer.EventTypeToPropagate = (int)EventTypeValue.Event0;
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;

            _mirror.Mirror();

            var mirroredIds = SelectionController.SelectedObjects
                .OfType<BaseEvent>()
                .OrderBy(evt => evt.JsonTime)
                .Select(evt => evt.CustomLightID.Single())
                .ToArray();
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, mirroredIds);

            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
        }

        [Test]
        public void MirrorBasicLightEventsPhysicallyPreservesColor()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var eventA = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f
            });
            var eventB = PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 3,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.BlueFade,
                FloatValue = 1f
            });
            var originalEventAValue = eventA.Value;
            var originalEventBValue = eventB.Value;

            // Physical mirroring should exchange the selected light lanes while leaving each event color unchanged.
            SelectionController.Select(eventA);
            SelectionController.Select(eventB, true);
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;

            _mirror.Mirror();

            var mirroredEvents = SelectionController.SelectedObjects
                .OfType<BaseEvent>()
                .OrderBy(evt => evt.JsonTime)
                .ToList();
            Assert.AreEqual((int)EventTypeValue.Event1, mirroredEvents[0].Type);
            Assert.AreEqual(originalEventAValue, mirroredEvents[0].Value);
            Assert.AreEqual((int)EventTypeValue.Event0, mirroredEvents[1].Type);
            Assert.AreEqual(originalEventBValue, mirroredEvents[1].Value);
        }

        [Test]
        public void MirrorManyLightIdEventsPhysicallyPreservesColors()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var events = new[]
            {
                PlaceUtils.Place(CreateLightIdEventWithValue(1, LightValue.RedFade)),
                PlaceUtils.Place(CreateLightIdEventWithValue(2, LightValue.BlueFade)),
                PlaceUtils.Place(CreateLightIdEventWithValue(3, LightValue.WhiteFade)),
                PlaceUtils.Place(CreateLightIdEventWithValue(4, LightValue.RedFade))
            };
            var originalValues = events.Select(evt => evt.Value).ToArray();

            // Light-ID mode must mirror a populated physical lane selection without also inverting its event colors.
            foreach (var evt in events)
            {
                SelectionController.Select(evt, SelectionController.HasSelectedObjects());
            }

            eventsContainer.EventTypeToPropagate = (int)EventTypeValue.Event0;
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;
            _mirror.Mirror();

            var mirroredEvents = SelectionController.SelectedObjects
                .OfType<BaseEvent>()
                .OrderBy(evt => evt.JsonTime)
                .ToList();
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, mirroredEvents.Select(evt => evt.CustomLightID.Single()).ToArray());
            CollectionAssert.AreEqual(
                originalValues,
                mirroredEvents.Select(evt => evt.Value).ToArray());

            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
        }

        // Keep test light events in time order so each selected visible lane can be asserted independently.
        private static BaseEvent CreateLightIdEvent(int lightId) => new()
        {
            JsonTime = lightId,
            Type = (int)EventTypeValue.Event0,
            Value = (int)LightValue.RedFade,
            FloatValue = 1f,
            CustomData = JSON.Parse($"{{\"lightID\": [{lightId}]}}")
        };

        // Create varied colors so a physical mirror cannot accidentally pass by inverting every event uniformly.
        private static BaseEvent CreateLightIdEventWithValue(int lightId, LightValue value) => new()
        {
            JsonTime = lightId,
            Type = (int)EventTypeValue.Event0,
            Value = (int)value,
            FloatValue = 1f,
            CustomData = JSON.Parse($"{{\"lightID\": [{lightId}]}}")
        };

        [Test]
        public void MirrorEventGradient()
        {
            Settings.Instance.MapVersion = 2;

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f,
                // Opaque gradient colors serialize canonically without an explicit alpha component.
                CustomData = JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0], \"_endColor\": [0, 1, 0], \"_easing\": \"easeLinear\"}}")
            };

            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);

            var expectedMirrored = BeatmapFactory.Clone(originalEventA);
            expectedMirrored.Value = (int)LightValue.BlueFade;
            expectedMirrored.CustomData =
                JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0], \"_endColor\": [1, 0, 0], \"_easing\": \"easeLinear\"}}");

            var expectedOriginal = BeatmapFactory.Clone(originalEventA);

            SelectionController.Select(eventA);

            _mirror.Mirror();
            eventA = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            BeatmapAssertion.IsEqual(expectedMirrored, eventA, "Perform mirror gradient event");

            // Undo mirror
            var undoObjects = PlaceUtils.Undo<BaseEvent>().ToList();
            BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo mirror gradient event");
        }

        [Test]
        public void MirrorEventRedBlue()
        {
            // A single event has no lane counterpart, but Mirror still swaps its red/blue value.
            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f
            };

            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);

            var expectedBlue = BeatmapFactory.Clone(originalEventA);
            expectedBlue.Value = (int)LightValue.BlueFade;

            var expectedRed = BeatmapFactory.Clone(originalEventA);

            SelectionController.Select(eventA);

            _mirror.Mirror();
            eventA = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            BeatmapAssertion.IsEqual(expectedBlue, eventA, "Perform mirror event");

            _mirror.Mirror();
            eventA = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            BeatmapAssertion.IsEqual(expectedRed, eventA, "Perform mirror event again");
        }

        [Test]
        public void MirrorBasicLightEventsAcrossMultipleLanes()
        {
            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f
            };
            var eventB = new BaseEvent
            {
                JsonTime = 3,
                Type = (int)EventTypeValue.Event1,
                Value = (int)LightValue.BlueFade,
                FloatValue = 1f
            };

            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);
            SelectionController.Select(eventA);
            SelectionController.Select(eventB, true);

            _mirror.Mirror();

            var mirroredEvents = SelectionController.SelectedObjects
                .OfType<BaseEvent>()
                .OrderBy(evt => evt.JsonTime)
                .ToList();

            // Multi-lane basic-light mirrors exchange event lanes/types while preserving each event's color.
            Assert.AreEqual(2, mirroredEvents.Count);
            Assert.AreEqual((int)EventTypeValue.Event1, mirroredEvents[0].Type);
            Assert.AreEqual((int)LightValue.RedFade, mirroredEvents[0].Value);
            Assert.AreEqual((int)EventTypeValue.Event0, mirroredEvents[1].Type);
            Assert.AreEqual((int)LightValue.BlueFade, mirroredEvents[1].Value);

            var undoEvents = PlaceUtils.Undo<BaseEvent>().OrderBy(evt => evt.JsonTime).ToList();
            Assert.AreEqual((int)EventTypeValue.Event0, undoEvents[0].Type);
            Assert.AreEqual((int)LightValue.RedFade, undoEvents[0].Value);
            Assert.AreEqual((int)EventTypeValue.Event1, undoEvents[1].Type);
            Assert.AreEqual((int)LightValue.BlueFade, undoEvents[1].Value);
        }

        [Test]
        public void MirrorEventRedWhiteBlue()
        {
            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f
            };

            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);

            var expectedWhite = BeatmapFactory.Clone(originalEventA);
            expectedWhite.Value = (int)LightValue.WhiteFade;

            var expectedBlue = BeatmapFactory.Clone(originalEventA);
            expectedBlue.Value = (int)LightValue.BlueFade;

            var expectedRed = BeatmapFactory.Clone(originalEventA);

            SelectionController.Select(eventA);

            _mirror.Mirror(false);
            eventA = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            BeatmapAssertion.IsEqual(expectedWhite, eventA, "Perform mirror cycle event");

            _mirror.Mirror(false);
            eventA = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            BeatmapAssertion.IsEqual(expectedBlue, eventA, "Perform mirror cycle event 2");

            _mirror.Mirror(false);
            eventA = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();
            BeatmapAssertion.IsEqual(expectedRed, eventA, "Perform mirror cycle event 3");
        }

        [Test]
        public void MirrorWallMappingExtensionsPrecision()
        {
            // What the actual fuck - example from mirroring in MMA2
            //{"_time":1.5,"_lineIndex":1446,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
            //{"_time":1.5,"_lineIndex":2958,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
            var wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = 1446,
                Type = 595141,
                Duration = 1,
                Width = 2596
            };

            var originalWallA = BeatmapFactory.Clone(wallA);
            wallA = PlaceUtils.Place(wallA);

            var expectedMirrored = BeatmapFactory.Clone(originalWallA);
            expectedMirrored.PosX = 2958;

            var expectedOriginal = BeatmapFactory.Clone(originalWallA);

            SelectionController.Select(wallA);

            _mirror.Mirror();
            wallA = SelectionController.SelectedObjects.OfType<BaseObstacle>().Single();
            BeatmapAssertion.IsEqual(expectedMirrored, wallA, "Perform ME wall mirror");

            // Undo mirror
            var undoObjects = PlaceUtils.Undo<BaseObstacle>().ToList();
            BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo ME wall mirror");
        }

        [Test]
        public void MirrorWallNoodleExtensionsCoordinates()
        {
            var wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Duration = 1,
                Width = 2,
                Height = 5,
                CustomData = JSON.Parse("{\"coordinates\": [-1.5, 0]}")
            };

            var originalWallA = BeatmapFactory.Clone(wallA);
            wallA = PlaceUtils.Place(wallA);

            var expectedMirrored = BeatmapFactory.Clone(originalWallA);
            expectedMirrored.PosX = (int)GridX.MiddleRight;
            expectedMirrored.Type = (int)ObstacleType.Full;
            expectedMirrored.CustomData = JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-0.5, 0]}}");

            var expectedOriginal = BeatmapFactory.Clone(originalWallA);
            expectedOriginal.Type = (int)ObstacleType.Full;
            expectedOriginal.CustomData = JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-1.5, 0]}}");

            SelectionController.Select(wallA);

            _mirror.Mirror();
            wallA = SelectionController.SelectedObjects.OfType<BaseObstacle>().Single();
            BeatmapAssertion.IsEqual(expectedMirrored, wallA, "Perform NE wall mirror");

            // Undo mirror
            var undoObjects = PlaceUtils.Undo<BaseObstacle>().ToList();
            BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo NE wall mirror");
        }

        // TODO: update rotation event test for more representative
        [Test]
        public void MirrorRotationEvent()
        {
            var eventA = new BaseRotationEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 33
            };

            // fuck kinda conflict did u have?
            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);

            var expectedMirrored = BeatmapFactory.Clone(originalEventA);
            expectedMirrored.Type = 15;
            expectedMirrored.Rotation = -33;

            var expectedUndo = BeatmapFactory.Clone(originalEventA);
            expectedUndo.Type = 15;

            SelectionController.Select(eventA);

            _mirror.Mirror();
            eventA = SelectionController.SelectedObjects.OfType<BaseRotationEvent>().Single();
            BeatmapAssertion.IsEqual(expectedMirrored, eventA, "Perform mirror rotation event");

            // Undo mirror
            var undoObjects = PlaceUtils.Undo<BaseRotationEvent>().ToList();
            BeatmapAssertion.IsEqual(expectedUndo, undoObjects[0], "Undo mirror rotation event");
        }
    }
}
