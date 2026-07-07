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

                var baseNoteA = new BaseNote
                {
                    JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                baseNoteA = PlaceUtils.Place(baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                    NoteCommand.InvertColor(containerA.NoteData);

                CheckUtils.CheckNote("Perform note inversion", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Blue, (int)NoteCutDirection.Left, 0);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckNote("Undo note inversion", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            }
        }

        [Test]
        public void InvertNoteAffectsSlider()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var chainsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();
            var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();

            var baseNote1 = new BaseNote
            {
                JsonTime = 1, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baseNote2 = new BaseNote
            {
                JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baseNote3 = new BaseNote
            {
                JsonTime = 3, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };

            var baseArc12 = new BaseArc { JsonTime = 1, TailJsonTime = 2, Color = (int)NoteColor.Red };
            var baseChain23 = new BaseChain { JsonTime = 2, TailJsonTime = 3, Color = (int)NoteColor.Red };

            baseNote1 = PlaceUtils.Place(baseNote1);
            baseNote2 = PlaceUtils.Place(baseNote2);
            baseNote3 = PlaceUtils.Place(baseNote3);

            baseArc12 = PlaceUtils.Place(baseArc12);
            baseChain23 = PlaceUtils.Place(baseChain23);

            if (notesContainer.LoadedContainers[baseNote1] is NoteContainer container1)
                NoteCommand.InvertColor(container1.NoteData);

            CheckUtils.CheckArc("Arc inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Blue, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc inversion", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain still not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote2] is NoteContainer container2)
                NoteCommand.InvertColor(container2.NoteData);

            CheckUtils.CheckArc("Arc inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Blue, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Blue, default, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc inversion", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Undo chain inversion", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote3] is NoteContainer container3)
                NoteCommand.InvertColor(container3.NoteData);

            CheckUtils.CheckArc("Arc not inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Arc still not inverted", arcsContainer, 0, 1, default, default, (int)NoteColor.Red, default, default, default, 2, default, default, default, default, default);
            CheckUtils.CheckChain("Chain not inverted", chainsContainer, 0, 2, default, default, (int)NoteColor.Red, default, default, 3, default, default, default, default);
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

                var baseNoteA = new BaseNote
                {
                    JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                        CutDirection = (int)NoteCutDirection.Left
                };
                baseNoteA = PlaceUtils.Place(baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                    inputController.ScrollUpdateDirection(containerA, 1);

                CheckUtils.CheckNote("Update note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.DownLeft, 0);

                // Undo direction
                actionContainer.Undo();

                CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0);
            }
        }
        
        [Test]
        public void UpdateNoteDirectionMergeAction()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

            var baseNoteA = new BaseNote
            {
                JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            baseNoteA = PlaceUtils.Place(baseNoteA);

            var containerA = notesContainer.LoadedContainers[baseNoteA] as NoteContainer;
            
            inputController.ScrollUpdateDirection(containerA, 1);

            CheckUtils.CheckNote("Update note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.DownLeft, 0);

            containerA = notesContainer.LoadedContainers[notesContainer.MapObjects[0]] as NoteContainer;
            
            inputController.ScrollUpdateDirection(containerA, 1);

            CheckUtils.CheckNote("Update note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Down, 0);
            
            // Undo merged direction
            actionContainer.Undo();

            CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Left, 0);

            // Redo merged direction
            actionContainer.Redo();
            
            CheckUtils.CheckNote("Undo note direction", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Down, 0);
        }

        [Test]
        public void UpdateNoteDirectionAffectsSlider()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            var arcsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var chainsContainer = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

            var notePlacement = Object.FindAnyObjectByType<NotePlacement>();
            var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();
            var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();
            var inputController = Object.FindAnyObjectByType<BeatmapNoteInputController>();

            var baseNote1 = new BaseNote
            {
                JsonTime = 1, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baseNote2 = new BaseNote
            {
                JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Up
            };
            var baseNote3 = new BaseNote
            {
                JsonTime = 3, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Right
            };

            var baseArc12 = new BaseArc { JsonTime = 1, TailJsonTime = 2, CutDirection = (int)NoteCutDirection.Left, TailCutDirection = (int)NoteCutDirection.Up };
            var baseChain23 = new BaseChain { JsonTime = 2, TailJsonTime = 3, CutDirection = (int)NoteCutDirection.Up };

            baseNote1 = PlaceUtils.Place(baseNote1);
            baseNote2 = PlaceUtils.Place(baseNote2);
            baseNote3 = PlaceUtils.Place(baseNote3);

            baseArc12 = PlaceUtils.Place(baseArc12);
            baseChain23 = PlaceUtils.Place(baseChain23);

            if (notesContainer.LoadedContainers[baseNote1] is NoteContainer container1)
                inputController.ScrollUpdateDirection(container1, 0);

            CheckUtils.CheckArc("Arc head direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.UpLeft, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc head direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction still not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote2] is NoteContainer container2)
                inputController.ScrollUpdateDirection(container2, 0);

            CheckUtils.CheckArc("Arc tail direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.UpRight, default, default);
            CheckUtils.CheckChain("Chain direction", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.UpRight, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Undo arc tail direction", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Undo chain direction", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            if (notesContainer.LoadedContainers[baseNote3] is NoteContainer container3)
                inputController.ScrollUpdateDirection(container3, 0);

            CheckUtils.CheckArc("Arc direction not changed", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);

            actionContainer.Undo();
            CheckUtils.CheckArc("Arc direction still not changed", arcsContainer, 0, 1, default, default, default, (int)NoteCutDirection.Left, default, default, 2, default, default, (int)NoteCutDirection.Up, default, default);
            CheckUtils.CheckChain("Chain direction still not changed", chainsContainer, 0, 2, default, default, default, (int)NoteCutDirection.Up, default, 3, default, default, default, default);
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var customDirection = 69;
                var localRotation = new JSONArray() { [0] = 0, [1] = 1, [2] = 2 };

                Settings.Instance.MapVersion = 3;
                var v3NoteA = new BaseNote
                {
                    JsonTime = 2, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                v3NoteA.CustomLocalRotation = localRotation;
                v3NoteA.CustomDirection = customDirection;

                v3NoteA = PlaceUtils.Place(v3NoteA);

                CheckUtils.CheckNote("Applies CustomProperties to v3 CustomData", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0,
                    new JSONObject() { ["localRotation"] = localRotation });

                Settings.Instance.MapVersion = 2;
                var v2NoteB = new BaseNote
                {
                    JsonTime = 4, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                v2NoteB.CustomDirection = customDirection;
                v2NoteB.CustomLocalRotation = localRotation;

                v2NoteB = PlaceUtils.Place(v2NoteB);

                CheckUtils.CheckNote("Applies CustomProperties to v2 CustomData", notesContainer, 1, 4, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteType.Red, (int)NoteCutDirection.Left, 0,
                    new JSONObject() { ["_localRotation"] = localRotation, ["_cutDirection"] = customDirection });
            }
        }
    }
}
