using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // TEMP machine-specific full-map diagnostic for the Spells beat-275 left white laser:
    // the authored b275 type-1 event drives lightID 2000 on the generated Quad
    // "modelScene0_solid1_0" (env idx49). Game shows the physical beam + fog bloom;
    // CM Playing reportedly shows only the fog. Diagnostic only — no parity assertions
    // beyond "the exact target exists"; A/B pixel counts are logged for lead review.
    [Explicit]
    public class SpellsBeat275FullMapParityTest : TestBase
    {
        private const string SourceMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/" +
            "35a0b (Spells - Joetastic & Swifter)/ExpertPlusStandard.dat";
        private const string OutputDir = "C:/Users/tdrak/AppData/Local/Temp/devin-spells275";

        private bool animationsBeforeTest;
        private float playerFovBeforeTest;
        private float playerOffsetBeforeTest;
        private float cameraFovBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            playerFovBeforeTest = Settings.Instance.PlayerCameraFOV;
            playerOffsetBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            cameraFovBeforeTest = Settings.Instance.CameraFOV;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(SourceMapPath)),
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

        [UnityTest]
        public IEnumerator Beat275Id2000BeamAndFogDiagnostics()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var report = new StringBuilder();
            Directory.CreateDirectory(OutputDir);

            IEnumerator SeekTo(float beat)
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
            }

            GeometryContainer FindTrack(string track) =>
                Object.FindAnyObjectByType<GeometryGridContainer>()
                    .LoadedContainers.Values
                    .OfType<GeometryContainer>()
                    .FirstOrDefault(c => c.EnvironmentEnhancement != null
                        && c.EnvironmentEnhancement.Track == track);

            void Describe(StringBuilder sb, string label, GeometryContainer container)
            {
                sb.AppendLine($"--- {label} ---");
                if (container == null)
                {
                    sb.AppendLine("  NOT GENERATED");
                    return;
                }
                var t = container.transform;
                var chain = new StringBuilder();
                for (var p = t; p != null; p = p.parent)
                    chain.Insert(0,
                        $"{p.name}(localPos={p.localPosition} localScale={p.localScale} worldPos={p.position} lossy={p.lossyScale} active={p.gameObject.activeInHierarchy}) > ");
                sb.AppendLine($"  hierarchy: {chain}");
                var renderers = container.GetComponentsInChildren<Renderer>(true);
                sb.AppendLine($"  renderers: {renderers.Length}");
                foreach (var r in renderers)
                {
                    var vp = camera.WorldToViewportPoint(r.bounds.center);
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    var col = mpb.GetColor("_Color");
                    var lit = mpb.GetFloat("_Lit");
                    sb.AppendLine($"    {r.name}: enabled={r.enabled} active={r.gameObject.activeInHierarchy} " +
                        $"bounds min={r.bounds.min} max={r.bounds.max} " +
                        $"viewportCenter={vp} shader={r.sharedMaterial.shader.name} " +
                        $"material={r.sharedMaterial.name} mpbColor={col} mpbLit={lit}");
                }
                if (container.MpbController != null && container.MpbController.Mpb != null)
                {
                    sb.AppendLine($"  containerMpb: _Color={container.MpbController.Mpb.GetColor("_Color")} " +
                        $"_Lit={container.MpbController.Mpb.GetFloat("_Lit")}");
                }
                var controllers = container.GetComponentsInChildren<ParametricBloomFogLightController>(true);
                foreach (var light in controllers)
                {
                    var box = light.BoxLight;
                    var fog = light.BloomFog;
                    sb.AppendLine($"    controller {light.name}: Type={light.Type} ID={light.ID} " +
                        $"Color={light.Color} ColorAlphaMultiplier={light.ColorAlphaMultiplier} " +
                        $"BloomFogIntensityMultiplier={light.BloomFogIntensityMultiplier} " +
                        $"EnabledRenderers={light.EnabledRenderers} IsPhysical={light.IsPhysical}");
                    if (box != null)
                        sb.AppendLine($"      box: Width={box.Width} Height={box.Height} Length={box.Length} " +
                            $"renderer={(box.Renderer == null ? "null" : $"enabled={box.Renderer.enabled} active={box.Renderer.gameObject.activeInHierarchy} bounds={box.Renderer.bounds}")}");
                    if (fog != null)
                        sb.AppendLine($"      bloomFog: enabled={fog.enabled} " +
                            $"cachedTransform={(fog.CachedTransform == null ? "null" : fog.CachedTransform.name)} " +
                            $"intensityMultiplier={fog.IntensityMultiplier} length={fog.Length}");
                }
            }

            var seenFirst27505 = false;
            try
            {
            foreach (var beat in new[] { 274.75f, 275f, 275.05f, 275.4f, 274.75f, 275.05f })
            {
                yield return SeekTo(beat);
                report.AppendLine($"=== beat {beat} camera pos={camera.transform.position} " +
                    $"rot={camera.transform.eulerAngles} fov={camera.fieldOfView} " +
                    $"aspect={camera.aspect} allowHDR={camera.allowHDR} ===");
                report.AppendLine($"  fog globals: attenuation={Shader.GetGlobalFloat("_CustomFogAttenuation")} " +
                    $"startY={Shader.GetGlobalFloat("_CustomFogHeightFogStartY")} " +
                    $"height={Shader.GetGlobalFloat("_CustomFogHeightFogHeight")}");
                Describe(report, "modelScene0_solid1_0 (lightID 2000)", FindTrack("modelScene0_solid1_0"));
                Describe(report, "modelScene0_bloom1_0 (lightID 3000)", FindTrack("modelScene0_bloom1_0"));

                // Controlled A/B capture on the SECOND b275.05 forward sample only.
                var second27505 = Mathf.Approximately(beat, 275.05f) && seenFirst27505;
                if (Mathf.Approximately(beat, 275.05f)) seenFirst27505 = true;
                if (second27505)
                {
                    var target = FindTrack("modelScene0_solid1_0");
                    Assert.That(target, Is.Not.Null,
                        "modelScene0_solid1_0 geometry was not generated — setup ambiguity.");
                    // Authored lightID 2000 maps to a per-type runtime ID (observed 32); the
                    // controller under this exact enhancement is the authored lightID-2000 one.
                    var controller = target.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                        .FirstOrDefault();
                    Assert.That(controller, Is.Not.Null,
                        "no ParametricBloomFogLightController under the target geometry.");
                    Assert.That(controller.BoxLight, Is.Not.Null, "ID2000 has no BoxLight.");
                    Assert.That(controller.BoxLight.Renderer, Is.Not.Null, "ID2000 BoxLight has no Renderer.");

                    var captureFormat = camera.allowHDR
                        ? RenderTextureFormat.DefaultHDR
                        : RenderTextureFormat.ARGB32;
                    var rt = new RenderTexture(1024, 576, 24, captureFormat);
                    var normal = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                    var boxOff = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                    var fogOff = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                    var previousTarget = camera.targetTexture;
                    var previousFogEnabled = controller.BloomFog != null && controller.BloomFog.enabled;
                    var previousBoxEnabled = controller.BoxLight.Renderer.enabled;
                    try
                    {
                        camera.targetTexture = rt;
                        camera.Render();
                        ReadRenderTexture(rt, normal);
                        File.WriteAllBytes(Path.Combine(OutputDir, "spells-b275-normal.png"),
                            normal.EncodeToPNG());

                        controller.BoxLight.Renderer.enabled = false;
                        camera.Render();
                        ReadRenderTexture(rt, boxOff);
                        File.WriteAllBytes(Path.Combine(OutputDir, "spells-b275-box-off.png"),
                            boxOff.EncodeToPNG());
                        controller.BoxLight.Renderer.enabled = previousBoxEnabled;

                        if (controller.BloomFog != null)
                        {
                            controller.BloomFog.enabled = false;
                            camera.Render();
                            ReadRenderTexture(rt, fogOff);
                            File.WriteAllBytes(Path.Combine(OutputDir, "spells-b275-fog-off.png"),
                                fogOff.EncodeToPNG());
                            controller.BloomFog.enabled = previousFogEnabled;
                        }
                    }
                    finally
                    {
                        controller.BoxLight.Renderer.enabled = previousBoxEnabled;
                        if (controller.BloomFog != null)
                            controller.BloomFog.enabled = previousFogEnabled;
                        camera.targetTexture = previousTarget;
                        Object.Destroy(rt);
                    }

                    // Delta stats — logged, not asserted.
                    ReportDelta(report, "box-on vs box-off", normal, boxOff);
                    if (controller.BloomFog != null)
                        ReportDelta(report, "box-on vs fog-off", normal, fogOff);
                    Object.Destroy(normal);
                    Object.Destroy(boxOff);
                    Object.Destroy(fogOff);
                }
            }
            }
            finally
            {
                var reportPath = Path.Combine(OutputDir, "spells-b275-full-map-runtime.txt");
                File.WriteAllText(reportPath, report.ToString());
                Debug.Log($"[Spells275] report: {reportPath}");
            }
        }

        // TEMP isolation matrix for the invisible b275 box: the Quad renders 0 pixels while
        // fog renders. Discriminates back-face culling (_CullMode=2 vs 0), HEIGHT_FOG keyword,
        // and missing material by cloning the shared material per variant — no asset writes.
        [UnityTest]
        public IEnumerator Beat275QuadCullingAndFogIsolationDiagnostics()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var report = new StringBuilder();
            Directory.CreateDirectory(OutputDir);

            atsc.MoveToJsonTime(274.75f);
            yield return null;
            yield return null;
            atsc.MoveToJsonTime(275.05f);
            yield return null;
            yield return null;

            var target = Object.FindAnyObjectByType<GeometryGridContainer>()
                .LoadedContainers.Values
                .OfType<GeometryContainer>()
                .FirstOrDefault(c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "modelScene0_solid1_0");
            Assert.That(target, Is.Not.Null,
                "modelScene0_solid1_0 geometry was not generated — setup ambiguity.");
            var controller = target.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                .FirstOrDefault();
            Assert.That(controller, Is.Not.Null, "no controller under target geometry.");
            var renderer = controller.BoxLight.Renderer;
            var fog = controller.BloomFog;
            var material = renderer.sharedMaterial;
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

            report.AppendLine($"camera pos={camera.transform.position} rot={camera.transform.eulerAngles} " +
                $"fov={camera.fieldOfView} aspect={camera.aspect} allowHDR={camera.allowHDR}");
            report.AppendLine($"material={material.name} shader={material.shader.name}");
            if (material.HasProperty("_CullMode"))
                report.AppendLine($"  _CullMode={material.GetFloat("_CullMode")}");
            foreach (var p in new[] { "_EnableHeightFog", "_FogHeightScale", "_FogHeightOffset",
                    "_FogScale", "_FogStartOffset" })
                if (material.HasProperty(p))
                    report.AppendLine($"  {p}={material.GetFloat(p)}");
            report.AppendLine($"  HEIGHT_FOG keyword={material.IsKeywordEnabled("HEIGHT_FOG")}");
            if (mesh != null && mesh.normals.Length > 0)
            {
                var worldNormal = renderer.transform.TransformDirection(mesh.normals[0]);
                var toCamera = camera.transform.position - renderer.bounds.center;
                report.AppendLine($"  mesh normal local={mesh.normals[0]} world={worldNormal} " +
                    $"dot(normal,toCamera)={Vector3.Dot(worldNormal, toCamera):F3}");
            }
            report.AppendLine($"  bounds={renderer.bounds} viewportCenter=" +
                $"{camera.WorldToViewportPoint(renderer.bounds.center)}");
            report.AppendLine($"  fog globals attenuation={Shader.GetGlobalFloat("_CustomFogAttenuation")} " +
                $"startY={Shader.GetGlobalFloat("_CustomFogHeightFogStartY")} " +
                $"height={Shader.GetGlobalFloat("_CustomFogHeightFogHeight")}");

            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
            var rt = new RenderTexture(1024, 576, 24, format);
            var previousTarget = camera.targetTexture;
            var previousBoxEnabled = renderer.enabled;
            var previousFogEnabled = fog != null && fog.enabled;
            Material clone = null;
            var textures = new List<Texture2D>();
            Texture2D Capture(string name)
            {
                camera.Render();
                var tex = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                textures.Add(tex);
                ReadRenderTexture(rt, tex);
                File.WriteAllBytes(Path.Combine(OutputDir, name), tex.EncodeToPNG());
                return tex;
            }
            try
            {
                camera.targetTexture = rt;

                var normal = Capture("spells-b275-isolate-normal.png");
                renderer.enabled = false;
                var boxOff = Capture("spells-b275-isolate-box-off.png");
                renderer.enabled = true;
                if (fog != null) fog.enabled = false;
                var fogOffBoxOn = Capture("spells-b275-isolate-fogoff-boxon.png");
                renderer.enabled = false;
                var fogOffBoxOff = Capture("spells-b275-isolate-fogoff-boxoff.png");
                renderer.enabled = true;
                if (fog != null) fog.enabled = previousFogEnabled;

                ReportDelta(report, "A normal vs B box-off", normal, boxOff);
                ReportDelta(report, "C fog-off box-on vs D fog-off box-off", fogOffBoxOn, fogOffBoxOff);

                // Material variants: clone per variant, restore sharedMaterial in finally.
                Texture2D Variant(bool cullOff, bool heightFogOff, string name, bool boxOn)
                {
                    if (clone != null) Object.Destroy(clone);
                    clone = new Material(material);
                    if (cullOff && clone.HasProperty("_CullMode"))
                        clone.SetFloat("_CullMode", 0f);
                    if (heightFogOff)
                    {
                        clone.DisableKeyword("HEIGHT_FOG");
                        if (clone.HasProperty("_EnableHeightFog"))
                            clone.SetFloat("_EnableHeightFog", 0f);
                    }
                    renderer.sharedMaterial = clone;
                    renderer.enabled = boxOn;
                    var tex = Capture(name);
                    renderer.sharedMaterial = material;
                    return tex;
                }

                var cullOn = Variant(true, false, "spells-b275-isolate-culloff-boxon.png", true);
                var cullOffTex = Variant(true, false, "spells-b275-isolate-culloff-boxoff.png", false);
                renderer.enabled = true;
                ReportDelta(report, "E culloff box-on vs F culloff box-off", cullOn, cullOffTex);

                var hfOn = Variant(false, true, "spells-b275-isolate-heightoff-boxon.png", true);
                var hfOff = Variant(false, true, "spells-b275-isolate-heightoff-boxoff.png", false);
                renderer.enabled = true;
                ReportDelta(report, "G heightfog-off box-on vs H box-off", hfOn, hfOff);

                var bothOn = Variant(true, true, "spells-b275-isolate-bothoff-boxon.png", true);
                var bothOff = Variant(true, true, "spells-b275-isolate-bothoff-boxoff.png", false);
                renderer.enabled = true;
                ReportDelta(report, "I cull+heightfog-off box-on vs J box-off", bothOn, bothOff);
            }
            finally
            {
                renderer.sharedMaterial = material;
                if (clone != null) Object.Destroy(clone);
                renderer.enabled = previousBoxEnabled;
                if (fog != null) fog.enabled = previousFogEnabled;
                camera.targetTexture = previousTarget;
                Object.Destroy(rt);
                foreach (var t in textures) Object.Destroy(t);
            }

            var reportPath = Path.Combine(OutputDir, "spells-b275-isolation.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[Spells275] isolation report: {reportPath}");
        }

        // Behavioral regression for the user's report: the authored b275 white flash on
        // lightID 2000 should render a physical beam surface (left half) AND its fog bloom.
        // Was RED: the Quad's face was culled (shared material _CullMode=2, authored rotation
        // leaves the front normal away from camera) so box on/off changed 0 pixels while fog
        // toggling changed ~95k; fixed by GeometryAppearanceSO's cull-off material variant for
        // TransparentLight Quad geometry. Fog leg stays GREEN either way.
        [UnityTest]
        public IEnumerator Beat275LeftSolidQuadRendersPhysicalSurfaceAndFog()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var report = new StringBuilder();
            var run = System.DateTime.UtcNow.Ticks;
            Directory.CreateDirectory(OutputDir);

            atsc.MoveToJsonTime(274.75f);
            yield return null;
            yield return null;

            var target = Object.FindAnyObjectByType<GeometryGridContainer>()
                .LoadedContainers.Values
                .OfType<GeometryContainer>()
                .FirstOrDefault(c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "modelScene0_solid1_0");
            Assert.That(target, Is.Not.Null,
                "modelScene0_solid1_0 geometry was not generated — setup ambiguity.");
            var controller = target.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                .FirstOrDefault();
            Assert.That(controller, Is.Not.Null, "no controller under target geometry.");
            Assert.That(controller.BoxLight, Is.Not.Null, "target has no BoxLight.");
            var renderer = controller.BoxLight.Renderer;
            var fog = controller.BloomFog;
            Assert.That(renderer, Is.Not.Null, "target BoxLight has no Renderer.");
            Assert.That(fog, Is.Not.Null, "target has no BloomFog object.");

            // Before the b275 flash the authored light is off.
            Assert.That(controller.Color.a, Is.LessThanOrEqualTo(0.02f),
                $"light off before b275 expected alpha<=0.02, got {controller.Color.a}");

            atsc.MoveToJsonTime(275.05f);
            yield return null;
            yield return null;

            Assert.That(renderer.enabled && renderer.gameObject.activeInHierarchy, Is.True,
                "BoxLight renderer should be enabled and active at b275.05.");
            Assert.That(fog.enabled, Is.True, "BloomFog should be enabled at b275.05.");
            Assert.That(controller.Color.a, Is.GreaterThanOrEqualTo(0.4f),
                $"b275 flash should drive color alpha >=0.4, got {controller.Color.a}");

            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;
            var rt = new RenderTexture(1024, 576, 24, format);
            var previousTarget = camera.targetTexture;
            var previousBoxEnabled = renderer.enabled;
            var previousFogEnabled = fog.enabled;
            Texture2D normal = null, boxOff = null, fogOff = null;
            var failures = new List<string>();
            try
            {
                camera.targetTexture = rt;
                normal = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                boxOff = new Texture2D(1024, 576, TextureFormat.RGBA32, false);
                fogOff = new Texture2D(1024, 576, TextureFormat.RGBA32, false);

                camera.Render();
                ReadRenderTexture(rt, normal);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-b275-parity-{run}-normal.png"),
                    normal.EncodeToPNG());

                renderer.enabled = false;
                camera.Render();
                ReadRenderTexture(rt, boxOff);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-b275-parity-{run}-boxoff.png"),
                    boxOff.EncodeToPNG());
                renderer.enabled = true;

                fog.enabled = false;
                camera.Render();
                ReadRenderTexture(rt, fogOff);
                File.WriteAllBytes(
                    Path.Combine(OutputDir, $"spells-b275-parity-{run}-fogoff.png"),
                    fogOff.EncodeToPNG());
                fog.enabled = previousFogEnabled;

                var boxChanged = CountRegion(report, "box-on vs box-off", normal, boxOff, 0, 512, "left-half");
                CountRegion(report, "box-on vs box-off", normal, boxOff, 0, 1024, "full");
                var fogChanged = CountRegion(report, "normal vs fog-off", normal, fogOff, 0, 1024, "full");
                CountRegion(report, "normal vs fog-off", normal, fogOff, 0, 512, "left-half");

                if (controller.BoxLight.Renderer.sharedMaterial.HasProperty("_CullMode"))
                    report.AppendLine($"material _CullMode={controller.BoxLight.Renderer.sharedMaterial.GetFloat("_CullMode")}");
                report.AppendLine($"camera pos={camera.transform.position} rot={camera.transform.eulerAngles} " +
                    $"fov={camera.fieldOfView} aspect={camera.aspect} allowHDR={camera.allowHDR}");

                if (boxChanged < 200)
                    failures.Add(
                        $"physical beam invisible in left half: box on/off changed {boxChanged} pixels, " +
                        "expected >=200 (back-facing Quad is culled).");
                if (fogChanged < 1000)
                    failures.Add(
                        $"fog bloom missing: fog on/off changed {fogChanged} pixels, expected >=1000.");
            }
            finally
            {
                renderer.enabled = previousBoxEnabled;
                fog.enabled = previousFogEnabled;
                camera.targetTexture = previousTarget;
                Object.Destroy(rt);
                if (normal != null) Object.Destroy(normal);
                if (boxOff != null) Object.Destroy(boxOff);
                if (fogOff != null) Object.Destroy(fogOff);
            }

            var reportPath = Path.Combine(OutputDir, $"spells-b275-parity-{run}-report.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[Spells275] parity report: {reportPath}");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // TEMP orientation check: the culling fix assumes the Quad's authored rotation reaches
        // the generated mesh unchanged (the single face is legitimately back-facing). Verify the
        // authored euler survives load and that the face really points away — rules a rotation
        // bug coexisting with the culled material.
        [UnityTest]
        public IEnumerator Beat275GeneratedQuadKeepsAuthoredRotation()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var camera = cameraManager.CameraControllers[1].Camera;
            var report = new StringBuilder();
            Directory.CreateDirectory(OutputDir);
            var run = System.DateTime.UtcNow.Ticks;

            var literal = new Vector3(-11.009871f, 62.107605f, -178.364471f);
            var target = Object.FindAnyObjectByType<GeometryGridContainer>()
                .LoadedContainers.Values
                .OfType<GeometryContainer>()
                .FirstOrDefault(c => c.EnvironmentEnhancement != null
                    && c.EnvironmentEnhancement.Track == "modelScene0_solid1_0");
            Assert.That(target, Is.Not.Null, "modelScene0_solid1_0 not generated.");
            var controller = target.GetComponentsInChildren<ParametricBloomFogLightController>(true)
                .FirstOrDefault();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.BoxLight, Is.Not.Null);
            var renderer = controller.BoxLight.Renderer;
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            Assert.That(target.EnvironmentEnhancement.Rotation, Is.Not.Null,
                "enhancement has no authored Rotation.");

            foreach (var beat in new[] { 274.75f, 275.05f, 275.4f, 274.75f })
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var authored = target.EnvironmentEnhancement.Rotation.Value;
                var worldRot = renderer.transform.rotation;
                var angle = Quaternion.Angle(worldRot, Quaternion.Euler(literal));
                var dot = Vector3.Dot(
                    renderer.transform.TransformDirection(mesh.normals[0]),
                    camera.transform.position - renderer.bounds.center);
                report.AppendLine($"beat {beat}: authoredEuler={authored} literal={literal} " +
                    $"actualWorldEuler={worldRot.eulerAngles} angleVsAuthored={angle:F3}deg " +
                    $"cameraPos={camera.transform.position} faceDotToCamera={dot:F3}");
                Assert.That(
                    Vector3.Distance(authored, literal), Is.LessThanOrEqualTo(0.01f),
                    $"beat {beat}: parsed authored rotation {authored} differs from source literal.");
                Assert.That(angle, Is.LessThanOrEqualTo(0.2f),
                    $"beat {beat}: generated Quad world rotation differs from authored euler by " +
                    $"{angle}deg — a parent/rotation transform bug may coexist with the cull fix.");
                Assert.That(dot, Is.LessThan(0f),
                    $"beat {beat}: expected the Quad face to be back-facing (dot<0), got {dot}.");
            }

            var reportPath = Path.Combine(OutputDir, $"spells-b275-orientation-{run}.txt");
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[Spells275] orientation report: {reportPath}");
        }

        private static void ReportDelta(StringBuilder sb, string label, Texture2D a, Texture2D b)
        {
            CountRegion(sb, label, a, b, 0, 1024, "full");
            CountRegion(sb, label, a, b, 0, 512, "left-half");
        }

        private static int CountRegion(
            StringBuilder sb, string label, Texture2D a, Texture2D b,
            int x0, int x1, string region)
        {
            var count = 0;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (var y = 0; y < 576; y++)
            for (var x = x0; x < x1; x++)
            {
                var ca = a.GetPixel(x, y);
                var cb = b.GetPixel(x, y);
                var delta = Mathf.RoundToInt(255f *
                    (Mathf.Abs(ca.r - cb.r) + Mathf.Abs(ca.g - cb.g) + Mathf.Abs(ca.b - cb.b)));
                if (delta < 24) continue;
                count++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            sb.AppendLine($"  delta {label} {region}: pixels>=24: {count} " +
                (count > 0 ? $"bbox x[{minX}..{maxX}] y[{minY}..{maxY}]" : "bbox none"));
            return count;
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

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = playerFovBeforeTest;
            Settings.Instance.PlayerCameraOffsetZ = playerOffsetBeforeTest;
            Settings.Instance.CameraFOV = cameraFovBeforeTest;
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
