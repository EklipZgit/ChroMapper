using System.Collections;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // TubeBloomAnimationTests covers the docs' AnimateComponent example event (additional-events page): a
    // TubeBloomPrePassLight component animated on track-bound geometry. Heck's AnimateComponent animates
    // colorAlphaMultiplier and bloomFogIntensityMultiplier on every TubeBloomPrePassLight under the track's
    // objects; the CM preview must drive the same multipliers on the track's ParametricBloomFogLightController,
    // hold finished values, and restore the authored component values when scrubbed back before the first event.
    public class TubeBloomAnimationTests : TestBase
    {
        private const string TrackName = "tubeLights";
        private const float AuthoredAlpha = 3f;
        private const float AuthoredBloomFog = 10f;

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return TestUtils.ReloadMap(3, CreateDifficulty());
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // AnimateComponentTubeBloomEventsDriveLightMultipliers walks the event sequence in order and requires
        // every seek to land synchronously on the as-if-played multipliers, covering the instant event, the
        // eased interpolation window, the post-finish hold, the final override, and the scrub-back restore.
        [UnityTest]
        public IEnumerator AnimateComponentTubeBloomEventsDriveLightMultipliers()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var controller = FindTrackLightController();
            Assert.That(controller, Is.Not.Null, "The track-bound geometry did not create its bloom-fog light controller.");

            // (seek beat, expected alpha, expected bloom-fog multiplier). Beats before the first event hold the
            // authored component values; the beat-4 event is instant, so beat 4 itself already carries its
            // value. The beat-10 event interpolates [2 @ 0, 6 @ 1 easeInQuad] over 4 beats, so beat 12 eases
            // to 0.5^2 = 0.25 -> 2 + 0.25 * 4 = 3.
            var checks = new[]
            {
                (3.5f, AuthoredAlpha, AuthoredBloomFog), // before every event: authored component values hold
                (4.5f, 1f, AuthoredBloomFog),            // beat 4 instant alpha event
                (9f, 1f, AuthoredBloomFog),              // hold after the instant event
                (12f, 1f, 3f),                            // halfway through the eased bloom-fog interpolation
                (14f, 1f, 6f),                            // interpolation end
                (17f, 5f, 6f),                            // beat 16 instant alpha event overrides the held alpha
                (0f, AuthoredAlpha, AuthoredBloomFog),    // scrub back before the first event restores authored values
            };

            foreach (var (beat, alpha, bloomFog) in checks)
            {
                atsc.MoveToJsonTime(beat);
                // No frame yields: the seek itself must land on the as-if-played state.
                Assert.That(
                    controller.ColorAlphaMultiplier,
                    Is.EqualTo(alpha).Within(0.0001f),
                    $"Seeking to beat {beat} left the light's color alpha multiplier at " +
                    $"{controller.ColorAlphaMultiplier} instead of {alpha}.");
                Assert.That(
                    controller.BloomFogIntensityMultiplier,
                    Is.EqualTo(bloomFog).Within(0.0001f),
                    $"Seeking to beat {beat} left the light's bloom-fog multiplier at " +
                    $"{controller.BloomFogIntensityMultiplier} instead of {bloomFog}.");
            }

            yield break;
        }

        // The fixture mirrors the reported component shape: a track-bound geometry cube carrying the
        // ILightWithId component (which creates CM's light controller) and the TubeBloomPrePassLight component
        // the events animate. SimpleJSON's JSONArray exposes Add without implementing IEnumerable, so the
        // arrays are built explicitly.
        private static JSONNode CreateDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["geometry"] = new JSONObject
                {
                    ["type"] = "Cube",
                    ["material"] = "standard"
                },
                ["components"] = new JSONObject
                {
                    ["ILightWithId"] = new JSONObject
                    {
                        ["type"] = 1,
                        ["lightID"] = 9999
                    },
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["colorAlphaMultiplier"] = AuthoredAlpha,
                        ["bloomFogIntensityMultiplier"] = AuthoredBloomFog
                    }
                },
                ["track"] = TrackName
            });

            var customEvents = new JSONArray();
            customEvents.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["colorAlphaMultiplier"] = JSON.Parse("[1]")
                    }
                }
            });
            customEvents.Add(new JSONObject
            {
                ["b"] = 10f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["duration"] = 4f,
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["bloomFogIntensityMultiplier"] = JSON.Parse("[[2, 0], [6, 1, \"easeInQuad\"]]")
                    }
                }
            });
            customEvents.Add(new JSONObject
            {
                ["b"] = 16f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["colorAlphaMultiplier"] = JSON.Parse("[5]")
                    }
                }
            });

            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = customEvents
                }
            };
        }

        private static ParametricBloomFogLightController FindTrackLightController() =>
            Object.FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Track == TrackName)
                .Select(container => container.GetComponentInChildren<ParametricBloomFogLightController>(true))
                .FirstOrDefault(controller => controller != null);

        // Restore the canonical empty shared map so later fixtures do not inherit the tube-bloom fixture.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
