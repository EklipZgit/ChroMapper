using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

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
            
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA = new BaseNote{ JsonTime = 2, PosX = (int)GridX.MiddleLeft, PosY = (int)GridY.Base, Type = (int)NoteType.Red, CutDirection = (int)NoteCutDirection.Down };
            var baseNoteB = new BaseNote{ JsonTime = 2, PosX = (int)GridX.MiddleRight, PosY = (int)GridY.Base, Type = (int)NoteType.Blue, CutDirection = (int)NoteCutDirection.Down };
            
            baseNoteA = PlaceUtils.Place(baseNoteA);
            baseNoteB = PlaceUtils.Place(baseNoteB);

            SelectionController.Select(baseNoteA);
            SelectionController.Select(baseNoteB, addsToSelection: true);

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
            CheckUtils.CheckNote("Left note after mirror", notesContainer, 0, 2, (int)GridX.MiddleLeft, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Down, 0);
            CheckUtils.CheckNote("Right note after mirror", notesContainer, 1, 2, (int)GridX.MiddleRight, (int)GridY.Base,
                (int)NoteType.Blue, (int)NoteCutDirection.Down, 0);
        }

        [Test]
        public void MirrorNoteME()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA =
                new BaseNote
                {
                    JsonTime = 2, PosX = -2345, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };

            baseNoteA = PlaceUtils.Place(baseNoteA);

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            CheckUtils.CheckNote("Perform note mirror", notesContainer, 0, 2, 5345, (int)GridY.Base, (int)NoteType.Blue,
                (int)NoteCutDirection.Right, 0);

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckNote("Undo note mirror", notesContainer, 0, 2, -2345, (int)GridY.Base, (int)NoteType.Red,
                (int)NoteCutDirection.Left, 0);
        }

        [Test]
        public void MirrorNoteNE()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA = new BaseNote
            {
                JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left, CustomData = JSON.Parse("{\"coordinates\": [-1, 0]}")
            };

            baseNoteA = PlaceUtils.Place(baseNoteA);

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            CheckUtils.CheckNote("Perform NE note mirror", notesContainer, 0, 2, (int)GridX.Right, (int)GridY.Base,
                (int)NoteType.Blue, (int)NoteCutDirection.Right, 0,
                JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [0, 0]}}"));

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckNote("Undo NE note inversion", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Left, 0,
                JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [-1, 0]}}"));
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
        [TestCase("[1,2]","[9,10]", EventGridContainer.PropMode.Prop)]
        public void MirrorEventLightID(string original, string mirror, EventGridContainer.PropMode propMode)
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade, FloatValue = 1f,
                CustomData = JSON.Parse($"{{\"lightID\": {original}}}")};

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            eventsContainer.EventTypeToPropagate = baseEventA.Type;
            eventsContainer.PropagationEditing = propMode;

            _mirror.Mirror();
            // I'm sorry if you're here after changing the lightID mapping for default env
            CheckUtils.CheckEvent("Perform mirror lightID event", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f, JSON.Parse($"{{\"lightID\": {mirror}}}"));

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckEvent("Undo mirror lightID event", eventsContainer, 0, 2, (int)EventTypeValue.BackLasers,
                (int)LightValue.RedFade, 1f, JSON.Parse($"{{\"lightID\": {original}}}"));

            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
        }

        [Test]
        public void MirrorEventGradient()
        {
            Settings.Instance.MapVersion = 2;
            
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent{ JsonTime = 2, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade, FloatValue = 1f,
                CustomData = JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}")};

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror();
            CheckUtils.CheckEvent("Perform mirror gradient event", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f,
                JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0, 1], \"_endColor\": [1, 0, 0, 1], \"_easing\": \"easeLinear\"}}"));

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckEvent("Undo mirror gradient event", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f,
                JSON.Parse(
                    "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));
        }

        [Test]
        public void MirrorEventRedBlue()
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade, FloatValue = 1f };

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror();
            CheckUtils.CheckEvent("Perform mirror event", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f);

            _mirror.Mirror();
            CheckUtils.CheckEvent("Perform mirror event again", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f);
        }

        [Test]
        public void MirrorEventRedWhiteBlue()
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.BackLasers, Value = (int)LightValue.RedFade, FloatValue = 1f };

            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror(false);
            CheckUtils.CheckEvent("Perform mirror cycle event", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade, 1f);

            _mirror.Mirror(false);
            CheckUtils.CheckEvent("Perform mirror cycle event 2", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f);

            _mirror.Mirror(false);
            CheckUtils.CheckEvent("Perform mirror cycle event 3", eventsContainer, 0, 2,
                (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f);
        }

        [Test]
        public void MirrorWallME()
        {
            var wallsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

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

            _mirror.Mirror();
            CheckUtils.CheckWall("Perform ME wall mirror", wallsContainer, 0, 2, 2958, 0, 595141, 1, 2596, 5);

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckWall("Undo ME wall mirror", wallsContainer, 0, 2, 1446, 0, 595141, 1, 2596, 5);
        }

        [Test]
        public void MirrorWallNE()
        {
            var wallsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

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
                CustomData = JSON.Parse("{\"coordinates\": [-1.5, 0]}"),
            };

            wallA = PlaceUtils.Place(wallA);

            SelectionController.Select(wallA);

            _mirror.Mirror();
            CheckUtils.CheckWall("Perform NE wall mirror", wallsContainer, 0, 2, (int)GridX.MiddleRight,
                (int)GridY.Base, (int)ObstacleType.Full, 1, 2, 5,
                JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-0.5, 0]}}"));

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckWall("Undo NE wall mirror", wallsContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)ObstacleType.Full, 1, 2, 5, JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-1.5, 0]}}"));
        }

        // TODO: update rotation event test for more representative
        [Test]
        public void MirrorRotationEvent()
        {
            var laneRotationProvider = Object.FindAnyObjectByType<LaneRotationProvider>();

            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(ObjectType.RotationEvent);

            var rotationEventPlacement = Object.FindAnyObjectByType<RotationEventPlacement>();

            var baseEventA = new BaseRotationEvent { JsonTime = 2, Type = (int)EventTypeValue.LateLaneRotation, Rotation = 33 };

            // fuck kinda conflict did u have?
            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            _mirror.Mirror();
            CheckUtils.CheckRotationEvent("Perform mirror rotation event", eventsContainer, 0, 2, 1, -33);

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckRotationEvent("Undo mirror rotation event", eventsContainer, 0, 2, 1, 33);
        }
    }
}
