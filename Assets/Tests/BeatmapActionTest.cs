using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class BeatmapActionTest : TestBase
    {
        [Test]
        public void ModifiedAction()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var noteA = new BaseNote { JsonTime = 2, Type = (int)NoteType.Red };
            noteA = PlaceUtils.Place(noteA);

            SelectionController.Select(noteA);

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            // Default precision is 3dp, but in editor it's 6dp so check 7dp
            selectionController.MoveSelection(-0.0000001f);

            var undoObjects = PlaceUtils.Undo<BaseNote>(actionContainer).ToList();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, undoObjects[0].JsonTime);

            var redoObjects = PlaceUtils.Redo<BaseNote>(actionContainer).ToList();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(1.9999999f, redoObjects[0].JsonTime);
        }

        [Test]
        public void CompositeTest()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            var noteA = new BaseNote { JsonTime = 2, Type = (int)NoteType.Red };
            var noteB = new BaseNote { JsonTime = 2, Type = (int)NoteType.Blue, PosX = 1, PosY = 1 };

            noteA = PlaceUtils.Place(noteA);

            SelectionController.Select(noteA);

            selectionController.ShiftSelection(1, 1);

            // Should conflict with existing note and delete it
            noteB = PlaceUtils.Place(noteB);

            SelectionController.Select(noteB);
            selectionController.ShiftSelection(1, 1);
            selectionController.Copy(true);

            selectionController.Paste();
            selectionController.Delete();

            void CheckState(
                int MapObjects,
                int selectedObjects,
                BaseNote note,
                int time,
                int type,
                int index,
                int layer)
            {
                Assert.AreEqual(MapObjects, notesContainer.MapObjects.Count);
                Assert.AreEqual(selectedObjects, SelectionController.SelectedObjects.Count);
                Assert.AreEqual(time, note.JsonTime);
                Assert.AreEqual(type, note.Type);
                Assert.AreEqual(index, note.PosX);
                Assert.AreEqual(layer, note.PosY);
            }

            // No notes loaded
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);

            // Undo delete action
            var actionObjects = PlaceUtils.Undo(actionContainer);
            CheckState(1, 1, actionObjects.OfType<BaseNote>().First(), 0, (int)NoteType.Blue, 2, 2);

            // Undo paste action
            PlaceUtils.Undo(actionContainer);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);

            // Undo cut action
            actionObjects = PlaceUtils.Undo(actionContainer);
            CheckState(1, 1, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Blue, 2, 2);

            // Undo movement
            actionObjects = PlaceUtils.Undo(actionContainer);
            CheckState(1, 1, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Blue, 1, 1);

            // Undo overwrite
            actionObjects = PlaceUtils.Undo(actionContainer);
            CheckState(1, 0, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Red, 1, 1);

            // Undo movement
            actionObjects = PlaceUtils.Undo(actionContainer);
            CheckState(1, 1, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Red, 0, 0);

            // Undo placement
            PlaceUtils.Undo(actionContainer);

            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, SelectionController.SelectedObjects.Count);

            // Redo it all! - Selection is lost :(
            actionObjects = PlaceUtils.Redo(actionContainer);
            CheckState(1, 0, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Red, 0, 0);

            // Moving it selects it
            actionObjects = PlaceUtils.Redo(actionContainer);
            CheckState(1, 1, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Red, 1, 1);

            // Everything is backwards
            actionObjects = PlaceUtils.Redo(actionContainer);
            CheckState(1, 0, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Blue, 1, 1);

            actionObjects = PlaceUtils.Redo(actionContainer);
            CheckState(1, 1, actionObjects.OfType<BaseNote>().First(), 2, (int)NoteType.Blue, 2, 2);

            PlaceUtils.Redo(actionContainer);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);

            // Redo paste
            actionObjects = PlaceUtils.Redo(actionContainer);
            CheckState(1, 1, actionObjects.OfType<BaseNote>().First(), 0, (int)NoteType.Blue, 2, 2);

            // Delete redo should still work even if our object isn't selected
            SelectionController.DeselectAll();

            // Redo delete
            PlaceUtils.Redo(actionContainer);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
            Assert.AreEqual(0, notesContainer.MapObjects.Count);
        }

        [Test]
        public void ModifiedWithConflictingAction()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();

            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

            PlaceUtils.Place(new BaseNote { JsonTime = 2, Type = (int)NoteType.Red });
            var noteB = PlaceUtils.Place(new BaseNote { JsonTime = 2, Type = (int)NoteType.Blue });

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, noteB.JsonTime);

            var undoNotes = PlaceUtils.Undo<BaseNote>(actionContainer).ToList();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, undoNotes[0].JsonTime);

            var redoNotes = PlaceUtils.Redo<BaseNote>(actionContainer).ToList();

            Assert.AreEqual(1, notesContainer.MapObjects.Count);
            Assert.AreEqual(2, redoNotes[0].JsonTime);
        }
    }
}