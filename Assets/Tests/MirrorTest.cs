using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class MirrorTest : TestBase
    {
        private BeatmapActionContainer _actionContainer;
        private ArcPlacement _arcPlacement;
        private ArcGridContainer _arcsContainer;
        private MirrorSelection _mirror;
        private NotePlacement _notePlacement;
        private NoteGridContainer _notesContainer;

        protected override IEnumerator OnMapLoaded()
        {
            _actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            _mirror = Object.FindAnyObjectByType<MirrorSelection>();
            _notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            _arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            _notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            _arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();
            yield break;
        }

        [SetUp]
        public void SpawnNotesAndArcs()
        {
            var baseNoteA = new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baseNoteB = new BaseNote
            {
                JsonTime = 3,
                Type = (int)NoteType.Blue,
                PosX = (int)GridX.Right,
                PosY = (int)GridY.Top,
                CutDirection = (int)NoteCutDirection.UpRight
            };
            var baseArc = new BaseArc
            {
                JsonTime = 2,
                Color = (int)NoteType.Blue,
                PosX = (int)GridX.MiddleLeft,
                PosY = (int)GridY.Base,
                CutDirection = (int)NoteCutDirection.Left,
                HeadControlPointLengthMultiplier = 1,
                TailJsonTime = 3,
                TailPosX = (int)GridX.MiddleRight,
                TailPosY = (int)GridY.Top,
                TailCutDirection = (int)NoteCutDirection.Right,
                TailControlPointLengthMultiplier = 2,
                MidAnchorMode = 0
            };

            baseNoteA = PlaceUtils.Place(baseNoteA);

            // Should conflict with existing note and delete it
            baseNoteB = PlaceUtils.Place(baseNoteB);
            baseArc = PlaceUtils.Place(baseArc);

            SelectionController.Select(baseNoteA);
            SelectionController.Select(baseNoteB, true);
            SelectionController.Select(baseArc, true);
        }

        [Test]
        public void MirrorInTime()
        {
            _mirror.MirrorTime();

            // Check we can still delete our objects
            var toDelete = _notesContainer.MapObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.MapObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.MapObjects.Count);
            Assert.AreEqual(1, _arcsContainer.MapObjects.Count);

            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Right,
                    PosY = (int)GridY.Top,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.UpRight,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[0],
                "Check first mirrored time");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 3,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[1],
                "Check second mirrored time");
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleRight,
                    PosY = (int)GridY.Top,
                    Color = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Right,
                    AngleOffset = 0,
                    HeadControlPointLengthMultiplier = 2,
                    TailJsonTime = 3,
                    TailPosX = (int)GridX.MiddleLeft,
                    TailPosY = (int)GridY.Base,
                    TailCutDirection = (int)NoteCutDirection.Left,
                    TailControlPointLengthMultiplier = 1,
                    MidAnchorMode = 0
                },
                _arcsContainer.MapObjects[0],
                "Check arc mirrored time");

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
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[0],
                "Check undo first mirrored time");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 3,
                    PosX = (int)GridX.Right,
                    PosY = (int)GridY.Top,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.UpRight,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[1],
                "Check undo second mirrored time ");
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleLeft,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0,
                    HeadControlPointLengthMultiplier = 1,
                    TailJsonTime = 3,
                    TailPosX = (int)GridX.MiddleRight,
                    TailPosY = (int)GridY.Top,
                    TailCutDirection = (int)NoteCutDirection.Right,
                    TailControlPointLengthMultiplier = 2,
                    MidAnchorMode = 0
                },
                _arcsContainer.MapObjects[0],
                "Check undo arc mirrored time");
        }

        [Test]
        public void Mirror()
        {
            _mirror.Mirror();

            // Check we can still delete our objects
            var toDelete = _notesContainer.MapObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.MapObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.MapObjects.Count);
            Assert.AreEqual(1, _arcsContainer.MapObjects.Count);

            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Right,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Right,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[0],
                "Check first mirrored note");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 3,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Top,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.UpLeft,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[1],
                "Check second mirrored note");
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleRight,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Right,
                    AngleOffset = 0,
                    HeadControlPointLengthMultiplier = 1,
                    TailJsonTime = 3,
                    TailPosX = (int)GridX.MiddleLeft,
                    TailPosY = (int)GridY.Top,
                    TailCutDirection = (int)NoteCutDirection.Left,
                    TailControlPointLengthMultiplier = 2,
                    MidAnchorMode = 0
                },
                _arcsContainer.MapObjects[0],
                "Check mirrored arc");

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
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[0],
                "Check undo first mirrored note");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 3,
                    PosX = (int)GridX.Right,
                    PosY = (int)GridY.Top,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.UpRight,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[1],
                "Check undo second mirrored note");
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleLeft,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0,
                    HeadControlPointLengthMultiplier = 1,
                    TailJsonTime = 3,
                    TailPosX = (int)GridX.MiddleRight,
                    TailPosY = (int)GridY.Top,
                    TailCutDirection = (int)NoteCutDirection.Right,
                    TailControlPointLengthMultiplier = 2,
                    MidAnchorMode = 0
                },
                _arcsContainer.MapObjects[0],
                "Check undo mirrored arc");
        }

        [Test]
        public void SwapColors()
        {
            _mirror.Mirror(false);

            // Check we can still delete our objects
            var toDelete = _notesContainer.MapObjects.FirstOrDefault();
            _notesContainer.DeleteObject(toDelete);
            Assert.AreEqual(1, _notesContainer.MapObjects.Count);

            _actionContainer.Undo();

            Assert.AreEqual(2, _notesContainer.MapObjects.Count);
            Assert.AreEqual(1, _arcsContainer.MapObjects.Count);

            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[0],
                "Check first mirrored color swap");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 3,
                    PosX = (int)GridX.Right,
                    PosY = (int)GridY.Top,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.UpRight,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[1],
                "Check second mirrored color swap");
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleLeft,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0,
                    HeadControlPointLengthMultiplier = 1,
                    TailJsonTime = 3,
                    TailPosX = (int)GridX.MiddleRight,
                    TailPosY = (int)GridY.Top,
                    TailCutDirection = (int)NoteCutDirection.Right,
                    TailControlPointLengthMultiplier = 2,
                    MidAnchorMode = 0
                },
                _arcsContainer.MapObjects[0],
                "Check mirrored arc color swap");

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
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[0],
                "Check undo first mirrored color swap");
            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 3,
                    PosX = (int)GridX.Right,
                    PosY = (int)GridY.Top,
                    Type = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.UpRight,
                    AngleOffset = 0
                },
                _notesContainer.MapObjects[1],
                "Check undo second mirrored color swap");
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 2,
                    PosX = (int)GridX.MiddleLeft,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteType.Blue,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = 0,
                    HeadControlPointLengthMultiplier = 1,
                    TailJsonTime = 3,
                    TailPosX = (int)GridX.MiddleRight,
                    TailPosY = (int)GridY.Top,
                    TailCutDirection = (int)NoteCutDirection.Right,
                    TailControlPointLengthMultiplier = 2,
                    MidAnchorMode = 0
                },
                _arcsContainer.MapObjects[0],
                "Check undo mirrored arc color swap");
        }
    }
}