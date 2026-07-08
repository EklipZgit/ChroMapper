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
    public class ChainTest : TestBase
    {
        [Test]
        public void CreateChain()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var baseNoteA = new BaseNote
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down
                };
                var baseNoteB = new BaseNote
                {
                    JsonTime = 3f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Upper,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up
                };
                baseNoteA = PlaceUtils.Place(baseNoteA);
                baseNoteB = PlaceUtils.Place(baseNoteB);

                SelectionController.Select(baseNoteA);
                SelectionController.Select(baseNoteB, true);
            }

            var chainContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (chainContainerCollection is ChainGridContainer chainsContainer)
            {
                var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();

                var objects = SelectionController.SelectedObjects.ToList();

                Assert.AreEqual(2, objects.Count);

                if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                    Assert.Fail("Both selected objects is not color note");
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                chainPlacement.TryCreateChainData(n1, n2, out var chain, out var tailNote);
                chain = PlaceUtils.Place(chain);

                BeatmapAssertion.IsEqual(
                    new BaseChain
                    {
                        JsonTime = 2f,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Color = (int)NoteColor.Red,
                        CutDirection = (int)NoteCutDirection.Down,
                        AngleOffset = 0,
                        TailJsonTime = 3f,
                        TailPosX = (int)GridX.Left,
                        TailPosY = (int)GridY.Upper,
                        SliceCount = 5,
                        Squish = 1
                    },
                    chainsContainer.MapObjects[0],
                    "Check generated chain");
                Assert.AreSame(n2, tailNote);
            }
        }

        [Test]
        public void CreateChainWithCoordinates()
        {
            var headCoordinates = new JSONArray { [0] = 69, [1] = 69 };
            var tailCoordinates = new JSONArray { [0] = 420, [1] = 420 };

            var headCustomData = new JSONObject { ["coordinates"] = headCoordinates };
            var tailCustomData = new JSONObject { ["coordinates"] = tailCoordinates };

            Settings.Instance.MapVersion = 3;
            var chainCustomData = new JSONObject
            {
                ["coordinates"] = headCoordinates, ["tailCoordinates"] = tailCoordinates
            };

            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var baseNoteA = new BaseNote
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down,
                    CustomData = headCustomData
                };

                var baseNoteB = new BaseNote
                {
                    JsonTime = 3f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Upper,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up,
                    CustomData = tailCustomData
                };

                baseNoteA = PlaceUtils.Place(baseNoteA);
                baseNoteB = PlaceUtils.Place(baseNoteB);

                SelectionController.Select(baseNoteA);
                SelectionController.Select(baseNoteB, true);
            }

            var chainContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (chainContainerCollection is ChainGridContainer chainsContainer)
            {
                var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();

                var objects = SelectionController.SelectedObjects.ToList();

                Assert.AreEqual(2, objects.Count);

                if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                    Assert.Fail("Both selected objects is not color note");
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                chainPlacement.TryCreateChainData(n1, n2, out var chain, out _);
                chain = PlaceUtils.Place(chain);

                BeatmapAssertion.IsEqual(
                    new BaseChain
                    {
                        JsonTime = 2f,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Color = (int)NoteColor.Red,
                        CutDirection = (int)NoteCutDirection.Down,
                        AngleOffset = 0,
                        TailJsonTime = 3f,
                        TailPosX = (int)GridX.Left,
                        TailPosY = (int)GridY.Upper,
                        SliceCount = 5,
                        Squish = 1,
                        CustomData = chainCustomData
                    },
                    chainsContainer.MapObjects[0],
                    "Check generated chain");
            }
        }

        [Test]
        public void InvertChain()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (containerCollection is ChainGridContainer chainsContainer)
            {
                var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();
                var inputController = Object.FindAnyObjectByType<BeatmapSharedNoteInputController>();

                var baseChain = new BaseChain
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteColor.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    TailJsonTime = 3f,
                    TailPosX = (int)GridX.Left,
                    TailPosY = (int)GridY.Base,
                    SliceCount = 5,
                    Squish = 1f
                };
                baseChain = PlaceUtils.Place(baseChain);

                if (chainsContainer.LoadedContainers[baseChain] is ChainContainer containerA)
                    SliderCommand.InvertColor(containerA.ChainData);

                BeatmapAssertion.IsEqual(
                    new BaseChain
                    {
                        JsonTime = 2f,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Color = (int)NoteColor.Blue,
                        CutDirection = (int)NoteCutDirection.Left,
                        AngleOffset = 0,
                        TailJsonTime = 3f,
                        TailPosX = (int)GridX.Left,
                        TailPosY = (int)GridY.Base,
                        SliceCount = 5,
                        Squish = 1f
                    },
                    chainsContainer.MapObjects[0],
                    "Perform chain inversion");

                // Undo invert
                actionContainer.Undo();

                BeatmapAssertion.IsEqual(
                    new BaseChain
                    {
                        JsonTime = 2f,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Color = (int)NoteColor.Red,
                        CutDirection = (int)NoteCutDirection.Left,
                        AngleOffset = 0,
                        TailJsonTime = 3f,
                        TailPosX = (int)GridX.Left,
                        TailPosY = (int)GridY.Base,
                        SliceCount = 5,
                        Squish = 1f
                    },
                    chainsContainer.MapObjects[0],
                    "Undo chain inversion");
            }
        }

        [Test]
        public void UpdateChainMultiplier()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (containerCollection is ChainGridContainer chainsContainer)
            {
                var chainPlacement = Object.FindAnyObjectByType<ChainPlacement>();
                var inputController = Object.FindAnyObjectByType<BeatmapChainInputController>();

                var baseChain = new BaseChain
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteColor.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    TailJsonTime = 3f,
                    TailPosX = (int)GridX.Left,
                    TailPosY = (int)GridY.Base,
                    SliceCount = 5,
                    Squish = 1f
                };
                baseChain = PlaceUtils.Place(baseChain);

                if (chainsContainer.LoadedContainers[baseChain] is ChainContainer containerA)
                    inputController.TweakChainSquish(containerA, 0.5f);

                BeatmapAssertion.IsEqual(
                    new BaseChain
                    {
                        JsonTime = 2f,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Color = (int)NoteColor.Red,
                        CutDirection = (int)NoteCutDirection.Left,
                        AngleOffset = 0,
                        TailJsonTime = 3f,
                        TailPosX = (int)GridX.Left,
                        TailPosY = (int)GridY.Base,
                        SliceCount = 5,
                        Squish = 1.5f
                    },
                    chainsContainer.MapObjects[0],
                    "Update chain multiplier");

                // Undo invert
                actionContainer.Undo();

                BeatmapAssertion.IsEqual(
                    new BaseChain
                    {
                        JsonTime = 2f,
                        PosX = (int)GridX.Left,
                        PosY = (int)GridY.Base,
                        Color = (int)NoteColor.Red,
                        CutDirection = (int)NoteCutDirection.Left,
                        AngleOffset = 0,
                        TailJsonTime = 3f,
                        TailPosX = (int)GridX.Left,
                        TailPosY = (int)GridY.Base,
                        SliceCount = 5,
                        Squish = 1f
                    },
                    chainsContainer.MapObjects[0],
                    "Undo update chain multiplier");
            }
        }
    }
}