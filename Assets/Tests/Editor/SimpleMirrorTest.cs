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

        protected override IEnumerator OnMapLoaded()
        {
            _mirror = Object.FindAnyObjectByType<MirrorSelection>();
            yield break;
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
        public void MirrorNoteME()
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
        public void MirrorNoteNE()
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

        // Should mirror to first relevant lightID
        [TestCase("[1]", "[10]", EventGridContainer.PropMode.Light)]
        [TestCase("[2]", "[9]", EventGridContainer.PropMode.Light)]
        [TestCase("[1,2]", "[10]", EventGridContainer.PropMode.Light)]

        // Should mirror to first relevant lightID group
        [TestCase("[1]", "[9,10]", EventGridContainer.PropMode.Prop)]
        [TestCase("[2]", "[9,10]", EventGridContainer.PropMode.Prop)]
        [TestCase("[1,2]", "[9,10]", EventGridContainer.PropMode.Prop)]
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
        public void MirrorEventGradient()
        {
            Settings.Instance.MapVersion = 2;

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f,
                CustomData = JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}")
            };

            var originalEventA = BeatmapFactory.Clone(eventA);
            eventA = PlaceUtils.Place(eventA);

            var expectedMirrored = BeatmapFactory.Clone(originalEventA);
            expectedMirrored.Value = (int)LightValue.BlueFade;
            expectedMirrored.CustomData =
                JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0, 1], \"_endColor\": [1, 0, 0, 1], \"_easing\": \"easeLinear\"}}");

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
        public void MirrorWallME()
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
        public void MirrorWallNE()
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
