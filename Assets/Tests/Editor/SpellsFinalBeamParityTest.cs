using System.Collections;
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
    // The final Spells laser burst uses the complete authored environment and all prior events.
    public class SpellsFinalBeamParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private Camera playingCamera;
        private Quaternion playingRotationBeforeTest;
        private float playingAspectBeforeTest;

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            var fixture = Path.Combine(Application.dataPath, "Tests", "Fixtures", "SpellsLaserWallFixture.json");
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(fixture)),
                beatsPerMinute: 150,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 215);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // The game's final beams have a bright pale surface; keep the fog identical while
        // measuring the box renderers' own channel balance in CM.
        [UnityTest]
        public IEnumerator FinalBurstPhysicalLaserSurfaceHasWhiteCore()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(526.9f);
            yield return null;
            yield return null;
            // FinalBurstPhysicalLaserSurfaceHasWhiteCore: the authored 527.x
            // cascade reaches four beam IDs by 527.5, matching the supplied frame.
            atsc.MoveToJsonTime(527.5f);
            yield return null;
            yield return null;

            // FinalBurstPhysicalLaserSurfaceHasWhiteCore: lock the capture pose so an
            // inherited editor camera angle cannot move these subpixel beams between runs.
            var camera = cameraManager.CameraControllers[1].Camera;
            playingCamera = camera;
            playingRotationBeforeTest = camera.transform.rotation;
            playingAspectBeforeTest = camera.aspect;
            camera.transform.rotation = Quaternion.identity;
            camera.aspect = 1f;
            Debug.Log($"[Spells527] beat={atsc.CurrentJsonTime} camera={camera.transform.position} " +
                $"rotation={camera.transform.rotation} aspect={camera.aspect}");
            var previousTarget = camera.targetTexture;
            // FinalBurstPhysicalLaserSurfaceHasWhiteCore: capture the Playing
            // camera's actual target format so the post-bloom path is representative.
            var cameraFormat = camera.allowHDR
                ? RenderTextureFormat.DefaultHDR
                : RenderTextureFormat.ARGB32;
            var capture = new RenderTexture(1024, 1024, 24, cameraFormat);
            var image = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = capture;
                camera.Render();
                ReadRenderTexture(capture, image);
                var output = Path.Combine(Application.dataPath, "..", "TestResults", "spells-beat527-cm.png");
                File.WriteAllBytes(output, image.EncodeToPNG());
                Debug.Log($"[Spells527] capture={output} fov={camera.fieldOfView} " +
                    $"postBloom={Shader.IsKeywordEnabled("POST_BLOOM")} " +
                    $"boost={Shader.GetGlobalFloat("_BaseColorBoost")} " +
                    $"threshold={Shader.GetGlobalFloat("_BaseColorBoostThreshold")}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(capture);
                Object.Destroy(image);
            }
            var targets = Object.FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Track?.StartsWith("rotating_") == true)
                .SelectMany(container => container.GetComponents<ObjectAnimator>()
                    .Where(animator => animator.LocalTarget != null)
                    .Select(animator => (container.EnvironmentEnhancement.Track, animator.LocalTarget)))
                .ToArray();
            Assert.That(targets.Length, Is.GreaterThanOrEqualTo(18));
            foreach (var (track, target) in targets)
            {
                var lights = target.GetComponentsInChildren<ParametricBloomFogLightController>(true);
                foreach (var light in lights)
                {
                    var box = light.BoxLight;
                    if (box == null)
                        continue;
                    var viewport = cameraManager.CameraControllers[1].Camera.WorldToViewportPoint(box.Renderer.bounds.center);
                    Debug.Log($"[Spells527] track={track} type={light.Type} id={light.ID} color={light.Color} " +
                        $"parentScale={target.localScale} boxScale={box.transform.localScale} " +
                        $"boxWidth={box.Width} widthStart={box.WidthStart} widthEnd={box.WidthEnd} " +
                        $"alpha={box.AlphaMultiplier} material={box.Renderer.sharedMaterial.name} " +
                        $"visible={box.Renderer.enabled} viewport={viewport} " +
                        $"meshBounds={box.Renderer.GetComponent<MeshFilter>().sharedMesh.bounds}");
                }
            }

            // FinalBurstPhysicalLaserSurfaceHasWhiteCore: isolate the authored box meshes from
            // the unchanged fog and measure the surface's blue/white contribution at beat 527.
            var beamRenderers = targets
                .SelectMany(entry => entry.LocalTarget.GetComponentsInChildren<ParametricBloomFogLightController>(true))
                .Where(light => light.BoxLight != null)
                .Select(light => light.BoxLight.Renderer)
                .Distinct()
                .ToArray();
            var wasEnabled = beamRenderers.Select(renderer => renderer.enabled).ToArray();
            var previousCameraTarget = camera.targetTexture;
            // FinalBurstPhysicalLaserSurfaceHasWhiteCore: beam subtraction must
            // use the same camera target format as the preview capture above.
            var sampleTarget = new RenderTexture(1024, 1024, 24, cameraFormat);
            var withBeams = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            var withoutBeams = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = sampleTarget;
                camera.Render();
                ReadRenderTexture(sampleTarget, withBeams);
                foreach (var renderer in beamRenderers)
                    renderer.enabled = false;
                camera.Render();
                ReadRenderTexture(sampleTarget, withoutBeams);

                var (litPixels, maxRed, redTotal, blueTotal) = MeasureSurface(withBeams, withoutBeams);

                Debug.Log($"[Spells527] surfacePixels={litPixels} maxRed={maxRed} " +
                    $"blueRedRatio={blueTotal / redTotal} redTotal={redTotal} blueTotal={blueTotal}");

                // FinalBurstPhysicalLaserSurfaceHasWhiteCore: hold the rest of the
                // scene fixed while measuring how much distance fog removes from the beam.
                var authoredAttenuation = Shader.GetGlobalFloat("_CustomFogAttenuation");
                var authoredOffset = Shader.GetGlobalFloat("_CustomFogOffset");
                var authoredHeight = Shader.GetGlobalFloat("_CustomFogHeightFogHeight");
                var authoredStartY = Shader.GetGlobalFloat("_CustomFogHeightFogStartY");
                Debug.Log($"[Spells527] fogAttenuation={authoredAttenuation} " +
                    $"fogOffset={authoredOffset} fogHeight={authoredHeight} fogStartY={authoredStartY}");
                try
                {
                    Shader.SetGlobalFloat("_CustomFogAttenuation", 0f);
                    foreach (var renderer in beamRenderers)
                        renderer.enabled = true;
                    camera.Render();
                    ReadRenderTexture(sampleTarget, withBeams);
                    foreach (var renderer in beamRenderers)
                        renderer.enabled = false;
                    camera.Render();
                    ReadRenderTexture(sampleTarget, withoutBeams);
                    var (noFogPixels, noFogMaxRed, noFogRed, noFogBlue) =
                        MeasureSurface(withBeams, withoutBeams);
                    Debug.Log($"[Spells527] noDistanceFogSurfacePixels={noFogPixels} " +
                        $"maxRed={noFogMaxRed} blueRedRatio={noFogBlue / noFogRed} " +
                        $"redTotal={noFogRed} blueTotal={noFogBlue}");
                }
                finally
                {
                    Shader.SetGlobalFloat("_CustomFogAttenuation", authoredAttenuation);
                }

                // FinalBurstPhysicalLaserSurfaceHasWhiteCore: keep an opposite-format
                // diagnostic render to expose any future camera-format regression.
                var alternateFormat = camera.allowHDR
                    ? RenderTextureFormat.ARGB32
                    : RenderTextureFormat.DefaultHDR;
                var alternateTarget = new RenderTexture(1024, 1024, 24, alternateFormat);
                try
                {
                    camera.targetTexture = alternateTarget;
                    foreach (var renderer in beamRenderers)
                        renderer.enabled = true;
                    camera.Render();
                    ReadRenderTexture(alternateTarget, withBeams);
                    foreach (var renderer in beamRenderers)
                        renderer.enabled = false;
                    camera.Render();
                    ReadRenderTexture(alternateTarget, withoutBeams);
                    var (alternatePixels, alternateMaxRed, alternateRed, alternateBlue) =
                        MeasureSurface(withBeams, withoutBeams);
                    Debug.Log($"[Spells527] alternateFormat={alternateFormat} " +
                        $"surfacePixels={alternatePixels} maxRed={alternateMaxRed} " +
                        $"blueRedRatio={alternateBlue / alternateRed} " +
                        $"redTotal={alternateRed} blueTotal={alternateBlue}");
                }
                finally
                {
                    camera.targetTexture = sampleTarget;
                    Object.Destroy(alternateTarget);
                }
                // FinalBurstPhysicalLaserSurfaceHasWhiteCore: the player's game setting is 4x MSAA,
                // while CM's quality profile disables it; isolate sample coverage before changing it.
                var msaaTarget = new RenderTexture(1024, 1024, 24, cameraFormat)
                {
                    antiAliasing = 4
                };
                try
                {
                    camera.targetTexture = msaaTarget;
                    foreach (var renderer in beamRenderers)
                        renderer.enabled = true;
                    camera.Render();
                    Graphics.Blit(msaaTarget, sampleTarget);
                    ReadRenderTexture(sampleTarget, withBeams);
                    foreach (var renderer in beamRenderers)
                        renderer.enabled = false;
                    camera.Render();
                    Graphics.Blit(msaaTarget, sampleTarget);
                    ReadRenderTexture(sampleTarget, withoutBeams);
                    var (msaaPixels, msaaMaxRed, msaaRed, msaaBlue) =
                        MeasureSurface(withBeams, withoutBeams);
                    Debug.Log($"[Spells527] msaa4SurfacePixels={msaaPixels} " +
                        $"maxRed={msaaMaxRed} blueRedRatio={msaaBlue / msaaRed} " +
                        $"redTotal={msaaRed} blueTotal={msaaBlue}");
                }
                finally
                {
                    camera.targetTexture = sampleTarget;
                    Object.Destroy(msaaTarget);
                }
                // FinalBurstPhysicalLaserSurfaceHasWhiteCore: Beat Saber 1.44.1's
                // exported gameplay camera has m_HDR=0; CM's HDR target loses the
                // pale core while the same beam and fog render paler in LDR.
                Assert.That(camera.allowHDR, Is.False,
                    "Playing mode must use the game's LDR gameplay-camera path.");
                // FinalBurstPhysicalLaserSurfaceHasWhiteCore: the gameplay
                // override must leave the mapper's editing camera untouched.
                Assert.That(cameraManager.CameraControllers[0].Camera.allowHDR, Is.True);
                Assert.That(litPixels, Is.GreaterThan(40),
                    "The beat-527 test did not isolate enough physical laser pixels.");
                // FinalBurstPhysicalLaserSurfaceHasWhiteCore: the game's pale
                // core requires a measurable neutral contribution in addition
                // to the orange source color; the HDR preview fell below 0.04.
                Assert.That(blueTotal / redTotal, Is.GreaterThan(0.1f),
                    "The final physical beams lost their pale core in Playing mode.");
            }
            finally
            {
                for (var i = 0; i < beamRenderers.Length; i++)
                    beamRenderers[i].enabled = wasEnabled[i];
                camera.targetTexture = previousCameraTarget;
                Object.Destroy(sampleTarget);
                Object.Destroy(withBeams);
                Object.Destroy(withoutBeams);
            }
        }

        private static void ReadRenderTexture(RenderTexture source, Texture2D destination)
        {
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destination.Apply();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private static (int LitPixels, float MaxRed, float RedTotal, float BlueTotal)
            MeasureSurface(Texture2D withBeams, Texture2D withoutBeams)
        {
            var redTotal = 0f;
            var blueTotal = 0f;
            var litPixels = 0;
            var maxRed = 0f;
            // FinalBurstPhysicalLaserSurfaceHasWhiteCore: renderer subtraction
            // identifies beam pixels across the whole view despite distant placement.
            for (var y = 100; y < 900; y++)
            {
                for (var x = 0; x < 1024; x++)
                {
                    var physical = withBeams.GetPixel(x, y) - withoutBeams.GetPixel(x, y);
                    var red = physical.r * 255f;
                    if (red < 16f)
                        continue;
                    redTotal += red;
                    blueTotal += physical.b * 255f;
                    litPixels++;
                    maxRed = Mathf.Max(maxRed, red);
                }
            }
            return (litPixels, maxRed, redTotal, blueTotal);
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            // FinalBurstPhysicalLaserSurfaceHasWhiteCore: failed assertions still
            // restore the shared camera before the next map-load teardown runs.
            if (playingCamera != null)
            {
                playingCamera.transform.rotation = playingRotationBeforeTest;
                playingCamera.aspect = playingAspectBeforeTest;
            }
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = animationsBeforeTest;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
