using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Tests.Editor
{
    public class GLSEventIconResolverTest
    {
        // Asset paths anchor integration coverage to the same prefab and atlas shipped by ChroMapper.
        private const string GlsPrefabPath = "Assets/_Prefabs/MapEditor/Beatmap/GLS Event.prefab";
        private const string GlsGroupPrefabPath = "Assets/_Prefabs/MapEditor/Beatmap/GLS Group.prefab";
        private const string GlsAtlasPath =
            "Assets/_Graphics/Textures/GLS Event Icons/GLS Event Icons.spriteatlasv2";
        private const string GlsEasingGeneratorPath = "Tools/Generate-GlsEasingIcons.ps1";
        private const string GlsRotationGeneratorPath = "Tools/Generate-GlsRotationDirectionIcons.ps1";
        private const string GlsOeIconExtractorPath = "Tools/Extract-OeGlsEventIcons.ps1";
        // PrefabWiresBothFacesToTheSharedSpriteAtlas keeps the icon-only no-bloom material independently testable.
        private const string GlsIconShaderPath = "Assets/_Graphics/Shaders/GLSIconSprite.shader";

        // GlsEasingAbbreviationsDistinguishTrueVariants prevents identical InOut labels and ambiguous Back/Circular names on every GLS node type.
        // ExtendedGlsEasingMenuSupportsAllLeads uses Sn and explicit powers to make the newly selectable curve families readable.
        [TestCase(EaseType.InQuadratic, "I^2")]
        [TestCase(EaseType.OutQuadratic, "O^2")]
        [TestCase(EaseType.InOutQuadratic, "IO^2")]
        [TestCase(EaseType.InSinusoidal, "ISn")]
        [TestCase(EaseType.OutSinusoidal, "OSn")]
        [TestCase(EaseType.InOutSinusoidal, "IOSn")]
        [TestCase(EaseType.InCubic, "I^3")]
        [TestCase(EaseType.OutCubic, "O^3")]
        [TestCase(EaseType.InOutCubic, "IO^3")]
        [TestCase(EaseType.InQuartic, "I^4")]
        [TestCase(EaseType.OutQuartic, "O^4")]
        [TestCase(EaseType.InOutQuartic, "IO^4")]
        [TestCase(EaseType.InQuintic, "I^5")]
        [TestCase(EaseType.OutQuintic, "O^5")]
        [TestCase(EaseType.InOutQuintic, "IO^5")]
        [TestCase(EaseType.InExponential, "IEx")]
        [TestCase(EaseType.OutExponential, "OEx")]
        [TestCase(EaseType.InOutExponential, "IOEx")]
        [TestCase(EaseType.InBack, "IBk")]
        [TestCase(EaseType.OutBack, "OBk")]
        [TestCase(EaseType.InOutBack, "IOTBk")]
        [TestCase(EaseType.InOutElastic, "IOTEl")]
        [TestCase(EaseType.InOutBounce, "IOTBo")]
        [TestCase(EaseType.BeatSaberInOutBack, "IOBk")]
        [TestCase(EaseType.BeatSaberInOutElastic, "IOEl")]
        [TestCase(EaseType.BeatSaberInOutBounce, "IOBo")]
        [TestCase(EaseType.InCircular, "ICr")]
        [TestCase(EaseType.OutCircular, "OCr")]
        [TestCase(EaseType.InOutCircular, "IOCr")]
        public void GlsEasingAbbreviationsDistinguishTrueVariants(EaseType easing, string expected)
        {
            var value = (int)easing;
            Assert.AreEqual(expected, Easing.IDToShortName[value]);
            // ColorHoverLabelsExplainEasingsOutsideNode moved color-node easing text to hover labels, so the compact face intentionally omits it.
            StringAssert.Contains(expected, GLSEventCommon.GetRotationInfo(new BaseLightRotationBase { EaseType = value }));
            StringAssert.Contains(expected, GLSEventCommon.GetTranslationInfo(new BaseLightTranslationBase { EaseType = value }));
            StringAssert.Contains(expected, GLSEventCommon.GetFloatFXInfo(new BaseFxEventFloat { Easing = value }));
        }

        // NamedEasingAbbreviationsDistinguishTrueVariants keeps basic-event labels consistent with GLS for the same standard curves.
        // NamedEasingAbbreviationsDistinguishTrueVariants keeps named basic-event easings aligned with the expanded GLS vocabulary.
        [TestCase("easeInQuad", "In^2")]
        [TestCase("easeOutQuad", "Out^2")]
        [TestCase("easeInOutQuad", "IO^2")]
        [TestCase("easeInSine", "InSn")]
        [TestCase("easeOutSine", "OutSn")]
        [TestCase("easeInOutSine", "IOSn")]
        [TestCase("easeInCubic", "In^3")]
        [TestCase("easeOutCubic", "Out^3")]
        [TestCase("easeInOutCubic", "IO^3")]
        [TestCase("easeInQuart", "In^4")]
        [TestCase("easeOutQuart", "Out^4")]
        [TestCase("easeInOutQuart", "IO^4")]
        [TestCase("easeInQuint", "In^5")]
        [TestCase("easeOutQuint", "Out^5")]
        [TestCase("easeInOutQuint", "IO^5")]
        [TestCase("easeInExpo", "InEx")]
        [TestCase("easeOutExpo", "OutEx")]
        [TestCase("easeInOutExpo", "IOEx")]
        [TestCase("easeInBack", "InBk")]
        [TestCase("easeOutBack", "OutBk")]
        [TestCase("easeInOutBack", "IOTBk")]
        [TestCase("easeInOutElastic", "IOTEl")]
        [TestCase("easeInOutBounce", "IOTBo")]
        [TestCase("easeInCirc", "InCr")]
        [TestCase("easeOutCirc", "OutCr")]
        [TestCase("easeInOutCirc", "IOCr")]
        public void NamedEasingAbbreviationsDistinguishTrueVariants(string easing, string expected)
        {
            Assert.AreEqual(expected, Easing.InternalNameToShortName[easing]);
        }

        // ColorNodeTwoColumnLayout: the lower-right slot renders the actual strobe fade curve (the native
        // InOutCubic or an authored strobeEasing). NoEasingStepMarkerIsAGeneratedRightAngle replaces the OE
        // block with the step glyph on both no-easing markers: instant transitions and hard strobes.
        [TestCase((int)EaseType.None, 0, 0, GLSEventIconType.NoEasingStep, GLSEventIconType.None)]
        [TestCase((int)EaseType.Linear, 0, 0, GLSEventIconType.EaseLinear, GLSEventIconType.None)]
        [TestCase((int)EaseType.InQuadratic, 4, 0, GLSEventIconType.EaseInQuadratic, GLSEventIconType.NoEasingStep)]
        [TestCase((int)EaseType.OutBounce, 4, 1, GLSEventIconType.EaseOutBounce, GLSEventIconType.EaseInOutCubic)]
        public void ColorIconsMatchOeMarkerStates(
            int easing,
            int frequency,
            int strobeFade,
            GLSEventIconType expectedPrimary,
            GLSEventIconType expectedSecondary)
        {
            var state = GLSEventIconResolver.Resolve(new BaseLightColorBase
            {
                Easing = easing,
                Frequency = frequency,
                StrobeFade = strobeFade
            });

            Assert.AreEqual(expectedPrimary, state.Primary);
            Assert.AreEqual(expectedSecondary, state.Secondary);
            Assert.AreEqual(GLSEventIconType.None, state.Tertiary);
        }

        // ColorNodeTwoColumnLayout: authored customData.strobeEasing replaces the native fade icon in the
        // lower-right slot, and authored strobeColorEasing owns the new bottom-left tertiary slot.
        [Test]
        public void ColorIconsUseEffectiveEasingsAcrossAllThreeSlots()
        {
            var state = GLSEventIconResolver.Resolve(new BaseLightColorBase
            {
                Easing = (int)EaseType.Linear,
                ChromaColorEasing = (int)EaseType.InCubic,
                Frequency = 4,
                StrobeFade = 1,
                ChromaStrobeEasing = (int)EaseType.OutBounce,
                ChromaStrobeColorEasing = (int)EaseType.InOutQuadratic
            });

            Assert.AreEqual(GLSEventIconType.EaseInCubic, state.Primary,
                "The top-left icon must track the effective colorEasing curve, not the interval easing.");
            Assert.AreEqual(GLSEventIconType.EaseOutBounce, state.Secondary,
                "The lower-right icon must track the authored strobeEasing curve.");
            Assert.AreEqual(GLSEventIconType.EaseInOutQuadratic, state.Tertiary,
                "The bottom-left icon must track the authored strobeColorEasing curve.");

            // GlsEasingCycleMatchesEditorOrder keeps strobeColorEasing=0 distinct from the absent inherit-interval state.
            var linearOverride = GLSEventIconResolver.Resolve(new BaseLightColorBase
            {
                ChromaStrobeColorEasing = (int)EaseType.Linear
            });
            Assert.AreEqual(GLSEventIconType.EaseLinear, linearOverride.Tertiary);
        }

        // Chroma's interval timing is also a strobe and must receive the same fade curve icon as frequency-based timing.
        [Test]
        public void ChromaStrobeIntervalUsesStrobeFadeIcon()
        {
            var state = GLSEventIconResolver.Resolve(new BaseLightColorBase
            {
                Easing = (int)EaseType.None,
                ChromaStrobeInterval = 0.25f,
                StrobeFade = 1
            });

            Assert.AreEqual(GLSEventIconType.NoEasingStep, state.Primary);
            Assert.AreEqual(GLSEventIconType.EaseInOutCubic, state.Secondary);
        }

        // NoEasingStepMarkerIsAGeneratedRightAngle verifies the generated step glyph reaches both no-easing
        // slots through the real prefab wiring and shares the curve family's wide aspect.
        [Test]
        public void NoEasingStepMarkerIsAGeneratedRightAngle()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var primaryTop = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var secondaryTop = instance.transform.Find("Secondary Icon Top").GetComponent<SpriteRenderer>();
                var container = instance.GetComponent<GLSEventContainer>();
                var colorEvent = new BaseLightColorBase
                {
                    Easing = (int)EaseType.None,
                    Frequency = 4,
                    StrobeFade = 0
                };
                container.EventData = colorEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(colorEvent));

                Assert.AreEqual("NoEasingStep", primaryTop.sprite.name);
                Assert.AreEqual("NoEasingStep", secondaryTop.sprite.name);
                Assert.AreEqual(
                    GLSEventIconView.EasingIconHeight * GLSEventIconView.ColorIconScale,
                    primaryTop.transform.localScale.x,
                    0.0001f);
                Assert.AreEqual(
                    GLSEventIconView.EasingIconHeight * GLSEventIconView.ColorIconScale,
                    secondaryTop.transform.localScale.x,
                    0.0001f);
                // The marker must come from the generator's outlined-stroke output, not the OE block.
                AssertPngContainsBlackAndWhitePixels(
                    "Assets/_Graphics/Textures/GLS Event Icons/Easings/NoEasingStep.png");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // Representative families ensure translation nodes select their exact generated curve rather than a shared lead shape.
        [TestCase(EaseType.Linear, GLSEventIconType.EaseLinear)]
        [TestCase(EaseType.InCubic, GLSEventIconType.EaseInCubic)]
        [TestCase(EaseType.InCircular, GLSEventIconType.EaseInCircular)]
        [TestCase(EaseType.OutElastic, GLSEventIconType.EaseOutElastic)]
        [TestCase(EaseType.BeatSaberInOutBounce, GLSEventIconType.EaseBeatSaberInOutBounce)]
        public void TranslationEasingUsesDistinctCurve(EaseType easing, GLSEventIconType expected)
        {
            var state = GLSEventIconResolver.Resolve(new BaseLightTranslationBase
            {
                EaseType = (int)easing
            });

            Assert.AreEqual(expected, state.Primary);
            Assert.AreEqual(GLSEventIconType.None, state.Secondary);
        }

        // Rotation markers pair the easing lead with their independent OE direction sprite.
        [TestCase(LightRotationDirection.Automatic, GLSEventIconType.RotationAutomatic)]
        [TestCase(LightRotationDirection.Clockwise, GLSEventIconType.RotationClockwise)]
        [TestCase(LightRotationDirection.CounterClockwise, GLSEventIconType.RotationCounterClockwise)]
        public void RotationDirectionUsesDirectionSprite(
            LightRotationDirection direction,
            GLSEventIconType expected)
        {
            var state = GLSEventIconResolver.Resolve(new BaseLightRotationBase
            {
                EaseType = (int)EaseType.InQuadratic,
                Direction = (int)direction
            });

            Assert.AreEqual(GLSEventIconType.EaseInQuadratic, state.Primary);
            Assert.AreEqual(expected, state.Secondary);
        }

        // Every non-None EaseType must resolve to a unique icon so newly supported curves cannot silently regress to placeholders.
        [Test]
        public void EveryEasingHasAUniqueIcon()
        {
            var resolvedIcons = new HashSet<GLSEventIconType>();
            foreach (EaseType easing in System.Enum.GetValues(typeof(EaseType)))
            {
                if (easing == EaseType.None)
                {
                    continue;
                }

                var state = GLSEventIconResolver.Resolve(new BaseLightTranslationBase
                {
                    EaseType = (int)easing
                });

                Assert.AreNotEqual(GLSEventIconType.None, state.Primary, $"{easing} has no icon.");
                Assert.IsTrue(resolvedIcons.Add(state.Primary), $"{easing} reuses {state.Primary}.");
            }

            Assert.AreEqual(34, resolvedIcons.Count);
        }

        // FloatFX easing is displayed like rotation and translation, while inherited GLS nodes remain intentionally icon-free.
        [Test]
        public void FloatFxUsesEasingAndInheritedNodesDisableIcons()
        {
            var inherited = GLSEventIconResolver.Resolve(new BaseLightColorBase { UsePrevious = 1 });
            var inheritedFloatFx = GLSEventIconResolver.Resolve(new BaseFxEventFloat { UsePrevious = 1 });
            var floatFx = GLSEventIconResolver.Resolve(new BaseFxEventFloat
            {
                Easing = (int)EaseType.InOutSinusoidal
            });

            Assert.AreEqual(GLSEventIconType.None, inherited.Primary);
            Assert.AreEqual(GLSEventIconType.None, inherited.Secondary);
            Assert.AreEqual(GLSEventIconType.None, inherited.Tertiary);
            Assert.AreEqual(GLSEventIconType.None, inheritedFloatFx.Primary);
            Assert.AreEqual(GLSEventIconType.None, inheritedFloatFx.Secondary);
            Assert.AreEqual(GLSEventIconType.None, inheritedFloatFx.Tertiary);
            Assert.AreEqual(GLSEventIconType.EaseInOutSinusoidal, floatFx.Primary);
            Assert.AreEqual(GLSEventIconType.None, floatFx.Secondary);
            Assert.AreEqual(GLSEventIconType.None, floatFx.Tertiary);
        }

        // Instantiate the real pooled prefab to catch broken serialized references, missing face pairs, or atlas import regressions.
        [Test]
        public void PrefabWiresBothFacesToTheSharedSpriteAtlas()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(GlsAtlasPath);

            Assert.IsNotNull(prefab);
            Assert.IsNotNull(atlas);
            // Runtime sprite enumeration verifies the packed output used by SpriteRenderer, unlike editor source-packable metadata.
            // NoEasingStepMarkerIsAGeneratedRightAngle grows the atlas by the generated step glyph, and
            // OutlineLessGlyphSetReadyForSettingSwap grows it by the 35 parallel outline-less glyphs.
            var packedSprites = new Sprite[78];
            Assert.AreEqual(78, atlas.GetSprites(packedSprites));

            var instance = Object.Instantiate(prefab);
            try
            {
                var iconView = instance.GetComponent<GLSEventIconView>();
                var primaryTop = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var secondaryTop = instance.transform.Find("Secondary Icon Top").GetComponent<SpriteRenderer>();
                var primarySide = instance.transform.Find("Primary Icon Side").GetComponent<SpriteRenderer>();
                var secondarySide = instance.transform.Find("Secondary Icon Side").GetComponent<SpriteRenderer>();
                var container = instance.GetComponent<GLSEventContainer>();

                Assert.IsNotNull(iconView);
                var rotationEvent = new BaseLightRotationBase
                {
                    EaseType = (int)EaseType.InQuadratic,
                    Direction = (int)LightRotationDirection.Clockwise
                };
                container.EventData = rotationEvent;
                container.SetIcons(new GLSEventIconState(
                    GLSEventIconType.Instant,
                    GLSEventIconType.RotationClockwise));

                Assert.IsTrue(primaryTop.enabled);
                Assert.IsTrue(primarySide.enabled);
                Assert.AreEqual("Instant", primaryTop.sprite.name);
                Assert.AreSame(primaryTop.sprite, primarySide.sprite);
                Assert.IsTrue(secondaryTop.enabled);
                Assert.IsTrue(secondarySide.enabled);
                Assert.AreEqual("RotationClockwise", secondaryTop.sprite.name);
                Assert.AreSame(secondaryTop.sprite, secondarySide.sprite);
                // GLSIconAlphaBlendingPreservesBloomAlpha requires filtered alpha blending whose Zero Zero alpha
                // factors write the zero bloom mask; CUSTOM_BLOOM_NONE_APPLY must stay out of this pass because its
                // a=0 overwrite would feed SrcAlpha blending zero coverage and hide every icon.
                Assert.AreEqual("ChroMapper/GLS Icon Sprite", primaryTop.sharedMaterial.shader.name);
                Assert.AreSame(primaryTop.sharedMaterial, secondaryTop.sharedMaterial);
                Assert.AreSame(primaryTop.sharedMaterial, primarySide.sharedMaterial);
                var iconShaderSource = System.IO.File.ReadAllText(GlsIconShaderPath);
                StringAssert.Contains("Blend SrcAlpha OneMinusSrcAlpha, Zero Zero", iconShaderSource);
                StringAssert.Contains("clip(color.a - _CutoutThreshold)", iconShaderSource);
                StringAssert.DoesNotContain("CUSTOM_BLOOM_NONE_APPLY(color)", iconShaderSource);
                // MinifiedIconAlphaLift pins the multiplicative minification lift that keeps distant
                // thin-stroke icons legible without re-hardening the filtered edge; the earlier
                // (a-0.5)*sqrt(footprint)+0.5 pivot collapsed mip-averaged alpha back into a binary
                // silhouette and produced the stair-stepped, scattered-dark-pixel artifacts.
                StringAssert.Contains("tex.a = saturate(tex.a * sqrt(max(footprint, 1.0)))", iconShaderSource);
                StringAssert.DoesNotContain("tex.a - 0.5", iconShaderSource);
                // TransparentTexelsStoreWhite requires alpha dilation off: the importer's dilation would
                // overwrite the generated white transparent RGB with the black ring color and reintroduce
                // the distant dark-speck artifact.
                var atlasMetaSource = System.IO.File.ReadAllText(GlsAtlasPath + ".meta");
                StringAssert.Contains("enableAlphaDilation: 0", atlasMetaSource);

                // Cycling every easing through the real prefab verifies enum order, serialized references, and sprite names together.
                foreach (EaseType easing in System.Enum.GetValues(typeof(EaseType)))
                {
                    if (easing == EaseType.None)
                    {
                        continue;
                    }

                    var translationEvent = new BaseLightTranslationBase
                    {
                        EaseType = (int)easing
                    };
                    var state = GLSEventIconResolver.Resolve(translationEvent);
                    container.EventData = translationEvent;
                    container.SetIcons(state);

                    Assert.AreEqual(state.Primary.ToString(), primaryTop.sprite.name, $"{easing} prefab mapping is incorrect.");
                    Assert.AreSame(primaryTop.sprite, primarySide.sprite);
                }

                container.EventData = new BaseLightTranslationBase();
                container.SetIcons(new GLSEventIconState(GLSEventIconType.None, GLSEventIconType.None));

                Assert.IsFalse(primaryTop.enabled);
                Assert.IsFalse(primarySide.enabled);
                Assert.IsFalse(secondaryTop.enabled);
                Assert.IsFalse(secondarySide.enabled);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // OutlineLessGlyphSetReadyForSettingSwap locks the dormant wiring: both themes render the outlined glyph
        // while the generated NoOutline set stays prefab-wired for whichever setting turns out to drive the swap.
        [Test]
        public void OutlineLessGlyphSetReadyForSettingSwap()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            var originalDarkTheme = Settings.Instance.DarkTheme;
            try
            {
                var iconView = instance.GetComponent<GLSEventIconView>();
                var primaryTop = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var container = instance.GetComponent<GLSEventContainer>();
                var translationEvent = new BaseLightTranslationBase { EaseType = (int)EaseType.InQuadratic };
                container.EventData = translationEvent;
                var state = GLSEventIconResolver.Resolve(translationEvent);

                // Both themes render the outlined glyph until the real outline-less trigger is identified.
                foreach (var darkTheme in new[] { true, false })
                {
                    Settings.Instance.DarkTheme = darkTheme;
                    container.SetIcons(state);
                    StringAssert.DoesNotContain("NoOutline", AssetDatabase.GetAssetPath(primaryTop.sprite));
                }

                // The parallel set stays fully wired index-for-index: easing slots point at NoOutline art while
                // non-generated markers share the outlined sprite.
                var viewType = typeof(GLSEventIconView);
                var icons = (Sprite[])viewType
                    .GetField("icons", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(iconView);
                var noOutlineIcons = (Sprite[])viewType
                    .GetField("noOutlineIcons", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(iconView);
                Assert.AreEqual(icons.Length, noOutlineIcons.Length);
                var easingIndex = (int)GLSEventIconType.EaseInQuadratic - 1;
                StringAssert.Contains("NoOutline", AssetDatabase.GetAssetPath(noOutlineIcons[easingIndex]));
                var rotationIndex = (int)GLSEventIconType.RotationClockwise - 1;
                Assert.AreSame(icons[rotationIndex], noOutlineIcons[rotationIndex]);
            }
            finally
            {
                Settings.Instance.DarkTheme = originalDarkTheme;
                Object.DestroyImmediate(instance);
            }
        }

        // TransformNodeLayoutMatchesOe verifies the screenshot-corrected corner ownership, shared heights, depth separation, and zero-extra-TMP contract.
        [Test]
        public void TransformNodeLayoutMatchesOe()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var iconView = instance.GetComponent<GLSEventIconView>();
                var easingTop = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var easingSide = instance.transform.Find("Primary Icon Side").GetComponent<SpriteRenderer>();
                var rotationTop = instance.transform.Find("Secondary Icon Top").GetComponent<SpriteRenderer>();
                var rotationSide = instance.transform.Find("Secondary Icon Side").GetComponent<SpriteRenderer>();
                var container = instance.GetComponent<GLSEventContainer>();
                var rotationEvent = new BaseLightRotationBase
                {
                    EaseType = (int)EaseType.InOutElastic,
                    Direction = (int)LightRotationDirection.CounterClockwise
                };

                container.EventData = rotationEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(rotationEvent));

                // TransformNodeLayoutMatchesOe moves both mirrored rotation columns one thirtieth of a node inward.
                Assert.AreEqual(0.216667f, easingTop.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(-0.216667f, rotationTop.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(0f, easingTop.transform.localPosition.x + rotationTop.transform.localPosition.x);
                // TransformNodeLayoutMatchesOe keeps the easing icon row identical across Rotation, Translation, and FloatFX.
                Assert.AreEqual(0.256667f, easingSide.transform.localPosition.y, 0.0001f);
                // TransformNodeLayoutMatchesOe keeps the enlarged direction icon's top edge fixed while it grows downward.
                Assert.AreEqual(0.2004f, rotationSide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual(0.372f, rotationSide.transform.localPosition.y + (rotationSide.transform.localScale.y * 0.5f), 0.0001f);
                // TransformNodeLayoutMatchesOe keeps artwork just five thousandths outside both physical node faces.
                Assert.AreEqual(0.505f, easingTop.transform.localPosition.y, 0.0001f);
                Assert.AreEqual(-0.505f, easingSide.transform.localPosition.z, 0.0001f);
                // EasingIconsBakeHorizontalStretchIntoArtwork moves the former 1.6x renderer stretch into the
                // generated texture, so the scale is uniform and outline thickness is equal on every axis.
                Assert.AreEqual(0.2277f, easingTop.transform.localScale.x, 0.0001f);
                Assert.AreEqual(0.2277f, easingTop.transform.localScale.y, 0.0001f);
                Assert.Greater(easingTop.sprite.rect.width, easingTop.sprite.rect.height);
                // RotationDirectionIconsAreOutlinedAndLarger grows direction artwork by 30% around its fixed top edge.
                Assert.AreEqual(0.3432f, rotationTop.transform.localScale.x, 0.0001f);
                Assert.AreEqual(0.3432f, rotationTop.transform.localScale.y, 0.0001f);
                Assert.AreEqual(2, instance.GetComponentsInChildren<TextMeshPro>(true).Length);
                // TransformValuesMoveDownFromThePreservedTranslationBaseline applies the same calibrated mesh offset on both faces.
                Assert.AreEqual(GLSEventCommon.TransformTextVerticalOffset, instance.transform.Find("TextTop").localPosition.z);
                Assert.AreEqual(GLSEventCommon.TransformTextVerticalOffset, instance.transform.Find("TextSide").localPosition.y);
                // TransformNodeLayoutMatchesOe keeps TMP outside the surface while removing the previous visible hover gap.
                Assert.AreEqual(0.506f, instance.transform.Find("TextTop").localPosition.y, 0.0001f);
                Assert.AreEqual(-0.506f, instance.transform.Find("TextSide").localPosition.z, 0.0001f);

                var translationEvent = new BaseLightTranslationBase { EaseType = (int)EaseType.InOutElastic };
                container.EventData = translationEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(translationEvent));

                Assert.AreEqual(0f, easingTop.transform.localPosition.x);
                Assert.AreEqual(0.256667f, easingSide.transform.localPosition.y, 0.0001f);
                var standardGlyphWidth = easingTop.sprite.rect.width;

                // CircularEasingsKeepOriginalWidth keeps the narrower footprint through a narrower generated
                // glyph at the same uniform scale, without moving off the shared center.
                var circularEvent = new BaseLightTranslationBase { EaseType = (int)EaseType.InOutCircular };
                container.EventData = circularEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(circularEvent));

                Assert.AreEqual(0.2277f, easingTop.transform.localScale.x, 0.0001f);
                Assert.Less(easingTop.sprite.rect.width, standardGlyphWidth);
                Assert.AreEqual(0f, easingTop.transform.localPosition.x);
                Assert.AreEqual(easingTop.transform.localPosition.x, easingSide.transform.localPosition.x);

                // FloatFxUsesTranslationLayout verifies FloatFX shares translation's centered easing position and lowered icon row.
                var floatFxEvent = new BaseFxEventFloat { Easing = (int)EaseType.InOutElastic };
                container.EventData = floatFxEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(floatFxEvent));

                Assert.AreEqual(0f, easingTop.transform.localPosition.x);
                Assert.AreEqual(0.256667f, easingSide.transform.localPosition.y, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // TransformNodeTextUsesIconAwareRows locks 70% labels in mirrored rotation columns and the full-size, one-third-height-lowered value.
        [Test]
        public void TransformNodeTextUsesIconAwareRows()
        {
            var rotation = GLSEventCommon.GetRotationInfo(new BaseLightRotationBase
            {
                Rotation = 135f,
                EaseType = (int)EaseType.InOutElastic,
                Direction = (int)LightRotationDirection.Clockwise,
                Loop = 3
            });
            var translation = GLSEventCommon.GetTranslationInfo(new BaseLightTranslationBase
            {
                Translation = 1.35f,
                EaseType = (int)EaseType.InOutElastic
            });
            var floatFx = GLSEventCommon.GetFloatFXInfo(new BaseFxEventFloat
            {
                Value = 1.35f,
                Easing = (int)EaseType.InOutElastic
            });

            StringAssert.Contains(
                "<size=58.8%><voffset=0.635em><margin-left=19.444%><margin-right=55.556%><align=center>3",
                rotation);
            StringAssert.Contains(
                "<voffset=0.585em><margin-left=55.556%><margin-right=19.444%><align=center>",
                rotation);
            StringAssert.Contains("135", rotation);
            StringAssert.DoesNotContain("CW", rotation);
            StringAssert.DoesNotContain("CCW", rotation);
            StringAssert.Contains("<line-height=55%><size=58.8%>", translation);
            StringAssert.Contains("<voffset=0.585em><align=center>", translation);
            // TransformNodeTextUsesIconAwareRows rejects alpha-hidden metric glyphs because ChroMapper's bloom text shader renders zero alpha as black.
            StringAssert.DoesNotContain("<alpha", translation);
            StringAssert.Contains("<voffset=0.635em> </voffset>", translation);
            StringAssert.Contains("<size=100%><voffset=-0.285em><align=center>135", translation);
            // FloatFxUsesTranslationLayout locks FloatFX to the exact shared easing/value typography while retaining percent scaling.
            Assert.AreEqual(translation, floatFx);
        }

        // ScaledRotationLabelsPreserveRequestedRows verifies font growth does not accidentally move easing while loop follows the enlarged direction icon.
        [Test]
        public void ScaledRotationLabelsPreserveRequestedRows()
        {
            const string beforeScaling =
                "<line-height=55%><size=49%><voffset=0.964em><margin-left=19.444%><margin-right=55.556%><align=center>3</voffset>\n" +
                "<voffset=0.679em><margin-left=55.556%><margin-right=19.444%><align=center>IOEl</voffset></size>\n" +
                "<margin=0%><size=100%><voffset=-0.296em><align=center>135</voffset></size></line-height>";
            var scaled = GLSEventCommon.GetRotationInfo(new BaseLightRotationBase
            {
                Rotation = 135f,
                EaseType = (int)EaseType.InOutElastic,
                Loop = 3
            });
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var display = instance.GetComponentsInChildren<TextMeshPro>(true)[0];
                var priorLoopBaseline = GetVisibleCharacterBaseline(display, beforeScaling, 0);
                var scaledLoopBaseline = GetVisibleCharacterBaseline(display, scaled, 0);
                var priorEasingBaseline = GetVisibleCharacterBaseline(display, beforeScaling, 1);
                var scaledEasingBaseline = GetVisibleCharacterBaseline(display, scaled, 1);

                // ScaledRotationLabelsPreserveRequestedRows measures the shared mesh shift too, preserving easing while loop follows the icon downward.
                const float previousTextVerticalOffset = -0.066667f;
                var meshShift = GLSEventCommon.TransformTextVerticalOffset - previousTextVerticalOffset;
                Assert.AreEqual(
                    -0.0396f,
                    ((scaledLoopBaseline - priorLoopBaseline) * display.transform.localScale.y) + meshShift,
                    0.002f);
                Assert.AreEqual(
                    0f,
                    ((scaledEasingBaseline - priorEasingBaseline) * display.transform.localScale.y) + meshShift,
                    0.002f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // TransformValueBaselinesMatchAcrossNodeTypes renders the real TMP geometry so rotation cannot drift above translation/FloatFX.
        [Test]
        public void TransformValueBaselinesMatchAcrossNodeTypes()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var display = instance.GetComponentsInChildren<TextMeshPro>(true)[0];
                var rotation = GLSEventCommon.GetRotationInfo(new BaseLightRotationBase
                {
                    Rotation = 135f,
                    EaseType = (int)EaseType.InOutElastic,
                    Loop = 3
                });
                var translation = GLSEventCommon.GetTranslationInfo(new BaseLightTranslationBase
                {
                    Translation = 1.35f,
                    EaseType = (int)EaseType.InOutElastic
                });

                // A rendered-character baseline catches TMP line-metric differences that identical value voffset tags alone miss.
                var rotationBaseline = GetLastVisibleCharacterBaseline(display, rotation);
                var translationBaseline = GetLastVisibleCharacterBaseline(display, translation);
                // TransformValuesMoveDownFromThePreservedTranslationBaseline verifies the visible value is exactly a tenth-node below its prior position.
                var translationNodeBaseline =
                    (translationBaseline * display.transform.localScale.y) +
                    GLSEventCommon.TransformTextVerticalOffset;
                Assert.AreEqual(translationBaseline, rotationBaseline, 0.0001f);
                Assert.AreEqual((-0.8403f * display.transform.localScale.y) - 0.1f, translationNodeBaseline, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // TransformIconAndLabelRowsMatchRequestedSpacing measures easing and loop shifts in node space, including the prefab TMP scale.
        [Test]
        public void EasingAndLoopLabelsMoveToRequestedRows()
        {
            const string previousCurrentRotation =
                "<line-height=55%><size=49%><margin-left=16.667%><margin-right=58.333%><align=center>3\n" +
                "<voffset=-0.852em><margin-left=58.333%><margin-right=16.667%><align=center>IOEl</voffset></size>\n" +
                "<margin=0%><size=100%><voffset=-0.583em><align=center>135</voffset></size></line-height>";
            var currentRotation = GLSEventCommon.GetRotationInfo(new BaseLightRotationBase
            {
                Rotation = 135f,
                EaseType = (int)EaseType.InOutElastic,
                Loop = 3
            });
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var display = instance.GetComponentsInChildren<TextMeshPro>(true)[0];
                var previousBaseline = GetVisibleCharacterBaseline(display, previousCurrentRotation, 1);
                var currentBaseline = GetVisibleCharacterBaseline(display, currentRotation, 1);
                var nodeSpaceShift =
                    ((currentBaseline - previousBaseline) * display.transform.localScale.y) +
                    GLSEventCommon.TransformTextVerticalOffset;
                var previousLoopBaseline = GetVisibleCharacterBaseline(display, previousCurrentRotation, 0);
                var currentLoopBaseline = GetVisibleCharacterBaseline(display, currentRotation, 0);
                var loopNodeSpaceShift =
                    ((currentLoopBaseline - previousLoopBaseline) * display.transform.localScale.y) +
                    GLSEventCommon.TransformTextVerticalOffset;

                Assert.AreEqual((-1f / 30f) - 0.0396f, loopNodeSpaceShift, 0.01f);
                Assert.AreEqual(1f / 15f, nodeSpaceShift, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ColorNodeLayoutUsesRequestedVerticalOffsets lifts the shared baseline by a fiftieth while the tertiary
        // icon and its hover label recover an additional twenty-fifth from their prior downward correction.
        [Test]
        public void ColorNodeLayoutUsesRequestedVerticalOffsets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var primaryTop = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var primarySide = instance.transform.Find("Primary Icon Side").GetComponent<SpriteRenderer>();
                var secondaryTop = instance.transform.Find("Secondary Icon Top").GetComponent<SpriteRenderer>();
                var secondarySide = instance.transform.Find("Secondary Icon Side").GetComponent<SpriteRenderer>();
                var tertiaryTop = instance.transform.Find("Tertiary Icon Top").GetComponent<SpriteRenderer>();
                var tertiarySide = instance.transform.Find("Tertiary Icon Side").GetComponent<SpriteRenderer>();
                var container = instance.GetComponent<GLSEventContainer>();
                var colorEvent = new BaseLightColorBase
                {
                    Easing = (int)EaseType.Linear,
                    ChromaColorEasing = (int)EaseType.InCubic,
                    Frequency = 4,
                    StrobeFade = 1,
                    ChromaStrobeEasing = (int)EaseType.OutBounce,
                    ChromaStrobeColorEasing = (int)EaseType.InOutQuadratic
                };
                container.EventData = colorEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(colorEvent));

                // Top-left transition easing icon tracks the authored colorEasing curve.
                Assert.AreEqual(-GLSEventIconView.StateIconHorizontalPosition, primaryTop.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(GLSEventCommon.ColorEasingIconHeight, primarySide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual("EaseInCubic", primaryTop.sprite.name);
                Assert.AreSame(primaryTop.sprite, primarySide.sprite);
                // Lower-right strobeEasing icon renders the authored fade curve without label text.
                Assert.AreEqual(GLSEventIconView.StateIconHorizontalPosition, secondaryTop.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(GLSEventCommon.ColorStrobeIconHeight, secondarySide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual("EaseOutBounce", secondaryTop.sprite.name);
                // Bottom-left strobeColorEasing icon.
                Assert.AreEqual(-GLSEventIconView.StateIconHorizontalPosition, tertiaryTop.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(GLSEventCommon.ColorTertiaryIconHeight, tertiarySide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual("EaseInOutQuadratic", tertiaryTop.sprite.name);
                Assert.AreSame(tertiaryTop.sprite, tertiarySide.sprite);

                // ColorNodeLayoutUsesRequestedVerticalOffsets locks the corrected shared baseline and relative icon adjustments.
                Assert.AreEqual(7f / 600f, primarySide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual(-17f / 120f, secondarySide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual(-151f / 600f, tertiarySide.transform.localPosition.y, 0.0001f);
                // Both color TMP faces use the corrected shared baseline before per-row voffsets apply.
                Assert.AreEqual(-53f / 600f, instance.transform.Find("TextTop").localPosition.z, 0.0001f);
                Assert.AreEqual(-53f / 600f, instance.transform.Find("TextSide").localPosition.y, 0.0001f);

                // Color icons render smaller than the shared transform easing art so three slots fit one face.
                Assert.Less(primaryTop.transform.localScale.x, GLSEventIconView.EasingIconHeight);
                Assert.AreEqual(
                    GLSEventIconView.EasingIconHeight * GLSEventIconView.ColorIconScale,
                    primaryTop.transform.localScale.x,
                    0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ColorHoverLabelsExplainEasingsOutsideNode drops the in-face easing abbreviations for hover labels while
        // rows 2 and 6 keep metric-preserving spaces so centered TMP never recenters the remaining values.
        [Test]
        public void ColorInfoOmitsEasingAbbreviations()
        {
            var info = GLSEventCommon.GetColorInfo(new BaseLightColorBase
            {
                Easing = (int)EaseType.Linear,
                ChromaColorEasing = (int)EaseType.InCubic,
                Brightness = 0.8f,
                Frequency = 4,
                StrobeFade = 1,
                StrobeBrightness = 0.5f,
                ChromaStrobeColorEasing = (int)EaseType.InOutQuadratic
            });

            var lines = info.Split('\n');
            Assert.AreEqual(6, lines.Length, "The two-column color layout needs six compressed text rows.");
            // Row 1: primary brightness stays centered at the top.
            StringAssert.Contains("80", lines[0]);
            // Row 2 keeps only the left-column metric space now that colorEasing explains itself on hover.
            StringAssert.Contains("margin-left=0.556em", lines[1]);
            StringAssert.Contains("<size=52%> </size>", lines[1]);
            StringAssert.DoesNotContain("I^3", info);
            // Row 3: strobe brightness in the right column keeps its corrected row-specific drop.
            StringAssert.Contains("50", lines[2]);
            StringAssert.Contains("margin-left=1.944em", lines[2]);
            StringAssert.Contains("<voffset=0.355556em>", lines[2]);
            // Row 4 reserves the lower-right band for the strobeEasing icon with no label text.
            StringAssert.Contains("margin-left=1.944em", lines[3]);
            // Row 5: strobe rate in the right column below the icon gap row.
            StringAssert.Contains("1/4", lines[4]);
            StringAssert.Contains("margin-left=1.944em", lines[4]);
            // Row 6 keeps only the left-column metric space now that strobeColorEasing explains itself on hover.
            StringAssert.Contains("margin-left=0.556em", lines[5]);
            StringAssert.Contains("<size=52%> </size>", lines[5]);
            StringAssert.DoesNotContain("IO^2", info);
            // The legacy mid-line strobe fade "L" marker is replaced by the strobeEasing icon.
            StringAssert.DoesNotContain(" L ", info);
        }

        // ColorInfoKeepsRowsFixedWithoutOptionalValues renders both TMP meshes so the centered prefab alignment
        // cannot recenter surviving rows when strobe brightness is absent: every optional row keeps a metric space.
        [Test]
        public void ColorInfoKeepsRowsFixedWithoutOptionalValues()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var display = instance.transform.Find("TextSide").GetComponent<TextMeshPro>();
                var withStrobeBrightness = GLSEventCommon.GetColorInfo(new BaseLightColorBase
                {
                    Easing = (int)EaseType.Linear,
                    Brightness = 1f,
                    Frequency = 4,
                    StrobeBrightness = 0.5f,
                    ChromaStrobeColorEasing = (int)EaseType.InOutQuadratic
                });
                var withoutStrobeBrightness = GLSEventCommon.GetColorInfo(new BaseLightColorBase
                {
                    Easing = (int)EaseType.Linear,
                    Brightness = 1f,
                    Frequency = 4,
                    ChromaStrobeColorEasing = (int)EaseType.InOutQuadratic
                });

                Assert.AreEqual(6, withStrobeBrightness.Split('\n').Length);
                Assert.AreEqual(6, withoutStrobeBrightness.Split('\n').Length);
                // The metric-preserving spacer must never surface a visible fallback glyph.
                StringAssert.DoesNotContain("50", withoutStrobeBrightness);
                // Identical baselines prove centered TMP does not recenter the surviving rows.
                Assert.AreEqual(
                    GetVisibleCharacterBaseline(display, withStrobeBrightness, 0),
                    GetVisibleCharacterBaseline(display, withoutStrobeBrightness, 0),
                    0.0001f);
                Assert.AreEqual(
                    GetVisibleCharacterBaseline(display, withStrobeBrightness, 5),
                    GetVisibleCharacterBaseline(display, withoutStrobeBrightness, 3),
                    0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ColorNodeTwoColumnLayout measures the real TMP baselines so each row lands under its icon band:
        // brightness on top, both easing labels in the left column, and the strobe track down the right.
        [Test]
        public void ColorInfoRowsLandInTheirColumnsAndBands()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var display = instance.transform.Find("TextSide").GetComponent<TextMeshPro>();
                var colorEvent = new BaseLightColorBase
                {
                    Easing = (int)EaseType.Linear,
                    ChromaColorEasing = (int)EaseType.InCubic,
                    Brightness = 0.8f,
                    Frequency = 4,
                    StrobeFade = 1,
                    StrobeBrightness = 0.5f,
                    ChromaStrobeColorEasing = (int)EaseType.InOutQuadratic
                };
                var container = instance.GetComponent<GLSEventContainer>();
                container.EventData = colorEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(colorEvent));
                display.text = GLSEventCommon.GetColorInfo(colorEvent);
                display.ForceMeshUpdate(true, true);

                var yOffset = display.transform.localPosition.y;
                var visible = display.textInfo.characterInfo
                    .Where(c => c.isVisible)
                    .Select(c => (
                        x: c.origin * display.transform.localScale.x,
                        y: (c.baseLine * display.transform.localScale.y) + yOffset))
                    .ToArray();
                // Visible glyph order: "80" | "50" | "1/4" (the easing rows and icon gap row are metric-only whitespace).
                var brightness = visible[0];
                var strobeBrightness = visible[2];
                var strobeRate = visible[4];

                // Column ownership keeps the strobe track right of face center while brightness stays centered.
                Assert.Greater(strobeBrightness.x, 0.1f, "The strobe brightness must sit in the right column.");
                Assert.Greater(strobeRate.x, 0.1f, "The strobe rate must sit in the right column.");
                Assert.Less(
                    System.Math.Abs(brightness.x),
                    0.15f,
                    "The primary brightness must stay centered.");

                // ColorNodeLayoutUsesRequestedVerticalOffsets measures the corrected shared and row-specific baselines.
                Assert.Greater(brightness.y, 0.11f, "The brightness must keep the corrected top band.");
                Assert.Less(brightness.y, 0.35f);
                Assert.Greater(strobeBrightness.y, -0.12f, "The strobe brightness must use its corrected row-specific drop.");
                Assert.Less(strobeBrightness.y, 0.08f);
                // The rate only follows the corrected shared baseline; its icon adjustment remains independent.
                Assert.Less(strobeRate.y, -0.23f, "The strobe rate must stay below the strobeEasing icon.");
                Assert.Greater(strobeRate.y, -0.47f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ColorHoverLabelsExplainEasingsOutsideNode verifies the lazy six-TMP clone, per-icon placement, exact titles, and full hide/rebind behavior.
        [Test]
        public void ColorHoverLabelsExplainEasingsOutsideNode()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var primaryTop = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var primarySide = instance.transform.Find("Primary Icon Side").GetComponent<SpriteRenderer>();
                var secondaryTop = instance.transform.Find("Secondary Icon Top").GetComponent<SpriteRenderer>();
                var secondarySide = instance.transform.Find("Secondary Icon Side").GetComponent<SpriteRenderer>();
                var tertiaryTop = instance.transform.Find("Tertiary Icon Top").GetComponent<SpriteRenderer>();
                var tertiarySide = instance.transform.Find("Tertiary Icon Side").GetComponent<SpriteRenderer>();
                var container = instance.GetComponent<GLSEventContainer>();
                var colorEvent = new BaseLightColorBase
                {
                    Easing = (int)EaseType.Linear,
                    ChromaColorEasing = (int)EaseType.InCubic,
                    Frequency = 4,
                    StrobeFade = 1,
                    ChromaStrobeEasing = (int)EaseType.OutBounce,
                    ChromaStrobeColorEasing = (int)EaseType.InOutQuadratic
                };
                container.EventData = colorEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(colorEvent));
                container.SetColorHover(true);

                var fadeTop = instance.transform.Find("Fade Ease Hover Top").GetComponent<TextMeshPro>();
                var fadeSide = instance.transform.Find("Fade Ease Hover Side").GetComponent<TextMeshPro>();
                var strobeTop = instance.transform.Find("Strobe Ease Hover Top").GetComponent<TextMeshPro>();
                var strobeSide = instance.transform.Find("Strobe Ease Hover Side").GetComponent<TextMeshPro>();
                var strobeColorTop = instance.transform.Find("Strobe Color Ease Hover Top").GetComponent<TextMeshPro>();
                var strobeColorSide = instance.transform.Find("Strobe Color Ease Hover Side").GetComponent<TextMeshPro>();
                Assert.IsNotNull(fadeTop);
                Assert.IsNotNull(fadeSide);
                Assert.IsNotNull(strobeTop);
                Assert.IsNotNull(strobeSide);
                Assert.IsNotNull(strobeColorTop);
                Assert.IsNotNull(strobeColorSide);

                Assert.IsTrue(fadeTop.gameObject.activeSelf && fadeTop.enabled);
                Assert.IsTrue(fadeSide.gameObject.activeSelf && fadeSide.enabled);
                Assert.IsTrue(strobeTop.gameObject.activeSelf && strobeTop.enabled);
                Assert.IsTrue(strobeSide.gameObject.activeSelf && strobeSide.enabled);
                Assert.IsTrue(strobeColorTop.gameObject.activeSelf && strobeColorTop.enabled);
                Assert.IsTrue(strobeColorSide.gameObject.activeSelf && strobeColorSide.enabled);

                StringAssert.Contains("<line-height=70%><size=50%>Fade Ease</size>", fadeTop.text);
                StringAssert.Contains("<size=70%>I^3</size>", fadeTop.text);
                StringAssert.Contains("<line-height=70%><size=50%>Fade Ease</size>", fadeSide.text);
                StringAssert.Contains("<size=70%>I^3</size>", fadeSide.text);
                StringAssert.Contains("<line-height=70%><size=50%>Strobe Ease</size>", strobeTop.text);
                StringAssert.Contains("<size=70%>OBo</size>", strobeTop.text);
                StringAssert.Contains("<line-height=70%><size=50%>Strobe Ease</size>", strobeSide.text);
                StringAssert.Contains("<size=70%>OBo</size>", strobeSide.text);
                StringAssert.Contains("<line-height=70%><size=50%>Strobe Color Ease</size>", strobeColorTop.text);
                StringAssert.Contains("<size=70%>IO^2 (Qd)</size>", strobeColorTop.text);
                StringAssert.Contains("<line-height=70%><size=50%>Strobe Color Ease</size>", strobeColorSide.text);
                StringAssert.Contains("<size=70%>IO^2 (Qd)</size>", strobeColorSide.text);
                // CompressedHoverEasingText aligns the readable edge toward the node on both sides.
                Assert.AreEqual(TextAlignmentOptions.Right, fadeTop.alignment);
                Assert.AreEqual(TextAlignmentOptions.Right, fadeSide.alignment);
                Assert.AreEqual(TextAlignmentOptions.Left, strobeTop.alignment);
                Assert.AreEqual(TextAlignmentOptions.Left, strobeSide.alignment);
                Assert.AreEqual(TextAlignmentOptions.Right, strobeColorTop.alignment);
                Assert.AreEqual(TextAlignmentOptions.Right, strobeColorSide.alignment);
                // CompressedHoverEasingText (line-height=70%) keeps the measured node-space
                // title-to-abbreviation separation near two tenths; the prior 0.05-0.15 band was
                // authored against line-height=42% and measured 0.18 once the tag was corrected.
                fadeTop.ForceMeshUpdate(true, true);
                var lineBaselines = fadeTop.textInfo.characterInfo
                    .Where(c => c.isVisible)
                    .GroupBy(c => c.lineNumber)
                    .Select(g => g.First().baseLine * fadeTop.transform.localScale.y)
                    .ToArray();
                Assert.AreEqual(2, lineBaselines.Length);
                var hoverLineSeparation = System.Math.Abs(lineBaselines[0] - lineBaselines[1]);
                Assert.Greater(hoverLineSeparation, 0.05f);
                Assert.Less(hoverLineSeparation, 0.25f);

                // Left labels sit beyond the left face edge; the strobe label sits beyond the right edge.
                Assert.Less(fadeTop.transform.localPosition.x, -0.5f);
                Assert.Less(fadeSide.transform.localPosition.x, -0.5f);
                Assert.Less(strobeColorTop.transform.localPosition.x, -0.5f);
                Assert.Less(strobeColorSide.transform.localPosition.x, -0.5f);
                Assert.Greater(strobeTop.transform.localPosition.x, 0.5f);
                Assert.Greater(strobeSide.transform.localPosition.x, 0.5f);
                // Labels share the same physical planes as the icon and text faces.
                Assert.Greater(fadeTop.transform.localPosition.y, 0.5f);
                Assert.Less(fadeSide.transform.localPosition.z, -0.5f);
                // Each label's vertical coordinate tracks its matching icon's, including the two
                // requested nudges: Fade sits +1/20 above ColorEasingIconHeight and Strobe Color sits
                // -1/15 below ColorTertiaryIconHeight (GLSEventIconView.EnsureColorHoverDisplays).
                Assert.AreEqual(primaryTop.transform.localPosition.z + (1f / 20f), fadeTop.transform.localPosition.z, 0.0001f);
                Assert.AreEqual(primarySide.transform.localPosition.y + (1f / 20f), fadeSide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual(secondaryTop.transform.localPosition.z, strobeTop.transform.localPosition.z, 0.0001f);
                Assert.AreEqual(secondarySide.transform.localPosition.y, strobeSide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual(tertiaryTop.transform.localPosition.z - (1f / 15f), strobeColorTop.transform.localPosition.z, 0.0001f);
                Assert.AreEqual(tertiarySide.transform.localPosition.y - (1f / 15f), strobeColorSide.transform.localPosition.y, 0.0001f);

                // QuadraticHoverLabelsNameOeFamily appends the OE family name only to the color node's outside easing labels.
                foreach (var quadratic in new[]
                {
                    EaseType.InQuadratic,
                    EaseType.OutQuadratic,
                    EaseType.InOutQuadratic
                })
                {
                    var quadraticEvent = new BaseLightColorBase
                    {
                        Easing = (int)EaseType.Linear,
                        ChromaColorEasing = (int)quadratic,
                        Frequency = 4,
                        StrobeFade = 1,
                        ChromaStrobeEasing = (int)quadratic,
                        ChromaStrobeColorEasing = (int)quadratic
                    };
                    container.EventData = quadraticEvent;
                    container.SetIcons(GLSEventIconResolver.Resolve(quadraticEvent));
                    StringAssert.Contains($"{Easing.IDToShortName[(int)quadratic]} (Qd)", fadeTop.text);
                    StringAssert.Contains($"{Easing.IDToShortName[(int)quadratic]} (Qd)", fadeSide.text);
                    StringAssert.Contains($"{Easing.IDToShortName[(int)quadratic]} (Qd)", strobeTop.text);
                    StringAssert.Contains($"{Easing.IDToShortName[(int)quadratic]} (Qd)", strobeSide.text);
                    StringAssert.Contains($"{Easing.IDToShortName[(int)quadratic]} (Qd)", strobeColorTop.text);
                    StringAssert.Contains($"{Easing.IDToShortName[(int)quadratic]} (Qd)", strobeColorSide.text);
                }

                // Repeated per-frame hover notifications must return before rebuilding label strings or touching TMP text.
                var retainedFadeText = fadeTop.text;
                container.SetColorHover(true);
                Assert.AreSame(retainedFadeText, fadeTop.text);

                container.SetColorHover(false);
                Assert.IsFalse(fadeTop.gameObject.activeSelf);
                Assert.IsFalse(fadeSide.gameObject.activeSelf);
                Assert.IsFalse(strobeTop.gameObject.activeSelf);
                Assert.IsFalse(strobeSide.gameObject.activeSelf);
                Assert.IsFalse(strobeColorTop.gameObject.activeSelf);
                Assert.IsFalse(strobeColorSide.gameObject.activeSelf);

                // A pooled rebind to a non-color event must never reactivate the retained label clones.
                var translationEvent = new BaseLightTranslationBase { EaseType = (int)EaseType.InOutElastic };
                container.EventData = translationEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(translationEvent));
                container.SetColorHover(true);
                Assert.IsFalse(fadeTop.gameObject.activeSelf);
                Assert.IsFalse(fadeSide.gameObject.activeSelf);
                Assert.IsFalse(strobeTop.gameObject.activeSelf);
                Assert.IsFalse(strobeSide.gameObject.activeSelf);
                Assert.IsFalse(strobeColorTop.gameObject.activeSelf);
                Assert.IsFalse(strobeColorSide.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ColorHoverLabelsExplainEasingsOutsideNode proves the outer preview forwards through the same pooled icon view.
        [Test]
        public void OuterPreviewColorHoverLabelsUseSharedIconView()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsGroupPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var colorEvent = new BaseLightColorBase
                {
                    Easing = (int)EaseType.Linear,
                    ChromaColorEasing = (int)EaseType.InCubic,
                    Frequency = 4
                };
                var box = new BaseLightColorEventBox
                {
                    Events = new[] { colorEvent }
                };
                var group = new BaseLightColorEventBoxGroup();
                group.Boxes.Add(box);
                group.ResortOrderedEvents();

                var container = instance.GetComponent<GLSGroupContainer>();
                container.EventBoxGroupData = group;
                container.PreviewEventData = colorEvent;
                container.SetIcons(colorEvent);
                container.SetColorHover(true);

                var fadeTop = instance.transform.Find("Fade Ease Hover Top").GetComponent<TextMeshPro>();
                Assert.IsNotNull(fadeTop);
                Assert.IsTrue(fadeTop.gameObject.activeSelf);
                StringAssert.Contains("Fade Ease", fadeTop.text);
                StringAssert.Contains("I^3", fadeTop.text);

                container.SetColorHover(false);
                Assert.IsFalse(fadeTop.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // OuterPreviewPrefabRendersIcons covers the outer GLS group path that previously only formatted text and never owned an icon view.
        [Test]
        public void OuterPreviewPrefabRendersIcons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlsGroupPrefabPath);
            var appearance = AssetDatabase.LoadAssetAtPath<GLSGroupAppearanceSO>(
                "Assets/__Scripts/Beatmap/Appearances/GLSGroupAppearanceSO.asset");
            var instance = Object.Instantiate(prefab);
            try
            {
                var rotationEvent = new BaseLightRotationBase
                {
                    Rotation = 90f,
                    EaseType = (int)EaseType.InQuadratic,
                    Direction = (int)LightRotationDirection.Clockwise,
                    Loop = 2
                };
                var box = new BaseLightRotationEventBox
                {
                    Events = new[] { rotationEvent }
                };
                var group = new BaseLightRotationEventBoxGroup();
                group.Boxes.Add(box);
                group.ResortOrderedEvents();

                var container = instance.GetComponent<GLSGroupContainer>();
                container.EventBoxGroupData = group;
                container.PreviewEventData = rotationEvent;
                appearance.SetAppearance(container);

                var iconView = instance.GetComponent<GLSEventIconView>();
                Assert.IsNotNull(iconView);
                var easingTop = instance.transform.Find("Primary Icon Top").GetComponent<SpriteRenderer>();
                var directionTop = instance.transform.Find("Secondary Icon Top").GetComponent<SpriteRenderer>();
                Assert.IsTrue(easingTop.enabled);
                Assert.AreEqual("EaseInQuadratic", easingTop.sprite.name);
                Assert.IsTrue(directionTop.enabled);
                Assert.AreEqual("RotationClockwise", directionTop.sprite.name);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // GeneratedEasingIconsUseReducedStrokeWidth locks the requested 25% reduction into the reproducible asset pipeline.
        [Test]
        public void GeneratedEasingIconsUseReducedStrokeWidth()
        {
            var source = System.IO.File.ReadAllText(GlsEasingGeneratorPath);

            // SingleBorderWidthConstant locks the authored border ring and the derived black/white pens, and
            // EasingIconsBakeHorizontalStretchIntoArtwork locks the display-aspect canvases into the pipeline.
            StringAssert.Contains("$outlineWidth = $foregroundWidth + (2 * $borderWidth)", source);
            // BorderSplitHalfwayBackToThick pins the ring at the midpoint of the two evaluated weights;
            // the total stroke stays 22.5px via the derived outline width.
            StringAssert.Contains("$borderWidth = 5.34375", source);
            StringAssert.Contains("$easingDisplayAspect = 0.3168 / 0.198", source);
            StringAssert.Contains("$circularDisplayAspect = 0.22 / 0.198", source);
            StringAssert.Contains("[System.Drawing.Color]::Black, $outlineWidth", source);
            StringAssert.Contains("[System.Drawing.Color]::White, $foregroundWidth", source);
            // OutlineLessGlyphSetReadyForSettingSwap locks the parallel white-only directory into the generator.
            StringAssert.Contains("$noOutlineSubdirectory = 'NoOutline'", source);
            // TransparentTexelsStoreWhite locks the a=0 white RGB fill that keeps straight-alpha mip
            // averages clean; black transparent texels produced the scattered dark specks seen on
            // distant icons.
            StringAssert.Contains("if ($pixelBytes[$p + 3] -eq 0)", source);
            // GeneratedIconsUseOutlinedStrokes verifies the checked-in output, not only the generator configuration.
            AssertPngContainsBlackAndWhitePixels(
                "Assets/_Graphics/Textures/GLS Event Icons/Easings/EaseInOutElastic.png");
            AssertPngTransparentPixelsAreWhite(
                "Assets/_Graphics/Textures/GLS Event Icons/Easings/EaseInOutElastic.png");
            // OutlineLessGlyphSetReadyForSettingSwap verifies the outline-less variant kept the white core but dropped every black pixel.
            AssertPngIsWhiteOnly(
                "Assets/_Graphics/Textures/GLS Event Icons/Easings/NoOutline/EaseInOutElastic.png");
        }

        // GeneratedRotationIconsUsePerfectMirroredArcs locks CW/CCW to mathematical circle halves and AUTO to pink-inset cat ears.
        [Test]
        public void GeneratedRotationIconsUsePerfectMirroredArcs()
        {
            Assert.IsTrue(
                System.IO.File.Exists(GlsRotationGeneratorPath),
                "The mathematical rotation icon generator is missing.");
            var source = System.IO.File.ReadAllText(GlsRotationGeneratorPath);

            // GeneratedRotationIconsUsePerfectMirroredArcs requires symmetric vertical canvas padding so enlarged arrows cannot be clipped.
            StringAssert.Contains("$iconWidth = 128", source);
            StringAssert.Contains("$iconHeight = 160", source);
            StringAssert.Contains("$verticalPadding = 16.0", source);
            StringAssert.Contains("$circleRadius = 43.0", source);
            StringAssert.Contains("New-CircularArcPoints", source);
            // GeneratedRotationIconsUsePerfectMirroredArcs requires AUTO's single bottom-contiguous ring with a deliberate opening at the top.
            StringAssert.Contains("New-OpenAutoRingPoints", source);
            StringAssert.Contains("Get-MirroredPoints", source);
            // GeneratedRotationIconsUsePerfectMirroredArcs constructs every arrow and AUTO ear from one triangle primitive.
            StringAssert.Contains("$arrowScale = 3.0", source);
            StringAssert.Contains("New-ArrowTriangle", source);
            // GeneratedRotationIconsUsePerfectMirroredArcs enlarges only the black silhouette to thicken every arrow border.
            StringAssert.Contains("$arrowBlackScale = 1.05", source);
            StringAssert.Contains("$arrowWhiteScale = 0.78", source);
            StringAssert.Contains("$autoPinkScale = $arrowWhiteScale * 0.5", source);
            // GeneratedRotationIconsUsePerfectMirroredArcs exposes direction/AUTO angles and placement as adjacent user-tunable constants.
            StringAssert.Contains("$directionArrowHeadingDegrees = -20.0", source);
            // GeneratedRotationIconsUsePerfectMirroredArcs preserves the user-tuned AUTO geometry independently from direction arrows.
            StringAssert.Contains("$autoArrowHeadingDegrees = 10", source);
            StringAssert.Contains("$autoArrowVerticalOffset = -7.0", source);
            StringAssert.Contains("$autoTriangleCenterOffset = 7.5", source);
            // GeneratedRotationIconsUsePerfectMirroredArcs draws complete color layers so white intersections erase internal black seams.
            StringAssert.Contains("Draw-BlackGlyphLayer", source);
            StringAssert.Contains("Draw-WhiteGlyphLayer", source);
            StringAssert.Contains("Draw-PinkAutoLayer", source);
            StringAssert.Contains("$outlineWidth = 18.0", source);
            StringAssert.Contains("255, 255, 182, 193", source);
            // GeneratedRotationIconsUsePerfectMirroredArcs prevents the OE reference extractor from reclaiming generated filenames.
            var extractor = System.IO.File.ReadAllText(GlsOeIconExtractorPath);
            StringAssert.DoesNotContain("RotationAutomatic", extractor);
            StringAssert.DoesNotContain("RotationClockwise", extractor);
            StringAssert.DoesNotContain("RotationCounterClockwise", extractor);
            // GeneratedIconsUseOutlinedStrokes verifies each selectable rotation direction was regenerated with both layers.
            AssertPngContainsBlackAndWhitePixels(
                "Assets/_Graphics/Textures/GLS Event Icons/RotationAutomatic.png");
            AssertPngContainsBlackAndWhitePixels(
                "Assets/_Graphics/Textures/GLS Event Icons/RotationClockwise.png");
            AssertPngContainsBlackAndWhitePixels(
                "Assets/_Graphics/Textures/GLS Event Icons/RotationCounterClockwise.png");
            // GeneratedRotationIconsUsePerfectMirroredArcs verifies AUTO contains the requested opaque light-pink inner triangles.
            AssertPngHasDimensionsAndColor(
                "Assets/_Graphics/Textures/GLS Event Icons/RotationAutomatic.png",
                128,
                160,
                new Color32(255, 182, 193, 255));
            AssertPngBorderIsTransparent(
                "Assets/_Graphics/Textures/GLS Event Icons/RotationAutomatic.png");
        }

        // GeneratedRotationIconsUsePerfectMirroredArcs checks the authored bitmap rather than trusting generator source constants alone.
        private static void AssertPngHasDimensionsAndColor(
            string path,
            int expectedWidth,
            int expectedHeight,
            Color32 expectedColor)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(System.IO.File.ReadAllBytes(path)));
                Assert.AreEqual(expectedWidth, texture.width);
                Assert.AreEqual(expectedHeight, texture.height);
                var found = false;
                foreach (var pixel in texture.GetPixels32())
                {
                    if (pixel.r == expectedColor.r
                        && pixel.g == expectedColor.g
                        && pixel.b == expectedColor.b
                        && pixel.a == expectedColor.a)
                    {
                        found = true;
                        break;
                    }
                }

                Assert.IsTrue(found, $"{path} does not contain the requested inner-triangle color.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // GeneratedRotationIconsUsePerfectMirroredArcs verifies padding survives generation instead of merely enlarging the bitmap around clipped pixels.
        private static void AssertPngBorderIsTransparent(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(System.IO.File.ReadAllBytes(path)));
                for (var x = 0; x < texture.width; x++)
                {
                    Assert.AreEqual(0f, texture.GetPixel(x, 0).a, 0.001f, $"{path} touches the bottom border at x={x}.");
                    Assert.AreEqual(0f, texture.GetPixel(x, texture.height - 1).a, 0.001f, $"{path} touches the top border at x={x}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // OutlineLessGlyphSetReadyForSettingSwap proves the parallel glyph kept its white core while dropping the black underlay entirely.
        private static void AssertPngIsWhiteOnly(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(System.IO.File.ReadAllBytes(path)));
                var whitePixelCount = 0;
                foreach (var pixel in texture.GetPixels32())
                {
                    if (pixel.a < 128)
                    {
                        continue;
                    }

                    Assert.IsFalse(
                        pixel.r < 32 && pixel.g < 32 && pixel.b < 32,
                        $"{path} still contains opaque black outline pixels.");
                    if (pixel.r > 223 && pixel.g > 223 && pixel.b > 223)
                    {
                        whitePixelCount++;
                    }
                }

                Assert.Greater(whitePixelCount, 0, $"{path} contains no opaque white foreground pixels.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // TransparentTexelsStoreWhite proves the generated artwork ships white RGB in fully transparent
        // texels; minification averages straight-alpha color equally, so black transparent pixels would
        // resurface as dark specks at distance even though they never render at base resolution.
        private static void AssertPngTransparentPixelsAreWhite(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(System.IO.File.ReadAllBytes(path)));
                foreach (var pixel in texture.GetPixels32())
                {
                    if (pixel.a != 0)
                    {
                        continue;
                    }

                    Assert.IsTrue(
                        pixel.r == 255 && pixel.g == 255 && pixel.b == 255,
                        $"{path} stores non-white RGB ({pixel.r},{pixel.g},{pixel.b}) in a fully transparent pixel.");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // GeneratedIconsUseOutlinedStrokes reads source PNG pixels directly so atlas/import behavior cannot mask missing artwork layers.
        private static void AssertPngContainsBlackAndWhitePixels(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(System.IO.File.ReadAllBytes(path)));
                var blackPixelCount = 0;
                var whitePixelCount = 0;
                foreach (var pixel in texture.GetPixels32())
                {
                    if (pixel.a < 128)
                    {
                        continue;
                    }

                    if (pixel.r < 32 && pixel.g < 32 && pixel.b < 32)
                    {
                        blackPixelCount++;
                    }

                    if (pixel.r > 223 && pixel.g > 223 && pixel.b > 223)
                    {
                        whitePixelCount++;
                    }
                }

                Assert.Greater(blackPixelCount, 0, $"{path} contains no opaque black outline pixels.");
                Assert.Greater(whitePixelCount, 0, $"{path} contains no opaque white foreground pixels.");
                // VisibleIconOutlinesSurviveAtlasDownsampling rejects the prior subpixel border that appeared pure white on GLS nodes.
                Assert.GreaterOrEqual(
                    (float)blackPixelCount / whitePixelCount,
                    0.5f,
                    $"{path} black outline is too thin to survive node-scale filtering.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // Render through the prefab's actual TMP settings because its line spacing and font metrics determine the visible row height.
        private static float GetLastVisibleCharacterBaseline(TextMeshPro display, string text)
        {
            return GetVisibleCharacterBaseline(display, text, -1);
        }

        // TMP rich-text tags are excluded from characterInfo, so a visible-character ordinal addresses the rendered glyph directly.
        private static float GetVisibleCharacterBaseline(TextMeshPro display, string text, int visibleCharacterIndex)
        {
            display.text = text;
            // Force inactive pooled prefab children to populate textInfo during batch-mode regression tests.
            display.ForceMeshUpdate(true, true);
            var visibleCount = 0;
            for (var i = 0; i < display.textInfo.characterCount; i++)
            {
                var character = display.textInfo.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                if (visibleCharacterIndex < 0)
                {
                    visibleCount++;
                    continue;
                }

                if (visibleCount == visibleCharacterIndex)
                {
                    return character.baseLine;
                }

                visibleCount++;
            }

            if (visibleCharacterIndex < 0 && visibleCount > 0)
            {
                for (var i = display.textInfo.characterCount - 1; i >= 0; i--)
                {
                    var character = display.textInfo.characterInfo[i];
                    if (character.isVisible)
                    {
                        return character.baseLine;
                    }
                }
            }

            Assert.Fail($"Expected visible character {visibleCharacterIndex} in transform text.");
            return 0f;
        }

    }
}
