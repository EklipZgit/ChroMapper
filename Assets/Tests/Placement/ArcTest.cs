using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    public class ArcTest : TestBase
    {
        [Test]
        public void CreateArc()
        {
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

            var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();

            var objects = SelectionController.SelectedObjects.ToList();

            Assert.AreEqual(2, objects.Count);

            if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                Assert.Fail("Both selected objects is not color note");
            var n1 = objects[0] as BaseNote;
            var n2 = objects[1] as BaseNote;

            var arc = arcPlacement.CreateArcData(n1, n2);
            var baselineArc = BeatmapFactory.Clone(arc);
            arc = PlaceUtils.Place(arc);

            BeatmapAssertion.IsUnchanged(baselineArc, arc, "Check generated arc");
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
                ["coordinates"] = headCoordinates, ["tailCoordinates"] = tailCoordinates
            };

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

            var arcPlacement = Object.FindAnyObjectByType<ArcPlacement>();

            var objects = SelectionController.SelectedObjects.ToList();

            Assert.AreEqual(2, objects.Count);

            if (!ArcPlacement.IsColorNote(objects[0]) || !ArcPlacement.IsColorNote(objects[1]))
                Assert.Fail("Both selected objects is not color note");
            var n1 = objects[0] as BaseNote;
            var n2 = objects[1] as BaseNote;

            var arc = arcPlacement.CreateArcData(n1, n2);
            var originalArc = BeatmapFactory.Clone(arc);
            arc = PlaceUtils.Place(arc);

            var expectedArc = BeatmapFactory.Clone(originalArc);
            expectedArc.CustomData = expectedArcCustomData;

            BeatmapAssertion.IsEqual(
                expectedArc,
                arc,
                "Check generated arc");
        }

        [Test]
        public void InvertArc()
        {
            var arc = new BaseArc
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
            var baselineArc = BeatmapFactory.Clone(arc);
            arc = PlaceUtils.Place(arc);

            arc = SliderCommand.InvertColor(arc) as BaseArc;

            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc,
                a => { a.Color = (int)NoteColor.Blue; },
                "Perform arc inversion");

            // Undo invert
            var undoObjects = PlaceUtils.Undo<BaseArc>().ToList();

            BeatmapAssertion.IsUnchanged(baselineArc, undoObjects[0], "Undo arc inversion");
        }

        [Test]
        public void UpdateArcMultiplier()
        {
            var containerCollection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (containerCollection is not ArcGridContainer arcsContainer) return;
            var inputController = Object.FindAnyObjectByType<BeatmapArcInputController>();

            var arc = new BaseArc
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
            var baselineArc = BeatmapFactory.Clone(arc);
            arc = PlaceUtils.Place(arc);

            if (arcsContainer.LoadedContainers[arc] is ArcContainer containerA)
                inputController.ChangeMu(containerA, 0.5f);

            // Hover tweaks are selection-neutral now, so resolve the live replacement from the collection.
            arc = arcsContainer.LoadedObjects.OfType<BaseArc>().Single();

            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc,
                a => { a.HeadControlPointLengthMultiplier += 0.5f; },
                "Update arc multiplier");

            if (arcsContainer.LoadedContainers[arc] is ArcContainer containerA2)
                inputController.ChangeTmu(containerA2, 0.5f);

            arc = arcsContainer.LoadedObjects.OfType<BaseArc>().Single();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                arc,
                a => { a.HeadControlPointLengthMultiplier += 0.5f; a.TailControlPointLengthMultiplier += 0.5f; },
                "Update arc tail multiplier");

            // Undo invert
            var undoTailObjects = PlaceUtils.Undo<BaseArc>().ToList();
            BeatmapAssertion.IsEqualWithChanges(
                baselineArc,
                undoTailObjects[0],
                a => { a.HeadControlPointLengthMultiplier += 0.5f; },
                "Undo update arc tail multiplier");

            var undoHeadObjects = PlaceUtils.Undo<BaseArc>().ToList();
            BeatmapAssertion.IsUnchanged(baselineArc, undoHeadObjects[0], "Undo update arc multiplier");
        }

        // SongBoundaryTestBase.DragToBeat spawns a real container, queues a spline recompute, then destroys the
        // container in teardown; the deferred drain must skip that destroyed entry instead of throwing in LateUpdate.
        [UnityTest]
        public IEnumerator DestroyedQueuedArcDoesNotThrowOnDrain()
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc) as ArcGridContainer;
            Assert.That(collection, Is.Not.Null);

            var queueField = typeof(ArcGridContainer).GetField("queuedUpdatingArcs",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(queueField, Is.Not.Null);
            var queue = (Queue<ArcContainer>)queueField.GetValue(collection);

            var container = (ArcContainer)collection.CreateContainer();
            container.ObjectData = new BaseArc { JsonTime = 1f, TailJsonTime = 2f };
            collection.RequestForSplineRecompute(container);
            Object.DestroyImmediate(container.gameObject);

            // Synchronous tests enqueue arcs without pumping frames, so the deferred drain (2 per LateUpdate)
            // can hold a large legitimate backlog; yield until it drains rather than assuming two frames suffice.
            var frames = 0;
            while (queue.Count > 0 && frames++ < 1000)
                yield return null;
            Assert.That(queue.Count, Is.EqualTo(0), "The destroyed arc entry must drain without stalling the queue.");
        }
    }
}