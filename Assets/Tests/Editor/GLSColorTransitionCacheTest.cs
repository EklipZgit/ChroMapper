using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // Lock down ribbon-source retention and cache rewiring before replacing the viewport scans with indexes.
    public class GLSColorTransitionCacheTest : TestBase
    {
        // StrobingTransitionRibbonUsesDestinationEasedPhaseColor reads the exact shader inputs that enable the ribbon's phase path.
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int easingId = Shader.PropertyToID("_EasingID");
        private static readonly int strobeColorAId = Shader.PropertyToID("_StrobeColorA");
        private static readonly int strobeColorBId = Shader.PropertyToID("_StrobeColorB");
        private static readonly int strobeFadeId = Shader.PropertyToID("_StrobeFade");
        private static readonly int strobeFrequencyAId = Shader.PropertyToID("_StrobeFrequencyA");
        private static readonly int strobeFrequencyBId = Shader.PropertyToID("_StrobeFrequencyB");
        private static readonly int strobeDurationId = Shader.PropertyToID("_StrobeDuration");
        private static readonly int useStrobeColorsId = Shader.PropertyToID("_UseStrobeColors");
        // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips reads the endpoint lookup that lets one ribbon carry every controlled light.
        private static readonly int lightDistributionTextureId = Shader.PropertyToID("_LightDistributionTex");
        private static readonly int lightDistributionWidthId = Shader.PropertyToID("_LightDistributionWidth");
        private static readonly int useLightDistributionId = Shader.PropertyToID("_UseLightDistribution");
        // WaveMapShiftStripsRenderThroughRealShader copies the produced lerp mode into a plain sample material.
        private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");

        private BaseDifficulty originalMap;

        [OneTimeSetUp]
        public void CaptureOriginalMap()
        {
            // Restore the scene-owned map after this isolated cache fixture so subsequent editor tests keep their shared state.
            originalMap = BeatSaberSongContainer.Instance.Map;
        }

        [OneTimeTearDown]
        public void RestoreOriginalMap()
        {
            // Avoid leaking the synthetic cache fixture map into test fixtures that run after this one.
            BeatSaberSongContainer.Instance.Map = originalMap;
        }

        // StrobingTransitionRibbonUsesDestinationEasedPhaseColor covers both endpoints because either one must enable the ribbon's LightColorTween phase path.
        [TestCase(true)]
        [TestCase(false)]
        public void StrobingTransitionRibbonUsesDestinationEasedPhaseColor(bool sourceStrobing)
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(1f, 0.25f, 0.5f, 1f);
            source.Brightness = 1f;
            source.StrobeColor = new Color(0.8f, 0.2f, 0.4f, 1f);
            source.StrobeBrightness = 0.5f;
            source.Frequency = sourceStrobing ? 2 : 0;
            transition.CustomColor = new Color(0.25f, 0.5f, 1f, 1f);
            transition.Brightness = 1f;
            transition.StrobeColor = new Color(0.2f, 0.6f, 1f, 1f);
            transition.StrobeBrightness = 0.75f;
            transition.Frequency = sourceStrobing ? 0 : 2;
            transition.Easing = (int)EaseType.InQuadratic;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS strobe ribbon test");

            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 0);

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(useStrobeColorsId),
                    Is.EqualTo(1f),
                    sourceStrobing
                        ? "A strobing source must enable the transition ribbon's phase path."
                        : "A strobing destination must enable the transition ribbon's phase path.");
                Assert.That(
                    properties.GetFloat(useLightDistributionId),
                    Is.EqualTo(0f),
                    "A ribbon without a known physical light count must retain the two-color shader path.");
                Assert.That(properties.GetInt(easingId), Is.EqualTo(Easing.EasingShaderId("easeInQuad")));
                var easedProgress = Easing.Quadratic.In(0.5f);
                var renderedStrobeColor = Color.LerpUnclamped(
                    properties.GetColor(strobeColorAId),
                    properties.GetColor(strobeColorBId),
                    easedProgress);
                // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips keeps fallback ribbon endpoints in renderer space so their alpha carries each event's authored strobe brightness.
                var expectedStrobeColor = Color.LerpUnclamped(
                    BasicEventColorLerp.ApplyBrightness(
                        source.StrobeColor.Value,
                        source.StrobeBrightness),
                    BasicEventColorLerp.ApplyBrightness(
                        transition.StrobeColor.Value,
                        transition.StrobeBrightness),
                    easedProgress);
                AssertColor(renderedStrobeColor, expectedStrobeColor);
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint requires every fragment to use LightColorTween's integrated frequency phase and cubic in/out strobe blend.
        [Test]
        public void StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(4f, 0, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(1f, 0.2f, 0.1f, 1f);
            source.Brightness = 0.5f;
            source.StrobeColor = new Color(0.1f, 0.8f, 0.2f, 1f);
            source.StrobeBrightness = 0.75f;
            source.Frequency = 1;
            transition.CustomColor = new Color(0.2f, 0.1f, 1f, 1f);
            transition.Brightness = 1f;
            transition.StrobeColor = new Color(1f, 0.4f, 0.1f, 1f);
            transition.StrobeBrightness = 0.25f;
            transition.Frequency = 3;
            transition.StrobeFade = 1;
            transition.Easing = (int)EaseType.InQuadratic;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS strobe fade ribbon test");

            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 0);

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(strobeFadeId),
                    Is.EqualTo(1f),
                    "A strobe-fade transition must enable the ribbon's continuous phase blend.");
                Assert.That(properties.GetFloat(strobeFrequencyAId), Is.EqualTo(1f));
                Assert.That(properties.GetFloat(strobeFrequencyBId), Is.EqualTo(3f));
                Assert.That(properties.GetFloat(strobeDurationId), Is.EqualTo(3f));

                const float progress = 0.5f;
                var easedProgress = Easing.Quadratic.In(progress);
                var normalColor = Color.LerpUnclamped(
                    properties.GetColor(colorAId),
                    properties.GetColor(colorBId),
                    easedProgress);
                var strobeColor = Color.LerpUnclamped(
                    properties.GetColor(strobeColorAId),
                    properties.GetColor(strobeColorBId),
                    easedProgress);
                var duration = properties.GetFloat(strobeDurationId);
                var elapsed = progress * duration;
                var elapsedHalf = (elapsed * elapsed) / (2f * duration);
                var phase = (((0f - properties.GetFloat(strobeFrequencyAId)) * elapsedHalf)
                        + (properties.GetFloat(strobeFrequencyAId) * elapsed)
                        + (properties.GetFloat(strobeFrequencyBId) * elapsedHalf))
                    % 1f;
                var fade = Easing.Cubic.InOut(1f - Mathf.Abs((phase * 2f) - 1f));
                Assert.That(fade, Is.EqualTo(0.5f).Within(0.0001f));
                var ribbonColor = Color.LerpUnclamped(normalColor, strobeColor, fade);
                var tween = new LightColorTween
                {
                    StartTimeAlpha = source.SongBpmTime,
                    StartTimeColor = source.SongBpmTime,
                    StartColor = properties.GetColor(colorAId),
                    StartAlpha = 1f,
                    StartStrobeFrequency = source.Frequency,
                    StartStrobeBrightness = 1f,
                    StartStrobeColor = properties.GetColor(strobeColorAId),
                    EndTimeAlpha = transition.SongBpmTime,
                    EndTimeColor = transition.SongBpmTime,
                    EndColor = properties.GetColor(colorBId),
                    EndAlpha = 1f,
                    EndStrobeFrequency = transition.Frequency,
                    EndStrobeBrightness = 1f,
                    EndStrobeColor = properties.GetColor(strobeColorBId),
                    StrobeFade = true,
                    Easing = Easing.Quadratic.In,
                    ColorLerpType = BasicEventColorLerpType.RGB
                };
                tween.UpdateTime(Mathf.Lerp(source.SongBpmTime, transition.SongBpmTime, progress));

                AssertColor(ribbonColor, tween.Color);
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips requires a four-row endpoint lookup so each light strip can reproduce its own normal and strobe transition.
        [Test]
        public void LightIdTransitionRibbonSplitsIntoPerLightShiftStrips()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    filterParam: 1,
                    boxCustomData: CreateShiftCustomData(
                        new[] { "r,0.5,lin,l" },
                        new[] { "b,0.25,lin,l" }),
                    filterType: (int)IndexFilterType.Division),
                CreateGroup(
                    5f,
                    0,
                    1,
                    filterParam: 1,
                    boxCustomData: CreateShiftCustomData(
                        new[] { "g,0.75,lin,l" },
                        new[] { "r,0.6,lin,l" }),
                    filterType: (int)IndexFilterType.Division)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            source.Brightness = 0.5f;
            source.StrobeColor = new Color(0.2f, 0.1f, 0.4f, 1f);
            source.StrobeBrightness = 0.4f;
            source.Frequency = 1;
            transition.CustomColor = new Color(0.2f, 0.1f, 1f, 1f);
            transition.Brightness = 1f;
            transition.StrobeColor = new Color(0.9f, 0.3f, 0.2f, 1f);
            transition.StrobeBrightness = 0.75f;
            transition.Frequency = 2;
            transition.Easing = (int)EaseType.InQuadratic;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS light-id ribbon test");

            try
            {
                // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips distinguishes renderer color resolution from filter and texture staging failures.
                AssertColor(
                    GLSEventCommon.GetLightColor(source, false, appearance),
                    new Color(0.1f, 0.2f, 0.3f, 0.5f));
                var sourceMainColors = new Color[4];
                var sourceStrobeColors = new Color[4];
                Assert.That(
                    GLSEventCommon.PopulateColorTransitionEndpoint(
                        source,
                        4,
                        false,
                        appearance,
                        sourceMainColors,
                        sourceStrobeColors),
                    Is.True);
                AssertColor(
                    sourceMainColors[0],
                    new Color(0.1f, 0.2f, 0.3f, 0.5f),
                    $"source main endpoint table: {string.Join(", ", sourceMainColors.Select(x => GLSEventCommon.FormatColor(x)))}");
                var controller = CreateRibbonController(ribbonObject, out var renderer);

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 4);

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(useLightDistributionId),
                    Is.EqualTo(1f),
                    "A filtered GLS transition must upload one endpoint strip per physical light.");
                Assert.That(properties.GetFloat(lightDistributionWidthId), Is.EqualTo(4f));
                // Per-light frequency/easing rows now own strobe evaluation instead of the legacy scalar flag.
                Assert.That(properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")), Is.EqualTo(1f));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(4));
                Assert.That(texture.height, Is.EqualTo(9));
                var textureDump = DescribeTexture(texture);
                for (var lightIndex = 0; lightIndex < 4; lightIndex++)
                {
                    var lightProgress = lightIndex / 3f;
                    var textureX = 3 - lightIndex;
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        new Color(0.1f + (0.5f * lightProgress), 0.2f, 0.3f, 0.5f),
                        textureDump,
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        new Color(0.2f, 0.1f + (0.75f * lightProgress), 1f, 1f),
                        textureDump,
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        new Color(0.2f, 0.1f, 0.4f + (0.25f * lightProgress), 0.4f),
                        textureDump,
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 3),
                        new Color(0.9f + (0.6f * lightProgress), 0.3f, 0.2f, 0.75f),
                        textureDump,
                        0.001f);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // LightIdTransitionRibbonKeepsSparseLanesBlackWithoutShifts verifies that light-ID lane control alone is enough to split the ribbon and leave unselected lanes dark.
        [Test]
        public void LightIdTransitionRibbonKeepsSparseLanesBlackWithoutShifts()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2,
                    filterChunks: 0,
                    brightnessDistribution: 0f),
                CreateGroup(
                    5f,
                    0,
                    1,
                    filterParam: 0,
                    filterType: (int)IndexFilterType.StepAndOffset,
                    filterParam1: 2,
                    filterChunks: 0,
                    brightnessDistribution: 0f)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(0.4f, 0.1f, 0.1f, 1f);
            source.Brightness = 0.5f;
            transition.CustomColor = new Color(0.1f, 0.1f, 0.4f, 1f);
            transition.Brightness = 1f;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS sparse light-id ribbon test");

            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 4);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                Assert.That(properties.GetFloat(useStrobeColorsId), Is.EqualTo(0f));
                Assert.That(texture, Is.Not.Null);
                var textureDump = DescribeTexture(texture);
                for (var lightIndex = 0; lightIndex < 4; lightIndex++)
                {
                    var textureX = 3 - lightIndex;
                    var selected = lightIndex is 0 or 2;
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        selected
                            ? new Color(0.4f, 0.1f, 0.1f, 0.5f)
                            : Color.black,
                        textureDump);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        selected
                            ? new Color(0.1f, 0.1f, 0.4f, 1f)
                            : Color.black,
                        textureDump,
                        0.001f);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // AllLightTransitionRibbonClearsPooledDistributionState guards uniform endpoint replacement and stale lookup cleanup when a pooled renderer changes transition types.
        [Test]
        public void AllLightTransitionRibbonClearsPooledDistributionState()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    boxCustomData: CreateShiftCustomData(
                        new[] { "r,0.5,lin,l" },
                        System.Array.Empty<string>())),
                CreateGroup(5f, 0, 1),
                CreateGroup(6f, 0, 0, brightnessDistribution: 0f),
                CreateGroup(10f, 0, 1, brightnessDistribution: 0f)));
            var distributedSource = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var uniformSource = map.LightColorEventBoxGroups[2].Boxes[0].Events[0];
            distributedSource.CustomColor = new Color(0.4f, 0.1f, 0.1f, 1f);
            uniformSource.CustomColor = new Color(0.1f, 0.1f, 0.4f, 1f);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var ribbonObject = new GameObject("GLS uniform ribbon test");

            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                var properties = new MaterialPropertyBlock();
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller,
                    distributedSource,
                    appearance,
                    _ => false,
                    4);
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));

                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller,
                    uniformSource,
                    appearance,
                    _ => false,
                    4);
                renderer.GetPropertyBlock(properties);
                // A uniform physical timeline still needs its independent clocks; verify it erased all old shifted endpoint rows.
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                Assert.That(properties.GetFloat(lightDistributionWidthId), Is.EqualTo(4f));
                var uniformTexture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.NotNull(uniformTexture);
                for (var light = 0; light < 4; light++)
                    AssertColor(uniformTexture.GetPixel(light, 0), new Color(0.1f, 0.1f, 0.4f, 1f));
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // OuterGlsNodeTransitionRibbonUsesPhysicalLightStrips proves the outer preview passes its represented event and environment light count into the shared distributed-ribbon path.
        [Test]
        public void OuterGlsNodeTransitionRibbonUsesPhysicalLightStrips()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(
                    1f,
                    0,
                    0,
                    boxCustomData: CreateShiftCustomData(
                        new[] { "r,0.5,lin,l" },
                        System.Array.Empty<string>()),
                    brightnessDistribution: 0f),
                CreateGroup(
                    5f,
                    0,
                    1,
                    boxCustomData: CreateShiftCustomData(
                        new[] { "g,0.75,lin,l" },
                        System.Array.Empty<string>()),
                    brightnessDistribution: 0f)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var transition = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            source.CustomColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            source.Brightness = 0.5f;
            transition.CustomColor = new Color(0.2f, 0.1f, 1f, 1f);
            transition.Brightness = 1f;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var groupAppearance = ScriptableObject.CreateInstance<GLSGroupAppearanceSO>();
            var appearanceField = typeof(GLSGroupAppearanceSO).GetField(
                "eventAppearance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(appearanceField, Is.Not.Null);
            appearanceField.SetValue(groupAppearance, appearance);
            var containerObject = new GameObject("GLS outer transition ribbon test");
            try
            {
                var container = containerObject.AddComponent<GLSGroupContainer>();
                // OuterGlsNodeTransitionRibbonUsesPhysicalLightStrips uses a preview ghost so test cleanup does not require collection singletons.
                var previewGhostField = typeof(GLSGroupContainer).GetField(
                    "isPreviewGhost",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(previewGhostField, Is.Not.Null);
                previewGhostField.SetValue(container, true);
                container.PreviewEventData = source;
                container.GlsLightCount = 4;
                container.lightGradientController = CreateRibbonController(
                    containerObject,
                    out var renderer);
                groupAppearance.UpdateTransitionRibbon(container, _ => false);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                Assert.That(texture, Is.Not.Null);
                AssertColor(
                    texture.GetPixel(3, 0),
                    new Color(0.1f, 0.2f, 0.3f, 0.5f),
                    DescribeTexture(texture),
                    0.001f);
            }
            finally
            {
                Object.DestroyImmediate(containerObject);
                Object.DestroyImmediate(groupAppearance);
                Object.DestroyImmediate(appearance);
            }
        }

        // DistributedStrobeRibbonRendersEveryLightStripAcrossWholeGradient draws isolated UV points so the shader itself proves strip selection and the removed right-half strobe restriction.
        [Test]
        public void DistributedStrobeRibbonRendersEveryLightStripAcrossWholeGradient()
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader)
            {
                enableInstancing = false
            };
            var endpointTexture = new Texture2D(
                4,
                4,
                TextureFormat.RGBAHalf,
                false,
                true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                var textureColors = new Color[16];
                // The shader reads row 0/1 as normal endpoints and row 2/3 as strobe endpoints; lane one is red→green and lane two is blue→blue.
                textureColors[1 + 4] = Color.red;
                textureColors[2 + 4] = Color.blue;
                textureColors[1 + 12] = Color.green;
                textureColors[2 + 12] = Color.blue;
                endpointTexture.SetPixels(textureColors);
                endpointTexture.Apply(false, false);
                material.SetTexture("_LightDistributionTex", endpointTexture);
                material.SetFloat("_UseLightDistribution", 1f);
                material.SetFloat("_LightDistributionWidth", 4f);
                material.SetFloat("_UseStrobeColors", 1f);
                material.SetFloat("_StrobeFade", 0f);
                material.SetFloat("_StrobeDuration", 1f);
                material.SetFloat("_StrobeFrequencyA", 0f);
                material.SetFloat("_StrobeFrequencyB", 1f);
                material.SetFloat("_EasingID", 0f);
                material.SetFloat("_UseHSV", 0f);
                material.SetColor("_ColorA", Color.black);
                material.SetColor("_ColorB", Color.black);
                material.SetColor("_StrobeColorA", Color.black);
                material.SetColor("_StrobeColorB", Color.black);

                var firstNormal = RenderGradientPixel(material, 0.5f, 0.3f);
                var secondNormal = RenderGradientPixel(material, 0.5f, 0.7f);
                var firstStrobe = RenderGradientPixel(material, 1f, 0.3f);
                var secondStrobe = RenderGradientPixel(material, 1f, 0.7f);

                Assert.That(firstNormal.r, Is.GreaterThan(firstNormal.g));
                Assert.That(secondNormal.b, Is.GreaterThan(secondNormal.r));
                Assert.That(firstStrobe.g, Is.GreaterThan(firstStrobe.r));
                Assert.That(secondStrobe.b, Is.GreaterThan(secondStrobe.g));
            }
            finally
            {
                Object.DestroyImmediate(endpointTexture);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void BoundaryQueryReturnsOnlySourcesWhoseTransitionsCrossTheBoundary()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(3f, 1, 0)));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        [Test]
        public void InnerBoundaryQueryReturnsOnlyTheActiveGroupSource()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(6f, 1, 1)));
            var retainedSources = new List<BaseLightColorBase>();

            GLSEventCommon.GetColorTransitionSourcesAt(
                4f,
                map.LightColorEventBoxGroups[0],
                null,
                retainedSources);

            Assert.That(
                retainedSources,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0].Boxes[0].Events[0] }));
        }

        [Test]
        public void BoundaryQueryIncludesTransitionsEndingAtTheBoundary()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1)));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        [Test]
        public void RemovingTransitionTargetRewiresThePreviousSourceToTheNextTarget()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(8f, 0, 1)));
            var firstSource = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var removedTarget = map.LightColorEventBoxGroups[1];

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(firstSource, out var initialEnd), Is.True);
            Assert.That(initialEnd, Is.EqualTo(5f));

            GLSEventCommon.RemoveColorTransitionGroup(removedTarget);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(firstSource, out var rewiredEnd), Is.True);
            Assert.That(rewiredEnd, Is.EqualTo(8f));

            var retainedSources = new List<BaseLightColorBase>();
            GLSEventCommon.GetColorTransitionSourcesAt(6f, map.LightColorEventBoxGroups[0], null, retainedSources);
            Assert.That(retainedSources, Is.EquivalentTo(new[] { firstSource }));
        }

        [Test]
        public void AddingTransitionTargetRewiresThePreviousSourceToTheCloserTarget()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(8f, 0, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var insertedTarget = new BaseLightColorEventBoxGroup(CreateGroup(5f, 0, 1));
            insertedTarget.SetMap(map);
            insertedTarget.RecomputeSongBpmTime();

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var initialEnd), Is.True);
            Assert.That(initialEnd, Is.EqualTo(8f));

            GLSEventCommon.AddColorTransitionGroup(insertedTarget);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var rewiredEnd), Is.True);
            Assert.That(rewiredEnd, Is.EqualTo(5f));

            var retainedSources = new List<BaseLightColorBase>();
            GLSEventCommon.GetColorTransitionSourcesAt(4f, map.LightColorEventBoxGroups[0], null, retainedSources);
            Assert.That(retainedSources, Is.EquivalentTo(new[] { source }));
        }

        [Test]
        public void BoundaryQueryKeepsEverySameTimestampSourceFromIndependentFilters()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0, 1),
                CreateGroup(1f, 0, 0, 2),
                CreateGroup(5f, 0, 1, 1),
                CreateGroup(5f, 0, 1, 2)));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0], map.LightColorEventBoxGroups[1] }));
        }

        [Test]
        public void BoundaryQueryDoesNotCrossGroupOrFilterTimelines()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0, 1),
                CreateGroup(5f, 0, 1, 2),
                CreateGroup(5f, 1, 1, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out _), Is.False);
            Assert.That(retainedGroups, Is.Empty);
        }

        [Test]
        public void BoundaryQueryReturnsOnlySourcesMatchingTheActiveTrack()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0, 1, "selected"),
                CreateGroup(5f, 0, 1, 1, "selected"),
                CreateGroup(2f, 0, 0, 2, "other"),
                CreateGroup(6f, 0, 1, 2, "other")));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, "selected", retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        // ===== "GLS Wave Testing" reconstruction =====
        // These tests retain the older six-group snapshot (CustomWIPLevels/GLS Wave Testing/
        // ExpertPlusStandard.dat, version 3.3.0, Info.dat 100 BPM) on WeaveEnvironment's
        // eight-light group ID 1. Six groups own nine nodes: all-light singles at beats
        // 0, 1.5, 11.25, 22, 46, plus one step-and-offset box at beat 11 selecting even
        // lights {0,2,4,6} with children at relative 0/6/13.75/23 (absolute 11/17/24.75/34).
        //
        // The requested per-light ribbon chronology resolves each light's next node across
        // filters, not only inside a serialized-filter sequence:
        //   even lights: A@1.5 -> B@11 -> all@11.25 -> C@22 -> D@46
        //   odd lights:  A@1.5 -> all@11.25 -> C@22 -> D@46
        // The all-light group at 11.25 cancels B's pending children; they never resume later.
        // Shift-only cases move those interrupting groups to another physical ID so the
        // same authored shift payloads can be tested on intervals that actually exist.
        // GLSColliderWaveTest separately reconstructs the current three-group map, where
        // B and C are sibling boxes and serialized first-box ownership divides the lights.
        // Both fixtures now require ribbon chronology to agree with production playback.
        private const int WaveLightCount = 8;

        // WaveMapFixtureRebuildsAuthoredGroupsNodesAndShiftPayloads guards the reconstruction itself:
        // group/node counts, absolute beats, filter coverage, authored-but-unparsed first-shift
        // strings, parsed second-shift instructions, and the beat-22/46 custom easing keys.
        [Test]
        public void WaveMapFixtureRebuildsAuthoredGroupsNodesAndShiftPayloads()
        {
            var map = LoadWaveTestingMap();
            var groups = map.LightColorEventBoxGroups;
            Assert.That(groups.Count, Is.EqualTo(6));
            Assert.That(groups.Select(x => x.ID), Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1 }));
            Assert.That(
                groups.Select(x => x.Boxes[0].Events.Length),
                Is.EqualTo(new[] { 1, 1, 4, 1, 1, 1 }));
            Assert.That(
                groups[2].Boxes[0].Events.Select(x => x.JsonTime),
                Is.EqualTo(new[] { 11f, 17f, 24.75f, 34f }));
            Assert.That(WaveNode(map, 0).JsonTime, Is.EqualTo(0f));
            Assert.That(WaveNode(map, 1).JsonTime, Is.EqualTo(1.5f));
            Assert.That(WaveNode(map, 3).JsonTime, Is.EqualTo(11.25f));
            Assert.That(WaveNode(map, 4).JsonTime, Is.EqualTo(22f));
            Assert.That(WaveNode(map, 5).JsonTime, Is.EqualTo(46f));
            Assert.That(
                IndexFilterHelper.Convert(groups[2].Boxes[0].IndexFilter, WaveLightCount)
                    ?.Select(x => x.Element),
                Is.EqualTo(new[] { 0, 2, 4, 6 }),
                "The beat-11 box's step-and-offset filter must select only the four even lights.");
            Assert.That(
                IndexFilterHelper.Convert(groups[0].Boxes[0].IndexFilter, WaveLightCount)
                    ?.Select(x => x.Element),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }));

            var firstShiftNode = WaveNode(map, 2, 2);
            Assert.That(firstShiftNode.Shifts, Is.EqualTo(new[] { "v,1,l,l" }));
            Assert.That(firstShiftNode.StrobeShifts, Is.EqualTo(new[] { "sv,1,l" }));
            var secondShiftNode = WaveNode(map, 2, 3);
            Assert.That(secondShiftNode.ParsedShifts.Count, Is.EqualTo(1));
            Assert.That(secondShiftNode.ParsedStrobeShifts.Count, Is.EqualTo(2));
            AssertColor(
                WaveNode(map, 1).CustomColor.Value,
                new Color(1f, 0.4f, 0.913f, 1f));
            Assert.That(WaveNode(map, 4).ChromaStrobeColorEasing, Is.EqualTo(1));
            Assert.That(WaveNode(map, 4).ChromaStrobeEasing, Is.EqualTo(11));
            Assert.That(WaveNode(map, 5).ChromaColorEasing, Is.EqualTo(11));

            // Same-filter control that already resolves today: the first A node's only follower is A@1.5.
            Assert.That(
                GLSEventCommon.TryGetColorTransitionEndTime(WaveNode(map, 0), out var firstEnd),
                Is.True);
            Assert.That(firstEnd, Is.EqualTo(1.5f));
        }

        // WaveMapAllLightRibbonSplitsAtFilteredInterruption: requested strips send the beat-1.5
        // node's even lanes to the beat-11 filtered child while odd lanes keep running to the
        // beat-11.25 all-light node; one scalar following event cannot express both, so this
        // asserts the per-light endpoint rows instead of a single transition time.
        [Test]
        public void WaveMapAllLightRibbonSplitsAtFilteredInterruption()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                AssertWaveRibbonStrips(
                    WaveNode(map, 1),
                    EvenOddWaveTargets(WaveNode(map, 2), WaveNode(map, 3)),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapFilteredRibbonEndsAtAllLightInterruption: requested strips carry the beat-11
        // filtered node's four controlled lights into the 11.25 all-light node instead of running
        // straight through to the next same-filter child at beat 17.
        [Test]
        public void WaveMapFilteredRibbonEndsAtAllLightInterruption()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                var source = WaveNode(map, 2, 0);
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Every light the beat-11 filtered node controls shares the 11.25 all-light node as its next node.");
                Assert.That(endTime, Is.EqualTo(11.25f));
                AssertWaveRibbonStrips(
                    source,
                    EvenOddWaveTargets(WaveNode(map, 3), null),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // A later group cancels the prior box's pending children; both sets of lights now continue to the beat-22 all-light node.
        [Test]
        public void WaveMapAllLightTakeoverDoesNotResumeFilteredChildren()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                AssertWaveRibbonStrips(
                    WaveNode(map, 3),
                    EvenOddWaveTargets(WaveNode(map, 4), WaveNode(map, 4)),
                    appearance);
                Assert.IsFalse(GLSEventCommon.TryGetColorTransitionEndTime(WaveNode(map, 2, 1), out _),
                    "The canceled beat-17 child must not reappear after the all-light takeover.");
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapFilteredRibbonRejoinsAllLightSequenceAtNextNode: requested strips send the
        // beat-17 child's controlled lights to the beat-22 all-light node rather than the
        // same-filter sibling at beat 24.75.
        [Test]
        public void WaveMapFilteredRibbonRejoinsAllLightSequenceAtNextNode()
        {
            var map = LoadWaveTestingMap();
            // Let the beat-17 child exist; the later beat-22 group still takes over and must receive its ribbon.
            map.LightColorEventBoxGroups[3].ID = 2;
            var appearance = CreateWaveAppearance();
            try
            {
                var source = WaveNode(map, 2, 1);
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Every light the beat-17 filtered node controls shares the beat-22 all-light node as its next node.");
                Assert.That(endTime, Is.EqualTo(22f));
                AssertWaveRibbonStrips(
                    source,
                    EvenOddWaveTargets(WaveNode(map, 4), null),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // Once canceled by a different group, later filtered children cannot interrupt C's continuation to the final all-light node.
        [Test]
        public void WaveMapAllLightContinuationIgnoresCanceledFilteredChildren()
        {
            var map = LoadWaveTestingMap();
            var appearance = CreateWaveAppearance();
            try
            {
                AssertWaveRibbonStrips(
                    WaveNode(map, 4),
                    EvenOddWaveTargets(WaveNode(map, 5), WaveNode(map, 5)),
                    appearance);
                Assert.IsFalse(GLSEventCommon.TryGetColorTransitionEndTime(WaveNode(map, 2, 2), out _),
                    "The canceled beat-24.75 child cannot claim any strip from C.");
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapMatchingFilterRibbonKeepsAuthoredPerLightShiftStrips pins the one requested link
        // that already resolves inside a single serialized-filter sequence: the 24.75 node reaches
        // the working "hs,-0.4,lin" child at beat 34 on even lanes only, leaving odd lanes masked
        // black. Expected colors are recomputed from the authored offsets with Unity HSV math so
        // the parse -> instruction -> endpoint chain is verified independently.
        [Test]
        public void WaveMapMatchingFilterRibbonKeepsAuthoredPerLightShiftStrips()
        {
            // Isolate shift rendering from group takeover; canceled nodes are covered by the interruption regressions.
            var map = LoadWaveTestingMap(interruptFiltered: false);
            var source = WaveNode(map, 2, 2);
            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS wave matching-filter ribbon test");
            try
            {
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True);
                Assert.That(endTime, Is.EqualTo(34f));

                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, _ => false, WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                var dump = DescribeTexture(texture);
                Color.RGBToHSV(new Color(0.179f, 1f, 0f), out var mainHue, out var mainSaturation, out var mainValue);
                Color.RGBToHSV(new Color(0.969f, 0f, 0.941f), out var strobeHue, out _, out var strobeValue);
                for (var order = 0; order < 4; order++)
                {
                    var lightIndex = order * 2;
                    var textureX = WaveLightCount - lightIndex - 1;
                    var progress = order / 3f;
                    // "hs,-0.4,lin" rotates hue and drops saturation by the same linear share of
                    // selected-light order; the box's 0.03 wave adds 0.01 alpha per step.
                    var expectedMain = Color.HSVToRGB(
                        Mathf.Repeat(mainHue - (0.4f * progress), 1f),
                        Mathf.Clamp01(mainSaturation - (0.4f * progress)),
                        mainValue,
                        true);
                    expectedMain.a = 0.8f + (0.03f * progress);
                    // Strobe applies "hs,0.4,lin" then "v,2,lin" cumulatively per light.
                    var expectedStrobe = Color.HSVToRGB(
                        Mathf.Repeat(strobeHue + (0.4f * progress), 1f),
                        1f,
                        strobeValue + (2f * progress),
                        true);
                    expectedStrobe.a = 1f;
                    var label = $"light{lightIndex}";
                    // Endpoint rows are RGBAHalf; literal rows need the existing 0.001 texel tolerance.
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        new Color(0f, 0.5f, 0f, 0.5f + (0.03f * progress)),
                        $"{label} source-main\n{dump}",
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        expectedMain,
                        $"{label} transition-main\n{dump}",
                        0.005f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        new Color(0f, 0.7f, 0.7f, 0.8f),
                        $"{label} source-strobe\n{dump}",
                        0.001f);
                    AssertColor(
                        texture.GetPixel(textureX, 3),
                        expectedStrobe,
                        $"{label} transition-strobe\n{dump}",
                        0.005f);
                }

                for (var lightIndex = 1; lightIndex < WaveLightCount; lightIndex += 2)
                {
                    var textureX = WaveLightCount - lightIndex - 1;
                    for (var row = 0; row < 4; row++)
                    {
                        AssertColor(
                            texture.GetPixel(textureX, row),
                            Color.black,
                            $"light{lightIndex} row{row} must stay masked\n{dump}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapShiftStripsRenderThroughRealShader samples the produced property block through the
        // actual Basic Gradient shader so strip coverage, not just the uploaded table, is pinned per
        // light. The beat-34 node authors sf=1, so the ribbon fades between bands with
        // Cubic_InOut(trianglePhase) over the 24.75->34 interval (duration 9.25, frequencies 1->3,
        // phase = frac(9.25 * (p + p^2))): progress 0.556 lands at phase ~0.002 (pure normal) and
        // 0.581 at phase ~0.497 (pure strobe).
        [Test]
        public void WaveMapShiftStripsRenderThroughRealShader()
        {
            // Keep the tested shift interval alive instead of expecting a canceled child to emit pixels.
            var map = LoadWaveTestingMap(interruptFiltered: false);
            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS wave strip render test");
            Material sampleMaterial = null;
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller,
                    WaveNode(map, 2, 2),
                    appearance,
                    _ => false,
                    WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                sampleMaterial = CreateWaveSampleMaterial(properties);

                var evenNormal = RenderGradientPixel(sampleMaterial, 0.556f, WaveLaneForLight(0));
                Assert.That(evenNormal.g, Is.GreaterThan(evenNormal.r), "light0 normal band stays green-dominant");
                Assert.That(evenNormal.g, Is.GreaterThan(evenNormal.b), "light0 normal band\n" + evenNormal);
                var firstStrobe = RenderGradientPixel(sampleMaterial, 0.581f, WaveLaneForLight(0));
                Assert.That(firstStrobe.r, Is.GreaterThan(firstStrobe.g), "light0 strobe band keeps the magenta payload");
                Assert.That(firstStrobe.b, Is.GreaterThan(firstStrobe.g), "light0 strobe band\n" + firstStrobe);
                var lastStrobe = RenderGradientPixel(sampleMaterial, 0.581f, WaveLaneForLight(6));
                Assert.That(
                    lastStrobe.g,
                    Is.GreaterThan(lastStrobe.r),
                    "light6's 'hs,0.4,lin'+'v,2,lin' strobe endpoint must differ from light0's, proving per-light strips");
                Assert.That(lastStrobe.g, Is.GreaterThan(lastStrobe.b), "light6 strobe band\n" + lastStrobe);
                var oddStrip = RenderGradientPixel(sampleMaterial, 0.581f, WaveLaneForLight(1));
                Assert.That(
                    oddStrip.maxColorComponent,
                    Is.LessThan(0.05f),
                    "An unselected odd lane must stay masked instead of overlaying all eight lights");
            }
            finally
            {
                if (sampleMaterial != null)
                {
                    Object.DestroyImmediate(sampleMaterial);
                }

                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapLastFilteredRibbonReachesFinalAllLightNode: requested strips carry the beat-34
        // shifted child forward to the beat-46 all-light node on even lanes instead of ending the
        // filtered sequence at the box's own last child.
        [Test]
        public void WaveMapLastFilteredRibbonReachesFinalAllLightNode()
        {
            // The final-child link exists only when an earlier different group has not canceled that child.
            var map = LoadWaveTestingMap(interruptFiltered: false);
            var appearance = CreateWaveAppearance();
            try
            {
                var source = WaveNode(map, 2, 3);
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Every light the beat-34 filtered node controls shares the beat-46 all-light node as its next node.");
                Assert.That(endTime, Is.EqualTo(46f));
                AssertWaveRibbonStrips(
                    source,
                    EvenOddWaveTargets(WaveNode(map, 5), null),
                    appearance);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapBoundarySourcesFollowPerLightChronology checks viewport retention against the same
        // per-light model: at beat 12 only the 11.25 node still owns strips (the beat-11 child's
        // evens already ended); at beats 23 and 35 only the beat-22 node continues to the final
        // node because the earlier filtered box's later children were canceled by takeover.
        [Test]
        public void WaveMapBoundarySourcesFollowPerLightChronology()
        {
            var map = LoadWaveTestingMap();
            var groups = map.LightColorEventBoxGroups;
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();
            var retainedSources = new List<BaseLightColorBase>();

            // Control beat that already matches: the beat-1.5 node sources every light's strip.
            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);
            Assert.That(retainedGroups, Is.EquivalentTo(new[] { groups[1] }));

            GLSEventCommon.GetColorTransitionSourcesAt(12f, groups[2], null, retainedSources);
            Assert.That(
                retainedSources,
                Is.Empty,
                "The beat-11 child's strips end at the 11.25 all-light interruption on its controlled lights.");
            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(12f, null, retainedGroups);
            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { groups[3] }),
                "Only the 11.25 all-light node still owns strips at beat 12; no straight-through 11->17 link may survive.");

            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(23f, null, retainedGroups);
            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { groups[4] }),
                "At beat 23 all lights continue 22->46; the canceled filtered children cannot regain ownership.");

            GLSEventCommon.GetColorTransitionSourcesAt(35f, groups[2], null, retainedSources);
            // Retention follows playback ownership too: canceled children must not resurrect offscreen containers.
            Assert.That(retainedSources, Is.Empty,
                "The canceled beat-34 child owns no interval after the all-light takeover.");
            retainedGroups.Clear();
            GLSEventCommon.GetColorTransitionSourceGroupsAt(35f, null, retainedGroups);
            Assert.That(retainedGroups, Is.EquivalentTo(new[] { groups[4] }));
        }

        // WaveMapAuthoredShiftStringsStayPreservedButParseEmpty documents the shipped map's first
        // shift node verbatim: "v,1,l,l" and "sv,1,l" keep their raw authored strings while 'l' in
        // the easing slot parses to nothing ('lin' is the easing token; a fourth 'l' only flags
        // per-light progress). The box's 0.03 brightness distribution still yields a small
        // main-channel gradient; the strobe row stays uniform.
        [Test]
        public void WaveMapAuthoredShiftStringsStayPreservedButParseEmpty()
        {
            var map = LoadWaveTestingMap();
            var node = WaveNode(map, 2, 2);
            Assert.That(node.Shifts, Is.EqualTo(new[] { "v,1,l,l" }));
            Assert.That(node.StrobeShifts, Is.EqualTo(new[] { "sv,1,l" }));
            Assert.That(node.ParsedShifts, Is.Empty);
            Assert.That(node.ParsedStrobeShifts, Is.Empty);

            var appearance = CreateWaveAppearance();
            try
            {
                var mainColors = new Color[WaveLightCount];
                var strobeColors = new Color[WaveLightCount];
                Assert.That(
                    GLSEventCommon.PopulateColorTransitionEndpoint(
                        node, WaveLightCount, false, appearance, mainColors, strobeColors),
                    Is.True);
                for (var order = 0; order < 4; order++)
                {
                    var lightIndex = order * 2;
                    AssertColor(
                        mainColors[lightIndex],
                        new Color(0f, 0.5f, 0f, 0.5f + (0.03f * (order / 3f))),
                        $"light{lightIndex} main keeps only the brightness-distribution gradient");
                    AssertColor(
                        strobeColors[lightIndex],
                        new Color(0f, 0.7f, 0.7f, 0.8f),
                        $"light{lightIndex} strobe stays uniform while 'sv,1,l' fails to parse");
                }

                for (var lightIndex = 1; lightIndex < WaveLightCount; lightIndex += 2)
                {
                    AssertColor(mainColors[lightIndex], Color.black, $"light{lightIndex} unselected");
                    AssertColor(strobeColors[lightIndex], Color.black, $"light{lightIndex} unselected");
                }
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // WaveMapCorrectedShiftInstructionsDistributeAcrossSelectedLights pairs the diagnostic above:
        // the intended spellings "v,1,lin,l" and "sv,1,lin" parse and distribute across the four
        // selected lights. With 8 chunks over 8 lights, chunk and affected-light progress coincide,
        // so both instructions share the same o/3 coordinate here.
        [Test]
        public void WaveMapCorrectedShiftInstructionsDistributeAcrossSelectedLights()
        {
            // Compare corrected shift parsing and pixels on a live interval, not one canceled by the older takeover fixture.
            var map = LoadWaveTestingMap(new[] { "v,1,lin,l" }, new[] { "sv,1,lin" }, interruptFiltered: false);
            var node = WaveNode(map, 2, 2);
            Assert.That(node.ParsedShifts.Count, Is.EqualTo(1));
            Assert.That(node.ParsedShifts[0].Targets, Is.EqualTo(GLSColorShiftTargets.Value));
            Assert.That(node.ParsedShifts[0].Offset, Is.EqualTo(1f));
            Assert.That(node.ParsedShifts[0].UsesAffectedLightProgress, Is.True);
            Assert.That(node.ParsedStrobeShifts.Count, Is.EqualTo(1));
            Assert.That(
                node.ParsedStrobeShifts[0].Targets,
                Is.EqualTo(GLSColorShiftTargets.Saturation | GLSColorShiftTargets.Value));
            Assert.That(node.ParsedStrobeShifts[0].UsesAffectedLightProgress, Is.False);

            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS corrected shift ribbon test");
            try
            {
                var mainColors = new Color[WaveLightCount];
                var strobeColors = new Color[WaveLightCount];
                Assert.That(
                    GLSEventCommon.PopulateColorTransitionEndpoint(
                        node, WaveLightCount, false, appearance, mainColors, strobeColors),
                    Is.True);

                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, node, appearance, _ => false, WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat(useLightDistributionId), Is.EqualTo(1f));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                var dump = DescribeTexture(texture);
                for (var order = 0; order < 4; order++)
                {
                    var lightIndex = order * 2;
                    var progress = order / 3f;
                    var textureX = WaveLightCount - lightIndex - 1;
                    // "v,1,lin,l" lifts HSV value by the per-light share; "sv,1,lin" pushes the cyan
                    // strobe's value identically through the chunk coordinate.
                    var expectedMain = new Color(0f, 0.5f + progress, 0f, 0.5f + (0.03f * progress));
                    var expectedStrobe =
                        new Color(0f, 0.7f + progress, 0.7f + progress, 0.8f);
                    var label = $"light{lightIndex}";
                    AssertColor(mainColors[lightIndex], expectedMain, $"{label} preview main");
                    AssertColor(strobeColors[lightIndex], expectedStrobe, $"{label} preview strobe");
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        expectedMain,
                        $"{label} ribbon source-main\n{dump}",
                        0.005f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        expectedStrobe,
                        $"{label} ribbon source-strobe\n{dump}",
                        0.005f);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // InstantColorTargetOptionallyDrawsSolidStepRibbon retains the requested step-strip option: an Instant
        // (i=0) follower holds the preceding state rather than suppressing its ribbon. Strobing sources
        // continue to depict their active pulse, while non-strobing sources remain solid until the step.
        // Keep this separate from the authored wave-map assertions, whose destinations all transition.
        [Test]
        public void InstantColorTargetOptionallyDrawsSolidStepRibbon()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 1),
                CreateGroup(5f, 0, 0)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            source.CustomColor = new Color(0.9f, 0.1f, 0.2f, 1f);
            source.Brightness = 1f;
            // The optional step-strip path requires a known physical group rather than the unknown-count legacy fallback.
            GLSEventCommon.SetColorTransitionLightCount(0, WaveLightCount);
            var instantTarget = map.LightColorEventBoxGroups[1].Boxes[0].Events[0];
            instantTarget.CustomColor = new Color(0.1f, 0.2f, 0.9f, 1f);
            instantTarget.Brightness = 1f;
            var appearance = CreateWaveAppearance();
            var ribbonObject = new GameObject("GLS step ribbon test");
            try
            {
                Assert.That(
                    GLSEventCommon.TryGetColorTransitionEndTime(source, out var endTime),
                    Is.True,
                    "Desired option: the interval before an Instant endpoint still owns a strip.");
                Assert.That(endTime, Is.EqualTo(5f));
                var controller = CreateRibbonController(ribbonObject, out var _);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, _ => false, WaveLightCount);
                Assert.That(
                    ribbonObject.activeSelf,
                    Is.True,
                    "Desired option: a solid step strip renders instead of hiding the ribbon.");
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // Ribbon tests share one minimal renderer fixture so property-block assertions exercise the production controller without scene prefab state.
        // Collider wave parity uses the same renderer fixture rather than duplicating its serialized dependency setup.
        internal static LightGradientController CreateRibbonController(
            GameObject ribbonObject,
            out MeshRenderer renderer)
        {
            renderer = ribbonObject.AddComponent<MeshRenderer>();
            var controller = ribbonObject.AddComponent<LightGradientController>();
            var rendererField = typeof(LightGradientController).GetField(
                "meshRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rendererField, Is.Not.Null);
            rendererField.SetValue(controller, renderer);
            return controller;
        }

        // DistributedStrobeRibbonRendersEveryLightStripAcrossWholeGradient draws a one-pixel quad with a single interpolated UV so assertions isolate the fragment result.
        // Collider wave parity reuses the real shader sampling path so fixtures cannot disagree about blending or color space.
        internal static Color RenderGradientPixel(Material material, float progress, float lane)
        {
            var renderTexture = new RenderTexture(
                1,
                1,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0, 0),
                    new Vector3(1, 0),
                    new Vector3(1, 1),
                    new Vector3(0, 1)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                uv = new[]
                {
                    new Vector2(progress, lane),
                    new Vector2(progress, lane),
                    new Vector2(progress, lane),
                    new Vector2(progress, lane)
                }
            };
            var readTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            var previousRenderTexture = RenderTexture.active;
            try
            {
                renderTexture.Create();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
                GL.PopMatrix();
                readTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                readTexture.Apply(false, false);
                return readTexture.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                Object.DestroyImmediate(readTexture);
                Object.DestroyImmediate(mesh);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        // Component assertions keep the ribbon regression sensitive to every HDR/alpha channel instead of Color's aggregate equality.
        private static void AssertColor(
            Color actual,
            Color expected,
            string context = null,
            float tolerance = 0.0001f)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), context);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), context);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), context);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), context);
        }

        // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips reports the full uploaded endpoint table when a row or light column regresses.
        private static string DescribeTexture(Texture2D texture)
        {
            var result = new StringBuilder();
            for (var y = 0; y < texture.height; y++)
            {
                result.Append($"row{y}:");
                for (var x = 0; x < texture.width; x++)
                {
                    var color = texture.GetPixel(x, y);
                    result.Append(
                        $" [{x}]=({color.r:0.###},{color.g:0.###},{color.b:0.###},{color.a:0.###})");
                }
            }

            return result.ToString();
        }

        // Create a map-scoped cache input without involving editor prefabs or viewport state.
        private static BaseDifficulty LoadMap(JSONNode json)
        {
            // Data-only fallback tests have no environment; do not inherit physical counts from another fixture's map.
            GLSEventCommon.ResetColorTransitionLightCounts();
            var map = BeatmapFactory.GetDifficultyFromJson(
                json,
                "testmap",
                BeatSaberSongContainer.Instance.Info,
                BeatSaberSongContainer.Instance.MapDifficultyInfo);
            BeatSaberSongContainer.Instance.Map = map;
            return map;
        }

        private static JSONNode CreateDifficultyJson(params JSONNode[] groups)
        {
            var groupArray = new JSONArray();
            foreach (var group in groups)
            {
                groupArray.Add(group);
            }

            return new JSONObject
            {
                ["version"] = "3.2.0",
                ["lightColorEventBoxGroups"] = groupArray,
            };
        }

        // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips builds box-authored shifts through the same JSON parser used by maps.
        private static JSONObject CreateShiftCustomData(string[] shifts, string[] strobeShifts)
        {
            var result = new JSONObject();
            var shiftArray = new JSONArray();
            foreach (var shift in shifts)
            {
                shiftArray.Add(shift);
            }

            var strobeShiftArray = new JSONArray();
            foreach (var strobeShift in strobeShifts)
            {
                strobeShiftArray.Add(strobeShift);
            }

            result[GLSColorShift.ShiftsKey] = shiftArray;
            result[GLSColorShift.StrobeShiftsKey] = strobeShiftArray;
            return result;
        }

        private static JSONNode CreateGroup(
            float time,
            int id,
            int transitionType,
            int filterParam = 1,
            string track = null,
            JSONNode boxCustomData = null,
            int filterType = 1,
            int filterParam1 = 0,
            int filterChunks = 1,
            float brightnessDistribution = 1f)
        {
            var eventArray = new JSONArray();
            eventArray.Add(new JSONObject
            {
                ["b"] = 0,
                ["i"] = transitionType,
                ["c"] = 1,
                ["s"] = 1,
                ["f"] = 0,
                ["sb"] = 0,
                ["sf"] = 0,
            });

            var box = new JSONObject
            {
                ["f"] = new JSONObject
                {
                    ["c"] = filterChunks,
                    ["f"] = filterType,
                    ["p"] = filterParam,
                    ["t"] = filterParam1,
                    ["r"] = 0,
                    ["n"] = 0,
                    ["s"] = 0,
                    ["l"] = 0,
                    ["d"] = 0,
                },
                ["w"] = 1,
                ["d"] = 1,
                ["r"] = brightnessDistribution,
                ["t"] = 1,
                ["b"] = 1,
                ["i"] = 0,
                ["e"] = eventArray,
            };
            if (boxCustomData != null)
            {
                // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips must exercise parsed box instructions rather than bypassing their production cache.
                box["customData"] = boxCustomData;
            }

            var boxArray = new JSONArray();
            boxArray.Add(box);

            var group = new JSONObject
            {
                ["b"] = time,
                ["g"] = id,
                ["e"] = boxArray,
            };

            if (track != null)
            {
                // Exercise the same group-level track predicate used by viewport retention.
                group["customData"] = new JSONObject { ["unusedKeyTrack"] = track };
            }

            return group;
        }

        // WaveNode indexes the reconstructed six-group topology: groups 0/1/3/4/5 hold one all-light
        // node each; group 2 holds the four step-and-offset children.
        private static BaseLightColorBase WaveNode(BaseDifficulty map, int groupIndex, int eventIndex = 0) =>
            map.LightColorEventBoxGroups[groupIndex].Boxes[0].Events[eventIndex];

        // Wave routing tests explicitly know the physical width; shift-only cases move interrupting groups to another light ID.
        private static BaseDifficulty LoadWaveTestingMap(
            string[] firstShiftNodeShifts = null,
            string[] firstShiftNodeStrobeShifts = null,
            bool interruptFiltered = true)
        {
            var map = LoadMap(CreateWaveTestingDifficultyJson(firstShiftNodeShifts, firstShiftNodeStrobeShifts));
            if (!interruptFiltered)
            {
                map.LightColorEventBoxGroups[3].ID = 2;
                map.LightColorEventBoxGroups[4].ID = 2;
            }
            GLSEventCommon.SetColorTransitionLightCount(1, WaveLightCount);
            return map;
        }

        // CreateWaveTestingDifficultyJson rebuilds the six authored groups verbatim from the map's
        // ExpertPlusStandard.dat; the optional overrides keep the same topology while repairing the
        // first shift node's malformed easing tokens for the corrected-shift comparison test.
        private static JSONNode CreateWaveTestingDifficultyJson(
            string[] firstShiftNodeShifts = null,
            string[] firstShiftNodeStrobeShifts = null)
        {
            var groups = new JSONArray();
            groups.Add(CreateWaveGroup(0f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0f, 1, 0f,
                    new JSONObject { ["color"] = WaveColor(1f, 0.4f, 0.913f) }))));
            groups.Add(CreateWaveGroup(1.5f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0f, 1, 0f,
                    new JSONObject { ["color"] = WaveColor(1f, 0.4f, 0.913f) }))));
            groups.Add(CreateWaveGroup(11f, CreateWaveBox(EvenStepWaveFilter(), 0.03f,
                WaveColorNode(0f, 1f, 5, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.942f),
                    }),
                WaveColorNode(6f, 0.6f, 0, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.942f),
                    }),
                WaveColorNode(13.75f, 0.5f, 1, 0.8f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0f, 0.5f, 0f),
                        ["strobeColor"] = WaveColor(0f, 0.7f, 0.7f),
                        ["strobeShifts"] = WaveStrings(firstShiftNodeStrobeShifts ?? new[] { "sv,1,l" }),
                        ["shifts"] = WaveStrings(firstShiftNodeShifts ?? new[] { "v,1,l,l" }),
                    }),
                WaveColorNode(23f, 0.8f, 3, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.941f),
                        ["shifts"] = WaveStrings("hs,-0.4,lin"),
                        ["strobeShifts"] = WaveStrings("hs,0.4,lin", "v,2,lin"),
                    }))));
            groups.Add(CreateWaveGroup(11.25f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0.5f, 1, 0f,
                    new JSONObject { ["color"] = WaveColor(1f, 0.4f, 0.913f) }))));
            groups.Add(CreateWaveGroup(22f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 0.2f, 1, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.5f, 0f, 0.913f),
                        ["strobeColorEasing"] = 1,
                        ["strobeEasing"] = 11,
                    }))));
            groups.Add(CreateWaveGroup(46f, CreateWaveBox(AllLightWaveFilter(), 0f,
                WaveColorNode(0f, 1f, 0, 1f,
                    new JSONObject
                    {
                        ["color"] = WaveColor(0.179f, 1f, 0f),
                        ["strobeColor"] = WaveColor(0.969f, 0f, 0.941f),
                        ["colorEasing"] = 11,
                    }))));
            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["lightColorEventBoxGroups"] = groups,
            };
        }

        private static JSONObject CreateWaveGroup(float beat, params JSONNode[] boxes)
        {
            var boxArray = new JSONArray();
            foreach (var box in boxes)
            {
                boxArray.Add(box);
            }

            return new JSONObject
            {
                ["b"] = beat,
                ["g"] = 1,
                ["e"] = boxArray,
            };
        }

        private static JSONObject CreateWaveBox(
            JSONNode filter,
            float brightnessDistribution,
            params JSONNode[] events)
        {
            var eventArray = new JSONArray();
            foreach (var evt in events)
            {
                eventArray.Add(evt);
            }

            return new JSONObject
            {
                ["f"] = filter,
                ["w"] = 0,
                ["d"] = 1,
                ["r"] = brightnessDistribution,
                ["t"] = 1,
                ["b"] = 0,
                ["i"] = 0,
                ["e"] = eventArray,
            };
        }

        private static JSONObject AllLightWaveFilter() =>
            new()
            {
                ["f"] = 1,
                ["p"] = 1,
                ["t"] = 0,
                ["r"] = 0,
                ["c"] = 0,
                ["n"] = 0,
                ["s"] = 0,
                ["l"] = 0,
                ["d"] = 0,
            };

        private static JSONObject EvenStepWaveFilter() =>
            new()
            {
                ["f"] = 2,
                ["p"] = 0,
                ["t"] = 2,
                ["r"] = 0,
                ["c"] = 8,
                ["n"] = 0,
                ["s"] = 0,
                ["l"] = 0,
                ["d"] = 0,
            };

        private static JSONObject WaveColorNode(
            float relativeBeat,
            float brightness,
            int frequency,
            float strobeBrightness,
            JSONNode customData) =>
            new()
            {
                ["b"] = relativeBeat,
                ["c"] = 0,
                ["s"] = brightness,
                ["i"] = 1,
                ["f"] = frequency,
                ["sb"] = strobeBrightness,
                ["sf"] = 1,
                ["customData"] = customData,
            };

        private static JSONArray WaveColor(params float[] channels)
        {
            var color = new JSONArray();
            foreach (var channel in channels)
            {
                color.Add(channel);
            }

            return color;
        }

        private static JSONArray WaveStrings(params string[] values)
        {
            var strings = new JSONArray();
            foreach (var value in values)
            {
                strings.Add(value);
            }

            return strings;
        }

        private static EventAppearanceSO CreateWaveAppearance()
        {
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            return appearance;
        }

        // EvenOddWaveTargets returns the requested per-light follower list: the filtered node on
        // even lanes and the all-light node on odd lanes (or nothing when a lane keeps no strip).
        private static BaseLightColorBase[] EvenOddWaveTargets(
            BaseLightColorBase evenTarget,
            BaseLightColorBase oddTarget) =>
            Enumerable.Range(0, WaveLightCount)
                .Select(lightIndex => lightIndex % 2 == 0 ? evenTarget : oddTarget)
                .ToArray();

        private static bool[] WaveCoveredLights(BaseLightColorBase evt)
        {
            var covered = new bool[WaveLightCount];
            if (evt.EventBoxData is BaseLightColorEventBox box
                && IndexFilterHelper.Convert(box.IndexFilter, WaveLightCount) is { } filter)
            {
                foreach (var entry in filter)
                {
                    covered[entry.Element] = true;
                }
            }

            return covered;
        }

        private static (Color[] main, Color[] strobe) PopulateWaveEndpoints(
            BaseLightColorBase evt,
            EventAppearanceSO appearance)
        {
            var main = new Color[WaveLightCount];
            var strobe = new Color[WaveLightCount];
            for (var lightIndex = 0; lightIndex < WaveLightCount; lightIndex++)
            {
                main[lightIndex] = Color.black;
                strobe[lightIndex] = Color.black;
            }

            if (evt != null)
            {
                GLSEventCommon.PopulateColorTransitionEndpoint(
                    evt, WaveLightCount, false, appearance, main, strobe);
            }

            return (main, strobe);
        }

        // AssertWaveRibbonStrips encodes the requested per-light model against the real renderer
        // payload: each strip runs from the source node's own distributed color to that light's next
        // chronological node; lights outside the source filter stay black instead of overlaying all
        // eight. Rows map physical light -> textureX = width - light - 1, matching the shader's
        // reversed lane sampling.
        private static void AssertWaveRibbonStrips(
            BaseLightColorBase source,
            BaseLightColorBase[] nextPerLight,
            EventAppearanceSO appearance)
        {
            var ribbonObject = new GameObject("GLS wave ribbon test");
            try
            {
                var controller = CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(
                    controller, source, appearance, _ => false, WaveLightCount);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                Assert.That(
                    properties.GetFloat(useLightDistributionId),
                    Is.EqualTo(1f),
                    $"beat {source.JsonTime:R}: per-light targets need the distributed endpoint payload");
                Assert.That(properties.GetFloat(lightDistributionWidthId), Is.EqualTo(WaveLightCount));
                var texture = properties.GetTexture(lightDistributionTextureId) as Texture2D;
                Assert.That(texture, Is.Not.Null, $"beat {source.JsonTime:R}: missing endpoint texture");
                var covered = WaveCoveredLights(source);
                var (sourceMain, sourceStrobe) = PopulateWaveEndpoints(source, appearance);
                var endpointCache = new Dictionary<BaseLightColorBase, (Color[] main, Color[] strobe)>();
                var dump = DescribeTexture(texture);
                for (var lightIndex = 0; lightIndex < WaveLightCount; lightIndex++)
                {
                    var textureX = WaveLightCount - lightIndex - 1;
                    var next = covered[lightIndex] ? nextPerLight[lightIndex] : null;
                    // A masked light has no target; initialize the tuple so the guarded endpoint assertions are definitely assigned.
                    (Color[] main, Color[] strobe) rows = default;
                    if (next != null && !endpointCache.TryGetValue(next, out rows))
                    {
                        rows = PopulateWaveEndpoints(next, appearance);
                        endpointCache.Add(next, rows);
                    }

                    var expectedSourceMain = covered[lightIndex] ? sourceMain[lightIndex] : Color.black;
                    // Unused strobe channels collapse to primary at stopped endpoints, independently of the phase at arrival.
                    var expectedSourceStrobe = covered[lightIndex]
                        ? GLSEventCommon.GetStrobeFrequency(source) > 0f ? sourceStrobe[lightIndex] : sourceMain[lightIndex]
                        : Color.black;
                    var expectedNextMain = next != null ? rows.main[lightIndex] : Color.black;
                    var expectedNextStrobe = next != null
                        ? GLSEventCommon.GetStrobeFrequency(next) > 0f ? rows.strobe[lightIndex] : rows.main[lightIndex]
                        : Color.black;
                    var label = $"beat {source.JsonTime:R} light{lightIndex}";
                    var targetLabel = next == null ? "none" : $"beat {next.JsonTime:R}";
                    // Endpoint rows are RGBAHalf: HDR strobe values near 3 carry ~0.002 quantization.
                    AssertColor(
                        texture.GetPixel(textureX, 0),
                        expectedSourceMain,
                        $"{label} source-main {GLSEventCommon.FormatColor(expectedSourceMain)}\n{dump}",
                        0.004f);
                    AssertColor(
                        texture.GetPixel(textureX, 1),
                        expectedNextMain,
                        $"{label} transition-main -> {targetLabel} {GLSEventCommon.FormatColor(expectedNextMain)}\n{dump}",
                        0.004f);
                    AssertColor(
                        texture.GetPixel(textureX, 2),
                        expectedSourceStrobe,
                        $"{label} source-strobe {GLSEventCommon.FormatColor(expectedSourceStrobe)}\n{dump}",
                        0.004f);
                    AssertColor(
                        texture.GetPixel(textureX, 3),
                        expectedNextStrobe,
                        $"{label} transition-strobe -> {targetLabel} {GLSEventCommon.FormatColor(expectedNextStrobe)}\n{dump}",
                        0.004f);
                }
            }
            finally
            {
                Object.DestroyImmediate(ribbonObject);
            }
        }

        // WaveLaneForLight mirrors the shader: floor(uv.y * width) selects a column and the texture
        // stores light lightIndex at column width - lightIndex - 1, so lane centers sit at
        // (width - 0.5 - lightIndex) / width.
        private static float WaveLaneForLight(int lightIndex) =>
            (WaveLightCount - 0.5f - lightIndex) / WaveLightCount;

        // CreateWaveSampleMaterial copies the produced property block into a plain material so
        // RenderGradientPixel evaluates the real shader instead of a test-side model.
        // Collider wave parity copies the same production payload instead of maintaining a second shader property list.
        internal static Material CreateWaveSampleMaterial(MaterialPropertyBlock properties)
        {
            var shader = Shader.Find("ChroMapper/Object/Basic Gradient");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader) { enableInstancing = false };
            material.SetColor("_ColorA", properties.GetColor(colorAId));
            material.SetColor("_ColorB", properties.GetColor(colorBId));
            material.SetColor("_StrobeColorA", properties.GetColor(strobeColorAId));
            material.SetColor("_StrobeColorB", properties.GetColor(strobeColorBId));
            material.SetFloat("_StrobeDuration", properties.GetFloat(strobeDurationId));
            material.SetFloat("_StrobeFade", properties.GetFloat(strobeFadeId));
            material.SetFloat("_StrobeFrequencyA", properties.GetFloat(strobeFrequencyAId));
            material.SetFloat("_StrobeFrequencyB", properties.GetFloat(strobeFrequencyBId));
            material.SetFloat("_UseStrobeColors", properties.GetFloat(useStrobeColorsId));
            material.SetInt("_EasingID", properties.GetInt(easingId));
            material.SetInt("_UseHSV", properties.GetInt(useHsvId));
            material.SetFloat("_UseLightDistribution", properties.GetFloat(useLightDistributionId));
            material.SetFloat("_LightDistributionWidth", properties.GetFloat(lightDistributionWidthId));
            // Collider wave pixel tests must exercise the production per-light clocks, not fall back to the old four-row shader branch.
            material.SetFloat("_UseLightTimeline", properties.GetFloat(Shader.PropertyToID("_UseLightTimeline")));
            material.SetFloat("_LightTimelineDuration", properties.GetFloat(Shader.PropertyToID("_LightTimelineDuration")));
            if (properties.GetTexture(lightDistributionTextureId) is Texture texture)
            {
                material.SetTexture("_LightDistributionTex", texture);
            }

            return material;
        }
    }
}
