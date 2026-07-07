using System.Collections;
using System.Linq;
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
    public class ArcTest : TestBase
    {
        [Test]
        public void CreateArc()
        {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var baseNoteA = new BaseNote
                {
                    JsonTime = 2f, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down
                };
                var baseNoteB = new BaseNote
                {
                    JsonTime = 3f, PosX = (int)GridX.Left, PosY = (int)GridY.Upper, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up
                };
                baseNoteA = PlaceUtils.Place(baseNoteA);
                baseNoteB = PlaceUtils.Place(baseNoteB);

                SelectionController.Select(baseNoteA);
                SelectionController.Select(baseNoteB, true);

            var arcContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (arcContainerCollection is ArcGridContainer arcsContainer)
            {
                var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();

                var objects = SelectionController.SelectedObjects.ToList();

                Assert.AreEqual(2, objects.Count);

                if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                    Assert.Fail("Both selected objects is not color note");
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                var arc = arcPlacement.CreateArcData(n1, n2);
                arc = PlaceUtils.Place(arc);

                CheckUtils.CheckArc("Check generated arc", arcsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Red, (int)NoteCutDirection.Down, 0, 1, 3f, (int)GridX.Left, (int)GridY.Upper,
                    (int)NoteCutDirection.Up, 1, 0);
            }
        }

        [Test]
        public void CreateArcWithCoordinates()
        {
            var headCoordinates = new JSONArray { [0] = 69, [1] = 69 };
            var tailCoordinates = new JSONArray { [0] = 420, [1] = 420 };

            var headCustomData = new JSONObject { ["coordinates"] = headCoordinates };
            var tailCustomData = new JSONObject { ["coordinates"] = tailCoordinates };

            var expectedArcCustomData = new JSONObject
            {
                ["coordinates"] = headCoordinates,
                ["tailCoordinates"] = tailCoordinates
            };

            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (containerCollection is NoteGridContainer notesContainer)
            {
                var notePlacement = Object.FindAnyObjectByType<NotePlacement>();

                var baseNoteA = new BaseNote
                {
                    JsonTime = 2f, PosX = (int)GridX.Left, PosY = (int)GridY.Base, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Down, CustomData = headCustomData
                };

                var baseNoteB = new BaseNote
                {
                    JsonTime = 3f, PosX = (int)GridX.Left, PosY = (int)GridY.Upper, Type = (int)NoteType.Red,
                    CutDirection = (int)NoteCutDirection.Up, CustomData = tailCustomData
                };

                baseNoteA = PlaceUtils.Place(baseNoteA);
                baseNoteB = PlaceUtils.Place(baseNoteB);

                SelectionController.Select(baseNoteA);
                SelectionController.Select(baseNoteB, true);
            }

            var arcContainerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (arcContainerCollection is ArcGridContainer arcsContainer)
            {
                var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();

                var objects = SelectionController.SelectedObjects.ToList();

                Assert.AreEqual(2, objects.Count);

                if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                    Assert.Fail("Both selected objects is not color note");
                var n1 = objects[0] as BaseNote;
                var n2 = objects[1] as BaseNote;

                var arc = arcPlacement.CreateArcData(n1, n2);
                arc = PlaceUtils.Place(arc);

                CheckUtils.CheckArc("Check generated arc", arcsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Red, (int)NoteCutDirection.Down, 0, 1, 3f, (int)GridX.Left, (int)GridY.Upper,
                    (int)NoteCutDirection.Up, 1, 0, expectedArcCustomData);
            }
        }

        [Test]
        public void InvertArc()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (containerCollection is ArcGridContainer arcsContainer)
            {
                var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();
                var inputController = Object.FindAnyObjectByType<BeatmapSharedNoteInputController>();

                var baseArc = new BaseArc
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteColor.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    HeadControlPointLengthMultiplier = 1f,
                    TailJsonTime = 3f,
                    TailPosX = (int)GridX.Left,
                    TailPosY = (int)GridY.Base,
                    TailCutDirection = (int)NoteCutDirection.Left,
                    TailControlPointLengthMultiplier = 1f,
                    MidAnchorMode = 0
                };
                baseArc = PlaceUtils.Place(baseArc);

                if (arcsContainer.LoadedContainers[baseArc] is ArcContainer containerA)
                    SliderCommand.InvertColor(containerA.ArcData);

                CheckUtils.CheckArc("Perform arc inversion", arcsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Blue, (int)NoteCutDirection.Left, 0, 1f, 3f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteCutDirection.Left, 1f, 0);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckArc("Undo arc inversion", arcsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 1f, 3f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteCutDirection.Left, 1f, 0);
            }
        }

        [Test]
        public void UpdateArcMultiplier()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (containerCollection is ArcGridContainer arcsContainer)
            {
                var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();
                var inputController = Object.FindAnyObjectByType<BeatmapArcInputController>();

                var baseArc = new BaseArc
                {
                    JsonTime = 2f,
                    PosX = (int)GridX.Left,
                    PosY = (int)GridY.Base,
                    Color = (int)NoteColor.Red,
                    CutDirection = (int)NoteCutDirection.Left,
                    HeadControlPointLengthMultiplier = 1f,
                    TailJsonTime = 3f,
                    TailPosX = (int)GridX.Left,
                    TailPosY = (int)GridY.Base,
                    TailCutDirection = (int)NoteCutDirection.Left,
                    TailControlPointLengthMultiplier = 1f,
                    MidAnchorMode = 0
                };
                baseArc = PlaceUtils.Place(baseArc);

                if (arcsContainer.LoadedContainers[baseArc] is ArcContainer containerA)
                    inputController.ChangeMu(containerA, 0.5f);

                CheckUtils.CheckArc("Update arc multiplier", arcsContainer, 0, 2f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 1.5f, 3f, (int)GridX.Left, (int)GridY.Base,
                    (int)NoteCutDirection.Left, 1f, 0);

                if (arcsContainer.LoadedContainers[arcsContainer.MapObjects[0]] is ArcContainer containerA2)
                    inputController.ChangeTmu(containerA2, 0.5f);

                CheckUtils.CheckArc("Update arc tail multiplier", arcsContainer, 0, 2f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 1.5f, 3f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteCutDirection.Left, 1.5f, 0);

                // Undo invert
                actionContainer.Undo();

                CheckUtils.CheckArc("Undo update arc tail multiplier", arcsContainer, 0, 2f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 1.5f, 3f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);

                actionContainer.Undo();

                CheckUtils.CheckArc("Undo update arc multiplier", arcsContainer, 0, 2f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteColor.Red, (int)NoteCutDirection.Left, 0, 1f, 3f, (int)GridX.Left,
                    (int)GridY.Base, (int)NoteCutDirection.Left, 1f, 0);
            }
        }
    }
}
