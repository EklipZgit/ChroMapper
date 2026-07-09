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
    public class ChainTest : TestBase
    {
        [Test]
        public void CreateChain()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var noteA = new BaseNote
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down
                };
                var noteB = new BaseNote
                {
                    JsonTime = 3f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Upper,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up
                };
                noteA = PlaceUtils.Place(noteA);
                noteB = PlaceUtils.Place(noteB);

                SelectionController.Select(noteA);
                SelectionController.Select(noteB, true);
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
                var originalChain = BeatmapFactory.Clone(chain);
                chain = PlaceUtils.Place(chain);

                var expected = BeatmapFactory.Clone(originalChain);
                BeatmapAssertion.IsEqual(expected, chain, "Check generated chain");
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

                var noteA = new BaseNote
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down,
                    CustomData = headCustomData
                };

                var noteB = new BaseNote
                {
                    JsonTime = 3f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Upper,
                    Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up,
                    CustomData = tailCustomData
                };

                noteA = PlaceUtils.Place(noteA);
                noteB = PlaceUtils.Place(noteB);

                SelectionController.Select(noteA);
                SelectionController.Select(noteB, true);
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
                var originalChain = BeatmapFactory.Clone(chain);
                chain = PlaceUtils.Place(chain);

                var expected = BeatmapFactory.Clone(originalChain);
                BeatmapAssertion.IsEqual(expected, chain, "Check generated chain");
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

                var chain = new BaseChain
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
                var originalChain = BeatmapFactory.Clone(chain);
                chain = PlaceUtils.Place(chain);

                var expectedOriginal = BeatmapFactory.Clone(originalChain);
                var expectedBlue = BeatmapFactory.Clone(originalChain);
                expectedBlue.Color = (int)NoteColor.Blue;

                if (chainsContainer.LoadedContainers[chain] is ChainContainer containerA)
                    chain = SliderCommand.InvertColor(containerA.ChainData) as BaseChain;

                BeatmapAssertion.IsEqual(expectedBlue, chain, "Perform chain inversion");

                // Undo invert
                var undoObjects = PlaceUtils.Undo<BaseChain>(actionContainer).ToList();

                BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo chain inversion");
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

                var chain = new BaseChain
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
                var originalChain = BeatmapFactory.Clone(chain);
                chain = PlaceUtils.Place(chain);

                var expectedOriginal = BeatmapFactory.Clone(originalChain);
                var expectedSquish1_5 = BeatmapFactory.Clone(originalChain);
                expectedSquish1_5.Squish = 1.5f;

                if (chainsContainer.LoadedContainers[chain] is ChainContainer containerA)
                    inputController.TweakChainSquish(containerA, 0.5f);

                chain = SelectionController.SelectedObjects.OfType<BaseChain>().Single();

                BeatmapAssertion.IsEqual(expectedSquish1_5, chain, "Update chain multiplier");

                // Undo invert
                var undoObjects = PlaceUtils.Undo<BaseChain>(actionContainer).ToList();

                BeatmapAssertion.IsEqual(expectedOriginal, undoObjects[0], "Undo update chain multiplier");
            }
        }
    }
}