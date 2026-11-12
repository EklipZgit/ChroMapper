using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    public class ArcGeometryParityTest : TestBase
    {
        [TestCase(false, 0.0225f)]
        [TestCase(true, 0.225f)]
        public void EndpointsUseNativeNoteEdgeOffsets(bool attached, float offset)
        {
            var arc = CreateArc(NoteCutDirection.Down, SliderMidAnchorMode.Straight);
            if (attached)
            {
                PlaceEndpointNote(arc, false, 0);
                PlaceEndpointNote(arc, true, 0);
            }

            var container = PlaceArc(arc);
            AssertVector(container.BaseSplinePoints[0], (Vector3)arc.GetPosition() + Vector3.down * offset);
            AssertVector(
                container.BaseSplinePoints[ArcContainer.NumSamples],
                (Vector3)arc.GetTailPosition() + Vector3.up * offset + Vector3.forward);
        }

        [TestCase(SliderMidAnchorMode.Clockwise, NoteCutDirection.Down, -2.5f)]
        [TestCase(SliderMidAnchorMode.CounterClockwise, NoteCutDirection.Down, 2.5f)]
        [TestCase(SliderMidAnchorMode.Clockwise, NoteCutDirection.Any, -2.5f)]
        [TestCase(SliderMidAnchorMode.CounterClockwise, NoteCutDirection.Any, 2.5f)]
        public void MiddleAnchorUsesNativeMetreOffset(
            SliderMidAnchorMode mode,
            NoteCutDirection direction,
            float offsetX)
        {
            var arc = CreateArc(direction, mode);
            var container = PlaceArc(arc);
            var expected = (Vector3)((arc.GetPosition() + arc.GetTailPosition()) * 0.5f);
            expected.x += offsetX;
            expected.z = 0.5f;
            AssertVector(container.BaseSplinePoints[ArcContainer.NumSamples / 2], expected);
        }

        [Test]
        public void CurveInheritsAttachedNoteAngleOffsets()
        {
            var arc = CreateArc(NoteCutDirection.Down, SliderMidAnchorMode.Straight);
            PlaceEndpointNote(arc, false, 30);
            PlaceEndpointNote(arc, true, -30);
            var container = PlaceArc(arc);
            Assert.That(container.ArcData.HeadNotes.Count, Is.EqualTo(1));
            Assert.That(container.ArcData.TailNotes.Count, Is.EqualTo(1));

            var headDirection = new Vector3(0.5f, -Mathf.Sqrt(0.75f), 0f);
            var tailDirection = new Vector3(-0.5f, -Mathf.Sqrt(0.75f), 0f);
            var head = (Vector3)arc.GetPosition() + headDirection * 0.225f;
            var tail = (Vector3)arc.GetTailPosition() - tailDirection * 0.225f + Vector3.forward;
            // SliderMeshController uses 2.5-metre tangents from the offset anchors.
            var expected = (head + 3f * (head + headDirection * 2.5f)
                + 3f * (tail - tailDirection * 2.5f) + tail) / 8f;
            AssertVector(container.BaseSplinePoints[ArcContainer.NumSamples / 2], expected);
        }

        [UnityTest]
        public IEnumerator AddingAndRemovingAnEndpointNoteRefreshesTheCurve()
        {
            var arc = CreateArc(NoteCutDirection.Down, SliderMidAnchorMode.Straight);
            var container = PlaceArc(arc);
            var note = PlaceUtils.Place(
                new BaseNote
                {
                    JsonTime = arc.JsonTime,
                    PosX = arc.PosX,
                    PosY = arc.PosY,
                    Color = arc.Color,
                    CutDirection = arc.CutDirection
                });
            yield return null;
            yield return null;
            AssertVector(container.BaseSplinePoints[0], (Vector3)arc.GetPosition() + Vector3.down * 0.225f);

            PlaceUtils.Delete(note);
            yield return null;
            yield return null;
            AssertVector(container.BaseSplinePoints[0], (Vector3)arc.GetPosition() + Vector3.down * 0.0225f);
        }

        private static BaseArc CreateArc(NoteCutDirection direction, SliderMidAnchorMode mode) => new()
        {
            JsonTime = 2f,
            TailJsonTime = 3f,
            PosX = 1,
            TailPosX = 1,
            PosY = 1,
            TailPosY = 1,
            Color = 0,
            CutDirection = (int)direction,
            TailCutDirection = (int)direction,
            HeadControlPointLengthMultiplier = 1f,
            TailControlPointLengthMultiplier = 1f,
            MidAnchorMode = (int)mode
        };

        private static void PlaceEndpointNote(BaseArc arc, bool tail, int angleOffset)
        {
            PlaceUtils.Place(
                new BaseNote
                {
                    JsonTime = tail ? arc.TailJsonTime : arc.JsonTime,
                    PosX = tail ? arc.TailPosX : arc.PosX,
                    PosY = tail ? arc.TailPosY : arc.PosY,
                    Color = arc.Color,
                    CutDirection = tail ? arc.TailCutDirection : arc.CutDirection,
                    AngleOffset = angleOffset
                });
        }

        private static ArcContainer PlaceArc(BaseArc arc)
        {
            var placed = PlaceUtils.Place(arc);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
            var container = (ArcContainer)collection.LoadedContainers[placed];
            container.RecomputePosition();
            return container;
        }

        private static void AssertVector(Vector3 actual, Vector3 expected) =>
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0001f), $"Expected {expected}, got {actual}");
    }
}
