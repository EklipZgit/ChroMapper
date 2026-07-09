using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class MirrorTest : TestBase
    {
        private ArcPlacement _arcPlacement;
        private ArcGridContainer _arcsContainer;
        private BaseArc _baseArc;
        private BaseNote _baseNoteA;
        private BaseNote _baseNoteB;
        private MirrorSelection _mirror;
        private NotePlacement _notePlacement;
        private NoteGridContainer _notesContainer;

        protected override IEnumerator OnMapLoaded()
        {
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
            var noteA = new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                CutDirection = (int)NoteCutDirection.Left
            };
            var noteB = new BaseNote
            {
                JsonTime = 3,
                Type = (int)NoteType.Blue,
                PosX = (int)GridX.Right,
                PosY = (int)GridY.Top,
                CutDirection = (int)NoteCutDirection.UpRight
            };
            var arc = new BaseArc
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

            _baseNoteA = PlaceUtils.Place(noteA);

            // Should conflict with existing note and delete it
            _baseNoteB = PlaceUtils.Place(noteB);
            _baseArc = PlaceUtils.Place(arc);

            SelectionController.Select(_baseNoteA);
            SelectionController.Select(_baseNoteB, true);
            SelectionController.Select(_baseArc, true);
        }

        [Test]
        public void MirrorInTime()
        {
            var expectedOriginalNoteA = BeatmapFactory.Clone(_baseNoteA);
            var expectedOriginalNoteB = BeatmapFactory.Clone(_baseNoteB);
            var expectedOriginalArc = BeatmapFactory.Clone(_baseArc);
            var expectedMirroredNoteA = BeatmapFactory.Clone(_baseNoteB);
            expectedMirroredNoteA.JsonTime = _baseNoteA.JsonTime;
            var expectedMirroredNoteB = BeatmapFactory.Clone(_baseNoteA);
            expectedMirroredNoteB.JsonTime = _baseNoteB.JsonTime;
            var expectedMirroredArc = BeatmapFactory.Clone(_baseArc);
            expectedMirroredArc.PosX = (int)GridX.MiddleRight;
            expectedMirroredArc.PosY = (int)GridY.Top;
            expectedMirroredArc.CutDirection = (int)NoteCutDirection.Right;
            expectedMirroredArc.HeadControlPointLengthMultiplier = 2;
            expectedMirroredArc.TailPosX = (int)GridX.MiddleLeft;
            expectedMirroredArc.TailPosY = (int)GridY.Base;
            expectedMirroredArc.TailCutDirection = (int)NoteCutDirection.Left;
            expectedMirroredArc.TailControlPointLengthMultiplier = 1;

            _mirror.MirrorTime();

            var mirroredObjects = SelectionController.SelectedObjects.ToList();
            var mirroredNotes = mirroredObjects.OfType<BaseNote>().OrderBy(note => note.JsonTime).ToList();
            var mirroredArc = mirroredObjects.OfType<BaseArc>().Single();

            // Check we can still delete our objects
            var toDelete = mirroredNotes.FirstOrDefault();
            PlaceUtils.Delete(toDelete);
            BeatmapAssertion.CollectionCount<BaseNote>(1);

            PlaceUtils.Undo();

            BeatmapAssertion.CollectionCount<BaseNote>(2);
            BeatmapAssertion.CollectionCount<BaseArc>(1);

            BeatmapAssertion.IsEqual(
                expectedMirroredNoteA,
                mirroredNotes[0],
                "Check first mirrored time");
            BeatmapAssertion.IsEqual(
                expectedMirroredNoteB,
                mirroredNotes[1],
                "Check second mirrored time");
            BeatmapAssertion.IsEqual(
                expectedMirroredArc,
                mirroredArc,
                "Check arc mirrored time");

            // Undo mirror
            var undoMirrorObjects = PlaceUtils.Undo();

            BeatmapAssertion.IsEqual(
                expectedOriginalNoteA,
                undoMirrorObjects.OfType<BaseNote>().ElementAt(0),
                "Check undo first mirrored time");
            BeatmapAssertion.IsEqual(
                expectedOriginalNoteB,
                undoMirrorObjects.OfType<BaseNote>().ElementAt(1),
                "Check undo second mirrored time ");
            BeatmapAssertion.IsEqual(
                expectedOriginalArc,
                undoMirrorObjects.OfType<BaseArc>().First(),
                "Check undo arc mirrored time");
        }

        [Test]
        public void Mirror()
        {
            var expectedOriginalNoteA = BeatmapFactory.Clone(_baseNoteA);
            var expectedOriginalNoteB = BeatmapFactory.Clone(_baseNoteB);
            var expectedOriginalArc = BeatmapFactory.Clone(_baseArc);
            var expectedMirroredNoteA = BeatmapFactory.Clone(_baseNoteA);
            expectedMirroredNoteA.PosX = (int)GridX.Right;
            expectedMirroredNoteA.Type = (int)NoteType.Blue;
            expectedMirroredNoteA.CutDirection = (int)NoteCutDirection.Right;

            var expectedMirroredNoteB = BeatmapFactory.Clone(_baseNoteB);
            expectedMirroredNoteB.PosX = (int)GridX.Left;
            expectedMirroredNoteB.Type = (int)NoteType.Red;
            expectedMirroredNoteB.CutDirection = (int)NoteCutDirection.UpLeft;

            var expectedMirroredArc = BeatmapFactory.Clone(_baseArc);
            expectedMirroredArc.PosX = (int)GridX.MiddleRight;
            expectedMirroredArc.Color = (int)NoteType.Red;
            expectedMirroredArc.CutDirection = (int)NoteCutDirection.Right;
            expectedMirroredArc.TailPosX = (int)GridX.MiddleLeft;
            expectedMirroredArc.TailCutDirection = (int)NoteCutDirection.Left;

            _mirror.Mirror();

            var mirroredObjects = SelectionController.SelectedObjects.ToList();
            var mirroredNotes = mirroredObjects.OfType<BaseNote>().OrderBy(note => note.JsonTime).ToList();
            var mirroredArc = mirroredObjects.OfType<BaseArc>().Single();

            // Check we can still delete our objects
            var toDelete = mirroredNotes.FirstOrDefault();
            PlaceUtils.Delete(toDelete);
            BeatmapAssertion.CollectionCount<BaseNote>(1);

            PlaceUtils.Undo();

            BeatmapAssertion.CollectionCount<BaseNote>(2);
            BeatmapAssertion.CollectionCount<BaseArc>(1);

            BeatmapAssertion.IsEqual(
                expectedMirroredNoteA,
                mirroredNotes[0],
                "Check first mirrored note");
            BeatmapAssertion.IsEqual(
                expectedMirroredNoteB,
                mirroredNotes[1],
                "Check second mirrored note");
            BeatmapAssertion.IsEqual(
                expectedMirroredArc,
                mirroredArc,
                "Check mirrored arc");

            // Undo mirror
            var undoMirrorObjects = PlaceUtils.Undo();

            BeatmapAssertion.IsEqual(
                expectedOriginalNoteA,
                undoMirrorObjects.OfType<BaseNote>().ElementAt(0),
                "Check undo first mirrored note");
            BeatmapAssertion.IsEqual(
                expectedOriginalNoteB,
                undoMirrorObjects.OfType<BaseNote>().ElementAt(1),
                "Check undo second mirrored note");
            BeatmapAssertion.IsEqual(
                expectedOriginalArc,
                undoMirrorObjects.OfType<BaseArc>().First(),
                "Check undo mirrored arc");
        }

        [Test]
        public void SwapColors()
        {
            var expectedOriginalNoteA = BeatmapFactory.Clone(_baseNoteA);
            var expectedOriginalNoteB = BeatmapFactory.Clone(_baseNoteB);
            var expectedOriginalArc = BeatmapFactory.Clone(_baseArc);
            var expectedSwappedNoteA = BeatmapFactory.Clone(_baseNoteA);
            expectedSwappedNoteA.Type = (int)NoteType.Blue;

            var expectedSwappedNoteB = BeatmapFactory.Clone(_baseNoteB);
            expectedSwappedNoteB.Type = (int)NoteType.Red;

            var expectedSwappedArc = BeatmapFactory.Clone(_baseArc);
            expectedSwappedArc.Color = (int)NoteType.Red;

            _mirror.Mirror(false);

            var mirroredObjects = SelectionController.SelectedObjects.ToList();
            var mirroredNotes = mirroredObjects.OfType<BaseNote>().OrderBy(note => note.JsonTime).ToList();
            var mirroredArc = mirroredObjects.OfType<BaseArc>().Single();

            // Check we can still delete our objects
            var toDelete = mirroredNotes.FirstOrDefault();
            PlaceUtils.Delete(toDelete);
            BeatmapAssertion.CollectionCount<BaseNote>(1);

            PlaceUtils.Undo();

            BeatmapAssertion.CollectionCount<BaseNote>(2);
            BeatmapAssertion.CollectionCount<BaseArc>(1);

            BeatmapAssertion.IsEqual(
                expectedSwappedNoteA,
                mirroredNotes[0],
                "Check first mirrored color swap");
            BeatmapAssertion.IsEqual(
                expectedSwappedNoteB,
                mirroredNotes[1],
                "Check second mirrored color swap");
            BeatmapAssertion.IsEqual(
                expectedSwappedArc,
                mirroredArc,
                "Check mirrored arc color swap");

            // Undo mirror
            var undoMirrorObjects = PlaceUtils.Undo();

            BeatmapAssertion.IsEqual(
                expectedOriginalNoteA,
                undoMirrorObjects.OfType<BaseNote>().ElementAt(0),
                "Check undo first mirrored color swap");
            BeatmapAssertion.IsEqual(
                expectedOriginalNoteB,
                undoMirrorObjects.OfType<BaseNote>().ElementAt(1),
                "Check undo second mirrored color swap");
            BeatmapAssertion.IsEqual(
                expectedOriginalArc,
                undoMirrorObjects.OfType<BaseArc>().First(),
                "Check undo mirrored arc color swap");
        }
    }
}