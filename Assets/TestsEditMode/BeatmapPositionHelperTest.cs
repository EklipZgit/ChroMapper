using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TestsEditMode
{
    public class BeatmapPositionHelperTest
    {
        private const float AssertionDelta = 1e-3f;

        private static IEnumerable<TestCaseData> AdjacentThresholdCases
        {
            get
            {
                const float t = BeatmapPositionHelper.HysteresisThreshold;
                const float d = 0.01f;

                yield return new TestCaseData(
                    new Vector2(3f + t - d, 0f),
                    new Vector2(2f, 0f),
                    new Vector2(2f, 0f)) { TestName = "BelowThresholdForward" };

                yield return new TestCaseData(
                    new Vector2(3f + t + d, 0f),
                    new Vector2(2f, 0f),
                    new Vector2(3f, 0f)) { TestName = "PastThresholdForward" };

                yield return new TestCaseData(
                    new Vector2(3f - t + d, 0f),
                    new Vector2(3f, 0f),
                    new Vector2(3f, 0f)) { TestName = "AboveThresholdBackward" };

                yield return new TestCaseData(
                    new Vector2(3f - t - d, 0f),
                    new Vector2(3f, 0f),
                    new Vector2(2f, 0f)) { TestName = "BelowThresholdBackward" };
            }
        }

        private static IEnumerable<TestCaseData> NegativeCoordinateCases
        {
            get
            {
                const float t = BeatmapPositionHelper.HysteresisThreshold;
                const float d = 0.01f;

                yield return new TestCaseData(
                    new Vector2(-1f + t, 0f),
                    new Vector2(-2f, 0f),
                    new Vector2(-2f, 0f)) { TestName = "NegativeStayAtPreviousForward" };

                yield return new TestCaseData(
                    new Vector2(-1f + t + d, 0f),
                    new Vector2(-2f, 0f),
                    new Vector2(-1f, 0f)) { TestName = "NegativeCrossForward" };

                yield return new TestCaseData(
                    new Vector2(-2f - t, 0f),
                    new Vector2(-2f, 0f),
                    new Vector2(-2f, 0f)) { TestName = "NegativeStayAtPreviousBackward" };

                yield return new TestCaseData(
                    new Vector2(-2f - t - d, 0f),
                    new Vector2(-2f, 0f),
                    new Vector2(-3f, 0f)) { TestName = "NegativeCrossBackward" };
            }
        }

        private static IEnumerable<TestCaseData> FarJumpCases
        {
            get
            {
                yield return new TestCaseData(
                    new Vector2(2.01f, 0f),
                    Vector2.zero,
                    new Vector2(2f, 0f)) { TestName = "LargePositiveJump" };

                yield return new TestCaseData(
                    new Vector2(-2.01f, 0f),
                    Vector2.zero,
                    new Vector2(-3f, 0f)) { TestName = "LargeNegativeJump" };
            }
        }

        private static IEnumerable<TestCaseData> IndependentAxesCases
        {
            get
            {
                yield return new TestCaseData(
                    new Vector2(3f + BeatmapPositionHelper.HysteresisThreshold - 0.01f, 5.9f),
                    new Vector2(2f, 4f),
                    new Vector2(2f, 5f)) { TestName = "XStaysYJumps" };

                yield return new TestCaseData(
                    new Vector2(-1f + BeatmapPositionHelper.HysteresisThreshold + 0.01f, -3f),
                    new Vector2(-2f, -4f),
                    new Vector2(-1f, -4f)) { TestName = "XCrossesYStays" };
            }
        }

        [TestCaseSource(nameof(AdjacentThresholdCases))]
        public void SnapWithHysteresis_ChangesOnlyPastAdjacentBoundaryThreshold(
            Vector2 raw,
            Vector2 previous,
            Vector2 expected)
        {
            var actual = BeatmapPositionHelper.SnapWithHysteresis(raw, previous);

            Assert.AreEqual(expected.x, actual.x, AssertionDelta);
            Assert.AreEqual(expected.y, actual.y, AssertionDelta);
        }

        [TestCaseSource(nameof(NegativeCoordinateCases))]
        public void SnapWithHysteresis_AppliesTheSameThresholdForNegativeCoordinates(
            Vector2 raw,
            Vector2 previous,
            Vector2 expected)
        {
            var actual = BeatmapPositionHelper.SnapWithHysteresis(raw, previous);

            Assert.AreEqual(expected.x, actual.x, AssertionDelta);
            Assert.AreEqual(expected.y, actual.y, AssertionDelta);
        }

        [TestCaseSource(nameof(FarJumpCases))]
        public void SnapWithHysteresis_SnapsFarJumpsImmediately(Vector2 raw, Vector2 previous, Vector2 expected)
        {
            var actual = BeatmapPositionHelper.SnapWithHysteresis(raw, previous);

            Assert.AreEqual(expected.x, actual.x, AssertionDelta);
            Assert.AreEqual(expected.y, actual.y, AssertionDelta);
        }

        [TestCaseSource(nameof(IndependentAxesCases))]
        public void SnapWithHysteresis_CoversBothAxesIndependently(Vector2 raw, Vector2 previous, Vector2 expected)
        {
            var actual = BeatmapPositionHelper.SnapWithHysteresis(raw, previous);

            Assert.AreEqual(expected.x, actual.x, AssertionDelta);
            Assert.AreEqual(expected.y, actual.y, AssertionDelta);
        }
    }
}