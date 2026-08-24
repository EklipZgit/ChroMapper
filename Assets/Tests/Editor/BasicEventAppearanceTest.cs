using System;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using NUnit.Framework;

namespace Tests.Editor
{
    public class BasicEventAppearanceTest
    {
        // Very slow Basic Event propagation needs thousandths for both its propagation and speed labels.
        [Test]
        public void RingRotationBelowTwoPropagationDisplaysThreeDecimalPlacesForPropagationAndSpeed()
        {
            var evt = new BaseEvent
            {
                CustomProp = 1.3456f,
                CustomSpeed = 12.3456f
            };

            Assert.AreEqual($"P1.346{Environment.NewLine}S12.346", GetRingRotationText(evt));
        }

        // Propagation always retains thousandths, and its under-three speed uses the same precision.
        [Test]
        public void RingRotationBetweenTwoAndThreeDisplaysThreePropagationAndSpeedDecimals()
        {
            var evt = new BaseEvent
            {
                CustomProp = 2.3456f,
                CustomSpeed = 12.3456f
            };

            Assert.AreEqual($"P2.346{Environment.NewLine}S12.346", GetRingRotationText(evt));
        }

        // Propagation retains thousandths above three, while speed returns to compact formatting at that boundary.
        [Test]
        public void RingRotationAboveThreeDisplaysThreePropagationDecimalsAndCompactSpeed()
        {
            var evt = new BaseEvent
            {
                CustomProp = 123.4564f,
                CustomSpeed = 12.3456f
            };

            Assert.AreEqual($"P123.456{Environment.NewLine}S12.3", GetRingRotationText(evt));
        }

        // Basic Event ring zoom retains thousandths for both Chroma step data and SmoothStep's integer fallback path.
        [TestCase(false)]
        [TestCase(true)]
        public void RingZoomDisplaysThreeStepDecimals(bool isSmoothStep)
        {
            var evt = new BaseEvent
            {
                CustomStep = 2.3456f
            };

            Assert.AreEqual("Z2.346", GetRingZoomText(evt, isSmoothStep));
        }

        private static string GetRingRotationText(BaseEvent evt)
        {
            // Invoke the production Basic Event formatter so no GLS appearance path participates in this regression.
            var method = typeof(EventAppearanceSO).GetMethod(
                "GetRingRotationText",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Basic Event ring rotation appearance formatter");
            return (string)method.Invoke(null, new object[] { evt });
        }

        private static string GetRingZoomText(BaseEvent evt, bool isSmoothStep)
        {
            // Invoke the production Basic Event zoom formatter so its two supported paths share this regression.
            var method = typeof(EventAppearanceSO).GetMethod(
                "GetRingZoomText",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Basic Event ring zoom appearance formatter");
            return (string)method.Invoke(null, new object[] { evt, isSmoothStep });
        }
    }
}
