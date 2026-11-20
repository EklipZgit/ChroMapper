using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Vanishing Point (BillieEnvironment): Moon.NeonTube.BakedBloom is a baked-glow
    // ParametricSpriteLight that native TubeBloomPrePassLight references explicitly via
    // dynamic3SliceSprite, restored in the environment data after the export dropped it.
    // The reported red bands and haze come from the Night sky gradient, covered by
    // BillieNightSkyGradientTest, not from this sprite.
    public class VanishingPointMoonBakedBloomTest : TestBase
    {
        private const string NeonTubeId =
            "BillieEnvironment.[0]Environment.[27]DayAndNight.[1]Night.[0]Moon.[0]NeonTube";

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
        public IEnumerator MoonNeonTubeDrivesExplicitBakedGlowSprite()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2, Difficulty(), environmentName: "BillieEnvironment");
            var neon = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == NeonTubeId).transform;
            var baked = neon.GetComponentInChildren<ParametricSpriteLight>(true);
            Assert.That(baked, Is.Not.Null, "The moon's BakedBloom sprite must exist.");
            Assert.That(baked.Renderer, Is.Not.Null);
            var tube = neon.GetComponent<ParametricBloomFogLightController>();
            Assert.That(tube, Is.Not.Null);
            Assert.That(tube.SpriteLight, Is.SameAs(baked),
                "The exported dynamic3SliceSprite ref must wire the BakedBloom sprite explicitly.");

            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(6.5f);
            yield return null;
            yield return null;

            var mpb = new MaterialPropertyBlock();
            baked.Renderer.GetPropertyBlock(mpb);
            var color = mpb.GetColor("_Color");
            Assert.That(color.r * color.a, Is.GreaterThan(0.01f),
                "The lit strobe must color the moon's baked-glow sprite.");
        }

        private static JSONNode Difficulty() => JSON.Parse(@"{
            ""_version"": ""2.2.0"",
            ""_events"": [
                { ""_time"": 4, ""_type"": 5, ""_value"": 1 },
                { ""_time"": 6, ""_type"": 4, ""_value"": 5 }
            ],
            ""_notes"": [],
            ""_obstacles"": [],
            ""_waypoints"": [],
            ""_customData"": {
                ""_environment"": [
                    {
                        ""_id"": ""Moon"",
                        ""_lookupMethod"": ""Contains"",
                        ""_localPosition"": [0,1,325],
                        ""_scale"": [12.5,12.5,1]
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
