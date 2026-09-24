using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // SpellsLaserWallTest reproduces the reported "Spells" map (BillieEnvironment, 150 BPM): eighteen
    // BottomPairLasers pillar regex lookups attach to rotating_L/R tracks whose beat-0 AnimateTrack events
    // instantly place, rotate, and scale each pillar into a distant laser wall. In game the wall is clearly
    // visible from the start; the report saw bloom fog from only some pillars and no laser meshes in CM.
    public class SpellsLaserWallTest : TestBase
    {
        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SpellsLaserWallFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 150,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 90);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // LaserWallPlacesRendersAndLightsEveryPillarFromBeatZero constrains the reported behavior: every
        // rotating_L/R enhancement captures its pillar, the beat-0 AnimateTrack transforms apply instantly,
        // and every pillar's mesh renderer stays enabled and lit.
        [UnityTest]
        public IEnumerator LaserWallPlacesRendersAndLightsEveryPillarFromBeatZero()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            var targets = Enumerable.Range(0, 9)
                .SelectMany(index => new[] { $"rotating_L_{index}", $"rotating_R_{index}" })
                .Select(track => (track, targets: FindEnhancedTargets(track)))
                .ToList();
            foreach (var (track, trackTargets) in targets)
            {
                Assert.That(
                    trackTargets,
                    Is.Not.Empty,
                    $"The '{track}' pillar regex captured no objects; the laser wall cannot render.");
            }

            atsc.MoveToJsonTime(0);
            yield return null;
            yield return null;

            // The beat-0 AnimateTrack on each rotating track is an instant V3 world position/rotation/scale,
            // so every pillar must sit at its authored wall coordinates (rotating_7/8 use Chroma's "yeet").
            var expectedPositions = new Dictionary<string, Vector3>
            {
                ["rotating_L_2"] = new Vector3(-117.4306488f, -336.8546753f, 322.645752f),
                ["rotating_L_5"] = new Vector3(-62.048027f, -336.8546753f, 225.819458f),
                ["rotating_R_0"] = new Vector3(129.6551056f, -336.8546753f, 228.8448334f),
                ["rotating_R_6"] = new Vector3(56.687973f, -336.8546753f, 314.8919373f),
            };
            foreach (var (track, expected) in expectedPositions)
            {
                foreach (var target in FindEnhancedTargets(track))
                {
                    Assert.That(
                        Vector3.Distance(target.position, expected),
                        Is.LessThan(0.01f),
                        $"The '{track}' pillar was at {target.position} instead of its beat-0 world position " +
                        $"{expected}; the laser wall is not placed.");
                    // The beat-0 AnimateTrack scale writes the pillar's localScale; the pillar's parents
                    // contribute their own scene scale to lossyScale.
                    Assert.That(
                        target.localScale,
                        Is.EqualTo(new Vector3(8.9388237f, 2f, 2f)).Within(0.01f),
                        $"The '{track}' pillar did not receive its beat-0 scale {target.localScale}.");
                }
            }

            var hiddenPillars = targets
                .Where(entry => !expectedPositions.ContainsKey(entry.track))
                .SelectMany(entry => entry.targets)
                .Where(target => !target.gameObject.activeInHierarchy
                    || target.GetComponentsInChildren<Renderer>(true).All(renderer => !renderer.enabled))
                .ToList();
            Assert.That(
                hiddenPillars,
                Is.Empty,
                "Some laser wall pillars had no enabled renderer in their hierarchy; the laser meshes are not " +
                "rendering while their bloom fog does, matching the report.");
        }

        // Temporary diagnostic for the reported invisible white lasers around beat 288: dumps every
        // light-registered controller's type/index/resolved-key plus mesh-vs-fog state so the failing
        // object set can be identified by lightID instead of guessed.
        [UnityTest]
        public IEnumerator DiagDumpLightStatesAtBeat288()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(288);
            yield return null;
            yield return null;

            var manager = Object.FindAnyObjectByType<BasicEventEffectManager>();
            var effects = manager.GetComponentsInChildren<BasicLightEffect>(true);
            foreach (var effect in effects)
            {
                var remap = new Dictionary<int, int>();
                foreach (var entry in effect.LightIdRemapEntries)
                    remap[(int)entry.y] = (int)entry.x;
                var field = typeof(BasicLightEffect).GetField(
                    "lightIDToController",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var table = (Dictionary<int, LightController>)field!.GetValue(effect);
                foreach (var (index, controller) in table)
                {
                    if (controller is not ParametricBloomFogLightController pbflc) continue;
                    remap.TryGetValue(index, out var authored);
                    var box = pbflc.BoxLight;
                    var fog = pbflc.BloomFog;
                    Debug.Log(
                        $"[Diag288] type={controller.Type} idx={index} authored={authored} " +
                        $"color={pbflc.Color} boxEnabled={(box != null && box.Renderer != null && box.Renderer.enabled)} " +
                        $"fogInt={(fog != null ? fog.IntensityMultiplier : -1f)} name={pbflc.gameObject.name}");
                }
            }

            yield break;
        }

        // Each rotating track's enhanced objects are the matched pillars' transforms.
        private static List<Transform> FindEnhancedTargets(string trackName) => Object
            .FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(container => container.EnvironmentEnhancement?.Track == trackName)
            .SelectMany(container => container.GetComponents<ObjectAnimator>())
            .Select(animator => animator.LocalTarget)
            .Where(target => target != null)
            .ToList();

        // Restore the canonical empty shared map so later fixtures do not inherit the laser wall fixture.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
