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
    // Vanishing Point (BillieEnvironment): the moon lives under DayAndNight.[1]Night, which
    // GameObjectSwitch activates while the color boost is on. The scene build skipped Night's
    // lights because the branch starts inactive, so the moon activated but stayed unlit;
    // EnvironmentDescriptor now registers switch-branch lights at initialize.
    public class VanishingPointDayNightBoostTest : TestBase
    {
        private const string DayAndNightId = "BillieEnvironment.[0]Environment.[27]DayAndNight";
        private const string DayId = DayAndNightId + ".[0]Day";
        private const string SunId = DayId + ".[0]Sun";
        private const string NightId = DayAndNightId + ".[1]Night";
        private const string MoonId = NightId + ".[0]Moon";

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
        public IEnumerator ColorBoostTogglesDayAndNightAroundTheMoonWindow()
        {
            yield return LoadDifficulty();
            var day = MarkerTransform(DayId);
            var night = MarkerTransform(NightId);
            var moon = MarkerTransform(MoonId);
            var sunSprite = MarkerTransform(SunId).GetComponentInChildren<SpriteRenderer>();
            var moonLight = moon.GetComponent<LightController>();
            var moonSprite = moon.GetComponentInChildren<SpriteRenderer>();
            Assert.That(day.gameObject.activeInHierarchy, Is.True, "Day must start active before the boost.");
            Assert.That(night.gameObject.activeInHierarchy, Is.False, "Night must start inactive before the boost.");

            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(5f);
            yield return null;
            Assert.That(night.gameObject.activeInHierarchy, Is.True,
                "Boost-on must activate Night like GameObjectSwitchEventEffect does in game.");
            Assert.That(day.gameObject.activeInHierarchy, Is.False,
                "Boost-on must deactivate Day like GameObjectSwitchEventEffect does in game.");
            Assert.That(moon.gameObject.activeInHierarchy, Is.True, "The moon must be visible while Night is active.");
            Assert.That(Descriptor().IsRegistered(moonLight), Is.True,
                "Native registers a light's controller when its object enables; the moon's light must register on boost.");

            clock.MoveToJsonTime(6.5f);
            yield return null;
            Assert.That(moonSprite.color.a, Is.GreaterThan(0.5f),
                "The lit beat-6 strobe must drive the moon's registered sprite light.");

            clock.MoveToJsonTime(9f);
            yield return null;
            Assert.That(night.gameObject.activeInHierarchy, Is.False, "Boost-off must deactivate Night again.");
            Assert.That(day.gameObject.activeInHierarchy, Is.True, "Boost-off must reactivate Day.");
            Assert.That(Descriptor().IsRegistered(moonLight), Is.True,
                "The moon's light must stay registered while Night is inactive so its states keep tracking events.");
            Assert.That(sunSprite.color.a, Is.GreaterThan(0.5f),
                "Day's scene-registered lights must be re-registered and driven after the toggles.");

            clock.MoveToJsonTime(11f);
            yield return null;
            Assert.That(night.gameObject.activeInHierarchy, Is.True, "The second boost-on must activate Night again.");
            Assert.That(moonSprite.color.a, Is.GreaterThan(0.5f),
                "Re-activation must re-register the moon's light so the second strobe also lights it.");
        }

        [UnityTest]
        public IEnumerator ContainsEnhancementResolvesTheInactiveMoon()
        {
            yield return LoadDifficulty();
            var moon = MarkerTransform(MoonId);
            Assert.That(moon.localScale.x, Is.EqualTo(12.5f).Within(0.001f),
                "The Contains 'Moon' enhancement must apply its authored scale even while Night is inactive.");
            Assert.That(moon.localPosition.z, Is.EqualTo(325f * 0.6f).Within(0.001f),
                "V2 localPosition is scaled by the 0.6 lane size.");
        }

        private static Transform MarkerTransform(string chromaId) => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == chromaId).transform;

        private static EnvironmentDescriptor Descriptor() => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor;

        private IEnumerator LoadDifficulty()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2, Difficulty(), environmentName: "BillieEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private static JSONNode Difficulty() => JSON.Parse(@"{
            ""_version"": ""2.2.0"",
            ""_events"": [
                { ""_time"": 4, ""_type"": 5, ""_value"": 1 },
                { ""_time"": 6, ""_type"": 4, ""_value"": 5 },
                { ""_time"": 8, ""_type"": 5, ""_value"": 0 },
                { ""_time"": 8.5, ""_type"": 4, ""_value"": 5 },
                { ""_time"": 10, ""_type"": 5, ""_value"": 1 },
                { ""_time"": 10.5, ""_type"": 4, ""_value"": 5 }
            ],
            ""_notes"": [],
            ""_obstacles"": [],
            ""_waypoints"": [],
            ""_customData"": {
                ""_environment"": [
                    {
                        ""_id"": ""BillieEnvironment.[0]Environment.[27]DayAndNight.[0]Day.[0]Sun"",
                        ""_lookupMethod"": ""Exact"",
                        ""_localPosition"": [0,1,325],
                        ""_scale"": [12.5,12.5,1]
                    },
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
