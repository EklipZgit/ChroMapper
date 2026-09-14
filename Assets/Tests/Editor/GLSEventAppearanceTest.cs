using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Shared;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSEventAppearanceTest
    {
        // Keep normal bright GLS nodes from rendering a dark strobe band merely because strobe brightness defaults to zero.
        [Test]
        public void BrightNonStrobingColorNodeDoesNotEnableStrobeBand()
        {
            var evt = new BaseLightColorBase
            {
                Brightness = 3.7f,
                Frequency = 0,
                StrobeBrightness = 0f,
                StrobeFade = 0
            };

            Assert.False(GLSEventCommon.IsStrobing(evt));
        }

        // Retain both OEM and Chroma timing forms as valid strobe-band triggers.
        [TestCase(1, null)]
        [TestCase(0, 1f)]
        public void TimedColorNodeEnablesStrobeBand(int frequency, float? chromaInterval)
        {
            var evt = new BaseLightColorBase
            {
                Frequency = frequency,
                ChromaStrobeInterval = chromaInterval
            };

            Assert.True(GLSEventCommon.IsStrobing(evt));
        }

        // ShiftedColorNodeInfoOmitsDistributionMarkers prevents shifts from adding triangle-like glyphs to the node's text overlay now that color bands carry that information.
        [Test]
        public void ShiftedColorNodeInfoOmitsDistributionMarkers()
        {
            var evt = CreateShiftedEvent(out _);

            var info = GLSEventCommon.GetColorInfo(evt);

            StringAssert.DoesNotContain("Δ", info);
            StringAssert.DoesNotContain("ΔS", info);
        }

        // ShiftedColorPreviewCachesSelectedLightsAndBlacksSkippedLights requires the preview cache to follow filter selection and dense affected-chunk shift progress.
        [Test]
        public void ShiftedColorPreviewCachesSelectedLightsAndBlacksSkippedLights()
        {
            var evt = CreateShiftedEvent(out _);
            evt.CustomColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            evt.StrobeColor = new Color(0.2f, 0.1f, 0.4f, 1f);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var mainColors = new Color[4];
            var strobeColors = new Color[4];
            var perLightDepthTable = new float[4];

            try
            {
                var enabled = PopulateColorDistributionPreview(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors,
                    perLightDepthTable);

                Assert.That(enabled, Is.True);
                AssertColor(mainColors[0], 0.1f, 0.2f, 0.3f, 1f);
                AssertColor(mainColors[1], 0f, 0f, 0f, 1f);
                AssertColor(mainColors[2], 1.1f, 0.2f, 0.3f, 1f);
                AssertColor(mainColors[3], 0f, 0f, 0f, 1f);
                AssertColor(strobeColors[0], 0.2f, 0.1f, 0.4f, 1f);
                AssertColor(strobeColors[1], 0f, 0f, 0f, 1f);
                AssertColor(strobeColors[2], 0.2f, 1.1f, 0.4f, 1f);
                AssertColor(strobeColors[3], 0f, 0f, 0f, 1f);
                // FrontToBackPreviewMapsMostShiftedLightFirst reverses physical IDs along node depth so the front shows the final shift and the back shows the source.
                Assert.That(perLightDepthTable, Is.EqualTo(new[] { 0.875f, 0.625f, 0.375f, 0.125f }));
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases locks l to dense selected-light progress while omitted and unknown modes retain affected-chunk progress.
        [Test]
        public void PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases()
        {
            var evt = CreatePerLightShiftEvent(out _);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.black;
            var mainColors = new Color[8];
            var strobeColors = new Color[8];
            var perLightDepthTable = new float[8];

            try
            {
                var enabled = PopulateColorDistributionPreview(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors,
                    perLightDepthTable);

                Assert.That(enabled, Is.True);
                AssertColor(mainColors[0], 0.1f, 0.2f, 0.3f, 1f);
                AssertColor(mainColors[1], 0.2f, 0.4f, 0.3f, 1f);
                AssertColor(mainColors[2], 0f, 0f, 0f, 1f);
                AssertColor(mainColors[4], 0.3f, 0.6f, 0.5f, 1f);
                AssertColor(mainColors[5], 0.4f, 0.8f, 0.5f, 1f);
                AssertColor(strobeColors[0], 0.2f, 0.1f, 0.4f, 1f);
                AssertColor(strobeColors[1], 0.5f, 0.5f, 0.4f, 1f);
                AssertColor(strobeColors[2], 0f, 0f, 0f, 1f);
                AssertColor(strobeColors[4], 0.8f, 0.9f, 0.8f, 1f);
                AssertColor(strobeColors[5], 1.1f, 1.3f, 0.8f, 1f);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // PerLightPlaybackEndpointsMatchPreviewAtBothStrobePhases exercises LightColorGroupEffect's actual endpoint resolvers and deterministic tween phases, including the unshifted-main strobe fallback.
        [TestCase(true)]
        [TestCase(false)]
        public void PerLightPlaybackEndpointsMatchPreviewAtBothStrobePhases(bool explicitStrobeColor)
        {
            var evt = CreatePerLightShiftEvent(out var box);
            if (!explicitStrobeColor)
            {
                evt.StrobeColor = null;
            }
            evt.Brightness = 0.5f;
            evt.StrobeBrightness = 0.25f;
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            var effectObject = new GameObject("Per-light shift playback test");
            effectObject.SetActive(false);
            var boost = effectObject.AddComponent<ColorBoostEffect>();
            var effect = effectObject.AddComponent<LightColorGroupEffect>();
            effect.ColorBoostEffect = boost;
            var mainColors = new Color[8];
            var strobeColors = new Color[8];
            var depth = new float[8];

            try
            {
                Assert.That(PopulateColorDistributionPreview(evt, appearance, mainColors, strobeColors, depth), Is.True);
                var filter = IndexFilterHelper.Convert(box.IndexFilter, 8);
                var normalResolver = typeof(LightColorGroupEffect).GetMethod(
                    "ResolveNormalColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var strobeResolver = typeof(LightColorGroupEffect).GetMethod(
                    "ResolveStrobeColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(normalResolver, Is.Not.Null);
                Assert.That(strobeResolver, Is.Not.Null);
                foreach (var entry in filter)
                {
                    var chunkProgress = entry.AffectedChunkOrder / (float)Mathf.Max(filter.VisibleCount - 1, 1);
                    var lightProgress = entry.AffectedLightOrder / (float)Mathf.Max(filter.AffectedLightCount - 1, 1);
                    var state = new LightColorEventStateData(evt, 0f, 0f, box, chunkProgress, lightProgress);
                    var normal = (Color)normalResolver.Invoke(effect, new object[] { state });
                    var strobe = (Color)strobeResolver.Invoke(effect, new object[] { state });
                    var strobeSource = evt.StrobeColor ?? evt.CustomColor.Value;
                    AssertColor(normal, 0.1f + (0.3f * lightProgress), 0.2f + (0.6f * lightProgress),
                        0.3f + (0.2f * chunkProgress), 1f);
                    AssertColor(strobe, strobeSource.r + (0.9f * lightProgress), strobeSource.g + (1.2f * lightProgress),
                        strobeSource.b + (0.4f * chunkProgress), 1f);
                    var tween = CreateStrobingRendererTween(evt, normal, strobe);
                    tween.UpdateTime(0.25f);
                    AssertColor(mainColors[entry.Element], tween.Color);
                    tween.UpdateTime(0.75f);
                    AssertColor(strobeColors[entry.Element], tween.Color);
                }
                foreach (var skippedLight in new[] { 2, 3, 6, 7 })
                {
                    AssertColor(mainColors[skippedLight], Color.black);
                    AssertColor(strobeColors[skippedLight], Color.black);
                }
            }
            finally
            {
                Object.DestroyImmediate(effectObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases compares every side-band entry with the exact color passed to LightController at minimum and peak strobe.
        [Test]
        public void ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases()
        {
            var evt = CreateReportedHsvShiftEvent(out var box);
            // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases verifies both authored entries survive JSON parsing before evaluating their combined result.
            Assert.That(evt.ParsedStrobeShifts.Count, Is.EqualTo(2));
            Assert.That(evt.ParsedStrobeShifts[0].Targets, Is.EqualTo(GLSColorShiftTargets.Hue | GLSColorShiftTargets.Saturation));
            Assert.That(evt.ParsedStrobeShifts[0].Offset, Is.EqualTo(0.4f));
            Assert.That(evt.ParsedStrobeShifts[1].Targets, Is.EqualTo(GLSColorShiftTargets.Value));
            Assert.That(evt.ParsedStrobeShifts[1].Offset, Is.EqualTo(2f));
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.black;
            var mainColors = new Color[8];
            var strobeColors = new Color[8];
            var perLightDepthTable = new float[8];

            try
            {
                var enabled = PopulateColorDistributionPreview(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors,
                    perLightDepthTable);

                Assert.That(enabled, Is.True);
                var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, mainColors.Length);
                Assert.That(indexFilter, Is.Not.Null);
                foreach (var entry in indexFilter)
                {
                    var progress = entry.AffectedChunkOrder
                        / (float)Mathf.Max(indexFilter.VisibleCount - 1, 1);
                    var normalColor = GLSColorShift.ApplyNormal(evt.CustomColor.Value, box, evt, progress);
                    var strobeColor = GLSColorShift.ApplyStrobe(evt.CustomColor.Value, box, evt, progress);
                    // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases defines valid HSV semantics independently so shared preview/playback bugs cannot agree and pass.
                    AssertColor(normalColor, EvaluateExpectedHsvShift(evt.CustomColor.Value, -0.4f, 0f, 0f, progress));
                    AssertColor(strobeColor, EvaluateExpectedHsvShift(evt.StrobeColor.Value, 0.4f, 0.4f, 2f, progress));
                    var tween = CreateStrobingRendererTween(evt, normalColor, strobeColor);

                    tween.UpdateTime(0.25f);
                    AssertColor(mainColors[entry.Element], tween.Color);
                    tween.UpdateTime(0.75f);
                    AssertColor(strobeColors[entry.Element], tween.Color);
                }
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // HdrShiftPreviewTextureNormalizesRgbWithoutClampingChannels preserves hue detail in the lit node shader while the raw cache remains identical to renderer input.
        [Test]
        public void HdrShiftPreviewTextureNormalizesRgbWithoutClampingChannels()
        {
            var evt = CreateReportedHsvShiftEvent(out _);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.black;
            var properties = new MaterialPropertyBlock();
            var preview = new GLSColorDistributionPreview();

            try
            {
                preview.Update(evt, 8, false, appearance, properties);

                var texture = properties.GetTexture(Shader.PropertyToID("_DistributionPreviewTex")) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                for (var lightIndex = 0; lightIndex < preview.PerLightStrobeColors.Length; lightIndex++)
                {
                    var rendererColor = preview.PerLightStrobeColors[lightIndex];
                    var expectedPreviewColor = EvaluateExpectedNodePreviewColor(rendererColor, appearance.OffColor);
                    AssertColor(texture.GetPixel(lightIndex, 1), expectedPreviewColor, 0.002f);
                }
            }
            finally
            {
                preview.Dispose();
                Object.DestroyImmediate(appearance);
            }
        }

        // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve verifies s, sb, and spatial f all reach the same display conversion used by the rest of the GLS node.
        [Test]
        public void MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve()
        {
            var evt = CreateBrightnessShiftEvent(out _);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.black;
            var properties = new MaterialPropertyBlock();
            var preview = new GLSColorDistributionPreview();

            try
            {
                preview.Update(evt, 2, false, appearance, properties);

                var texture = properties.GetTexture(Shader.PropertyToID("_DistributionPreviewTex")) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                AssertColor(
                    GLSEventCommon.GetColor(evt, false, appearance),
                    EvaluateExpectedNodePreviewColor(
                        BasicEventColorLerp.ApplyBrightness(evt.CustomColor.Value, evt.Brightness),
                        appearance.OffColor));
                AssertColor(
                    GLSEventCommon.GetStrobeColor(evt, false, appearance),
                    EvaluateExpectedNodePreviewColor(
                        BasicEventColorLerp.ApplyBrightness(evt.StrobeColor.Value, evt.StrobeBrightness),
                        appearance.OffColor));
                for (var lightIndex = 0; lightIndex < 2; lightIndex++)
                {
                    AssertColor(
                        texture.GetPixel(lightIndex, 0),
                        EvaluateExpectedNodePreviewColor(preview.PerLightColors[lightIndex], appearance.OffColor),
                        0.002f);
                    AssertColor(
                        texture.GetPixel(lightIndex, 1),
                        EvaluateExpectedNodePreviewColor(preview.PerLightStrobeColors[lightIndex], appearance.OffColor),
                        0.002f);
                }
                Assert.That(preview.PerLightColors[0].a, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(preview.PerLightColors[1].a, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(preview.PerLightStrobeColors[0].a, Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(preview.PerLightStrobeColors[1].a, Is.EqualTo(0.2f).Within(0.0001f));
            }
            finally
            {
                preview.Dispose();
                Object.DestroyImmediate(appearance);
            }
        }

        // SourceDistributionTexelRendersIdenticallyToMainNodeSurface exercises the shipped Note shader so CPU-equal colors cannot hide a texture/uniform rendering difference.
        [Test]
        public void SourceDistributionTexelRendersIdenticallyToMainNodeSurface()
        {
            var evt = CreateBrightnessShiftEvent(out _);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.black;
            var properties = new MaterialPropertyBlock();
            var preview = new GLSColorDistributionPreview();
            var shader = Shader.Find("ChroMapper/Object/Note");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            try
            {
                preview.Update(evt, 2, false, appearance, properties);
                var surfaceColor = GLSEventCommon.GetColor(evt, false, appearance);
                var texture = properties.GetTexture(Shader.PropertyToID("_DistributionPreviewTex")) as Texture2D;
                Assert.That(texture, Is.Not.Null);
                AssertColor(texture.GetPixel(0, 0), surfaceColor, 0.002f);
                material.SetColor("_Color", surfaceColor);
                material.SetTexture("_DistributionPreviewTex", texture);
                material.SetVector(
                    "_DistributionPreviewDepthRange",
                    properties.GetVector(Shader.PropertyToID("_DistributionPreviewDepthRange")));
                material.SetFloat("_DistributionPreviewChamferDepth", 0.1f);
                material.SetFloat("_ColorMultiplier", 1f);
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
                // SourceDistributionTexelRendersIdenticallyToMainNodeSurface supplies deterministic diffuse lighting and removes culling/depth state from the one-pixel branch comparison.
                material.EnableKeyword("DIFFUSE");
                material.DisableKeyword("SPECULAR");
                material.SetFloat("_CullMode", 0f);
                material.SetFloat("_ZTest", 8f);
                material.SetFloat("_ZWrite", 0f);

                var uniformPixel = RenderNoteSidePixel(material, false);
                var texturePixel = RenderNoteSidePixel(material, true);

                Assert.That(uniformPixel.maxColorComponent, Is.GreaterThan(0.01f));
                AssertColor(texturePixel, uniformPixel, 0.01f);
            }
            finally
            {
                preview.Dispose();
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(appearance);
            }
        }

        // DistributionTextureUsesFullWidthEndpointSections prevents center-to-center mapping from rendering the first and last lights at half the width of interior sections.
        [Test]
        public void DistributionTextureUsesFullWidthEndpointSections()
        {
            var evt = CreateReportedHsvShiftEvent(out _);
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            var properties = new MaterialPropertyBlock();
            var preview = new GLSColorDistributionPreview();

            try
            {
                preview.Update(evt, 8, false, appearance, properties);

                var depthRange = properties.GetVector(Shader.PropertyToID("_DistributionPreviewDepthRange"));
                Assert.That(depthRange.x, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(depthRange.y, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                preview.Dispose();
                Object.DestroyImmediate(appearance);
            }
        }

        // DistributionShaderUsesBeveledEventBlockInset reserves the chamfer for band height and maps all light sections across only the flat side face.
        [Test]
        public void DistributionShaderUsesBeveledEventBlockInset()
        {
            var shader = Shader.Find("ChroMapper/Object/Note");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            try
            {
                Assert.That(material.HasProperty("_DistributionPreviewChamferDepth"), Is.True);
                Assert.That(material.GetFloat("_DistributionPreviewChamferDepth"), Is.EqualTo(0.1f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        // BrightnessDistributionEnablesPreviewWithoutColorShifts keeps the bottom distribution band useful for ordinary GLS brightness distributions.
        [Test]
        public void BrightnessDistributionEnablesPreviewWithoutColorShifts()
        {
            var box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":0,\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":1,\"t\":1,\"b\":1,\"i\":0," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":0,\"i\":0,\"f\":0,\"sb\":1,\"sf\":0}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            evt.CustomColor = Color.white;
            // BrightnessDistributionEnablesPreviewWithoutColorShifts verifies the fixture reaches the same filter ordering consumed by GLS playback before checking preview colors.
            Assert.That(box.BrightnessDistribution, Is.EqualTo(1f));
            Assert.That(box.BrightnessDistributionType, Is.EqualTo((int)Beatmap.Enums.DistributionType.Wave));
            Assert.That(box.BrightnessAffectFirst, Is.EqualTo(1));
            var filter = IndexFilterHelper.Convert(box.IndexFilter, 3);
            Assert.That(filter, Is.Not.Null);
            var distributionOrders = new List<int>();
            foreach (var entry in filter)
            {
                distributionOrders.Add(entry.DistributionOrder);
            }
            Assert.That(distributionOrders, Is.EqualTo(new[] { 0, 1, 2 }));
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            appearance.OffColor = Color.clear;
            var mainColors = new Color[3];
            var strobeColors = new Color[3];
            var perLightDepthTable = new float[3];

            try
            {
                var enabled = PopulateColorDistributionPreview(
                    evt,
                    appearance,
                    mainColors,
                    strobeColors,
                    perLightDepthTable);

                Assert.That(enabled, Is.True);
                // BrightnessDistributionEnablesPreviewWithoutColorShifts matches LightColorTween by retaining shifted RGB and carrying distributed brightness in alpha.
                AssertColor(mainColors[0], 1f, 1f, 1f, 0f);
                AssertColor(mainColors[1], 1f, 1f, 1f, 0.5f);
                AssertColor(mainColors[2], 1f, 1f, 1f, 1f);
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        // CustomDataColorAlphaMultipliesBrightnessWithoutScalingHdrRgb protects basic-event RGBA parity for the normal GLS phase.
        [Test]
        public void CustomDataColorAlphaMultipliesBrightnessWithoutScalingHdrRgb()
        {
            var tween = CreateTween(
                new Color(4f, 0.25f, 2f, 3f),
                new Color(4f, 0.25f, 2f, 3f),
                startBrightness: 2f,
                endBrightness: 2f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 4f, 0.25f, 2f, 6f);
        }

        // ExplicitStrobeColorAlphaMultipliesSbWithoutScalingHdrRgb catches the regression where preview replaces authored alpha with sb.
        [Test]
        public void ExplicitStrobeColorAlphaMultipliesSbWithoutScalingHdrRgb()
        {
            var tween = CreateTween(
                new Color(0.5f, 0.25f, 0.125f, 1f),
                new Color(0.5f, 0.25f, 0.125f, 1f),
                startBrightness: 1f,
                endBrightness: 1f,
                startStrobeColor: new Color(4f, 0.25f, 2f, 3f),
                endStrobeColor: new Color(4f, 0.25f, 2f, 3f),
                startStrobeBrightness: 2f,
                endStrobeBrightness: 2f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 4f, 0.25f, 2f, 6f);
        }

        // InheritedStrobeColorComposesEndpointAlphaBeforeTweening covers omitted strobeColor and crossed HDR alpha/sb transitions.
        [Test]
        public void InheritedStrobeColorComposesEndpointAlphaBeforeTweening()
        {
            var tween = CreateTween(
                new Color(4f, 0.25f, 2f, 2f),
                new Color(0.5f, 3f, 0.125f, 4f),
                startBrightness: 1f,
                endBrightness: 1f,
                startStrobeBrightness: 3f,
                endStrobeBrightness: 5f);

            tween.UpdateTime(0.75f);

            AssertColor(tween.Color, 1.375f, 2.3125f, 0.59375f, 16.5f);
        }

        // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve uses two lights so linear f offsets expose both source and fully shifted brightness.
        private static BaseLightColorBase CreateBrightnessShiftEvent(out BaseLightColorEventBox box)
        {
            box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":2,\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":1,\"b\":1,\"i\":0," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":0.5,\"i\":0,\"f\":1,\"sb\":0.4,\"sf\":0," +
                "\"customData\":{\"color\":[1,0,0],\"strobeColor\":[0,0,1]," +
                "\"shifts\":[\"f,0.5,lin\"],\"strobeShifts\":[\"f,-0.5,lin\"]}}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            return evt;
        }

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases selects physical lights 0,1,4,5 so chunk and l coordinates diverge at both interior lights.
        private static BaseLightColorBase CreatePerLightShiftEvent(out BaseLightColorEventBox box)
        {
            box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":4,\"f\":2,\"p\":0,\"t\":2,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":1," +
                "\"customData\":{\"shifts\":[\"r,0.3,lin,l\",\"b,0.2,lin,future,discard\"]," +
                "\"strobeShifts\":[\"r,0.9,lin,l\"]}," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":1,\"sb\":1,\"sf\":0," +
                "\"customData\":{\"color\":[0.1,0.2,0.3],\"strobeColor\":[0.2,0.1,0.4]," +
                "\"shifts\":[\"g,0.6,lin,l,discard\"]," +
                "\"strobeShifts\":[\"g,1.2,lin,l\",\"b,0.4,lin\"]}}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            return evt;
        }

        // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases reproduces the authored colors and independent normal/strobe hue instructions from the reported map.
        private static BaseLightColorBase CreateReportedHsvShiftEvent(out BaseLightColorEventBox box)
        {
            box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":8,\"f\":1,\"p\":1,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":1,\"b\":1,\"i\":0," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":1,\"sb\":0,\"sf\":0," +
                "\"customData\":{\"color\":[0.179,1,0],\"strobeColor\":[0.969,0,0.941]," +
                "\"shifts\":[\"h,-0.4,lin\"],\"strobeShifts\":[\"hs,0.4,lin\",\"v,2,lin\"]}}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            return evt;
        }

        // ShiftedColorPreviewCachesSelectedLightsAndBlacksSkippedLights builds one step filter whose selected chunks expose both ends of the shift range.
        private static BaseLightColorBase CreateShiftedEvent(out BaseLightColorEventBox box)
        {
            box = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":4,\"f\":2,\"p\":0,\"t\":2,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":1," +
                "\"customData\":{\"shifts\":[\"r,1,lin\"]}," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":1,\"sb\":1,\"sf\":0," +
                "\"customData\":{\"strobeShifts\":[\"g,1,lin\"]}}]}"
            ));
            var evt = box.Events[0];
            evt.EventBoxData = box;
            return evt;
        }

        // Preview fixtures share the production cache entry point while keeping their fixed no-boost setup concise.
        private static bool PopulateColorDistributionPreview(
            BaseLightColorBase evt,
            EventAppearanceSO appearance,
            Color[] mainColors,
            Color[] strobeColors,
            float[] perLightDepthTable) =>
            GLSEventCommon.PopulateColorDistributionPreview(
                evt,
                mainColors.Length,
                false,
                appearance,
                mainColors,
                strobeColors,
                perLightDepthTable);

        // SourceDistributionTexelRendersIdenticallyToMainNodeSurface draws identical side-face geometry through the uniform and texture branches of the shipped shader.
        private static Color RenderNoteSidePixel(Material material, bool distributionPreview)
        {
            var renderTexture = new RenderTexture(
                1,
                1,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            var resultTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0f, -0.4f, 0.4f),
                    new Vector3(1f, -0.4f, 0.4f),
                    new Vector3(1f, -0.3f, 0.4f),
                    new Vector3(0f, -0.3f, 0.4f)
                },
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            var previousRenderTexture = RenderTexture.active;

            try
            {
                material.SetFloat("_DistributionPreviewEnabled", distributionPreview ? 1f : 0f);
                material.SetFloat("_StrobeColorEnabled", 0f);
                renderTexture.Create();
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.black);
                GL.PushMatrix();
                GL.LoadOrtho();
                material.SetPass(0);
                Graphics.DrawMeshNow(mesh, Matrix4x4.Translate(new Vector3(0f, 0.85f, 0f)));
                GL.PopMatrix();
                resultTexture.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0, false);
                resultTexture.Apply(false, false);
                return resultTexture.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                renderTexture.Release();
                Object.DestroyImmediate(resultTexture);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(renderTexture);
            }
        }

        // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve independently decomposes renderer HDR into chroma and intensity before applying the existing node dimness curve.
        private static Color EvaluateExpectedNodePreviewColor(Color color, Color offColor)
        {
            var maximumChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var hdrIntensity = Mathf.Max(maximumChannel, 1f);
            if (maximumChannel > 1f)
            {
                color.r /= maximumChannel;
                color.g /= maximumChannel;
                color.b /= maximumChannel;
            }

            var effectiveBrightness = color.a * hdrIntensity;
            // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve avoids applying renderer alpha twice while retaining the established opaque node-color endpoint.
            color.a = 1f;
            var clampedOffColor = Color.Lerp(offColor, color, 0.175f);
            return Color.Lerp(clampedOffColor, color, effectiveBrightness);
        }

        // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases keeps the expected model independent from GLSColorShift while preserving source alpha.
        private static Color EvaluateExpectedHsvShift(
            Color source,
            float hueOffset,
            float saturationOffset,
            float valueOffset,
            float progress)
        {
            Color.RGBToHSV(source, out var hue, out var saturation, out var value);
            var expected = Color.HSVToRGB(
                Mathf.Repeat(hue + (hueOffset * progress), 1f),
                Mathf.Clamp01(saturation + (saturationOffset * progress)),
                value + (valueOffset * progress),
                true);
            expected.a = source.a;
            return expected;
        }

        // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases mirrors LightColorGroupEffect's constant-event tween so its Color is exactly what LightController receives.
        private static LightColorTween CreateStrobingRendererTween(
            BaseLightColorBase evt,
            Color normalColor,
            Color strobeColor) =>
            new()
            {
                StartTimeAlpha = 0f,
                StartTimeColor = 0f,
                StartColor = normalColor,
                StartAlpha = evt.Brightness,
                StartStrobeFrequency = evt.Frequency,
                StartStrobeBrightness = evt.StrobeBrightness,
                StartStrobeColor = strobeColor,
                EndTimeAlpha = 1f,
                EndTimeColor = 1f,
                EndColor = normalColor,
                EndAlpha = evt.Brightness,
                EndStrobeFrequency = evt.Frequency,
                EndStrobeBrightness = evt.StrobeBrightness,
                EndStrobeColor = strobeColor,
                ColorLerpType = BasicEventColorLerpType.RGB
            };

        // Tests construct one deterministic cycle so 0.75 beats always selects the hard strobe-on phase.
        private static LightColorTween CreateTween(
            Color startColor,
            Color endColor,
            float startBrightness,
            float endBrightness,
            Color startStrobeColor = default,
            Color endStrobeColor = default,
            float startStrobeBrightness = 0f,
            float endStrobeBrightness = 0f)
        {
            return new LightColorTween
            {
                StartTimeAlpha = 0f,
                StartTimeColor = 0f,
                StartColor = startColor,
                StartAlpha = startBrightness,
                StartStrobeFrequency = startStrobeBrightness > 0f ? 1f : 0f,
                StartStrobeBrightness = startStrobeBrightness,
                StartStrobeColor = startStrobeColor,
                EndTimeAlpha = 1f,
                EndTimeColor = 1f,
                EndColor = endColor,
                EndAlpha = endBrightness,
                EndStrobeFrequency = endStrobeBrightness > 0f ? 1f : 0f,
                EndStrobeBrightness = endStrobeBrightness,
                EndStrobeColor = endStrobeColor,
                ColorLerpType = BasicEventColorLerpType.RGB
            };
        }

        // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases compares all renderer-input channels through the same component-sensitive assertion.
        private static void AssertColor(Color actual, Color expected) =>
            AssertColor(actual, expected.r, expected.g, expected.b, expected.a);

        // HdrShiftPreviewTextureNormalizesRgbWithoutClampingChannels allows only RGBAHalf quantization error when validating uploaded texture pixels.
        private static void AssertColor(Color actual, Color expected, float tolerance)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        // Component assertions prove HDR channels remain independent instead of hiding a mismatch behind Color equality tolerances.
        private static void AssertColor(Color actual, float red, float green, float blue, float alpha)
        {
            Assert.That(actual.r, Is.EqualTo(red).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(green).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(blue).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(alpha).Within(0.0001f));
        }
    }
}
