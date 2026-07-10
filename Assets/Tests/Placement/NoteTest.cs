using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class NoteTest : TestBase
    {
        [Test]
        public void InvertNote()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                Object.FindAnyObjectByType<NotePlacement>();

                var noteA = new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                var baselineNoteA = BeatmapFactory.Clone(noteA);
                noteA = PlaceUtils.Place(noteA);

                if (notesContainer.LoadedContainers[noteA] is NoteContainer containerA)
                    noteA = NoteCommand.InvertColor(containerA.NoteData);

                BeatmapAssertion.IsEqualWithChanges(
                    baselineNoteA,
                    noteA,
                    n => { n.Type = (int)NoteType.Blue; },
                    "Perform note inversion");

                // Undo invert
                var undoObjects = PlaceUtils.Undo<BaseNote>().ToList();

                BeatmapAssertion.IsUnchanged(baselineNoteA, undoObjects[0], "Undo note inversion");
            }
        }

        [Test]
        public void InvertNoteAffectsSlider()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

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

            var baselineArc = BeatmapFactory.Clone(arc12);
            arc12 = PlaceUtils.Place(arc12);
            var baselineChain = BeatmapFactory.Clone(chain23);
            chain23 = PlaceUtils.Place(chain23);

            // Baseline clones are immutable; expected states derived via IsEqualWithChanges/IsUnchanged below.

            if (notesContainer.LoadedContainers[note1] is NoteContainer container1)
                note1 = NoteCommand.InvertColor(container1.NoteData);

            var undoImmediateObjects = PlaceUtils.Undo();
            var redoImmediateObjects = PlaceUtils.Redo();
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc12,
                a => { a.Color = (int)NoteColor.Blue; },
                "Arc inverted");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain not inverted");

            var undoNote1Objects = PlaceUtils.Undo();
            arc12 = undoNote1Objects.OfType<BaseArc>().First();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc12,
                a => { a.Color = (int)NoteColor.Red; },
                "Undo arc inversion");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain still not inverted");

            if (notesContainer.LoadedContainers[note2] is NoteContainer container2)
                note2 = NoteCommand.InvertColor(container2.NoteData);

            undoImmediateObjects = PlaceUtils.Undo();
            redoImmediateObjects = PlaceUtils.Redo();
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            chain23 = redoImmediateObjects.OfType<BaseChain>().Single();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc12,
                a => { a.Color = (int)NoteColor.Blue; },
                "Arc inverted");
            BeatmapAssertion.IsEqualWithChanges(
                baselineChain,
                chain23,
                c => { c.Color = (int)NoteColor.Blue; },
                "Chain inverted");

            var undoNote2Objects = PlaceUtils.Undo();
            arc12 = undoNote2Objects.OfType<BaseArc>().First();
            chain23 = undoNote2Objects.OfType<BaseChain>().First();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc12,
                a => { a.Color = (int)NoteColor.Red; },
                "Undo arc inversion");
            BeatmapAssertion.IsEqualWithChanges(
                baselineChain,
                chain23,
                c => { c.Color = (int)NoteColor.Red; },
                "Undo chain inversion");

            if (notesContainer.LoadedContainers[note3] is NoteContainer container3)
                note3 = NoteCommand.InvertColor(container3.NoteData);

            BeatmapAssertion.IsUnchanged(baselineArc, arc12, "Arc not inverted");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain not inverted");

            PlaceUtils.Undo();
            BeatmapAssertion.IsUnchanged(baselineArc, arc12, "Arc still not inverted");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain not inverted");
        }

        [Test]
        public void UpdateNoteDirection()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note) as NoteGridContainer;
            Object.FindAnyObjectByType<NotePlacement>();
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

            var noteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baselineNoteA = BeatmapFactory.Clone(noteA);
            noteA = PlaceUtils.Place(noteA);

            if (notesContainer.LoadedContainers[noteA] is NoteContainer containerA)
                inputController.ScrollUpdateDirection(containerA, 1);

            noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

            BeatmapAssertion.IsEqualWithChanges(
                baselineNoteA,
                noteA,
                n => { n.CutDirection = (int)NoteCutDirection.DownLeft; },
                "Update note direction");

            // Undo direction
            var undoObjects = PlaceUtils.Undo<BaseNote>().ToList();

            BeatmapAssertion.IsUnchanged(baselineNoteA, undoObjects[0], "Undo note direction");
        }

        [Test]
        public void UpdateNoteDirectionMergeAction()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

            var noteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baselineNoteA = BeatmapFactory.Clone(noteA);
            noteA = PlaceUtils.Place(noteA);

            var containerA = notesContainer.LoadedContainers[noteA] as NoteContainer;

            inputController.ScrollUpdateDirection(containerA, 1);

            noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

            BeatmapAssertion.IsEqualWithChanges(
                baselineNoteA,
                noteA,
                n => { n.CutDirection = (int)NoteCutDirection.DownLeft; },
                "Update note direction");

            containerA = notesContainer.LoadedContainers[noteA] as NoteContainer;

            inputController.ScrollUpdateDirection(containerA, 1);

            noteA = SelectionController.SelectedObjects.OfType<BaseNote>().Single();

            BeatmapAssertion.IsEqualWithChanges(
                baselineNoteA,
                noteA,
                n => { n.CutDirection = (int)NoteCutDirection.Down; },
                "Update note direction");

            // Undo merged direction
            var undoDirectionObjects = PlaceUtils.Undo<BaseNote>().ToList();

            BeatmapAssertion.IsUnchanged(baselineNoteA, undoDirectionObjects[0], "Undo note direction");

            // Redo merged direction
            var redoDirectionObjects = PlaceUtils.Redo<BaseNote>().ToList();

            BeatmapAssertion.IsEqualWithChanges(
                baselineNoteA,
                redoDirectionObjects[0],
                n => { n.CutDirection = (int)NoteCutDirection.Down; },
                "Undo note direction");
        }

        [Test]
        public void UpdateNoteDirectionAffectsSlider()
        {
            var notesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
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

            var baselineArc = BeatmapFactory.Clone(arc12);
            arc12 = PlaceUtils.Place(arc12);
            var baselineChain = BeatmapFactory.Clone(chain23);
            chain23 = PlaceUtils.Place(chain23);

            if (notesContainer.LoadedContainers[note1] is NoteContainer container1)
                inputController.ScrollUpdateDirection(container1, 0);

            var undoImmediateObjects = PlaceUtils.Undo();
            var redoImmediateObjects = PlaceUtils.Redo();
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc12,
                a => { a.CutDirection = (int)NoteCutDirection.UpLeft; },
                "Arc head direction");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain direction not changed");

            var undoNote1DirectionObjects = PlaceUtils.Undo();
            arc12 = undoNote1DirectionObjects.OfType<BaseArc>().First();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc12,
                a => { a.CutDirection = (int)NoteCutDirection.Left; },
                "Undo arc head direction");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain direction still not changed");

            if (notesContainer.LoadedContainers[note2] is NoteContainer container2)
                inputController.ScrollUpdateDirection(container2, 0);

            undoImmediateObjects = PlaceUtils.Undo();
            redoImmediateObjects = PlaceUtils.Redo();
            arc12 = redoImmediateObjects.OfType<BaseArc>().Single();
            chain23 = redoImmediateObjects.OfType<BaseChain>().Single();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc12,
                a => { a.TailCutDirection = (int)NoteCutDirection.UpRight; },
                "Arc tail direction");
            BeatmapAssertion.IsEqualWithChanges(
                baselineChain,
                chain23,
                c => { c.CutDirection = (int)NoteCutDirection.UpRight; },
                "Chain direction");

            var undoNote2DirectionObjects = PlaceUtils.Undo();
            arc12 = undoNote2DirectionObjects.OfType<BaseArc>().First();
            chain23 = undoNote2DirectionObjects.OfType<BaseChain>().First();
            BeatmapAssertion.IsUnchanged(baselineArc, arc12, "Undo arc tail direction");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Undo chain direction");

            if (notesContainer.LoadedContainers[note3] is NoteContainer container3)
                inputController.ScrollUpdateDirection(container3, 0);

            BeatmapAssertion.IsUnchanged(baselineArc, arc12, "Arc direction not changed");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain direction not changed");

            PlaceUtils.Undo();
            BeatmapAssertion.IsUnchanged(baselineArc, arc12, "Arc direction still not changed");
            BeatmapAssertion.IsUnchanged(baselineChain, chain23, "Chain direction still not changed");
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            var customDirection = 69;
            var localRotation = new JSONArray { [0] = 0, [1] = 1, [2] = 2 };

            var savedMapVersion = Settings.Instance.MapVersion;
            try
            {
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
            finally
            {
                Settings.Instance.MapVersion = savedMapVersion;
            }
        }
    }
}
