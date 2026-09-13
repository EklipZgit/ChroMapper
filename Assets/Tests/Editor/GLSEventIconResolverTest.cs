using System.Collections.Generic;
using System.Linq;
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
            StringAssert.Contains(expected, GLSEventCommon.GetColorInfo(new BaseLightColorBase { Easing = value }));
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

        // ColorNodeTwoColumnLayout: the middle-right slot renders the actual strobe fade curve (the native
        // InOutCubic or an authored strobeEasing) while hard strobes keep the Instant marker.
        [TestCase((int)EaseType.None, 0, 0, GLSEventIconType.Instant, GLSEventIconType.None)]
        [TestCase((int)EaseType.Linear, 0, 0, GLSEventIconType.EaseLinear, GLSEventIconType.None)]
        [TestCase((int)EaseType.InQuadratic, 4, 0, GLSEventIconType.EaseInQuadratic, GLSEventIconType.Instant)]
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
        // middle-right slot, and authored strobeColorEasing owns the new bottom-left tertiary slot.
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
                "The middle-right icon must track the authored strobeEasing curve.");
            Assert.AreEqual(GLSEventIconType.EaseInOutQuadratic, state.Tertiary,
                "The bottom-left icon must track the authored strobeColorEasing curve.");
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

            Assert.AreEqual(GLSEventIconType.Instant, state.Primary);
            Assert.AreEqual(GLSEventIconType.EaseInOutCubic, state.Secondary);
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
            var packedSprites = new Sprite[42];
            Assert.AreEqual(42, atlas.GetSprites(packedSprites));

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
                // PrefabWiresBothFacesToTheSharedSpriteAtlas requires low-threshold alpha clipping with a zero bloom mask.
                Assert.AreEqual("ChroMapper/GLS Icon Sprite", primaryTop.sharedMaterial.shader.name);
                Assert.AreSame(primaryTop.sharedMaterial, secondaryTop.sharedMaterial);
                Assert.AreSame(primaryTop.sharedMaterial, primarySide.sharedMaterial);
                var iconShaderSource = System.IO.File.ReadAllText(GlsIconShaderPath);
                StringAssert.Contains("Blend Off", iconShaderSource);
                StringAssert.Contains("clip(color.a - _CutoutThreshold)", iconShaderSource);
                StringAssert.Contains("CUSTOM_BLOOM_NONE_APPLY(color)", iconShaderSource);

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
                // TransformNodeLayoutMatchesOe locks the additional 20% horizontal stretch for non-Circular easing glyphs.
                Assert.AreEqual(0.36432f, easingTop.transform.localScale.x, 0.0001f);
                Assert.AreEqual(0.2277f, easingTop.transform.localScale.y, 0.0001f);
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

                // CircularEasingsKeepOriginalWidth verifies Circular glyphs retain 0.22 width without moving off the shared center.
                var circularEvent = new BaseLightTranslationBase { EaseType = (int)EaseType.InOutCircular };
                container.EventData = circularEvent;
                container.SetIcons(GLSEventIconResolver.Resolve(circularEvent));

                Assert.AreEqual(0.253f, easingTop.transform.localScale.x, 0.0001f);
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

        // ColorNodeTwoColumnLayout places the transition easing icon+abbrev top-left, strobeColorEasing
        // icon+abbrev bottom-left, and the strobe track down the middle-right with a dedicated strobeEasing icon.
        [Test]
        public void ColorNodeLayoutPlacesIconsInLeftAndRightColumns()
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
                // Middle-right strobeEasing icon renders the authored fade curve without label text.
                Assert.AreEqual(GLSEventIconView.StateIconHorizontalPosition, secondaryTop.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(GLSEventCommon.ColorStrobeIconHeight, secondarySide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual("EaseOutBounce", secondaryTop.sprite.name);
                // Bottom-left strobeColorEasing icon.
                Assert.AreEqual(-GLSEventIconView.StateIconHorizontalPosition, tertiaryTop.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(GLSEventCommon.ColorTertiaryIconHeight, tertiarySide.transform.localPosition.y, 0.0001f);
                Assert.AreEqual("EaseInOutQuadratic", tertiaryTop.sprite.name);
                Assert.AreSame(tertiaryTop.sprite, tertiarySide.sprite);

                // Color icons render smaller than the shared transform easing art so three slots fit one face.
                Assert.Less(primaryTop.transform.localScale.x, GLSEventIconView.EasingIconWidth);
                Assert.AreEqual(
                    GLSEventIconView.EasingIconWidth * GLSEventIconView.ColorIconScale,
                    primaryTop.transform.localScale.x,
                    0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ColorNodeTwoColumnLayout drops the old centered "L" easing abbrev and the strobe-line fade marker
        // for a left easing column, a right strobe column, and no strobeEasing label text.
        [Test]
        public void ColorInfoArrangesEasingLabelsLeftAndStrobeTrackRight()
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
            // Row 2: effective color easing abbrev under the top-left icon in the left column.
            StringAssert.Contains("I^3", lines[1]);
            StringAssert.Contains("margin-left=0.556em", lines[1]);
            // Row 3: strobe brightness in the right column just below the brightness.
            StringAssert.Contains("50", lines[2]);
            StringAssert.Contains("margin-left=1.944em", lines[2]);
            // Row 4 reserves the middle-right band for the strobeEasing icon with no label text.
            StringAssert.Contains("margin-left=1.944em", lines[3]);
            // Row 5: strobe rate in the right column below the icon gap row.
            StringAssert.Contains("1/4", lines[4]);
            StringAssert.Contains("margin-left=1.944em", lines[4]);
            // Row 6: authored strobeColorEasing abbrev in the left column.
            StringAssert.Contains("IO^2", lines[5]);
            StringAssert.Contains("margin-left=0.556em", lines[5]);
            // The legacy mid-line strobe fade "L" marker is replaced by the strobeEasing icon.
            StringAssert.DoesNotContain(" L ", info);
        }

        // ColorNodeTwoColumnLayout keeps the row cadence stable on non-strobing nodes so the easing
        // labels never drift when the right column empties.
        [Test]
        public void ColorInfoKeepsRowCadenceWithoutStrobe()
        {
            var info = GLSEventCommon.GetColorInfo(new BaseLightColorBase
            {
                Easing = (int)EaseType.Linear,
                Brightness = 1f
            });

            var lines = info.Split('\n');
            Assert.AreEqual(6, lines.Length);
            StringAssert.Contains("100", lines[0]);
            StringAssert.Contains("L", lines[1]);
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
                // Visible glyph order: "80" | "I^3" | "50" | "1/4" | "IO^2" (the icon gap row is whitespace).
                var brightness = visible[0];
                var easingAbbrev = visible[2];
                var strobeBrightness = visible[5];
                var strobeRate = visible[7];
                var strobeColorAbbrev = visible[10];

                // Column ownership: left-column labels sit left of face center, the strobe track right of it.
                Assert.Less(easingAbbrev.x, -0.1f, "The color easing abbrev must sit in the left column.");
                Assert.Less(strobeColorAbbrev.x, -0.1f, "The strobeColorEasing abbrev must sit in the left column.");
                Assert.Greater(strobeBrightness.x, 0.1f, "The strobe brightness must sit in the right column.");
                Assert.Greater(strobeRate.x, 0.1f, "The strobe rate must sit in the right column.");
                Assert.Less(
                    System.Math.Abs(brightness.x),
                    0.15f,
                    "The primary brightness must stay centered.");

                // Vertical bands keep each label inside its icon slot on the node face.
                Assert.Greater(brightness.y, 0.18f, "The brightness must keep the top band.");
                Assert.Less(brightness.y, 0.40f);
                Assert.Greater(easingAbbrev.y, 0.02f, "The easing abbrev must sit just under the top-left icon.");
                Assert.Less(easingAbbrev.y, 0.20f);
                Assert.Greater(strobeBrightness.y, 0.02f, "The strobe brightness must stay just under the brightness.");
                Assert.Less(strobeBrightness.y, 0.20f);
                Assert.Less(strobeRate.y, -0.18f, "The strobe rate must sit below the strobeEasing icon.");
                Assert.Greater(strobeRate.y, -0.40f);
                Assert.Less(strobeColorAbbrev.y, -0.20f, "The strobeColorEasing abbrev must reach the bottom-left band.");
                Assert.Greater(strobeColorAbbrev.y, -0.46f);
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

            // GeneratedIconsUseOutlinedStrokes locks black 120%-width underpainting and a 90%-width white foreground into the easing pipeline.
            StringAssert.Contains("$outlineWidth = $lineWidth * 2.0", source);
            StringAssert.Contains("$foregroundWidth = $lineWidth * 0.9", source);
            StringAssert.Contains("[System.Drawing.Color]::Black, $outlineWidth", source);
            StringAssert.Contains("[System.Drawing.Color]::White, $foregroundWidth", source);
            // GeneratedIconsUseOutlinedStrokes verifies the checked-in output, not only the generator configuration.
            AssertPngContainsBlackAndWhitePixels(
                "Assets/_Graphics/Textures/GLS Event Icons/Easings/EaseInOutElastic.png");
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
