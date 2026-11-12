using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class NoteJumpRotationTest
    {
        [TestCase(0f, 0f)]
        [TestCase(0.05f, 64.65638f)]
        [TestCase(0.1f, 104.61622f)]
        [TestCase(0.125f, 110f)]
        [TestCase(0.25f, 95.857864f)]
        [TestCase(0.375f, 90f)]
        [TestCase(0.5f, 90f)]
        [TestCase(1f, 90f)]
        public void SineStagesReachNativeIntermediateAngles(float progress, float angle)
        {
            var actual = NoteJumpRotation.Evaluate(Quaternion.Euler(0f, 0f, 90f), new Vector3(0f, 0f, 20f), progress);
            Assert.That(Quaternion.Angle(actual, Quaternion.Euler(0f, 0f, angle)), Is.LessThan(0.05f));
        }

        [Test]
        public void ChainHeadUsesTheSameSineStageWithoutTheRandomTilt()
        {
            var actual = NoteJumpRotation.Evaluate(Quaternion.Euler(0f, 0f, 90f), Vector3.zero, 0.05f);
            Assert.That(Quaternion.Angle(actual, Quaternion.Euler(0f, 0f, 52.900673f)), Is.LessThan(0.05f));
        }
    }
}
