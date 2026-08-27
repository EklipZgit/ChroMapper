using System;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using NUnit.Framework;

namespace Tests.Editor
{
    public class BasicEventAppearanceTest
    {
        // Low propagation must not increase an unrelated speed above-one value from
        // hundredths to thousandths in the Basic Event ring-rotation label.
        [Test]
        public void RingRotationBelowTwoPropagationDisplaysThreePropagationAndTwoSpeedDecimals()
        {
            var evt = new BaseEvent
            {
                CustomProp = 1.3456f,
                CustomSpeed = 12.3456f
            };

            Assert.AreEqual($"P1.346{Environment.NewLine}S12.35", GetRingRotationText(evt));
        }

        // Speed below one needs thousandths regardless of the propagation value displayed
        // beside it, while propagation independently retains its own thousandths.
        [Test]
        public void RingRotationSpeedBelowOneDisplaysThreeDecimalsWithLowPropagation()
        {
            var evt = new BaseEvent
            {
                CustomProp = 2.3456f,
                CustomSpeed = 0.3456f
            };

            Assert.AreEqual($"P2.346{Environment.NewLine}S0.346", GetRingRotationText(evt));
        }

        // High propagation must not alter the normal hundredths used by speed values at
        // or above one, proving speed precision is not selected from propagation.
        [Test]
        public void RingRotationAboveThreePropagationDisplaysThreePropagationAndTwoSpeedDecimals()
        {
            var evt = new BaseEvent
            {
                CustomProp = 123.4564f,
                CustomSpeed = 12.3456f
            };

            Assert.AreEqual($"P123.456{Environment.NewLine}S12.35", GetRingRotationText(evt));
        }

        // A sub-one speed must keep thousandths even with high propagation, guarding the
        // opposite independence direction from the low-propagation regression.
        [Test]
        public void RingRotationSpeedBelowOneDisplaysThreeDecimalsWithHighPropagation()
        {
            var evt = new BaseEvent
            {
                CustomProp = 123.4564f,
                CustomSpeed = 0.3456f
            };

            Assert.AreEqual($"P123.456{Environment.NewLine}S0.346", GetRingRotationText(evt));
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

        // Basic Event ring zoom needs thousandths below speed one so its fine speed
        // precision is visible instead of being rounded to the normal hundredths.
        [Test]
        public void RingZoomSpeedBelowOneDisplaysThreeDecimals()
        {
            var evt = new BaseEvent
            {
                CustomSpeed = 0.3456f
            };

            Assert.AreEqual("S0.346", GetRingZoomText(evt, false));
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
