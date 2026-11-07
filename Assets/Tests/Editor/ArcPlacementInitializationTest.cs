using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class ArcPlacementInitializationTest : TestBase
    {
        [Test]
        public void InactivePlacementReadsCurrentDefaultsWhenActivated()
        {
            var settings = Settings.Instance;
            var previousHead = settings.DefaultArcHeadMultiplier;
            var previousTail = settings.DefaultArcTailMultiplier;
            var root = new GameObject("Inactive arc placement");
            root.SetActive(false);
            try
            {
                settings.DefaultArcHeadMultiplier = 1f;
                settings.DefaultArcTailMultiplier = 1f;
                var placement = root.AddComponent<ArcPlacement>();
                placement.LaneRotationProvider = Object.FindAnyObjectByType<LaneRotationProvider>();
                settings.DefaultArcHeadMultiplier = 2.5f;
                settings.DefaultArcTailMultiplier = 3.5f;
                root.SetActive(true);

                Assert.That(placement.HeadMultiplier, Is.EqualTo(2.5f));
                Assert.That(placement.TailMultiplier, Is.EqualTo(3.5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                settings.DefaultArcHeadMultiplier = previousHead;
                settings.DefaultArcTailMultiplier = previousTail;
            }
        }
    }
}
