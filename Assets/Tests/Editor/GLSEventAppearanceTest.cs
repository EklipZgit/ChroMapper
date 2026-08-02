using Beatmap.Base;
using NUnit.Framework;

namespace Tests.Editor
{
    public class GLSEventAppearanceTest
    {
        // Keep normal bright GLS nodes from rendering a dark strobe band merely because strobe brightness defaults to zero.
        [Test]
        public void BrightNonStrobingColorNodeDoesNotEnableStrobeBand()
        {
            var evt = new BaseLightColorBase
            {
                Brightness = 3.7f,
                Frequency = 0,
                StrobeBrightness = 0f,
                StrobeFade = 0
            };

            Assert.False(GLSEventCommon.IsStrobing(evt));
        }

        // Retain both OEM and Chroma timing forms as valid strobe-band triggers.
        [TestCase(1, null)]
        [TestCase(0, 1f)]
        public void TimedColorNodeEnablesStrobeBand(int frequency, float? chromaInterval)
        {
            var evt = new BaseLightColorBase
            {
                Frequency = frequency,
                ChromaStrobeInterval = chromaInterval
            };

            Assert.True(GLSEventCommon.IsStrobing(evt));
        }
    }
}
