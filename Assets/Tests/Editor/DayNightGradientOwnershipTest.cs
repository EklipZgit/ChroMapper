using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Vanishing Point (BillieEnvironment): Day and Night TintColor controllers both write the
    // same BloomPrePassBackgroundColorsGradient. LightshowController.LateUpdate refreshes every
    // collected component, so an inactive Night used to overwrite Day's tint every frame;
    // CombinedLightsController.ShouldRefresh now follows isActiveAndEnabled so only the live
    // branch writes the shared gradient.
    public class DayNightGradientOwnershipTest : TestBase
    {
        private const string DayAndNightId = "BillieEnvironment.[0]Environment.[27]DayAndNight";
        private const string DayId = DayAndNightId + ".[0]Day";
        private const string NightId = DayAndNightId + ".[1]Night";

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
        public IEnumerator InactiveBranchDoesNotOverwriteTheSharedGradient()
        {
            yield return LoadDifficulty();
            var dayMarker = MarkerTransform(DayId);
            var nightMarker = MarkerTransform(NightId);
            var dayController = TintController(dayMarker, "Day");
            var nightController = TintController(nightMarker, "Night");
            var dayGradient = dayController.BloomPrePassBackgroundColorsGradient;
            var nightGradient = nightController.BloomPrePassBackgroundColorsGradient;
            Assert.That(dayGradient, Is.Not.Null, "Day tint controller must reference a gradient.");
            Assert.That(nightGradient, Is.Not.Null, "Night tint controller must reference a gradient.");
            Assert.That(dayGradient, Is.SameAs(nightGradient),
                "Day and Night tint controllers must share one BloomPrePassBackgroundColorsGradient.");
            var gradient = dayGradient;
            var lightshow = Object.FindAnyObjectByType<LightshowController>();
            Assert.That(lightshow, Is.Not.Null);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();

            clock.MoveToJsonTime(6.5f);
            yield return null;
            yield return null;
            Assert.That(nightMarker.gameObject.activeInHierarchy, Is.True,
                "The beat-4 boost must activate Night so its Start initializes the controller.");

            clock.MoveToJsonTime(9f);
            yield return null;
            yield return null;
            Assert.That(dayMarker.gameObject.activeInHierarchy, Is.True);
            Assert.That(nightMarker.gameObject.activeInHierarchy, Is.False);

            dayController.Refresh();
            var expectedDayTint = gradient.TintColor;
            lightshow.LateUpdate();
            AssertTint(gradient.TintColor, expectedDayTint, "shared gradient after Day refresh and LateUpdate");
            Assert.That(expectedDayTint.r, Is.GreaterThan(0.01f),
                "Day's tint must be a real color for this check to discriminate.");

            clock.MoveToJsonTime(6.5f);
            yield return null;
            yield return null;
            Assert.That(nightMarker.gameObject.activeInHierarchy, Is.True);
            nightController.Refresh();
            var expectedNightTint = gradient.TintColor;
            lightshow.LateUpdate();
            AssertTint(gradient.TintColor, expectedNightTint,
                "shared gradient after Night refresh and LateUpdate");

            clock.MoveToJsonTime(9f);
            yield return null;
            yield return null;
            Assert.That(dayMarker.gameObject.activeInHierarchy, Is.True);
            Assert.That(nightMarker.gameObject.activeInHierarchy, Is.False);
            dayController.Refresh();
            expectedDayTint = gradient.TintColor;
            lightshow.LateUpdate();
            AssertTint(gradient.TintColor, expectedDayTint,
                "shared gradient after returning to Day and LateUpdate");

            var wasEnabled = dayController.enabled;
            var sentinel = new Color(0.17f, 0.29f, 0.41f, 0.53f);
            try
            {
                dayController.enabled = false;
                gradient.TintColor = sentinel;
                lightshow.LateUpdate();
                AssertTint(gradient.TintColor, sentinel,
                    "shared gradient while the Day controller component is disabled");
            }
            finally
            {
                dayController.enabled = wasEnabled;
            }
            yield return null;
            AssertTint(gradient.TintColor, expectedDayTint,
                "shared gradient after Day's controller is re-enabled");
        }

        private static void AssertTint(Color actual, Color expected, string because)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001f), because + " (r)");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001f), because + " (g)");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001f), because + " (b)");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.00001f), because + " (a)");
        }

        private static Transform MarkerTransform(string chromaId) => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == chromaId).transform;

        private static BloomPrePassBackgroundColorsGradientTintColorLightsController TintController(
            Transform marker, string owner)
        {
            var controller = marker.GetComponent<BloomPrePassBackgroundColorsGradientTintColorLightsController>();
            Assert.That(controller, Is.Not.Null, $"Missing {owner} tint controller under {marker.name}.");
            return controller;
        }

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
                { ""_time"": 6, ""_type"": 4, ""_value"": 1, ""_customData"": { ""_color"": [1, 1, 1, 1] } },
                { ""_time"": 8, ""_type"": 5, ""_value"": 0 },
                { ""_time"": 8.5, ""_type"": 4, ""_value"": 1, ""_customData"": { ""_color"": [1, 1, 1, 1] } },
                { ""_time"": 10, ""_type"": 5, ""_value"": 1 },
                { ""_time"": 10.5, ""_type"": 4, ""_value"": 1, ""_customData"": { ""_color"": [1, 1, 1, 1] } }
            ],
            ""_notes"": [],
            ""_obstacles"": [],
            ""_waypoints"": [],
            ""_customData"": {
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
