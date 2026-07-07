using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class NotesContainerTest : TestBase
    {
        [Test]
        public void RefreshSpecialAngles()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA = new BaseNote
            {
                JsonTime = 4,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.Left
            };
            baseNoteA = PlaceUtils.Place(baseNoteA);
            var containerA = noteGridContainer.LoadedContainers[baseNoteA] as NoteContainer;

            var baseNoteB = new BaseNote
            {
                JsonTime = 4,
                Type = (int)NoteType.Red,
                PosX = (int)GridX.MiddleLeft
            };
            baseNoteB = PlaceUtils.Place(baseNoteB);
            var containerB = noteGridContainer.LoadedContainers[baseNoteB] as NoteContainer;

            // These tests are based of the examples in this image
            // https://media.discordapp.net/attachments/443569023951568906/681978249139585031/unknown.png

            // ◌◌◌◌
            // ◌→◌◌
            // ◌◌→◌
            UpdateNote(containerA, (int)GridX.MiddleLeft, (int)GridY.Upper, (int)NoteCutDirection.Right);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Right);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(90, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(90, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↙◌
            // ◌◌◌◌
            // ◌◌↙◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Top, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(333.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌↓↓◌
            UpdateNote(containerA, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.Down);
            UpdateNote(containerB, (int)GridX.MiddleLeft, (int)GridY.Base, (int)NoteCutDirection.Down);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(0, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↙◌◌◌
            // ↙◌◌◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ◌◌◌◌
            // ↙◌◌↙
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Base, (int)NoteCutDirection.DownLeft);
            UpdateNote(containerB, (int)GridX.Right, (int)GridY.Base, (int)NoteCutDirection.DownLeft);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(315, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌◌◌
            // ↘◌◌◌
            // ◌◌↘◌
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);
            UpdateNote(containerB, (int)GridX.MiddleRight, (int)GridY.Base, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            Assert.AreEqual(63.43, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(63.43, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Changing this note to be in another beat should stop the angles snapping
            baseNoteA.JsonTime = 13;
            UpdateNote(containerA, (int)GridX.Left, (int)GridY.Upper, (int)NoteCutDirection.DownRight);

            noteGridContainer.RefreshSpecialAngles(baseNoteA, true, false);
            noteGridContainer.RefreshSpecialAngles(baseNoteB, true, false);
            Assert.AreEqual(45, containerA.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerB.DirectionTarget.localEulerAngles.z, 0.01);

            // Make cleanup work
            baseNoteA.JsonTime = 14;
        }

        [Test]
        public void RefreshSpecialAnglesOnDirectionChange()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            // ◌◌◌◌
            // ◌◌◌◌
            // ◌←◌◌
            var baseNoteBottom = new BaseNote { JsonTime = 4, PosX = 1, CutDirection = (int)NoteCutDirection.Left};
            baseNoteBottom = PlaceUtils.Place(baseNoteBottom);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌←◌◌
            var baseNoteTop = new BaseNote { JsonTime = 4, PosX = 2, PosY = 2, CutDirection = (int)NoteCutDirection.Down };
            baseNoteTop = PlaceUtils.Place(baseNoteTop);
            
            var containerBottom = noteGridContainer.LoadedContainers[baseNoteBottom] as NoteContainer;
            var containerTop = noteGridContainer.LoadedContainers[baseNoteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↙◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            containerBottom = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[0]] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[1]] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↓◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            containerBottom = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[0]] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[1]] as NoteContainer;
            Assert.AreEqual(333.43, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(333.43, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ◌↘◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            containerBottom = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[0]] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[1]] as NoteContainer;
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
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            
            // ◌◌◌◌
            // ◌◌◌◌
            // ←◌◌◌
            var baseNoteBottom = new BaseNote { JsonTime = 4, CutDirection = (int)NoteCutDirection.Left };
            baseNoteBottom = PlaceUtils.Place(baseNoteBottom);

            // ◌◌↓◌
            // ◌◌◌◌
            // ←◌◌◌
            var baseNoteTop = new BaseNote { JsonTime = 4 , PosX = 2,  PosY = 2, CutDirection = (int)NoteCutDirection.Down };
            baseNoteTop = PlaceUtils.Place(baseNoteTop);
            
            var containerBottom = noteGridContainer.LoadedContainers[baseNoteBottom] as NoteContainer;
            var containerTop = noteGridContainer.LoadedContainers[baseNoteTop] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(270, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↙◌◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            containerBottom = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[0]] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[1]] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(315, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);

            // ◌◌↓◌
            // ◌◌◌◌
            // ↓◌◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            containerBottom = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[0]] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[1]] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(0, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);
            
            // ◌◌↓◌
            // ◌◌◌◌
            // ↘◌◌◌
            inputController.ScrollUpdateDirection(containerBottom, 1);
            containerBottom = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[0]] as NoteContainer;
            containerTop = noteGridContainer.LoadedContainers[noteGridContainer.MapObjects[1]] as NoteContainer;
            Assert.AreEqual(0, containerTop.DirectionTarget.localEulerAngles.z, 0.01);
            Assert.AreEqual(45, containerBottom.DirectionTarget.localEulerAngles.z, 0.01);
        }

        [Test]
        public void RefreshSpecialAnglesIgnoresPrecisionPlacement()
        {
            var noteGridContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var baseNoteA = new BaseNote { JsonTime = 4, PosX = 1};
            baseNoteA = PlaceUtils.Place(baseNoteA);

            var baseNoteB = new BaseNote { JsonTime = 4 };
            baseNoteB = PlaceUtils.Place(baseNoteB);
            
            var containerA = noteGridContainer.LoadedContainers[baseNoteA] as NoteContainer;
            var containerB = noteGridContainer.LoadedContainers[baseNoteB] as NoteContainer;
            
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
            var baseNote = (BaseNote)container.ObjectData;
            baseNote.PosX = PosX;
            baseNote.PosY = PosY;
            baseNote.CutDirection = cutDirection;
            container.UpdateGridPosition();
            container.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(baseNote);
        }

        [Test]
        public void ShiftInTime()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            
            var baseNoteA = new BaseNote
            {
                JsonTime = 2,
                Type = (int)NoteType.Red
            };
            baseNoteA = PlaceUtils.Place(baseNoteA);

            var baseNoteB = new BaseNote
            {
                JsonTime = 3,
                Type = (int)NoteType.Red
            };
            baseNoteB = PlaceUtils.Place(baseNoteB);

            SelectionController.Select(baseNoteB, false, false, false);

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            selectionController.MoveSelection(-2);

            var baseNoteBAfterMove = new BaseNote
            {
                JsonTime = 1,
                Type = (int)NoteType.Red
            };

            notesContainer.DeleteObject(baseNoteBAfterMove);

            Assert.AreEqual(1, notesContainer.LoadedContainers.Count);
            Assert.AreEqual(1, notesContainer.MapObjects.Count);
        }
    }
}
