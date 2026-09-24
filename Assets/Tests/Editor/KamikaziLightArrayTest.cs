using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
       // KamikaziLightArrayTest reproduces the reported "Kamikazi Light Level Repro 2" map: 4070 generated
    // ILightWithId geometry lights parked at z -10000 that fly in on per-track AnimateTrack events starting at
    // beat 14, inside a DefaultEnvironment whose vanilla Environment and GameCore are hidden by the map's own
    // enhancement entries. The array must spawn, stay parked before beat 14, and become visible after it.
    public class KamikaziLightArrayTest : TestBase
    {
        private const int ExpectedLightCount = 4070;

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int litId = Shader.PropertyToID("_AnimationSpawned");
        private bool? animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "KamikaziLightArrayFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return LoadFixture();
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The 4070 generated lights and the fixture's map objects must survive between tests; the default
        // cleanup would delete every custom event and leave later seeks with nothing to drive.
        protected override void CleanupTestObjects()
        {
        }

        private static IEnumerator LoadFixture()
        {
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 202,
                environmentName: "DefaultEnvironment",
                songLengthSeconds: 80);
        }

        // GeneratedLightArraySpawnsParksAndFliesInAtBeat14 constrains the whole reported behavior: the array
        // exists, it is parked before its first event, and it leaves the parking position once the beat 14
        // AnimateTrack events run. The reported regression left the array not displaying at all.
        [UnityTest]
        public IEnumerator GeneratedLightArraySpawnsParksAndFliesInAtBeat14()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var lights = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Geometry != null)
                .ToList();

            Assert.That(
                lights,
                Has.Count.EqualTo(ExpectedLightCount),
                "The map's generated ILightWithId geometry did not all spawn.");

            var targets = lights
                .Select(container => container.Animator.LocalTarget)
                .Where(target => target != null)
                .ToList();
            Assert.That(
                targets,
                Has.Count.EqualTo(ExpectedLightCount),
                "Every generated light needs an animation target to fly in on.");

            atsc.MoveToJsonTime(0);
            yield return null;
            yield return null;
            // The map authors two groups: 3996 event-driven lights parked at z -10000 before beat 14, plus 74
            // static lights on one shared track (66 at their authored z 40 positions and 8 transform-less
            // inline-material triangles at the origin) that are always visible.
            var parkedAtStart = targets.Count(target => target.position.z < -9999f);
            Assert.That(
                parkedAtStart,
                Is.EqualTo(3996),
                "The event-driven generated lights were not parked at z -10000 before their beat-14 events.");
            var staticVisible = targets.Count(target => target.position.z > -9999f);
            Assert.That(
                staticVisible,
                Is.EqualTo(74),
                "The static generated lights were not at their authored visible positions.");

            // The map flies the parked lights in four waves and each wave's own duration parks it back at
            // (0, 0, -10000) before the next one: 1024 lights at beat 14 for 64 beats, 612 at beat 98 for 4
            // beats, 800 at beat 150 for 32 beats, and 1560 at beat 214 for 64 beats.
            // The production renderer list (MpbController.Renderers) holds each light's actual shape renderer,
            // not the prefab's disabled selection outline mesh.
            var brokenRenderers = lights
                .Where(container => container.MpbController.Renderers.Count == 0
                    || container.MpbController.Renderers.Any(renderer =>
                        renderer == null || !renderer.enabled || renderer.sharedMaterial == null))
                .ToList();
            Assert.That(
                brokenRenderers,
                Is.Empty,
                "A generated light had no enabled renderer with an assigned material. Samples: " +
                string.Join("; ", brokenRenderers.Take(5).Select(container =>
                    $"track='{container.EnvironmentEnhancement?.Track}' " +
                    $"material={container.EnvironmentEnhancement?.Geometry?["material"]}")));

            foreach (var (beat, visible) in new[]
                     {
                         (20f, 1024),
                         (100f, 612),
                         (152f, 800),
                         (216f, 1560),
                     })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var flownIn = targets.Count(target => target.position.z > -1000f);
                Assert.That(
                    flownIn,
                    Is.EqualTo(visible + 74),
                    $"Seeking to beat {beat} left {flownIn} generated lights visible instead of the expected " +
                    $"{visible + 74} ({visible} in active waves plus the 74 static lights); the light array is " +
                    "not displaying.");

                // The map's normal light events must actually light the flown-in lights: the TransparentLight
                // material renders nothing at zero alpha, so an unlit-but-positioned array is invisible to the
                // user even though every position and renderer flag checks out.
                var litLights = lights.Count(container =>
                {
                    var color = container.MpbController.Mpb.GetColor(colorId);
                    return color.a > 0.001f || container.MpbController.Mpb.GetFloat(litId) > 0.5f;
                });
                Assert.That(
                    litLights,
                    Is.GreaterThan(0),
                    $"No generated lights are lit at beat {beat}; the light events are not reaching the array.");
            }
        }

        // PlayingThroughTheMapRendersTheFirstWave reproduces the deployed-build report: running through the map
        // left none of the generated lights rendering even though large seeks worked. Real playback advances the
        // audio time continuously and pushes each track's animation once per frame; Jenkins tests cannot depend
        // on AudioSource playback, so this steps the time one frame at a time through the wave start, which
        // exercises the exact per-frame TrackAnimator.Update push and ObjectAnimator.LateUpdate apply streaming
        // playback uses.
        [UnityTest]
        public IEnumerator PlayingThroughTheMapRendersTheFirstWave()
        {
            // The teardown restores these because any failed assertion must not leave Playing mode active
            // across the next test's seek.
            animationsBeforeTest = Settings.Instance.Animations;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            Settings.Instance.Animations = true;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var targets = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Geometry != null)
                .Select(container => container.Animator.LocalTarget)
                .Where(target => target != null)
                .ToList();
            Assert.That(targets, Has.Count.EqualTo(ExpectedLightCount));

            // Run through beats 13.5 to 16 one frame per step, exactly like continuous playback time.
            for (var beat = 13.5f; beat <= 16f; beat += 0.05f)
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
            }

            var flownIn = targets.Count(target => target.position.z > -1000f);
            Assert.That(
                flownIn,
                Is.EqualTo(1024 + 74),
                $"Running through the start of the map left {flownIn} generated lights visible instead of the " +
                "expected 1098 (the beat-14 wave plus the static lights); the light array is not rendering " +
                "during playback.");
        }

        // LoadingAMapWhileInPlayingModeKeepsTheTimeControllerAlive pins the deployed-build root cause behind
        // "none of them are rendering": with a preview mode active, the 03_Mapper scene Start fired
        // OnTimeChanged into ObstacleGridContainer before any map data existed and RefreshWalls dereferenced
        // null sorted arrays; the exception aborted AudioTimeSyncController.Start after ResetTime, skipping
        // the audio clip assignment, the OnLevelLoaded subscription (so time never advanced again),
        // Initialized, and editor-state registration. Loading maps in playing mode must keep the time
        // controller alive and the light array animating.
        [UnityTest]
        public IEnumerator LoadingAMapWhileInPlayingModeKeepsTheTimeControllerAlive()
        {
            // Enter playing mode before the load, exactly like testing several maps without leaving preview.
            animationsBeforeTest = Settings.Instance.Animations;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            Settings.Instance.Animations = true;
            uiMode.SetUIMode(UIModeType.Playing, false);

            yield return LoadFixture();
            // The reload replaced the scene's map, so the shared baseline must point at this fixture instance
            // for the remaining tests' ResetSharedMapState to stay coherent.
            TestUtils.CaptureCurrentMapAsSharedBaseline();

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(
                atsc.Initialized,
                Is.True,
                "AudioTimeSyncController.Start aborted while loading in playing mode; without it the song time " +
                "never advances again and nothing renders during run-through.");

            atsc.MoveToJsonTime(20);
            yield return null;
            yield return null;
            var flownIn = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Geometry != null)
                .Select(container => container.Animator.LocalTarget)
                .Where(target => target != null)
                .Count(target => target.position.z > -1000f);
            Assert.That(
                flownIn,
                Is.EqualTo(1024 + 74),
                $"After loading in playing mode, seeking to beat 20 left {flownIn} generated lights visible " +
                "instead of the expected 1098.");
        }

        // Restore the editing mode per test so a failed assertion cannot leave Playing mode active across the
        // next case (KamikaziLightArrayTest's original run hung the whole runner that way); the fixture map
        // itself stays loaded and the empty-map restore happens once in the class teardown.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            // Re-find the mode objects: the playing-mode load test reloads mid-test, so the captured refs can
            // be destroyed scene objects whose fake-null check would silently skip this restore.
            var currentUiMode = uiMode != null ? uiMode : Object.FindAnyObjectByType<UIMode>();
            if (currentUiMode != null)
            {
                currentUiMode.SetUIMode(UIModeType.Normal, false);
            }

            var currentCameraManager = cameraManager != null
                ? cameraManager
                : Object.FindAnyObjectByType<CameraManager>();
            if (currentCameraManager != null)
            {
                currentCameraManager.SelectCamera(CameraType.Editing);
            }

            if (animationsBeforeTest.HasValue)
            {
                Settings.Instance.Animations = animationsBeforeTest.Value;
                animationsBeforeTest = null;
            }

            yield break;
        }

        // Restore the canonical empty shared map once per class so later fixtures do not inherit the
        // 4070-light fixture.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
