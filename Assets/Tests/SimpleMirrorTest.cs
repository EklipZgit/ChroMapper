using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class SimpleMirrorTest : TestBase
    {
        private BeatmapActionContainer _actionContainer;
        private MirrorSelection _mirror;

        protected override IEnumerator OnMapLoaded()
        {
            _actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
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
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.MiddleLeft,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Down
            };
            var baseNoteB = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.MiddleRight,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Blue,
                CutDirection = (int)NoteCutDirection.Down
            };

            baseNoteA = PlaceUtils.Place(baseNoteA);
            baseNoteB = PlaceUtils.Place(baseNoteB);

            SelectionController.Select(baseNoteA);
            SelectionController.Select(baseNoteB, true);

            _mirror.Mirror();
            AssertNoteDoubleState(notesContainer);

            _mirror.Mirror();
            AssertNoteDoubleState(notesContainer);

            _actionContainer.Undo();
            AssertNoteDoubleState(notesContainer);

            _actionContainer.Undo();
            AssertNoteDoubleState(notesContainer);
        }

        private void AssertNoteDoubleState(NoteGridContainer notesContainer)
        {
            Assert.AreEqual(2, notesContainer.MapObjects.Count, "Notes should not be deleted");
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count, "Mirrored notes should be selected");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleLeft,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down,
                    AngleOffset = 0
                },
                notesContainer.MapObjects[0],
                "Left note after mirror");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleRight,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Down,
                    AngleOffset = 0
                },
                notesContainer.MapObjects[1],
                "Right note after mirror");
        }

        [Test]
        public void MirrorNoteME()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA =
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = -2345,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };

            baseNoteA = PlaceUtils.Place(baseNoteA);

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = 5345,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Right,
                    AngleOffset = 0
                },
                notesContainer.MapObjects[0],
                "Perform note mirror");

            // Undo mirror
            _actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = -2345,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0
                },
                notesContainer.MapObjects[0],
                "Undo note mirror");
        }

        [Test]
        public void MirrorNoteNE()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left,
                CustomData = JSON.Parse("{\"coordinates\": [-1, 0]}")
            };

            baseNoteA = PlaceUtils.Place(baseNoteA);

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Right,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Right,
                    AngleOffset = 0,
                    CustomData = JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [0, 0]}}")
                },
                notesContainer.MapObjects[0],
                "Perform NE note mirror");

            // Undo mirror
            _actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0,
                    CustomData = JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [-1, 0]}}")
                },
                notesContainer.MapObjects[0],
                "Undo NE note inversion");
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

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.BackLasers,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f,
                CustomData = JSON.Parse($"{{\"lightID\": {original}}}")
            };

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            eventsContainer.EventTypeToPropagate = baseEventA.Type;
            eventsContainer.PropagationEditing = propMode;

            _mirror.Mirror();
            // I'm sorry if you're here after changing the lightID mapping for default env
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.BlueFade,
                    FloatValue = 1f,
                    CustomData = JSON.Parse($"{{\"lightID\": {mirror}}}")
                },
                eventsContainer.MapObjects[0],
                "Perform mirror lightID event");

            // Undo mirror
            _actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.RedFade,
                    FloatValue = 1f,
                    CustomData = JSON.Parse($"{{\"lightID\": {original}}}")
                },
                eventsContainer.MapObjects[0],
                "Undo mirror lightID event");

            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
        }

        [Test]
        public void MirrorEventGradient()
        {
            Settings.Instance.MapVersion = 2;

            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.BackLasers,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f,
                CustomData = JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}")
            };

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.BlueFade,
                    FloatValue = 1f,
                    CustomData =
                        JSON.Parse(
                            "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0, 1], \"_endColor\": [1, 0, 0, 1], \"_easing\": \"easeLinear\"}}")
                },
                eventsContainer.MapObjects[0],
                "Perform mirror gradient event");

            // Undo mirror
            _actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.RedFade,
                    FloatValue = 1f,
                    CustomData =
                        JSON.Parse(
                            "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}")
                },
                eventsContainer.MapObjects[0],
                "Undo mirror gradient event");
        }

        [Test]
        public void MirrorEventRedBlue()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.BackLasers,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f
            };

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.BlueFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Perform mirror event");

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.RedFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Perform mirror event again");
        }

        [Test]
        public void MirrorEventRedWhiteBlue()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.BackLasers,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f
            };

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror(false);
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.WhiteFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Perform mirror cycle event");

            _mirror.Mirror(false);
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.BlueFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Perform mirror cycle event 2");

            _mirror.Mirror(false);
            BeatmapAssertion.IsEqual(
                new BaseEvent
                {
                    JsonTime = 2,
                    Type = (int)EventTypeValue.BackLasers,
                    Value = (int)LightValue.RedFade,
                    FloatValue = 1f
                },
                eventsContainer.MapObjects[0],
                "Perform mirror cycle event 3");
        }

        [Test]
        public void MirrorWallME()
        {
            var wallsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var wallPlacement = Object.FindAnyObjectByType<ObstaclePlacement>();
            wallPlacement.CreateVisual();

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

            wallA = PlaceUtils.Place(wallA);

            SelectionController.Select(wallA);

            var mirroredWallA = BeatmapFactory.Clone(wallA);
            mirroredWallA.PosX = 2958;

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(mirroredWallA, wallsContainer.MapObjects[0], "Perform ME wall mirror");

            // Undo mirror
            _actionContainer.Undo();
            BeatmapAssertion.IsEqual(wallA, wallsContainer.MapObjects[0], "Undo ME wall mirror");
        }

        [Test]
        public void MirrorWallNE()
        {
            var wallsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var wallPlacement = Object.FindAnyObjectByType<ObstaclePlacement>();
            wallPlacement.CreateVisual();

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

            wallA = PlaceUtils.Place(wallA);

            SelectionController.Select(wallA);

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleRight,
                    PosY = (int)GridY.Base,
                    Duration = 1,
                    Width = 2,
                    Height = 5,
                    Type = (int)ObstacleType.Full,
                    CustomData = JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-0.5, 0]}}")
                },
                wallsContainer.MapObjects[0],
                "Perform NE wall mirror");

            // Undo mirror
            _actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Duration = 1,
                    Width = 2,
                    Height = 5,
                    Type = (int)ObstacleType.Full,
                    CustomData = JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-1.5, 0]}}")
                },
                wallsContainer.MapObjects[0],
                "Undo NE wall mirror");
        }

        // TODO: update rotation event test for more representative
        [Test]
        public void MirrorRotationEvent()
        {
            var laneRotationProvider = Object.FindAnyObjectByType<LaneRotationProvider>();

            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(
                    ObjectType.RotationEvent);

            var rotationEventPlacement = Object.FindAnyObjectByType<RotationEventPlacement>();

            var baseEventA = new BaseRotationEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.LateLaneRotation, Rotation = 33
            };

            // fuck kinda conflict did u have?
            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror();
            BeatmapAssertion.IsEqual(
                new BaseRotationEvent { JsonTime = 2, Type = 1 == 0 ? 14 : 15, Rotation = -33 },
                eventsContainer.MapObjects[0],
                "Perform mirror rotation event");

            // Undo mirror
            _actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseRotationEvent { JsonTime = 2, Type = 1 == 0 ? 14 : 15, Rotation = 33 },
                eventsContainer.MapObjects[0],
                "Undo mirror rotation event");
        }
    }
}