using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // The installed 1.44.1 Chroma.dll's BloomFogCustomizer and AnimateComponent target
    // BloomFogEnvironment components found under matched scene objects. The V2 FogAnimatorV2
    // binds the active fog track only when AssignFogTrack's callback fires. These tests cover
    // the target and timing cases omitted by the earlier Spells and Heliov tests.
    public class BloomFogChromaParityAuditTest : TestBase
    {
        private const string EnvironmentName = "KaleidoscopeEnvironment";
        private const string NonFogObjectId = "KaleidoscopeEnvironment.[0]Environment.[4]PlayersPlace";
        private const float DefaultAttenuation = 0.001f;
        private const float DefaultExposureLimit = 5000f;

        protected override void CleanupTestObjects()
        {
        }

        // Chroma's BloomFogCustomizer logs and skips an enhancement whose matched object
        // contains no BloomFogEnvironment component. PlayersPlace exists in this scene and
        // must not mutate the separate environment fog owner.
        [UnityTest]
        public IEnumerator EnhancementOnNonFogObjectKeepsEnvironmentFog()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = NonFogObjectId,
                ["lookupMethod"] = "Exact",
                ["components"] = new JSONObject
                {
                    ["BloomFogEnvironment"] = new JSONObject
                    {
                        ["attenuation"] = 0.04f,
                        ["offset"] = 8f,
                        ["height"] = 40f,
                        ["startY"] = 70f
                    }
                }
            });

            yield return LoadV3(environment, new JSONArray());
            AssertNonFogObjectExists();
            AssertDefaultFog();
        }

        // The installed Chroma BloomFogCustomizer recognizes four float keys. Exposure limit
        // and legacy exposure are environment defaults, not supported enhancement overrides;
        // writing them here makes CM render a configuration the game does not use.
        [UnityTest]
        public IEnumerator UnsupportedEnhancementExposureKeysLeaveGameDefaults()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = "KaleidoscopeEnvironment.[0]Environment",
                ["lookupMethod"] = "Exact",
                ["components"] = new JSONObject
                {
                    ["BloomFogEnvironment"] = new JSONObject
                    {
                        ["autoExposureLimit"] = 42f,
                        ["legacyAutoExposure"] = true
                    }
                }
            });

            yield return LoadV3(environment, new JSONArray());
            var fog = Fog();
            Assert.That(fog.AutoExposureLimit, Is.EqualTo(DefaultExposureLimit).Within(0.001f));
            Assert.That(fog.LegacyAutoExposure, Is.False);
        }

        // Heck's AnimateComponent asks the track for BloomFogEnvironment components and
        // skips the event when none are attached. A valid PlayersPlace track still has no fog
        // component, so its fog-shaped event must leave the scene's fog unchanged.
        [UnityTest]
        public IEnumerator AnimateComponentOnNonFogTrackKeepsEnvironmentFog()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = NonFogObjectId,
                ["lookupMethod"] = "Exact",
                ["track"] = "playersPlace"
            });
            var events = new JSONArray();
            events.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = "playersPlace",
                    ["BloomFogEnvironment"] = new JSONObject
                    {
                        ["attenuation"] = JSON.Parse("[0.05]")
                    }
                }
            });

            yield return LoadV3(environment, events);
            AssertNonFogObjectExists();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5f);
            AssertDefaultFog();
        }

        // Chroma's V2 FogAnimatorV2 has no active track until AssignFogTrack fires. A later
        // assignment replaces the earlier track, and seeking backward must restore the track
        // that would have been active at the earlier beat.
        [UnityTest]
        public IEnumerator LegacyFogBindingFollowsAssignmentCallbacksAndTrackSwitches()
        {
            var events = new JSONArray();
            events.Add(LegacyEvent(0f, "AnimateTrack", "fogA", 0.005f));
            events.Add(LegacyEvent(10f, "AssignFogTrack", "fogA"));
            events.Add(LegacyEvent(20f, "AnimateTrack", "fogB", 0.0002f));
            events.Add(LegacyEvent(20f, "AssignFogTrack", "fogB"));
            var difficulty = new JSONObject
            {
                ["_version"] = "2.2.0",
                ["_customData"] = new JSONObject { ["_customEvents"] = events }
            };

            yield return TestUtils.ReloadMap(2, difficulty, environmentName: EnvironmentName);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(5f);
            Assert.That(Fog().Attenuation, Is.EqualTo(DefaultAttenuation).Within(1e-09f),
                "The fogA track became active before its beat-10 AssignFogTrack callback.");
            atsc.MoveToJsonTime(12f);
            Assert.That(Fog().Attenuation, Is.EqualTo(0.005f).Within(1e-09f));
            atsc.MoveToJsonTime(21f);
            Assert.That(Fog().Attenuation, Is.EqualTo(0.0002f).Within(1e-09f));
            atsc.MoveToJsonTime(12f);
            Assert.That(Fog().Attenuation, Is.EqualTo(0.005f).Within(1e-09f));
        }

        // Chroma registers all four V3 BloomFogEnvironment float properties. Distinct values
        // catch an omitted field even when attenuation alone makes the scene look plausible.
        [UnityTest]
        public IEnumerator AnimateComponentOnFogOwnerUpdatesAllFourParameters()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = "KaleidoscopeEnvironment.[0]Environment",
                ["lookupMethod"] = "Exact",
                ["track"] = "fogOwner"
            });
            var events = new JSONArray();
            events.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = "fogOwner",
                    ["BloomFogEnvironment"] = new JSONObject
                    {
                        ["attenuation"] = JSON.Parse("[0.03]"),
                        ["offset"] = JSON.Parse("[7]"),
                        ["height"] = JSON.Parse("[35]"),
                        ["startY"] = JSON.Parse("[90]")
                    }
                }
            });

            yield return LoadV3(environment, events);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5f);
            AssertFog(0.03f, 7f, 35f, 90f);
        }

        // V2 FogAnimatorV2 reads four underscored AnimateTrack floats only after the
        // AssignFogTrack callback; the Heliov fixture did not exercise _offset.
        [UnityTest]
        public IEnumerator LegacyFogTrackUpdatesAllFourParameters()
        {
            var values = new JSONObject
            {
                ["_track"] = "fog",
                ["_attenuation"] = JSON.Parse("[0.02]"),
                ["_offset"] = JSON.Parse("[6]"),
                ["_height"] = JSON.Parse("[45]"),
                ["_startY"] = JSON.Parse("[80]"),
                ["_duration"] = 0f
            };
            var events = new JSONArray();
            events.Add(new JSONObject
            {
                ["_time"] = 4f,
                ["_type"] = "AnimateTrack",
                ["_data"] = values
            });
            events.Add(LegacyEvent(4f, "AssignFogTrack", "fog"));
            var difficulty = new JSONObject
            {
                ["_version"] = "2.2.0",
                ["_customData"] = new JSONObject { ["_customEvents"] = events }
            };

            yield return TestUtils.ReloadMap(2, difficulty, environmentName: EnvironmentName);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(5f);
            AssertFog(0.02f, 6f, 45f, 80f);
        }

        private static JSONNode LegacyEvent(float beat, string type, string track, float? attenuation = null)
        {
            var data = new JSONObject { ["_track"] = track };
            if (attenuation.HasValue)
            {
                var points = new JSONArray();
                points.Add(attenuation.Value);
                data["_attenuation"] = points;
                data["_duration"] = 0f;
            }

            return new JSONObject
            {
                ["_time"] = beat,
                ["_type"] = type,
                ["_data"] = data
            };
        }

        private static IEnumerator LoadV3(JSONArray environment, JSONArray customEvents)
        {
            var difficulty = new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = customEvents
                }
            };
            yield return TestUtils.ReloadMap(3, difficulty, environmentName: EnvironmentName);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private static BloomFogParams Fog() =>
            Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BloomFogParams;

        private static void AssertNonFogObjectExists()
        {
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            Assert.That(descriptor.ChromaIDMarkers.Any(marker => marker.ChromaID == NonFogObjectId), Is.True,
                "The PlayersPlace target must exist for this component-scoping regression.");
        }

        private static void AssertDefaultFog()
        {
            var fog = Fog();
            Assert.That(fog.Attenuation, Is.EqualTo(DefaultAttenuation).Within(1e-09f));
            Assert.That(fog.Offset, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(fog.Height, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(fog.StartY, Is.EqualTo(-20f).Within(0.0001f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogAttenuation"),
                Is.EqualTo(DefaultAttenuation).Within(1e-09f));
        }

        private static void AssertFog(float attenuation, float offset, float height, float startY)
        {
            var fog = Fog();
            Assert.That(fog.Attenuation, Is.EqualTo(attenuation).Within(1e-09f));
            Assert.That(fog.Offset, Is.EqualTo(offset).Within(0.0001f));
            Assert.That(fog.Height, Is.EqualTo(height).Within(0.0001f));
            Assert.That(fog.StartY, Is.EqualTo(startY).Within(0.0001f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogAttenuation"), Is.EqualTo(attenuation).Within(1e-09f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogOffset"), Is.EqualTo(offset).Within(0.0001f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogHeight"), Is.EqualTo(height).Within(0.0001f));
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogStartY"), Is.EqualTo(startY).Within(0.0001f));
        }

        // A fresh environment must replace every fixture before another test runs, including
        // after a failed assertion inside a loaded map's animation path.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
