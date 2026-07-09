using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class NotesContainerTest : TestBase
    {
        [Test]
        public void RefreshSpecialAngles()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            var noteA = new BaseNote { JsonTime = 4, Type = (int)NoteType.Red, PosX = (int)GridX.Left };
            noteA = PlaceUtils.Place(noteA);
            var containerA = noteGridContainer.LoadedContainers[noteA] as NoteContainer;

            var noteB = new BaseNote { JsonTime = 4, Type = (int)NoteType.Red, PosX = (int)GridX.MiddleLeft };
            noteB = PlaceUtils.Place(noteB);
            var containerB = noteGridContainer.LoadedContainers[noteB] as NoteContainer;

            // These tests are based of the examples in this image
            // https://media.discordapp.net/attachments/443569023951568906/681978249139585031/unknown.png

            // ◌◌◌◌
            // ◌→◌◌
            // ◌◌→◌
            UpdateNote(containerA, (int)GridX.MiddleLeft, (int)GridY.Upper, (int)NoteCutDirection.Right);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Right);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(90, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(333.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            Assert.AreEqual(63.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            noteA.JsonTime = 13;
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(noteA, true, false);
            noteGridContainer.RefreshSpecialAngles(noteB, true, false);
            Assert.AreEqual(45, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Make cleanup work
            noteA.JsonTime = 14;
        }

        [Test]
        public void RefreshSpecialAnglesOnDirectionChange()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            Object.FindAnyObjectByType<NotePlacement>();

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌←◌◌
            var noteBottom = new BaseNote { JsonTime = 4, PosX = 1, CutDirection = (int)NoteCutDirection.Left };
            noteBottom = PlaceUtils.Place(noteBottom);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌←◌◌
            var noteTop =
                new BaseNote { JsonTime = 4, PosX = 2, PosY = 2, CutDirection = (int)NoteCutDirection.Down };
            noteTop = PlaceUtils.Place(noteTop);

            var containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            var containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↙◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            noteBottom = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            noteBottom = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(333.43, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↘◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            noteBottom = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);
        }

        [Test]
        public void RefreshSpecialAnglesOnDirectionChange2()
        {
            // Test that angles are not changed when they shouldn't be
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            Object.FindAnyObjectByType<NotePlacement>();

            // ◌◌◌◌
            // ◌◌◌◌
            // ←◌◌◌
            var noteBottom = new BaseNote { JsonTime = 4, CutDirection = (int)NoteCutDirection.Left };
            noteBottom = PlaceUtils.Place(noteBottom);

            // ◌◌↓◌
            // ◌◌◌◌
            // ←◌◌◌
            var noteTop =
                new BaseNote { JsonTime = 4, PosX = 2, PosY = 2, CutDirection = (int)NoteCutDirection.Down };
            noteTop = PlaceUtils.Place(noteTop);

            var containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            var containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↙◌◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            noteBottom = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↓◌◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            noteBottom = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↘◌◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            noteBottom = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            containerBottom = noteGridContainer.LoadedContainers[noteBottom] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);
        }

        [Test]
        public void RefreshSpecialAnglesIgnoresPrecisionPlacement()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            var noteA = new BaseNote { JsonTime = 4, PosX = 1 };
            noteA = PlaceUtils.Place(noteA);

            var noteB = new BaseNote { JsonTime = 4 };
            noteB = PlaceUtils.Place(noteB);

            var containerA = noteGridContainer.LoadedContainers[noteA] as NoteContainer;
            var containerB = noteGridContainer.LoadedContainers[noteB] as NoteContainer;

            // ME precision placed
            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, 1000);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, 1000);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // NE precision placed
            (containerA.ObjectData as BaseNote).CustomCoordinate = new JSONArray { [0] = 0, [1] = 2 };
            (containerB.ObjectData as BaseNote).CustomCoordinate = new JSONArray { [0] = -1, [1] = 0 };
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);
        }

        private void UpdateNote(NoteContainer container, int PosX, int PosY, int cutDirection)
        {
            var note = (BaseNote)container.ObjectData;
            note.PosX = PosX;
            note.PosY = PosY;
            note.CutDirection = cutDirection;
            container.UpdateGridPosition();
            container.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(note);
        }

        [Test]
        public void ShiftInTime()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            Object.FindAnyObjectByType<NotePlacement>();

            var noteA = new BaseNote { JsonTime = 2, Type = (int)NoteType.Red };
            noteA = PlaceUtils.Place(noteA);

            var noteB = new BaseNote { JsonTime = 3, Type = (int)NoteType.Red };
            noteB = PlaceUtils.Place(noteB);

            SelectionController.Select(noteB, false, false, false);

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            selectionController.MoveSelection(-2);

            var noteBAfterMove = new BaseNote { JsonTime = 1, Type = (int)NoteType.Red };

            PlaceUtils.Delete(noteBAfterMove);

            BeatmapAssertion.CollectionCount<BaseNote>(1);
        }
    }
}