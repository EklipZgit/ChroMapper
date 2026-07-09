using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class NoteTest : TestBase
    {
        [Test]
        public void InvertNote()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var noteA = new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                var originalNoteA = BeatmapFactory.Clone(noteA);
                noteA = PlaceUtils.Place(noteA);

                var expectedInvertedNote = BeatmapFactory.Clone(originalNoteA);
                expectedInvertedNote.Type = (int)NoteType.Blue;
                var expectedOriginalNote = BeatmapFactory.Clone(originalNoteA);

                if (notesContainer.LoadedContainers[noteA] is NoteContainer containerA)
                    noteA = NoteCommand.InvertColor(containerA.NoteData);

                BeatmapAssertion.IsEqual(expectedInvertedNote, noteA, "Perform note inversion");

                // Undo invert
                var undoObjects = PlaceUtils.Undo<BaseNote>(actionContainer).ToList();

                BeatmapAssertion.IsEqual(expectedOriginalNote, undoObjects[0], "Undo note inversion");
            }
        }

        [Test]
        public void InvertNoteAffectsSlider()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var chainsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();
            var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();

            var note1 = new BaseNote
            {
                JsonTime = 1,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var note2 = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var note3 = new BaseNote
            {
                JsonTime = 3,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };

            var arc12 = new BaseArc { JsonTime = 1, TailJsonTime = 2, Color = (int)NoteColor.Red };
            var chain23 = new BaseChain { JsonTime = 2, TailJsonTime = 3, Color = (int)NoteColor.Red };

            note1 = PlaceUtils.Place(note1);
            note2 = PlaceUtils.Place(note2);
            note3 = PlaceUtils.Place(note3);

            var originalArc12 = BeatmapFactory.Clone(arc12);
            arc12 = PlaceUtils.Place(arc12);
            var originalChain23 = BeatmapFactory.Clone(chain23);
            chain23 = PlaceUtils.Place(chain23);

            var expectedArcRed = BeatmapFactory.Clone(originalArc12);
            var expectedChainRed = BeatmapFactory.Clone(originalChain23);
            var expectedArcBlue = BeatmapFactory.Clone(originalArc12);
            expectedArcBlue.Color = (int)NoteColor.Blue;
            var expectedChainBlue = BeatmapFactory.Clone(originalChain23);
            expectedChainBlue.Color = (int)NoteColor.Blue;

            if (notesContainer.LoadedContainers[note1] is NoteContainer container1)
                note1 = NoteCommand.InvertColor(container1.NoteData);

            var undoImmediateObjects = PlaceUtils.Undo(actionContainer);
            var redoImmediateObjects = PlaceUtils.Redo(actionContainer);
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            BeatmapAssertion.IsEqual(expectedArcBlue, arc12, "Arc inverted");
            BeatmapAssertion.IsEqual(expectedChainRed, chain23, "Chain not inverted");

            var undoNote1Objects = PlaceUtils.Undo(actionContainer);
            arc12 = undoNote1Objects.OfType<BaseArc>().First();
            BeatmapAssertion.IsEqual(expectedArcRed, arc12, "Undo arc inversion");
            BeatmapAssertion.IsEqual(expectedChainRed, chain23, "Chain still not inverted");

            if (notesContainer.LoadedContainers[note2] is NoteContainer container2)
                note2 = NoteCommand.InvertColor(container2.NoteData);

            undoImmediateObjects = PlaceUtils.Undo(actionContainer);
            redoImmediateObjects = PlaceUtils.Redo(actionContainer);
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            chain23 = redoImmediateObjects.OfType<BaseChain>().Single();
            BeatmapAssertion.IsEqual(expectedArcBlue, arc12, "Arc inverted");
            BeatmapAssertion.IsEqual(expectedChainBlue, chain23, "Chain inverted");

            var undoNote2Objects = PlaceUtils.Undo(actionContainer);
            arc12 = undoNote2Objects.OfType<BaseArc>().First();
            chain23 = undoNote2Objects.OfType<BaseChain>().First();
            BeatmapAssertion.IsEqual(expectedArcRed, undoNote2Objects.OfType<BaseArc>().First(), "Undo arc inversion");
            BeatmapAssertion.IsEqual(
                expectedChainRed,
                undoNote2Objects.OfType<BaseChain>().First(),
                "Undo chain inversion");

            if (notesContainer.LoadedContainers[note3] is NoteContainer container3)
                note3 = NoteCommand.InvertColor(container3.NoteData);

            BeatmapAssertion.IsEqual(expectedArcRed, arc12, "Arc not inverted");
            BeatmapAssertion.IsEqual(expectedChainRed, chain23, "Chain not inverted");

            PlaceUtils.Undo(actionContainer);
            BeatmapAssertion.IsEqual(expectedArcRed, arc12, "Arc still not inverted");
            BeatmapAssertion.IsEqual(expectedChainRed, chain23, "Chain not inverted");
        }

        [Test]
        public void UpdateNoteDirection()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
                var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

                var noteA = new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                var originalNoteA = BeatmapFactory.Clone(noteA);
                noteA = PlaceUtils.Place(noteA);

                var expectedUpdatedNote = BeatmapFactory.Clone(originalNoteA);
                expectedUpdatedNote.CutDirection = (int)NoteCutDirection.DownLeft;
                var expectedOriginalNote = BeatmapFactory.Clone(originalNoteA);

                if (notesContainer.LoadedContainers[noteA] is NoteContainer containerA)
                    inputController.ScrollUpdateDirection(containerA, 1);

                noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

                BeatmapAssertion.IsEqual(expectedUpdatedNote, noteA, "Update note direction");

                // Undo direction
                var undoObjects = PlaceUtils.Undo<BaseNote>(actionContainer).ToList();

                BeatmapAssertion.IsEqual(expectedOriginalNote, undoObjects[0], "Undo note direction");
            }
        }

        [Test]
        public void UpdateNoteDirectionMergeAction()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

            var noteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var originalNoteA = BeatmapFactory.Clone(noteA);
            noteA = PlaceUtils.Place(noteA);

            var expectedOriginalNote = BeatmapFactory.Clone(originalNoteA);
            var expectedFirstDirection = BeatmapFactory.Clone(originalNoteA);
            expectedFirstDirection.CutDirection = (int)NoteCutDirection.DownLeft;
            var expectedSecondDirection = BeatmapFactory.Clone(originalNoteA);
            expectedSecondDirection.CutDirection = (int)NoteCutDirection.Down;

            var containerA = notesContainer.LoadedContainers[noteA] as NoteContainer;

            inputController.ScrollUpdateDirection(containerA, 1);

            noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

            BeatmapAssertion.IsEqual(expectedFirstDirection, noteA, "Update note direction");

            containerA = notesContainer.LoadedContainers[noteA] as NoteContainer;

            inputController.ScrollUpdateDirection(containerA, 1);

            noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

            BeatmapAssertion.IsEqual(expectedSecondDirection, noteA, "Update note direction");

            // Undo merged direction
            var undoDirectionObjects = PlaceUtils.Undo<BaseNote>(actionContainer).ToList();

            BeatmapAssertion.IsEqual(expectedOriginalNote, undoDirectionObjects[0], "Undo note direction");

            // Redo merged direction
            var redoDirectionObjects = PlaceUtils.Redo<BaseNote>(actionContainer).ToList();

            BeatmapAssertion.IsEqual(expectedSecondDirection, redoDirectionObjects[0], "Undo note direction");
        }

        [Test]
        public void UpdateNoteDirectionAffectsSlider()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var chainsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();
            var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

            var note1 = new BaseNote
            {
                JsonTime = 1,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var note2 = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Up
            };
            var note3 = new BaseNote
            {
                JsonTime = 3,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Right
            };

            var arc12 = new BaseArc
            {
                JsonTime = 1,
                TailJsonTime = 2,
                CutDirection = (int)NoteCutDirection.Left,
                TailCutDirection = (int)NoteCutDirection.Up
            };
            var chain23 = new BaseChain { JsonTime = 2, TailJsonTime = 3, CutDirection = (int)NoteCutDirection.Up };

            note1 = PlaceUtils.Place(note1);
            note2 = PlaceUtils.Place(note2);
            note3 = PlaceUtils.Place(note3);

            var originalArc12 = BeatmapFactory.Clone(arc12);
            arc12 = PlaceUtils.Place(arc12);
            var originalChain23 = BeatmapFactory.Clone(chain23);
            chain23 = PlaceUtils.Place(chain23);

            var expectedArcOriginal = BeatmapFactory.Clone(originalArc12);
            var expectedChainOriginal = BeatmapFactory.Clone(originalChain23);
            var expectedArcHeadDirection = BeatmapFactory.Clone(originalArc12);
            expectedArcHeadDirection.CutDirection = (int)NoteCutDirection.UpLeft;
            var expectedArcTailDirection = BeatmapFactory.Clone(originalArc12);
            expectedArcTailDirection.TailCutDirection = (int)NoteCutDirection.UpRight;
            var expectedChainTailDirection = BeatmapFactory.Clone(originalChain23);
            expectedChainTailDirection.CutDirection = (int)NoteCutDirection.UpRight;

            if (notesContainer.LoadedContainers[note1] is NoteContainer container1)
                inputController.ScrollUpdateDirection(container1, 0);

            var undoImmediateObjects = PlaceUtils.Undo(actionContainer);
            var redoImmediateObjects = PlaceUtils.Redo(actionContainer);
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            BeatmapAssertion.IsEqual(expectedArcHeadDirection, arc12, "Arc head direction");
            BeatmapAssertion.IsEqual(expectedChainOriginal, chain23, "Chain direction not changed");

            var undoNote1DirectionObjects = PlaceUtils.Undo(actionContainer);
            arc12 = undoNote1DirectionObjects.OfType<BaseArc>().First();
            BeatmapAssertion.IsEqual(
                expectedArcOriginal,
                arc12,
                "Undo arc head direction");
            BeatmapAssertion.IsEqual(expectedChainOriginal, chain23, "Chain direction still not changed");

            if (notesContainer.LoadedContainers[note2] is NoteContainer container2)
                inputController.ScrollUpdateDirection(container2, 0);

            undoImmediateObjects = PlaceUtils.Undo(actionContainer);
            redoImmediateObjects = PlaceUtils.Redo(actionContainer);
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            chain23 = redoImmediateObjects.OfType<BaseChain>().Single();
            BeatmapAssertion.IsEqual(expectedArcTailDirection, arc12, "Arc tail direction");
            BeatmapAssertion.IsEqual(expectedChainTailDirection, chain23, "Chain direction");

            var undoNote2DirectionObjects = PlaceUtils.Undo(actionContainer);
            arc12 = undoNote2DirectionObjects.OfType<BaseArc>().First();
            chain23 = undoNote2DirectionObjects.OfType<BaseChain>().First();
            BeatmapAssertion.IsEqual(
                expectedArcOriginal,
                arc12,
                "Undo arc tail direction");
            BeatmapAssertion.IsEqual(
                expectedChainOriginal,
                chain23,
                "Undo chain direction");

            if (notesContainer.LoadedContainers[note3] is NoteContainer container3)
                inputController.ScrollUpdateDirection(container3, 0);

            BeatmapAssertion.IsEqual(expectedArcOriginal, arc12, "Arc direction not changed");
            BeatmapAssertion.IsEqual(expectedChainOriginal, chain23, "Chain direction not changed");

            PlaceUtils.Undo(actionContainer);
            BeatmapAssertion.IsEqual(expectedArcOriginal, arc12, "Arc direction still not changed");
            BeatmapAssertion.IsEqual(expectedChainOriginal, chain23, "Chain direction still not changed");
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var customDirection = 69;
                var localRotation = new JSONArray { [0] = 0, [1] = 1, [2] = 2 };

                Settings.Instance.MapVersion = 3;
                var v3NoteA = new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                v3NoteA.CustomLocalRotation = localRotation;
                v3NoteA.CustomDirection = customDirection;

                var expectedV3NoteA = BeatmapFactory.Clone(v3NoteA);
                expectedV3NoteA.CustomData = new JSONObject { ["localRotation"] = localRotation };
                v3NoteA = PlaceUtils.Place(v3NoteA);

                BeatmapAssertion.IsEqual(expectedV3NoteA, v3NoteA, "Applies CustomProperties to v3 CustomData");

                Settings.Instance.MapVersion = 2;
                var v2NoteB = new BaseNote
                {
                    JsonTime = 4,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                v2NoteB.CustomDirection = customDirection;
                v2NoteB.CustomLocalRotation = localRotation;

                var expectedV2NoteB = BeatmapFactory.Clone(v2NoteB);
                expectedV2NoteB.CustomData = new JSONObject
                {
                    ["_localRotation"] = localRotation, ["_cutDirection"] = customDirection
                };
                v2NoteB = PlaceUtils.Place(v2NoteB);

                BeatmapAssertion.IsEqual(expectedV2NoteB, v2NoteB, "Applies CustomProperties to v2 CustomData");
            }
        }
    }
}