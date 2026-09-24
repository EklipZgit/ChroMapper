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
    // MaterialTrackAnimationTest covers Heck material color animation: a customData.materials entry can carry
    // a "track" so AnimateTrack "color" animates every geometry using that material (Give In To You by
    // JRE_McNuggies & Nugget crashes map load with an NRE in GeometryAppearanceSO.SetGeometryAppearance
    // because Geometry.prefab lost its AnimationTarget child when the prefab was recreated, leaving
    // GeometryContainer.MaterialAnimator null for any tracked material).
    public class MaterialTrackAnimationTest : TestBase
    {
        private const string TrackName = "matTrack";
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return TestUtils.ReloadMap(3, CreateDifficulty());
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // TrackedMaterialAnimatesGeometryColor requires the spawn to survive SetGeometryAppearance (the NRE),
        // a dedicated material animator on the container, and the track's animated color to reach the
        // container's material property block after a seek.
        [UnityTest]
        public IEnumerator TrackedMaterialAnimatesGeometryColor()
        {
            var container = FindGeometryContainer();
            Assert.That(container, Is.Not.Null, "The geometry enhancement did not spawn a container.");
            Assert.That(
                container.MaterialAnimator,
                Is.Not.Null,
                "The container has no material animator; tracked materials throw NRE during SetGeometryAppearance.");

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            // The color event interpolates black -> red over beats 4..8.
            atsc.MoveToJsonTime(9f);
            // One frame so the track push reaches the aggregator's LateUpdate application into the MPB.
            yield return null;

            var color = container.MpbController.Mpb.GetColor(ColorId);
            Assert.That(
                color.r,
                Is.EqualTo(1f).Within(0.01f),
                $"The track's animated color never reached the geometry material (got {color}); in game the " +
                "material's track colors the object red by beat 8.");
            Assert.That(color.g, Is.LessThan(0.05f), $"Expected animated red, got {color}.");
        }

        // The fixture mirrors Give In To You's shape: a materials entry whose "track" field binds a named
        // track, and a geometry cube referencing that material by name.
        private static JSONNode CreateDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["geometry"] = new JSONObject
                {
                    ["type"] = "Cube",
                    ["material"] = "glowmat"
                }
            });

            var customEvents = new JSONArray();
            customEvents.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateTrack",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["duration"] = 4f,
                    ["color"] = JSON.Parse("[[0, 0, 0, 1, 0], [1, 0, 0, 1, 1]]")
                }
            });

            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = customEvents,
                    ["materials"] = new JSONObject
                    {
                        ["glowmat"] = new JSONObject
                        {
                            ["color"] = JSON.Parse("[0.2, 0.2, 0.2, 1]"),
                            ["shader"] = "OpaqueLight",
                            ["track"] = TrackName
                        }
                    }
                }
            };
        }

        private static GeometryContainer FindGeometryContainer() =>
            Object.FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(container => container.EnvironmentEnhancement != null
                    && container.EnvironmentEnhancement.Geometry != null);

        // Restore the canonical empty shared map so later fixtures do not inherit the material fixture.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
