using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Dormant BTS pillar lasers revived by Chroma _active:true must register their
    // light controller so map events reach them; unactivated branches stay unregistered.
    public class BtsPillarActivationTest : TestBase
    {
        private const string LaserRId =
            "BTSEnvironment.[0]Environment.[18]PillarPair (1).[1]PillarR.[2]LaserR";
        private const string OtherLaserRId =
            "BTSEnvironment.[0]Environment.[20]PillarPair (2).[1]PillarR.[2]LaserR";

        private bool previousAnimations;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator ActiveEnhancementRegistersDormantLaser()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2, Difficulty(), environmentName: "BTSEnvironment");

            var laser = MarkerTransform(LaserRId).GetComponent<ParametricBloomFogLightController>();
            Assert.That(laser.gameObject.activeInHierarchy, Is.True,
                "The _active enhancement must activate the dormant laser.");
            Assert.That(Descriptor().IsRegistered(laser), Is.True,
                "The revived branch's light must register so the event insert covers it.");

            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(4.5f);
            yield return null;
            yield return null;

            Assert.That(laser.Color.maxColorComponent, Is.GreaterThan(0.01f),
                "The type-1 event must deliver color to the revived laser controller.");

            var otherLaser = MarkerTransform(OtherLaserRId)
                .GetComponent<ParametricBloomFogLightController>();
            Assert.That(Descriptor().IsRegistered(otherLaser), Is.False,
                "Branches never activated by an enhancement must stay unregistered.");
        }

        private static Transform MarkerTransform(string chromaId) => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == chromaId).transform;

        private static EnvironmentDescriptor Descriptor() => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor;

        private static JSONNode Difficulty() => JSON.Parse(@"{
            ""_version"": ""2.2.0"",
            ""_events"": [
                { ""_time"": 4, ""_type"": 1, ""_value"": 5 }
            ],
            ""_notes"": [],
            ""_obstacles"": [],
            ""_waypoints"": [],
            ""_customData"": {
                ""_environment"": [
                    {
                        ""_id"": ""BTSEnvironment.[0]Environment.[18]PillarPair (1).[1]PillarR.[2]LaserR"",
                        ""_lookupMethod"": ""Exact"",
                        ""_active"": 1
                    }
                ],
                ""_customEvents"": []
            }
        }");

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            Settings.Instance.Animations = previousAnimations;
            yield break;
        }
    }
}
