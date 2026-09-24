using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // HeliovMapParityTest keeps Swifter's complete 1,098-entry BTS environment, its preceding
    // custom events through beat 525, the real lighting events, and the opening notes and rain walls.
    // The four tests isolate the reported rain, first cliff, beat-102 valley/beat-457 mountain,
    // and beat-518 powerline views at the map's 150 BPM. The map uses legacy AssignFogTrack plus
    // AnimateTrack fog fields, whereas Spells uses AnimateComponent.
    public class HeliovMapParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private bool colorFakeWallsBeforeTest;
        private float playerCameraOffsetZBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "HeliovEnvironmentFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            colorFakeWallsBeforeTest = Settings.Instance.ColorFakeWalls;
            playerCameraOffsetZBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            Settings.Instance.Animations = true;
            Settings.Instance.ColorFakeWalls = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;

            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 150,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 215);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [SetUp]
        public void EnableReportedPreviewSettings()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.ColorFakeWalls = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
        }

        // RainWallsUseAuthoredColorInPlayingMode: the beat-7.5 rain obstacles have negative
        // duration but carry an explicit gray/transparent custom color. The editor's fake-wall
        // diagnostic tint must not replace that gameplay color when Playing renders the rain.
        [UnityTest]
        public IEnumerator RainWallsUseAuthoredColorInPlayingMode()
        {
            EnterPlayingMode();
            yield return SeekTo(7.5f);

            var rain = Object.FindObjectsByType<ObstacleContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(wall => wall.ObstacleData != null
                    && Mathf.Abs(wall.ObstacleData.JsonTime - 7.5f) < 0.0001f);
            Assert.That(rain, Is.Not.Null, "The first authored rain wall did not load.");
            Assert.That(rain.ObstacleData.Duration, Is.LessThan(0f));
            Assert.That(rain.ObstacleData.CustomColor, Is.Not.Null);
            var actual = rain.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
            var authored = rain.ObstacleData.CustomColor.Value;
            Assert.That(actual.r, Is.EqualTo(authored.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(authored.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(authored.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(authored.a).Within(0.0001f));
        }

        // CliffSceneAtFirstNotesUsesFogTrackAndEnvironment: at beat 6 the camera rides the
        // player track into the 1,049 duplicated pillar meshes. The map sets the fog height and
        // start Y here; without them the fog blacks out the authored cliff and distant objects.
        [UnityTest]
        public IEnumerator CliffSceneAtFirstNotesUsesFogTrackAndEnvironment()
        {
            EnterPlayingMode();
            yield return SeekTo(6.1f);

            var camera = cameraManager.CameraControllers[1].transform;
            Assert.That(camera.position.y, Is.GreaterThan(500f),
                "AssignPlayerToTrack did not move the camera to the elevated first-note cliff.");
            var constructed = Object.FindObjectsByType<GeometryContainer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(container => container.EnvironmentEnhancement?.Track?.StartsWith("blenderEnv0_") == true);
            Assert.That(constructed, Is.GreaterThanOrEqualTo(1000),
                "The map's duplicated pillar scenery was not constructed from environment enhancements.");
            AssertFog(3e-06f, 200f, 277.73256f, "first-note cliff");
        }

        // ValleyAndMountainFogFollowAuthoredTrack covers both reported distant views. The beat-102
        // values place a 200-unit fog layer below the new valley camera; beat 454 moves the 300-unit
        // layer for the mountain visible at beat 457. A stale fog state hides distant scenery.
        [UnityTest]
        public IEnumerator ValleyAndMountainFogFollowAuthoredTrack()
        {
            EnterPlayingMode();
            yield return SeekTo(103f);
            AssertFog(1e-05f, 200f, -32.931479f, "beat-102 valley");

            yield return SeekTo(457f);
            AssertFog(3e-06f, 300f, 49.320374f, "beat-457 mountain");
        }

        // PowerlineWaterFogUsesBeat518Values: the water scene intentionally sets height to zero
        // and moves the fog start far below the camera while increasing attenuation. Dropping the
        // height/startY animation leaves an opaque fog plane in front of the powerlines.
        [UnityTest]
        public IEnumerator PowerlineWaterFogUsesBeat518Values()
        {
            EnterPlayingMode();
            yield return SeekTo(518.1f);
            AssertFog(1e-04f, 0f, -69765.12657f, "beat-518 powerline water");

            // The shared Heliov fixture must also restore the earlier cliff's fog on a reverse
            // seek; retaining the powerline layer would black out the opening after scrubbing.
            yield return SeekTo(6.1f);
            AssertFog(3e-06f, 200f, 277.73256f, "first-note cliff after reverse seek");
        }

        // HeliovBeat41LaserProjectsFromItsAuthoredLeftSource: the beat-41.5 type-2
        // flash targets IDs 201 and 202, which the map assigns to the duplicated
        // mainLight and mainLight2 lasers. Inspect both actual light quads so a
        // half-missing beam cannot hide behind correct fog parameter assertions.
        [UnityTest]
        public IEnumerator Beat41LaserProjectsFromItsAuthoredLeftSource()
        {
            EnterPlayingMode();
            // The Playing camera binds its player track during Update; establish the
            // first-note scene before advancing through the same view to the flash.
            yield return SeekTo(6.1f);
            yield return SeekTo(41.5f);

            var camera = cameraManager.CameraControllers[1].Camera;
            var playerTrack = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack("player");
            var propertyStates = string.Join(",", playerTrack.AnimatedProperties
                .Select(pair => pair.Key + ":" + pair.Value.StartTime));
            var childStates = string.Join(";", playerTrack.Children
                .Select(child => child.name + ":" + child.enabled + ":" + child.OffsetPosition.Count +
                    ":" + (child.LocalTarget != null ? child.LocalTarget.position.ToString() : "null")));
            Debug.Log($"[HeliovLaserDiag] cameraController={cameraManager.CameraControllers[1].transform.position} " +
                $"camera={camera.transform.position} active={camera.gameObject.activeInHierarchy} " +
                $"enabled={camera.enabled} near={camera.nearClipPlane} far={camera.farClipPlane} " +
                $"uiMode={UIMode.AnimationMode} beat={Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentJsonTime} " +
                $"playerTrack={playerTrack.Track.transform.position} " +
                $"playerObjectParent={playerTrack.Track.ObjectParentTransform.position} " +
                $"trackEnabled={playerTrack.enabled} " +
                $"children={playerTrack.Children.Count} cachedChildren={playerTrack.CachedChildren.Length} " +
                $"props={propertyStates} childStates={childStates}");
            var containers = Object.FindObjectsByType<GeometryContainer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Track is "mainLight" or "mainLight2")
                .ToArray();
            Assert.That(containers.Length, Is.EqualTo(2), "Both authored sky lasers must be enhanced.");

            foreach (var container in containers)
            {
                // The pooled geometry prefab also carries an unassigned animator; inspect the
                // animator created for this enhancement's actual environment target.
                var animator = container.GetComponents<Beatmap.Animations.ObjectAnimator>()
                    .FirstOrDefault(candidate => candidate.LocalTarget != null);
                Assert.That(animator, Is.Not.Null);
                var target = animator.LocalTarget;
                var light = target.GetComponentInChildren<ParametricBloomFogLightController>(true);
                Assert.That(light, Is.Not.Null);
                Assert.That(light.Color.a, Is.GreaterThan(0.01f),
                    $"{container.EnvironmentEnhancement.Track} did not receive the beat-41.5 flash.");
                var fogLight = light.BloomFog;
                Assert.That(fogLight, Is.Not.Null);
                var length = fogLight.Length * fogLight.MultiplyLengthByAlphaBloomFogMultiplier;
                var start = fogLight.transform.TransformPoint(0f, -length * fogLight.Center, 0f);
                var end = fogLight.transform.TransformPoint(0f, length * (1f - fogLight.Center), 0f);
                var projectedStart = camera.WorldToViewportPoint(start);
                var projectedEnd = camera.WorldToViewportPoint(end);
                var quads = new BloomfogQuad[1];
                var count = 0;
                fogLight.ApplyToQuad(ref count, quads, camera.worldToCameraMatrix,
                    camera.projectionMatrix, 0.02f);
                Debug.Log($"[HeliovLaserDiag] track={container.EnvironmentEnhancement.Track} " +
                    $"id={light.ID} color={light.Color} camera={camera.transform.position} " +
                    $"source={target.position} start={start} end={end} " +
                    $"projectedStart={projectedStart} projectedEnd={projectedEnd} " +
                    $"length={fogLight.Length} center={fogLight.Center} " +
                    $"widths={fogLight.StartWidth},{fogLight.EndWidth} " +
                    $"alphas={fogLight.StartAlpha},{fogLight.EndAlpha} " +
                    $"intensity={fogLight.IntensityMultiplier} " +
                    $"quadColors={quads[0].Vertex0Color},{quads[0].Vertex2Color} " +
                    $"quadCount={count} quadStart={quads[0].Vertex0Position} " +
                    $"quadEnd={quads[0].Vertex2Position} " +
                    $"quadStartView={quads[0].Vertex0ViewPos} quadEndView={quads[0].Vertex2ViewPos}");
            }

            var bloomController = Object.FindAnyObjectByType<BloomfogRenderingController>();
            var rawField = typeof(BloomfogRenderingController).GetField("bloomfogRaw",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var finalField = typeof(BloomfogRenderingController).GetField("bloomfogTex",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var rendererField = typeof(BloomfogRenderingController).GetField("bloomfogRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var rawTexture = (RenderTexture)rawField.GetValue(bloomController);
            var renderer = (BloomfogRendererSO)rendererField.GetValue(bloomController);
            renderer.RenderToTexture(camera, rawTexture, out _);
            CaptureFogTexture(rawTexture, "heliov-beat41-raw.png");
            CaptureFogTexture((RenderTexture)finalField.GetValue(bloomController), "heliov-beat41-final.png");
            var registeredLights = BloomFogObject.AllBloomFogLights.ToArray();
            try
            {
                foreach (var container in containers)
                {
                    var laser = container.GetComponents<Beatmap.Animations.ObjectAnimator>()
                        .First(candidate => candidate.LocalTarget != null).LocalTarget
                        .GetComponentInChildren<ParametricBloomFogLightController>(true).BloomFog;
                    BloomFogObject.AllBloomFogLights.Clear();
                    BloomFogObject.AllBloomFogLights.Add(laser);
                    renderer.RenderToTexture(camera, rawTexture, out _);
                    CaptureFogTexture(rawTexture,
                        $"heliov-beat41-{container.EnvironmentEnhancement.Track}.png");
                }
            }
            finally
            {
                BloomFogObject.AllBloomFogLights.Clear();
                BloomFogObject.AllBloomFogLights.AddRange(registeredLights);
            }

            var oldTarget = camera.targetTexture;
            var sceneTexture = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture = sceneTexture;
                camera.Render();
                CaptureFogTexture(sceneTexture, "heliov-beat41-scene.png");
                CaptureFogTexture((RenderTexture)finalField.GetValue(bloomController),
                    "heliov-beat41-final-active.png");
            }
            finally
            {
                camera.targetTexture = oldTarget;
                Object.Destroy(sceneTexture);
            }

            Assert.Fail("Capture both laser sources and quads before choosing the geometric parity assertion.");
        }

        private static void CaptureFogTexture(RenderTexture renderTexture, string name)
        {
            Assert.That(renderTexture, Is.Not.Null);
            var previous = RenderTexture.active;
            var image = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                image.Apply();
                var output = Path.Combine(Application.dataPath, "..", "TestResults", name);
                File.WriteAllBytes(output, image.EncodeToPNG());
                Debug.Log($"[HeliovLaserDiag] captured={output}");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(image);
            }
        }

        private static void AssertFog(float attenuation, float height, float startY, string view)
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.That(context.Descriptor, Is.Not.Null);
            var fog = context.Descriptor.BloomFogParams;
            Assert.That(fog.Attenuation, Is.EqualTo(attenuation).Within(1e-09f),
                $"{view}: authored fog attenuation did not reach the descriptor.");
            Assert.That(fog.Height, Is.EqualTo(height).Within(0.001f),
                $"{view}: authored fog height did not reach the descriptor.");
            Assert.That(fog.StartY, Is.EqualTo(startY).Within(0.1f),
                $"{view}: authored fog start Y did not reach the descriptor.");
            Assert.That(Shader.GetGlobalFloat("_CustomFogAttenuation"),
                Is.EqualTo(attenuation).Within(1e-09f),
                $"{view}: fog attenuation did not reach the renderer.");
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogHeight"),
                Is.EqualTo(height).Within(0.001f),
                $"{view}: fog height did not reach the renderer.");
            Assert.That(Shader.GetGlobalFloat("_CustomFogHeightFogStartY"),
                Is.EqualTo(startY).Within(0.1f),
                $"{view}: fog start Y did not reach the renderer.");
        }

        private void EnterPlayingMode()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // Restore preview mode and settings even after a failed assertion; otherwise the next
        // mapper scene Start runs with a stale Playing camera and can hang the Unity test runner.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.ColorFakeWalls = colorFakeWallsBeforeTest;
            Settings.Instance.PlayerCameraOffsetZ = playerCameraOffsetZBeforeTest;
            yield break;
        }

        // Clear the full Heliov fixture after the class so later tests get their ordinary shared map.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
