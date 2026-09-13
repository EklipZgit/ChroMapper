using System.Collections.Generic;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
// The regression fixture creates authoritative GLS groups through the same factory used by loaded maps.
using Beatmap.Helper;
using Beatmap.Shared;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.Editor
{
    public class GLSColorEasingInputTest : TestBase
    {
        // CtrlShiftScrollCyclesToFirstCustomColorEasing proves Ctrl+Shift+scroll moves a Linear node to the first
        // authored customData.colorEasing curve instead of toggling the transition off.
        [Test]
        public void InnerGlsColorNodeCtrlShiftScrollCyclesToFirstCustomColorEasing()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 1, null);
            var containerObject = new GameObject("Inner color easing test container");
            var controllerObject = new GameObject("Inner color easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual((int)EaseType.Linear, evt.Easing,
                    "Ctrl+Shift+scroll must keep a Linear transition while cycling authored color easings.");
                Assert.AreEqual(1, evt.CustomData["colorEasing"].AsInt,
                    "Ctrl+Shift+scroll up must author the first custom curve in customData.colorEasing.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // CtrlShiftScrollOnInstantPromotesThenCycles proves an instant node first gains the OEM Linear transition
        // and only then receives a customData.colorEasing curve, because a custom interval easing needs a transition.
        [Test]
        public void InnerGlsInstantColorNodeCtrlShiftScrollPromotesToLinear()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null);
            var containerObject = new GameObject("Inner instant easing test container");
            var controllerObject = new GameObject("Inner instant easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual((int)EaseType.Linear, evt.Easing,
                    "Scrolling an instant node forward must promote it to the OEM Linear transition.");
                Assert.IsFalse(evt.CustomData.HasKey("colorEasing"),
                    "The OEM Linear state must not serialize a redundant customData.colorEasing key.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // CtrlShiftScrollPastLastCurveWrapsToNoTransition proves the cycle ends by removing the transition entirely
        // instead of leaving a stale curve behind.
        [Test]
        public void InnerGlsColorNodeCtrlShiftScrollWrapsToNoTransition()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, "\"colorEasing\":102", 1, null);
            var containerObject = new GameObject("Inner wrap easing test container");
            var controllerObject = new GameObject("Inner wrap easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual((int)EaseType.None, evt.Easing,
                    "Scrolling past the last authored curve must wrap to the no-transition state.");
                Assert.IsFalse(evt.CustomData.HasKey("colorEasing"));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // CtrlShiftScrollDownFromFirstCurveRestoresOemLinear proves the customData key is removed when the cycle
        // lands on the OEM Linear slot so the native easing takes over.
        [Test]
        public void InnerGlsColorNodeCtrlShiftScrollDownRestoresOemLinear()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, "\"colorEasing\":1", 1, null);
            var containerObject = new GameObject("Inner restore easing test container");
            var controllerObject = new GameObject("Inner restore easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, -1f, Key.LeftCtrl, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual((int)EaseType.Linear, evt.Easing,
                    "Scrolling down past the first curve must restore the OEM Linear transition.");
                Assert.IsFalse(evt.CustomData.HasKey("colorEasing"),
                    "The OEM slot must remove customData.colorEasing so the native curve takes over.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterCtrlShiftScrollCyclesColorEasing proves the outer lane preview nodes share the same color-easing cycle.
        [Test]
        public void OuterGlsColorNodeCtrlShiftScrollCyclesColorEasing()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(0, null, 1, null);
            var containerObject = new GameObject("Outer color easing test container");
            var controllerObject = new GameObject("Outer color easing test controller");
            try
            {
                var container = CreateOuterContainer(containerObject, group, group.Boxes[0].Events[0]);
                var controller = CreateOuterController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual((int)EaseType.Linear, evt.Easing);
                Assert.AreEqual(1, evt.CustomData["colorEasing"].AsInt);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // AltShiftScrollCyclesStrobeColorEasing proves Alt+Shift+scroll authors customData.strobeColorEasing without
        // touching the interval easing or the normal color easing.
        [Test]
        public void InnerGlsColorNodeAltShiftScrollCyclesStrobeColorEasing()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, "\"colorEasing\":4", 1, null);
            var containerObject = new GameObject("Inner strobe color easing test container");
            var controllerObject = new GameObject("Inner strobe color easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(1, evt.CustomData["strobeColorEasing"].AsInt,
                    "Alt+Shift+scroll up must author the first custom curve in customData.strobeColorEasing.");
                Assert.AreEqual(4, evt.CustomData["colorEasing"].AsInt,
                    "The strobe color easing cycle must not disturb the authored colorEasing.");
                Assert.AreEqual((int)EaseType.Linear, evt.Easing);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // AltShiftScrollOnInstantPromotesToLinear proves a strobe color easing on an instant node promotes the node
        // to Linear so the authored curve has an interval to drive.
        [Test]
        public void InnerGlsInstantColorNodeAltShiftScrollPromotesToLinear()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null);
            var containerObject = new GameObject("Inner instant strobe easing test container");
            var controllerObject = new GameObject("Inner instant strobe easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual((int)EaseType.Linear, evt.Easing,
                    "Authoring strobeColorEasing on an instant node must promote it to a Linear transition.");
                Assert.AreEqual(1, evt.CustomData["strobeColorEasing"].AsInt);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // AltShiftScrollPastLastCurveRemovesKey proves the strobe color easing cycle wraps to the unset state by
        // deleting the key rather than writing an out-of-range value.
        [Test]
        public void InnerGlsColorNodeAltShiftScrollWrapsToUnset()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, "\"strobeColorEasing\":102", 1, null);
            var containerObject = new GameObject("Inner strobe wrap test container");
            var controllerObject = new GameObject("Inner strobe wrap test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.IsFalse(evt.CustomData.HasKey("strobeColorEasing"),
                    "Scrolling past the last strobe color curve must remove the key and restore native progress.");
                Assert.AreEqual((int)EaseType.Linear, evt.Easing);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterAltShiftScrollCyclesStrobeColorEasing proves the outer lane view shares the strobe color easing cycle.
        [Test]
        public void OuterGlsColorNodeAltShiftScrollCyclesStrobeColorEasing()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(0, null, 1, null);
            var containerObject = new GameObject("Outer strobe easing test container");
            var controllerObject = new GameObject("Outer strobe easing test controller");
            try
            {
                var container = CreateOuterContainer(containerObject, group, group.Boxes[0].Events[0]);
                var controller = CreateOuterController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual(1, replacement.Boxes[0].Events[0].CustomData["strobeColorEasing"].AsInt);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterGhostAltShiftScrollCyclesStrobeColorEasing proves the translucent later-node preview shares the cycle.
        [Test]
        public void OuterGhostGlsColorNodeAltShiftScrollCyclesStrobeColorEasing()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(0, null, 0, null);
            var containerObject = new GameObject("Outer ghost strobe easing test container");
            var controllerObject = new GameObject("Outer ghost strobe easing test controller");
            try
            {
                var container = CreateOuterContainer(containerObject, group, group.Boxes[0].Events[1], ghost: true);
                var controller = CreateOuterController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual(1, replacement.Boxes[0].Events[1].CustomData["strobeColorEasing"].AsInt,
                    "The ghost preview's strobe color easing must land on the later node it represents.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("strobeColorEasing"));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // ShiftScrollFromOemFadeCyclesToCustomStrobeEasing proves Shift+scroll from the native fade state authors the
        // first customData.strobeEasing curve while keeping strobe fade enabled.
        [Test]
        public void InnerGlsColorNodeShiftScrollCyclesToCustomStrobeEasing()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, null, 1, null);
            var containerObject = new GameObject("Inner strobe fade easing test container");
            var controllerObject = new GameObject("Inner strobe fade easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(1, evt.StrobeFade,
                    "Scrolling forward from the OEM fade must keep strobe fade enabled.");
                Assert.AreEqual(1, evt.CustomData["strobeEasing"].AsInt,
                    "Shift+scroll from the OEM fade must author the first customData.strobeEasing curve.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // ShiftScrollSkipsNativeInOutCubicCurve proves the custom strobeEasing cycle excludes the native default
        // InOutCubic curve, which is reachable only through the OEM fade state.
        [Test]
        public void InnerGlsColorNodeShiftScrollSkipsNativeInOutCubic()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, "\"strobeEasing\":8", 1, null);
            var containerObject = new GameObject("Inner skip cubic test container");
            var controllerObject = new GameObject("Inner skip cubic test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(1, evt.StrobeFade);
                Assert.AreEqual(10, evt.CustomData["strobeEasing"].AsInt,
                    "The strobeEasing cycle must skip InOutCubic (9) because it is the native default curve.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // ShiftScrollPastLastStrobeCurveWrapsToFadeOff proves the cycle ends at strobe fade off with the key removed.
        [Test]
        public void InnerGlsColorNodeShiftScrollWrapsToFadeOff()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, "\"strobeEasing\":102", 1, null);
            var containerObject = new GameObject("Inner fade wrap test container");
            var controllerObject = new GameObject("Inner fade wrap test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(0, evt.StrobeFade,
                    "Scrolling past the last strobeEasing curve must wrap to strobe fade off.");
                Assert.IsFalse(evt.CustomData.HasKey("strobeEasing"));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // ShiftScrollDownFromOemFadeDisablesFade proves scrolling backward from the native fade state returns to
        // strobe fade off, matching the reverse direction of the cycle.
        [Test]
        public void InnerGlsColorNodeShiftScrollDownDisablesFade()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, null, 1, null);
            var containerObject = new GameObject("Inner fade off test container");
            var controllerObject = new GameObject("Inner fade off test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, -1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(0, evt.StrobeFade);
                Assert.IsFalse(evt.CustomData.HasKey("strobeEasing"),
                    "The strobe-fade-off state must not serialize a strobeEasing key.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterShiftScrollCyclesStrobeEasing proves the outer lane view shares the strobe fade easing cycle.
        [Test]
        public void OuterGlsColorNodeShiftScrollCyclesStrobeEasing()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(1, null, 1, null);
            var containerObject = new GameObject("Outer fade easing test container");
            var controllerObject = new GameObject("Outer fade easing test controller");
            try
            {
                var container = CreateOuterContainer(containerObject, group, group.Boxes[0].Events[0]);
                var controller = CreateOuterController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(1, evt.StrobeFade);
                Assert.AreEqual(1, evt.CustomData["strobeEasing"].AsInt,
                    "Shift+scroll from the OEM fade must author the first customData.strobeEasing curve.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // V3ColorNodeParsesTrackEasingKeys proves all three metadata keys load into the model.
        [Test]
        public void V3ColorNodeParsesTrackEasingKeys()
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{ \"b\": 0, \"i\": 1, \"c\": 0, \"s\": 1, \"f\": 1, \"sb\": 1, \"sf\": 1, \"customData\": {" +
                "\"colorEasing\": 4, \"strobeColorEasing\": 5, \"strobeEasing\": 16 } }"));

            Assert.AreEqual(4, evt.ChromaColorEasing);
            Assert.AreEqual(5, evt.ChromaStrobeColorEasing);
            Assert.AreEqual(16, evt.ChromaStrobeEasing);
        }

        // V3ColorNodeSerializesTrackEasingKeys proves the model fields round-trip into customData on save.
        [Test]
        public void V3ColorNodeSerializesTrackEasingKeys()
        {
            // strobeEasing is only serialized while the fade is active; the field still survives clone/apply.
            var evt = new BaseLightColorBase { Easing = (int)EaseType.Linear, StrobeFade = 1 };
            evt.ChromaColorEasing = 4;
            evt.ChromaStrobeColorEasing = 5;
            evt.ChromaStrobeEasing = 16;
            evt.WriteCustom();

            Assert.AreEqual(4, evt.CustomData["colorEasing"].AsInt);
            Assert.AreEqual(5, evt.CustomData["strobeColorEasing"].AsInt);
            Assert.AreEqual(16, evt.CustomData["strobeEasing"].AsInt);
        }

        // V3ColorNodeInstantStripsColorEasing proves an instant node's colorEasing cannot drive an interval and is
        // dropped, mirroring the ChromaGLS converter normalization.
        [Test]
        public void V3ColorNodeInstantStripsColorEasing()
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{ \"b\": 0, \"i\": 0, \"c\": 0, \"s\": 1, \"customData\": { \"colorEasing\": 4 } }"));

            Assert.AreEqual((int)EaseType.None, evt.Easing);
            Assert.IsNull(evt.ChromaColorEasing,
                "An Instant node's colorEasing has no interval to drive and must be dropped.");
        }

        // V3ColorNodeStrobeEasingExcludesNativeDefault proves authoring the native InOutCubic fade curve is
        // normalized away because the OEM fade already uses it.
        [Test]
        public void V3ColorNodeStrobeEasingExcludesNativeDefault()
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{ \"b\": 0, \"i\": 1, \"c\": 0, \"s\": 1, \"sf\": 1, \"customData\": { \"strobeEasing\": 9 } }"));

            Assert.IsNull(evt.ChromaStrobeEasing,
                "strobeEasing=9 (InOutCubic) is the native fade curve and must not be retained as custom metadata.");
        }

        // V3ColorNodeTrackEasingsSurviveCloneAndApply proves the metadata survives group cloning and Apply.
        [Test]
        public void V3ColorNodeTrackEasingsSurviveCloneAndApply()
        {
            var evt = new BaseLightColorBase { Easing = (int)EaseType.Linear };
            evt.ChromaColorEasing = 4;
            evt.ChromaStrobeColorEasing = 5;
            evt.ChromaStrobeEasing = 16;
            evt.WriteCustom();

            var clone = (BaseLightColorBase)evt.Clone();
            Assert.AreEqual(4, clone.ChromaColorEasing);
            Assert.AreEqual(5, clone.ChromaStrobeColorEasing);
            Assert.AreEqual(16, clone.ChromaStrobeEasing);

            var applied = new BaseLightColorBase();
            applied.Apply(clone);
            Assert.AreEqual(4, applied.ChromaColorEasing);
            Assert.AreEqual(5, applied.ChromaStrobeColorEasing);
            Assert.AreEqual(16, applied.ChromaStrobeEasing);
        }

        // V3ColorNodeTrackEasingsMarkIsChroma proves a node carrying only easing metadata still renders custom colors.
        [Test]
        public void V3ColorNodeTrackEasingsMarkIsChroma()
        {
            var evt = new BaseLightColorBase { Easing = (int)EaseType.Linear };
            evt.ChromaColorEasing = 4;
            evt.WriteCustom();

            Assert.IsTrue(evt.IsChroma(),
                "customData.colorEasing changes rendering, so the node must count as Chroma content.");
        }

        // TweenAppliesStrobeColorEasingOnlyToStrobeTrack proves the preview strobe RGB track uses its own curve.
        [Test]
        public void TweenAppliesStrobeColorEasingOnlyToStrobeTrack()
        {
            var tween = CreateTween();
            tween.StartStrobeFrequency = 1f;
            tween.EndStrobeFrequency = 1f;
            tween.StartStrobeColor = new Color(0f, 0f, 0f, 1f);
            tween.EndStrobeColor = new Color(0.8f, 0.4f, 0.2f, 1f);
            tween.StrobeColorEasing = Easing.Cubic.In;

            // phase = 0.75 -> strobe half; InCubic(0.75) = 0.421875 must drive the strobe RGB lerp only.
            tween.UpdateTime(0.75f);
            Assert.AreEqual(0.8f * 0.421875f, tween.Color.r, 0.000001f,
                "StrobeColorEasing must ease the strobe color track independently of the interval easing.");

            // phase = 0.25 -> normal half stays on the interval's Linear progress.
            tween.UpdateTime(0.25f);
            Assert.AreEqual(0.25f, tween.Color.r, 0.000001f,
                "StrobeColorEasing must not leak into the normal color track.");
        }

        // TweenAppliesStrobeEasingOnlyToFadeCurve proves the preview replaces only the native InOutCubic fade curve.
        [Test]
        public void TweenAppliesStrobeEasingOnlyToFadeCurve()
        {
            var tween = CreateTween();
            tween.StartStrobeFrequency = 1f;
            tween.EndStrobeFrequency = 1f;
            tween.StrobeFade = true;
            tween.StartStrobeColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            tween.EndStrobeColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            tween.StrobeEasing = Easing.Back.In;

            // phase = 0.125 -> fade input 1-|2p-1| = 0.25; InBack(0.25) = -0.0641366 vs native InOutCubic = 0.0625.
            tween.UpdateTime(0.125f);
            Assert.AreEqual(0.125f - (0.0641366f * 0.475f), tween.Color.r, 0.000001f,
                "StrobeEasing must replace the native InOutCubic fade curve while keeping the raw phase input.");
        }

        // TweenKeepsNativeFadeWithoutStrobeEasing proves omitting the override retains the exact native fade.
        [Test]
        public void TweenKeepsNativeFadeWithoutStrobeEasing()
        {
            var tween = CreateTween();
            tween.StartStrobeFrequency = 1f;
            tween.EndStrobeFrequency = 1f;
            tween.StrobeFade = true;
            tween.StartStrobeColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            tween.EndStrobeColor = new Color(0.6f, 0.6f, 0.6f, 1f);

            tween.UpdateTime(0.125f);
            Assert.AreEqual(0.125f + (0.0625f * 0.475f), tween.Color.r, 0.000001f,
                "Without StrobeEasing the fade must stay on the native InOutCubic curve.");
        }

        // RibbonGradientUsesColorEasingOverIntervalEasing proves the ribbon follows customData.colorEasing rather
        // than the interval's own Linear transition.
        [Test]
        public void RibbonGradientUsesColorEasingOverIntervalEasing()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, "\"colorEasing\":4", secondTransition: 1);
            var source = group.Boxes[0].Events[0];
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            var controllerObject = new GameObject("Ribbon easing test controller");
            var rendererObject = new GameObject("Ribbon easing test renderer");
            try
            {
                rendererObject.transform.SetParent(controllerObject.transform);
                var renderer = rendererObject.AddComponent<MeshRenderer>();
                var controller = controllerObject.AddComponent<LightGradientController>();
                SetPrivateField(controller, "meshRenderer", renderer);

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.AreEqual(Easing.EasingShaderId("easeInSine"), block.GetInt("_EasingID"),
                    "The transition ribbon must follow customData.colorEasing when it overrides the interval easing.");
            }
            finally
            {
                Object.DestroyImmediate(rendererObject);
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // BasicGradientDispatchesBeatSaberInOutVariants proves the three BS-specific InOut curves get shader cases so
        // ribbons and gradients can render the full authored easing set.
        [TestCase("easeBeatSaberInOutBack", "BeatSaberInOutBack")]
        [TestCase("easeBeatSaberInOutElastic", "BeatSaberInOutElastic")]
        [TestCase("easeBeatSaberInOutBounce", "BeatSaberInOutBounce")]
        public void BasicGradientDispatchesBeatSaberInOutVariants(string easingName, string functionName)
        {
            var shaderId = Easing.EasingShaderId(easingName);
            var source = System.IO.File.ReadAllText("Assets/_Graphics/Shaders/Object/BasicGradient.shader");
            var caseMarker = $"case {shaderId}:";
            var start = source.IndexOf(caseMarker, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, $"BasicGradient must dispatch shader id {shaderId} for {easingName}.");
            var end = source.IndexOf("break;", start, System.StringComparison.Ordinal);
            Assert.Greater(end, start);
            StringAssert.Contains($"t = {functionName}(t);", source.Substring(start, end - start));
        }

        // InternalNameForIdCoversAuthoredCurves proves every scrollable custom easing resolves to a shader name.
        [Test]
        public void InternalNameForIdCoversAuthoredCurves()
        {
            for (var id = 1; id <= 30; id++)
            {
                Assert.IsNotNull(Easing.IDToInternalName.GetValueOrDefault(id), $"Missing internal name for {id}.");
            }

            Assert.AreEqual("easeBeatSaberInOutBack", Easing.InternalNameForID(100));
            Assert.AreEqual("easeBeatSaberInOutElastic", Easing.InternalNameForID(101));
            Assert.AreEqual("easeBeatSaberInOutBounce", Easing.InternalNameForID(102));
            Assert.AreEqual("easeLinear", Easing.InternalNameForID(0));
        }

        // V3ColorNodeParsesHsvEasingType proves customData.easingType loads onto the transition endpoint model.
        [Test]
        public void V3ColorNodeParsesHsvEasingType()
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{ \"b\": 0, \"i\": 1, \"c\": 0, \"s\": 1, \"customData\": { \"easingType\": \"HSV\" } }"));

            Assert.AreEqual("HSV", evt.CustomLerpType,
                "customData.easingType=HSV must parse onto the color node's lerp-type field.");
        }

        // V3ColorNodeNormalizesRgbAndUnknownEasingType proves RGB is equivalent to absent metadata and that
        // unknown strings cannot survive parsing, matching the ChromaGLS converter normalization.
        [TestCase("RGB")]
        [TestCase("banana")]
        [TestCase("trueHSV")]
        public void V3ColorNodeNormalizesRgbAndUnknownEasingType(string raw)
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{ \"b\": 0, \"i\": 1, \"c\": 0, \"s\": 1, \"customData\": { \"easingType\": \"" + raw + "\" } }"));

            Assert.IsNull(evt.CustomLerpType,
                $"easingType={raw} is equivalent to leaving the key out and must normalize to the default.");
            evt.WriteCustom();
            Assert.IsFalse(evt.CustomData.HasKey("easingType"),
                "The normalized default must remove easingType so ChromaGLS reads the native RGB path.");
        }

        // V3ColorNodeSerializesHsvEasingType proves the authored HSV state writes the exact serialized value.
        [Test]
        public void V3ColorNodeSerializesHsvEasingType()
        {
            var evt = new BaseLightColorBase { Easing = (int)EaseType.Linear };
            evt.CustomLerpType = "HSV";
            evt.WriteCustom();

            Assert.AreEqual("HSV", evt.CustomData["easingType"].Value,
                "The authored HSV state must serialize customData.easingType for ChromaGLS.");
            Assert.AreEqual("HSV", evt.ToJson()["customData"]["easingType"].Value,
                "The easingType key must survive the node's JSON export.");
        }

        // V3ColorNodeEasingTypeSurvivesCloneAndApply proves the lerp type survives group cloning and Apply.
        [Test]
        public void V3ColorNodeEasingTypeSurvivesCloneAndApply()
        {
            var evt = new BaseLightColorBase { Easing = (int)EaseType.Linear };
            evt.CustomLerpType = "HSV";
            evt.WriteCustom();

            var clone = (BaseLightColorBase)evt.Clone();
            Assert.AreEqual("HSV", clone.CustomLerpType);

            var applied = new BaseLightColorBase();
            applied.Apply(clone);
            Assert.AreEqual("HSV", applied.CustomLerpType);
        }

        // V3ColorNodeEasingTypeMarksIsChroma proves an HSV-only node still counts as Chroma content.
        [Test]
        public void V3ColorNodeEasingTypeMarksIsChroma()
        {
            var evt = new BaseLightColorBase { Easing = (int)EaseType.Linear };
            evt.CustomLerpType = "HSV";
            evt.WriteCustom();

            Assert.IsTrue(evt.IsChroma(),
                "customData.easingType changes color interpolation, so the node must count as Chroma content.");
        }

        // TweenAppliesTrueHsvAngularLerpToColor proves easingType=HSV uses the shortest angular hue path:
        // purple (hue 0.9) to orange (hue 0.1) must pass through red at the seam, never green.
        [Test]
        public void TweenAppliesTrueHsvAngularLerpToColor()
        {
            var tween = CreateTween();
            tween.StartColor = Color.HSVToRGB(0.9f, 1f, 1f);
            tween.EndColor = Color.HSVToRGB(0.1f, 1f, 1f);
            tween.ColorLerpType = BasicEventColorLerpType.TrueHSV;

            tween.UpdateTime(0.5f);
            Assert.AreEqual(1f, tween.Color.r, 0.000001f,
                "Angular HSV must cross the 0/1 hue seam through red, not detour through cyan like linear hue.");
            Assert.AreEqual(0f, tween.Color.g, 0.000001f);
            Assert.AreEqual(0f, tween.Color.b, 0.000001f);
        }

        // TweenAppliesTrueHsvAngularLerpToStrobeColor proves the strobe color track shares the transition's
        // color-space choice instead of staying on RGB.
        [Test]
        public void TweenAppliesTrueHsvAngularLerpToStrobeColor()
        {
            var tween = CreateTween();
            tween.StartStrobeFrequency = 1f;
            tween.EndStrobeFrequency = 1f;
            tween.StartStrobeColor = Color.HSVToRGB(0.9f, 1f, 1f);
            tween.EndStrobeColor = Color.HSVToRGB(0.1f, 1f, 1f);
            tween.ColorLerpType = BasicEventColorLerpType.TrueHSV;

            // phase = 0.75 -> strobe half; LerpAngle(324, 36, 0.75) wraps through the seam to hue 0.05 = (1, 0.3, 0).
            tween.UpdateTime(0.75f);
            Assert.AreEqual(1f, tween.Color.r, 0.000001f,
                "The strobe color track must follow the transition's trueHSV interpolation.");
            Assert.AreEqual(0.3f, tween.Color.g, 0.000001f);
            Assert.AreEqual(0f, tween.Color.b, 0.000001f);
        }

        // RibbonGradientUsesAheadNodeHsvEasingType proves the ribbon's color-space flag belongs to the
        // transition's ahead node, which owns the interval in GLS terms.
        [Test]
        public void RibbonGradientUsesAheadNodeHsvEasingType()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, "\"easingType\":\"HSV\"", secondTransition: 1);
            var source = group.Boxes[0].Events[0];
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            var controllerObject = new GameObject("Ribbon easingType test controller");
            var rendererObject = new GameObject("Ribbon easingType test renderer");
            try
            {
                rendererObject.transform.SetParent(controllerObject.transform);
                var renderer = rendererObject.AddComponent<MeshRenderer>();
                var controller = controllerObject.AddComponent<LightGradientController>();
                SetPrivateField(controller, "meshRenderer", renderer);

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.AreEqual((int)BasicEventColorLerpType.TrueHSV, block.GetInt("_UseHSV"),
                    "The ribbon must render the ahead node's authored HSV easingType as true angular HSV.");
            }
            finally
            {
                Object.DestroyImmediate(rendererObject);
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // RibbonGradientIgnoresSourceNodeEasingType proves the source node's own easingType does not leak into
        // its outgoing ribbon; only the ahead node's metadata owns the transition.
        [Test]
        public void RibbonGradientIgnoresSourceNodeEasingType()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, "\"easingType\":\"HSV\"", 0, null, secondTransition: 1);
            var source = group.Boxes[0].Events[0];
            var appearance = ScriptableObject.CreateInstance<EventAppearanceSO>();
            var controllerObject = new GameObject("Ribbon source easingType test controller");
            var rendererObject = new GameObject("Ribbon source easingType test renderer");
            try
            {
                rendererObject.transform.SetParent(controllerObject.transform);
                var renderer = rendererObject.AddComponent<MeshRenderer>();
                var controller = controllerObject.AddComponent<LightGradientController>();
                SetPrivateField(controller, "meshRenderer", renderer);

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.AreEqual((int)BasicEventColorLerpType.RGB, block.GetInt("_UseHSV"),
                    "The source node's easingType must not affect the ribbon; the ahead node owns the transition.");
            }
            finally
            {
                Object.DestroyImmediate(rendererObject);
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(appearance);
            }
        }

        // InnerGlsRibbonAltScrollSetsHsvOnAheadNode proves alt+scroll on a color ribbon authors
        // customData.easingType=HSV on the ahead node rather than the ribbon's source node.
        [Test]
        public void InnerGlsRibbonAltScrollSetsHsvOnAheadNode()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Inner ribbon easingType test container");
            var controllerObject = new GameObject("Inner ribbon easingType test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;

                SendChordScroll(controller, 1f, Key.LeftAlt);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var ahead = replacement.Boxes[0].Events[1];
                Assert.AreEqual("HSV", ahead.CustomData["easingType"].Value,
                    "Alt+scroll on a transition ribbon must author easingType=HSV on the ahead node.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("easingType"),
                    "The ribbon's source node must not receive the easingType edit.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("color"),
                    "Alt+scroll on a ribbon must not fall through to the source node's brightness or color.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsRibbonAltScrollClearsHsvOnAheadNode proves the second toggle removes the authored HSV state.
        [Test]
        public void InnerGlsRibbonAltScrollClearsHsvOnAheadNode()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, "\"easingType\":\"HSV\"", secondTransition: 1);
            var containerObject = new GameObject("Inner ribbon clear easingType test container");
            var controllerObject = new GameObject("Inner ribbon clear easingType test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;

                SendChordScroll(controller, 1f, Key.LeftAlt);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.IsFalse(replacement.Boxes[0].Events[1].CustomData.HasKey("easingType"),
                    "Toggling an HSV ribbon must restore the absent RGB default on the ahead node.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsRibbonCtrlShiftScrollCyclesAheadColorEasing proves ctrl+shift+scroll on a ribbon cycles
        // customData.colorEasing on the ahead node, leaving the source node untouched.
        [Test]
        public void InnerGlsRibbonCtrlShiftScrollCyclesAheadColorEasing()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Inner ribbon color easing test container");
            var controllerObject = new GameObject("Inner ribbon color easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var ahead = replacement.Boxes[0].Events[1];
                Assert.AreEqual(1, ahead.CustomData["colorEasing"].AsInt,
                    "Ctrl+Shift+scroll on a ribbon must author the first custom curve on the ahead node.");
                var source = replacement.Boxes[0].Events[0];
                Assert.IsFalse(source.CustomData.HasKey("colorEasing"),
                    "The source node must not receive the ribbon's colorEasing edit.");
                Assert.AreEqual((int)EaseType.None, source.Easing,
                    "The source node's instant transition must stay unchanged by ribbon input.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsRibbonAltShiftScrollCyclesAheadStrobeColorEasing proves alt+shift+scroll on a ribbon owns
        // the ahead node's customData.strobeColorEasing cycle, same as on the node.
        [Test]
        public void InnerGlsRibbonAltShiftScrollCyclesAheadStrobeColorEasing()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Inner ribbon strobe easing test container");
            var controllerObject = new GameObject("Inner ribbon strobe easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;

                SendChordScroll(controller, 1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var ahead = replacement.Boxes[0].Events[1];
                Assert.AreEqual(1, ahead.CustomData["strobeColorEasing"].AsInt,
                    "Alt+Shift+scroll on a ribbon must author strobeColorEasing on the ahead node.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("strobeColorEasing"),
                    "The source node must not receive the ribbon's strobeColorEasing edit.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsRibbonShiftScrollCyclesAheadStrobeEasing proves shift+scroll on a ribbon drives the ahead
        // node's strobe fade easing cycle, same binding as on the node.
        [Test]
        public void InnerGlsRibbonShiftScrollCyclesAheadStrobeEasing()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1, secondStrobeFade: 1);
            var containerObject = new GameObject("Inner ribbon strobe fade test container");
            var controllerObject = new GameObject("Inner ribbon strobe fade test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var ahead = replacement.Boxes[0].Events[1];
                Assert.AreEqual(1, ahead.StrobeFade,
                    "Shift+scroll on a ribbon must keep the ahead node's strobe fade enabled.");
                Assert.AreEqual(1, ahead.CustomData["strobeEasing"].AsInt,
                    "Shift+scroll on a ribbon must author the first customData.strobeEasing curve on the ahead node.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("strobeEasing"),
                    "The source node must not receive the ribbon's strobeEasing edit.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsRibbonCtrlAltScrollLeavesBothNodesUnchanged proves the node-only chords are suppressed on a
        // ribbon, matching the Basic Event ribbon's ctrl+alt no-op. The wired precision controller keeps the
        // frequency path executable so the regression only passes through the ribbon suppression.
        [Test]
        public void InnerGlsRibbonCtrlAltScrollLeavesBothNodesUnchanged()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Inner ribbon noop test container");
            var controllerObject = new GameObject("Inner ribbon noop test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;
                SetPrivateField(
                    controller,
                    "ScrollPrecisionController",
                    controllerObject.AddComponent<ScrollPrecisionController>());

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftAlt);

                Assert.AreSame(group, GetOpenColorGroup(),
                    "Ctrl+Alt+scroll on a ribbon must be a no-op and must not replace the authoritative group.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsRibbonCtrlAltShiftScrollLeavesBothNodesUnchanged proves the three-modifier strobe
        // brightness chord is suppressed on a ribbon while every overlapping ribbon chord is guarded out.
        [Test]
        public void InnerGlsRibbonCtrlAltShiftScrollLeavesBothNodesUnchanged()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Inner ribbon triple noop test container");
            var controllerObject = new GameObject("Inner ribbon triple noop test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;
                SetPrivateField(
                    controller,
                    "ScrollPrecisionController",
                    controllerObject.AddComponent<ScrollPrecisionController>());

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftAlt, Key.LeftShift);

                Assert.AreSame(group, GetOpenColorGroup(),
                    "Ctrl+Alt+Shift+scroll on a ribbon must be a no-op and must not replace the authoritative group.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterGlsRibbonAltScrollSetsHsvOnAheadNode proves the outer lane preview shares the ribbon
        // easingType toggle and still mutates the ahead node.
        [Test]
        public void OuterGlsRibbonAltScrollSetsHsvOnAheadNode()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Outer ribbon easingType test container");
            var controllerObject = new GameObject("Outer ribbon easingType test controller");
            try
            {
                var container = CreateOuterContainer(containerObject, group, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateOuterController(controllerObject, container);
                controller.HitOverride = ribbon;

                SendChordScroll(controller, 1f, Key.LeftAlt);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual("HSV", replacement.Boxes[0].Events[1].CustomData["easingType"].Value,
                    "Alt+scroll on an outer ribbon must author easingType=HSV on the ahead node.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("easingType"),
                    "The outer ribbon's source node must not receive the easingType edit.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterGlsRibbonCtrlShiftScrollCyclesAheadColorEasing proves the outer lane preview shares the
        // ribbon color-easing cycle against the ahead node.
        [Test]
        public void OuterGlsRibbonCtrlShiftScrollCyclesAheadColorEasing()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Outer ribbon color easing test container");
            var controllerObject = new GameObject("Outer ribbon color easing test controller");
            try
            {
                var container = CreateOuterContainer(containerObject, group, group.Boxes[0].Events[0]);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateOuterController(controllerObject, container);
                controller.HitOverride = ribbon;

                SendChordScroll(controller, 1f, Key.LeftCtrl, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual(1, replacement.Boxes[0].Events[1].CustomData["colorEasing"].AsInt,
                    "Ctrl+Shift+scroll on an outer ribbon must author colorEasing on the ahead node.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("colorEasing"));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // Isolate the authored composite from host focus and physical devices so the raycast refactor retains deterministic input coverage.
        private static void SendChordScroll(
            CMInput.IGLSColorObjectsActions controller,
            float scrollY,
            params Key[] modifiers)
        {
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.NotNull(sharedInput);
            var sharedMapWasEnabled = sharedInput.GLSColorObjects.enabled;
            sharedInput.GLSColorObjects.Disable();
            // The fixture owns device state while these tests drive same-frame modifier+scroll callbacks.
            var inputFixture = new InputTestFixture();
            inputFixture.Setup();
            var input = new CMInput();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            try
            {
                input.GLSColorObjects.SetCallbacks(controller);
                input.GLSColorObjects.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(modifiers));
                InputSystem.Update();
                InputSystem.QueueStateEvent(
                    mouse,
                    new MouseState { scroll = new Vector2(0f, scrollY) });
                InputSystem.Update();
            }
            finally
            {
                // Dispose the isolated actions before restoring the original runtime and shared application map.
                input.GLSColorObjects.Disable();
                input.Dispose();
                inputFixture.TearDown();
                if (sharedMapWasEnabled)
                {
                    sharedInput.GLSColorObjects.Enable();
                }
            }
        }

        // Place one authoritative group with two events; the first event carries the exercised strobe/custom state.
        private static BaseLightColorEventBoxGroup PlaceColorGroup(
            int primaryStrobeFade,
            string primaryCustom,
            int primaryTransition,
            string secondCustom,
            int secondTransition = 0,
            int secondStrobeFade = 0)
        {
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                $@"{{ ""b"": 20, ""g"": 1, ""e"": [
                    {{ ""f"": {{ ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }}, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ {{ ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": {primaryTransition}, ""f"": 1, ""sb"": 1, ""sf"": {primaryStrobeFade}{CustomJson(primaryCustom)} }},
                                 {{ ""b"": 0.75, ""c"": 1, ""s"": 1, ""i"": {secondTransition}, ""f"": 1, ""sb"": 1, ""sf"": {secondStrobeFade}{CustomJson(secondCustom)} }} ] }}
                ] }}"));
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;
            return group;
        }

        private static string CustomJson(string custom) =>
            string.IsNullOrEmpty(custom) ? string.Empty : $", \"\"customData\"\": {{{custom}}}";

        // Build a data-only inner container exactly like the strobe-fade regression fixture.
        private static GLSEventContainer CreateInnerContainer(GameObject containerObject, BaseLightColorBase evt)
        {
            var container = containerObject.AddComponent<GLSEventContainer>();
            // Data-only test containers still need the lifecycle dependency that OnDestroy unregisters from.
            container.VisualSettings = GetInitializedVisualSettings();
            container.EventData = evt;
            SetPrivateField(container, "highlighted", true);
            return container;
        }

        private static TestGLSEventColorInputController CreateInnerController(
            GameObject controllerObject,
            GLSEventContainer container)
        {
            var controller = controllerObject.AddComponent<TestGLSEventColorInputController>();
            controller.IsHovering = true;
            controller.HoveredObject = container;
            controller.RaycastTarget = container;
            return controller;
        }

        // Build a data-only outer container; the ghost flag distinguishes the later preview node when requested.
        private static GLSGroupContainer CreateOuterContainer(
            GameObject containerObject,
            BaseLightColorEventBoxGroup group,
            BaseLightColorBase preview,
            bool ghost = false)
        {
            var container = containerObject.AddComponent<GLSGroupContainer>();
            container.VisualSettings = GetInitializedVisualSettings();
            container.EventBoxGroupData = group;
            container.PreviewEventData = preview;
            if (ghost)
            {
                SetPrivateField(container, "isPreviewGhost", true);
            }

            return container;
        }

        private static TestGLSGroupColorInputController CreateOuterController(
            GameObject controllerObject,
            GLSGroupContainer container)
        {
            var controller = controllerObject.AddComponent<TestGLSGroupColorInputController>();
            controller.IsHovering = true;
            controller.HoveredObject = container;
            controller.RaycastTarget = container;
            return controller;
        }

        // A ribbon hover resolves the owning container while the physical hit stays on its gradient child.
        private static GameObject CreateRibbonHitObject(GameObject containerObject)
        {
            var ribbonObject = new GameObject("Transition ribbon hit target");
            ribbonObject.transform.SetParent(containerObject.transform);
            ribbonObject.AddComponent<LightGradientController>();
            return ribbonObject;
        }

        // The preview tween needs only the interval endpoints and master easing; strobe state is layered per test.
        private static LightColorTween CreateTween() => new()
        {
            StartTimeAlpha = 0f,
            EndTimeAlpha = 1f,
            StartTimeColor = 0f,
            EndTimeColor = 1f,
            StartColor = new Color(0f, 0f, 0f, 1f),
            EndColor = new Color(1f, 1f, 1f, 1f),
            StartAlpha = 0f,
            EndAlpha = 1f,
            Easing = Easing.Linear,
        };

        // Read the replacement parent published by the GLS action so assertions never inspect the stale pre-scroll event instance.
        private static BaseLightColorEventBoxGroup GetOpenColorGroup() =>
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext as BaseLightColorEventBoxGroup;

        // Set the production workspace for each regression so its controller represents the same view exercised by users.
        private static void SetEditingMode(EditingMode editingMode) =>
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = editingMode;

        // Reuse a scene-owned asset so test teardown follows ObjectContainer's normal initialized lifecycle.
        private static VisualSettingsSO GetInitializedVisualSettings()
        {
            var containers = Object.FindObjectsByType<ObjectContainer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var container in containers)
            {
                if (container.VisualSettings != null)
                {
                    return container.VisualSettings;
                }
            }

            Assert.Fail("The loaded editor scene had no initialized ObjectContainer VisualSettings dependency.");
            return null;
        }

        // Test containers set only the private state that distinguishes production hover paths; all mutation data remains authoritative.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? typeof(ObjectContainer).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, $"Could not find test field {fieldName}.");
            field.SetValue(target, value);
        }

        private class TestGLSEventColorInputController : BeatmapGLSEventColorInputController
        {
            public GLSEventContainer RaycastTarget;
            public GameObject HitOverride;

            // Override shared picking so the easing regressions do not need a physical raycast against pooled nodes.
            protected override bool RaycastFirstObject(out GLSEventContainer firstObject)
            {
                firstObject = RaycastTarget;
                // GLSEasingTypeRibbonInputTest: ribbon chords read the physical hit from the shared frame
                // cache, so the override must publish it exactly like the production raycast does.
                BeatmapRaycastCache.FirstHit = HitOverride != null
                    ? HitOverride
                    : firstObject != null ? firstObject.gameObject : null;
                BeatmapRaycastCache.HasHit = firstObject != null;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return firstObject != null;
            }
        }

        private class TestGLSGroupColorInputController : BeatmapGLSGroupColorInputController
        {
            public GLSGroupContainer RaycastTarget;
            public GameObject HitOverride;

            // Override shared picking so primary and ghost regressions do not need a separate GLS-only test seam.
            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                // GLSEasingTypeRibbonInputTest: ribbon chords read the physical hit from the shared frame
                // cache, so the override must publish it exactly like the production raycast does.
                BeatmapRaycastCache.FirstHit = HitOverride != null
                    ? HitOverride
                    : firstObject != null ? firstObject.gameObject : null;
                BeatmapRaycastCache.HasHit = firstObject != null;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return firstObject != null;
            }
        }
    }
}
