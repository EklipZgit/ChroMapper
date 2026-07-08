using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
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

                var baseNoteA = new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                baseNoteA = PlaceUtils.Place(baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                    NoteCommand.InvertColor(containerA.NoteData);

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
                    notesContainer.MapObjects[0],
                    "Perform note inversion");

                // Undo invert
                actionContainer.Undo();

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
                    notesContainer.MapObjects[0],
                    "Undo note inversion");
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

            var baseNote1 = new BaseNote
            {
                JsonTime = 1,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baseNote2 = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baseNote3 = new BaseNote
            {
                JsonTime = 3,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
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

            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Blue,
                    CutDirection = default,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = default,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc inverted");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain not inverted");

            actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = default,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Undo arc inversion");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain still not inverted");

            if (notesContainer.LoadedContainers[baseNote2] is NoteContainer container2)
                NoteCommand.InvertColor(container2.NoteData);

            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Blue,
                    CutDirection = default,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = default,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc inverted");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Blue,
                    CutDirection = default,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain inverted");

            actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = default,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Undo arc inversion");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Undo chain inversion");

            if (notesContainer.LoadedContainers[baseNote3] is NoteContainer container3)
                NoteCommand.InvertColor(container3.NoteData);

            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = default,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc not inverted");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain not inverted");

            actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = default,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc still not inverted");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = (int)NoteColor.Red,
                    CutDirection = default,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain not inverted");
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
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Left
                };
                baseNoteA = PlaceUtils.Place(baseNoteA);

                if (notesContainer.LoadedContainers[baseNoteA] is NoteContainer containerA)
                    inputController.ScrollUpdateDirection(containerA, 1);

                BeatmapAssertion.IsEqual(
                    new BaseNote
                    {
                        JsonTime = 2,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Type = (int)NoteType.Red,
                        CutDirection = (int)NoteCutDirection.DownLeft,
                        AngleOffset = 0
                    },
                    notesContainer.MapObjects[0],
                    "Update note direction");

                // Undo direction
                actionContainer.Undo();

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
                    notesContainer.MapObjects[0],
                    "Undo note direction");
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

            var baseNoteA = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            baseNoteA = PlaceUtils.Place(baseNoteA);

            var containerA = notesContainer.LoadedContainers[baseNoteA] as NoteContainer;

            inputController.ScrollUpdateDirection(containerA, 1);

            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.DownLeft,
                    AngleOffset = 0
                },
                notesContainer.MapObjects[0],
                "Update note direction");

            containerA = notesContainer.LoadedContainers[notesContainer.MapObjects[0]] as NoteContainer;

            inputController.ScrollUpdateDirection(containerA, 1);

            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down,
                    AngleOffset = 0
                },
                notesContainer.MapObjects[0],
                "Update note direction");

            // Undo merged direction
            actionContainer.Undo();

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
                notesContainer.MapObjects[0],
                "Undo note direction");

            // Redo merged direction
            actionContainer.Redo();

            BeatmapAssertion.IsEqual(
                new BaseNote
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down,
                    AngleOffset = 0
                },
                notesContainer.MapObjects[0],
                "Undo note direction");
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

            var baseNote1 = new BaseNote
            {
                JsonTime = 1,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Left
            };
            var baseNote2 = new BaseNote
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Up
            };
            var baseNote3 = new BaseNote
            {
                JsonTime = 3,
                PosX = (int)GridX.Left,
                PosY = (int)GridY.Base,
                Type = (int)NoteType.Red,
                CutDirection = (int)NoteCutDirection.Right
            };

            var baseArc12 = new BaseArc
            {
                JsonTime = 1,
                TailJsonTime = 2,
                CutDirection = (int)NoteCutDirection.Left,
                TailCutDirection = (int)NoteCutDirection.Up
            };
            var baseChain23 = new BaseChain { JsonTime = 2, TailJsonTime = 3, CutDirection = (int)NoteCutDirection.Up };

            baseNote1 = PlaceUtils.Place(baseNote1);
            baseNote2 = PlaceUtils.Place(baseNote2);
            baseNote3 = PlaceUtils.Place(baseNote3);

            baseArc12 = PlaceUtils.Place(baseArc12);
            baseChain23 = PlaceUtils.Place(baseChain23);

            if (notesContainer.LoadedContainers[baseNote1] is NoteContainer container1)
                inputController.ScrollUpdateDirection(container1, 0);

            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.UpLeft,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = (int)NoteCutDirection.Up,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc head direction");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Up,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain direction not changed");

            actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = (int)NoteCutDirection.Up,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Undo arc head direction");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Up,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain direction still not changed");

            if (notesContainer.LoadedContainers[baseNote2] is NoteContainer container2)
                inputController.ScrollUpdateDirection(container2, 0);

            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = (int)NoteCutDirection.UpRight,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc tail direction");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.UpRight,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain direction");

            actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = (int)NoteCutDirection.Up,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Undo arc tail direction");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Up,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Undo chain direction");

            if (notesContainer.LoadedContainers[baseNote3] is NoteContainer container3)
                inputController.ScrollUpdateDirection(container3, 0);

            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = (int)NoteCutDirection.Up,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc direction not changed");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Up,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain direction not changed");

            actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseArc
                {
                    JsonTime = 1,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Left,
                    AngleOffset = default,
                    HeadControlPointLengthMultiplier = default,
                    TailJsonTime = 2,
                    TailPosX = default,
                    TailPosY = default,
                    TailCutDirection = (int)NoteCutDirection.Up,
                    TailControlPointLengthMultiplier = default,
                    MidAnchorMode = default
                },
                arcsContainer.MapObjects[0],
                "Arc direction still not changed");
            BeatmapAssertion.IsEqual(
                new BaseChain
                {
                    JsonTime = 2,
                    PosX = default,
                    PosY = default,
                    Color = default,
                    CutDirection = (int)NoteCutDirection.Up,
                    AngleOffset = default,
                    TailJsonTime = 3,
                    TailPosX = default,
                    TailPosY = default,
                    SliceCount = default,
                    Squish = default
                },
                chainsContainer.MapObjects[0],
                "Chain direction still not changed");
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

                v3NoteA = PlaceUtils.Place(v3NoteA);

                BeatmapAssertion.IsEqual(
                    new BaseNote
                    {
                        JsonTime = 2,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Type = (int)NoteType.Red,
                        CutDirection = (int)NoteCutDirection.Left,
                        AngleOffset = 0,
                        CustomData = new JSONObject { ["localRotation"] = localRotation }
                    },
                    notesContainer.MapObjects[0],
                    "Applies CustomProperties to v3 CustomData");

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

                v2NoteB = PlaceUtils.Place(v2NoteB);

                BeatmapAssertion.IsEqual(
                    new BaseNote
                    {
                        JsonTime = 4,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Type = (int)NoteType.Red,
                        CutDirection = (int)NoteCutDirection.Left,
                        AngleOffset = 0,
                        CustomData =
                            new JSONObject
                            {
                                ["_localRotation"] = localRotation, ["_cutDirection"] = customDirection
                            }
                    },
                    notesContainer.MapObjects[1],
                    "Applies CustomProperties to v2 CustomData");
            }
        }
    }
}