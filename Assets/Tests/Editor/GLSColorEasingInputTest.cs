using System.Collections.Generic;
using System.Linq;
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
        // GlsEasingCycleMatchesEditorOrder locks the official-editor prefix, each custom true-InOut beside its
        // Beat Saber IO equivalent, the appended remainder, and strobe's IOCr-first exception.
        [Test]
        public void GlsEasingCycleMatchesEditorOrder()
        {
            var allValues = (EaseType[])typeof(GLSEventHoverMutation)
                .GetField("AllEasingValues", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            var strobeValues = (EaseType[])typeof(GLSEventHoverMutation)
                .GetField("StrobeFadeEasingValues", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            var expectedAll = new[]
            {
                EaseType.None,
                EaseType.Linear,
                EaseType.InQuadratic,
                EaseType.OutQuadratic,
                EaseType.InOutQuadratic,
                EaseType.InCircular,
                EaseType.OutCircular,
                EaseType.InOutCircular,
                EaseType.InBack,
                EaseType.OutBack,
                EaseType.BeatSaberInOutBack,
                EaseType.InOutBack,
                EaseType.InElastic,
                EaseType.OutElastic,
                EaseType.BeatSaberInOutElastic,
                EaseType.InOutElastic,
                EaseType.InBounce,
                EaseType.OutBounce,
                EaseType.BeatSaberInOutBounce,
                EaseType.InOutBounce,
                EaseType.InSinusoidal,
                EaseType.OutSinusoidal,
                EaseType.InOutSinusoidal,
                EaseType.InCubic,
                EaseType.OutCubic,
                EaseType.InOutCubic,
                EaseType.InQuartic,
                EaseType.OutQuartic,
                EaseType.InOutQuartic,
                EaseType.InQuintic,
                EaseType.OutQuintic,
                EaseType.InOutQuintic,
                EaseType.InExponential,
                EaseType.OutExponential,
                EaseType.InOutExponential
            };
            var expectedStrobe = new[] { EaseType.None, EaseType.InOutCircular }
                .Concat(expectedAll.Where(v => v != EaseType.None && v != EaseType.InOutCircular))
                .ToArray();

            Assert.AreEqual(expectedAll, allValues);
            Assert.AreEqual(expectedStrobe, strobeValues);
        }

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
            var group = PlaceColorGroup(0, "\"colorEasing\":18", 1, null);
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
                Assert.AreEqual(0, evt.CustomData["strobeColorEasing"].AsInt,
                    "Alt+Shift+scroll up must author the explicit Linear strobe-color override.");
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
                Assert.AreEqual(0, evt.CustomData["strobeColorEasing"].AsInt,
                    "The promoted node's first strobe-color slot is the explicit Linear override.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsColorNodeAltShiftScrollAdvancesPastLinear proves explicit strobeColorEasing=0 is a real
        // cycle slot rather than the absent inherit-interval state.
        [Test]
        public void InnerGlsColorNodeAltShiftScrollAdvancesPastLinear()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, "\"strobeColorEasing\":0", 1, null);
            var containerObject = new GameObject("Inner linear strobe color easing test container");
            var controllerObject = new GameObject("Inner linear strobe color easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.AreEqual(1, replacement.Boxes[0].Events[0].CustomData["strobeColorEasing"].AsInt,
                    "The shared easing order must advance explicit Linear to InQuadratic.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsColorNodeAltShiftScrollDownRemovesLinear proves scrolling backward from strobeColorEasing=0
        // reaches the None slot by removing the override instead of serializing an invalid -1.
        [Test]
        public void InnerGlsColorNodeAltShiftScrollDownRemovesLinear()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, "\"strobeColorEasing\":0", 1, null);
            var containerObject = new GameObject("Inner unset strobe color easing test container");
            var controllerObject = new GameObject("Inner unset strobe color easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, -1f, Key.LeftAlt, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("strobeColorEasing"),
                    "The None slot must remove strobeColorEasing so the track follows the interval easing.");
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
            var group = PlaceColorGroup(0, "\"strobeColorEasing\":18", 1, null);
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
                    "Scrolling past the last strobe color curve must remove the key and restore interval-driven progress.");
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
                Assert.AreEqual(0, replacement.Boxes[0].Events[0].CustomData["strobeColorEasing"].AsInt);
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
                Assert.AreEqual(0, replacement.Boxes[0].Events[1].CustomData["strobeColorEasing"].AsInt,
                    "The ghost preview's strobe color easing must land on the later node it represents.");
                Assert.IsFalse(replacement.Boxes[0].Events[0].CustomData.HasKey("strobeColorEasing"));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsColorNodeShiftScrollStartsWithInOutCircular proves Shift+scroll enters the requested
        // strobe-specific None -> IOCr -> Linear sequence before resuming the shared curve order.
        [Test]
        public void InnerGlsColorNodeShiftScrollStartsWithInOutCircular()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 1, null);
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
                    "Scrolling forward from no fade must enable strobe fade.");
                Assert.AreEqual((int)EaseType.InOutCircular, evt.CustomData["strobeEasing"].AsInt,
                    "The first enabled strobe fade easing must be IOCr.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsColorNodeShiftScrollWritesLinearOverride proves strobeEasing=0 remains authored metadata;
        // unlike other tracks, an absent strobe key already means the native InOutCubic fade.
        [Test]
        public void InnerGlsColorNodeShiftScrollWritesLinearOverride()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, "\"strobeEasing\":21", 1, null);
            var containerObject = new GameObject("Inner linear strobe easing test container");
            var controllerObject = new GameObject("Inner linear strobe easing test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(1, evt.StrobeFade);
                Assert.AreEqual(0, evt.ChromaStrobeEasing,
                    "IOCr must advance to an explicitly authored Linear strobe fade.");
                Assert.AreEqual(0, evt.CustomData["strobeEasing"].AsInt,
                    "Linear must serialize because absent strobeEasing means native InOutCubic, not Linear.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsColorNodeShiftScrollReturnsToNativeInOutCubic proves the shared order reaches InOutCubic
        // and normalizes it back to the OEM fade state by removing customData.strobeEasing.
        [Test]
        public void InnerGlsColorNodeShiftScrollReturnsToNativeInOutCubic()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, "\"strobeEasing\":8", 1, null);
            var containerObject = new GameObject("Inner native cubic test container");
            var controllerObject = new GameObject("Inner native cubic test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(1, evt.StrobeFade);
                Assert.IsFalse(evt.CustomData.HasKey("strobeEasing"),
                    "InOutCubic must serialize as the absent native strobe-fade curve.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // InnerGlsColorNodeShiftScrollFromNativeInOutCubicContinuesSharedOrder proves the absent key occupies
        // the real InOutCubic slot, so the next strobe curve is InQuartic rather than restarting at IOCr.
        [Test]
        public void InnerGlsColorNodeShiftScrollFromNativeInOutCubicContinuesSharedOrder()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, null, 1, null);
            var containerObject = new GameObject("Inner native cubic next test container");
            var controllerObject = new GameObject("Inner native cubic next test controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                var controller = CreateInnerController(controllerObject, container);

                SendChordScroll(controller, 1f, Key.LeftShift);

                var replacement = GetOpenColorGroup();
                Assert.NotNull(replacement);
                var evt = replacement.Boxes[0].Events[0];
                Assert.AreEqual(1, evt.StrobeFade);
                Assert.AreEqual((int)EaseType.InQuartic, evt.CustomData["strobeEasing"].AsInt,
                    "Native InOutCubic must continue into the shared remainder at InQuartic.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // ShiftScrollPastLastStrobeCurveWrapsToFadeOff proves the shared order's final InOutExponential slot
        // wraps to strobe fade off with the key removed.
        [Test]
        public void InnerGlsColorNodeShiftScrollWrapsToFadeOff()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, "\"strobeEasing\":18", 1, null);
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

        // ShiftScrollDownFromInOutCircularDisablesFade proves scrolling backward from the first enabled
        // strobe easing returns to fade off in the requested None -> IOCr sequence.
        [Test]
        public void InnerGlsColorNodeShiftScrollDownDisablesFade()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(1, "\"strobeEasing\":21", 1, null);
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

        // OuterShiftScrollCyclesStrobeEasing proves the outer lane view shares the requested None -> IOCr
        // strobe-specific entry before resuming the shared easing order.
        [Test]
        public void OuterGlsColorNodeShiftScrollCyclesStrobeEasing()
        {
            SetEditingMode(EditingMode.GLS);
            var group = PlaceColorGroup(0, null, 1, null);
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
                Assert.AreEqual((int)EaseType.InOutCircular, evt.CustomData["strobeEasing"].AsInt,
                    "Shift+scroll from no fade must author IOCr in customData.strobeEasing.");
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

        // V3ColorNodeParsesLinearOptionalTrackEasings proves both optional-track 0 values remain authored overrides:
        // absent strobeEasing means native InOutCubic, while absent strobeColorEasing follows the interval curve.
        [Test]
        public void V3ColorNodeParsesLinearOptionalTrackEasings()
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{ \"b\": 0, \"i\": 1, \"c\": 0, \"s\": 1, \"sf\": 1, \"customData\": { " +
                "\"strobeEasing\": 0, \"strobeColorEasing\": 0 } }"));

            Assert.AreEqual(0, evt.ChromaStrobeEasing,
                "strobeEasing=0 must round-trip because the absent key means native InOutCubic.");
            Assert.AreEqual(0, evt.ChromaStrobeColorEasing,
                "strobeColorEasing=0 must round-trip because the absent key follows the interval easing.");

            var authored = new BaseLightColorBase
            {
                Easing = (int)EaseType.Linear,
                StrobeFade = 1,
                ChromaStrobeEasing = 0,
                ChromaStrobeColorEasing = 0
            };
            authored.WriteCustom();
            Assert.AreEqual(0, authored.CustomData["strobeEasing"].AsInt);
            Assert.AreEqual(0, authored.CustomData["strobeColorEasing"].AsInt);
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

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 0);

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

            Assert.AreEqual(BasicEventColorLerpType.TrueHSV, evt.CustomLerpType,
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

            Assert.AreEqual(BasicEventColorLerpType.RGB, evt.CustomLerpType,
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
            evt.CustomLerpType = BasicEventColorLerpType.TrueHSV;
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
            evt.CustomLerpType = BasicEventColorLerpType.TrueHSV;
            evt.WriteCustom();

            var clone = (BaseLightColorBase)evt.Clone();
            Assert.AreEqual(BasicEventColorLerpType.TrueHSV, clone.CustomLerpType);

            var applied = new BaseLightColorBase();
            applied.Apply(clone);
            Assert.AreEqual(BasicEventColorLerpType.TrueHSV, applied.CustomLerpType);
        }

        // V3ColorNodeEasingTypeMarksIsChroma proves an HSV-only node still counts as Chroma content.
        [Test]
        public void V3ColorNodeEasingTypeMarksIsChroma()
        {
            var evt = new BaseLightColorBase { Easing = (int)EaseType.Linear };
            evt.CustomLerpType = BasicEventColorLerpType.TrueHSV;
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

        // HsvStrobeFadeUsesAngularColorBlend proves easingType also owns the pulse fade between the
        // already-resolved normal and strobe colors, rather than falling back to an RGB crossfade.
        [Test]
        public void HsvStrobeFadeUsesAngularColorBlend()
        {
            var tween = CreateTween();
            tween.StartColor = tween.EndColor = Color.HSVToRGB(0.9f, 1f, 1f);
            tween.StartStrobeColor = tween.EndStrobeColor = Color.HSVToRGB(0.1f, 1f, 1f);
            tween.StartAlpha = tween.EndAlpha = 1f;
            tween.StartStrobeBrightness = tween.EndStrobeBrightness = 1f;
            tween.StartStrobeFrequency = tween.EndStrobeFrequency = 1f;
            tween.StrobeFade = true;
            tween.ColorLerpType = BasicEventColorLerpType.TrueHSV;

            // phase=0.25 produces the native cubic fade midpoint; angular HSV must cross the seam through red.
            tween.UpdateTime(0.25f);
            Assert.AreEqual(1f, tween.Color.r, 0.000001f);
            Assert.AreEqual(0f, tween.Color.g, 0.000001f);
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

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 0);

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

                GLSEventCommon.UpdateColorTransitionRibbon(controller, source, appearance, _ => false, 0);

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

        // ShiftClickingGlsRibbonDoesNotSelectOwningNode keeps a ribbon child from resolving as its source node.
        [Test]
        public void ShiftClickingGlsRibbonDoesNotSelectOwningNode()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var source = group.Boxes[0].Events[0];
            var containerObject = new GameObject("Inner ribbon selection owner");
            var controllerObject = new GameObject("Inner ribbon selection controller");
            try
            {
                var container = CreateInnerContainer(containerObject, source);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.HitOverride = ribbon;
                ConfigureBaseInputDependencies(controller, EditingMode.EventBox);
                SelectionController.DeselectAll();

                SendShiftLeftClick(controller);

                Assert.IsFalse(SelectionController.IsObjectSelected(source),
                    "Shift-clicking a GLS transition ribbon must not select its owning source node.");
            }
            finally
            {
                SelectionController.DeselectAll();
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // HoveringGlsRibbonDoesNotHighlightOwningNode preserves ribbon controls without outlining either endpoint node.
        [Test]
        public void HoveringGlsRibbonDoesNotHighlightOwningNode()
        {
            SetEditingMode(EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var containerObject = new GameObject("Inner ribbon hover owner");
            var controllerObject = new GameObject("Inner ribbon hover controller");
            try
            {
                var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                SetPrivateField(container, "highlighted", false);
                var ribbon = CreateRibbonHitObject(containerObject);
                var controller = CreateInnerController(controllerObject, container);
                controller.IsHovering = false;
                controller.HoveredObject = null;
                controller.HitOverride = ribbon;
                BeatmapRaycastCache.FirstHit = ribbon;
                BeatmapRaycastCache.HasHit = true;
                BeatmapRaycastCache.HasRaycastThisFrame = true;

                typeof(BeatmapGLSEventInputController<BaseLightColorBase>)
                    .GetMethod("SetHoveredContainer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, new object[] { container });

                Assert.IsTrue(controller.IsHovering,
                    "Ribbon-specific scroll controls must retain hover ownership.");
                Assert.IsFalse(container.Highlighted,
                    "Hovering a transition ribbon must not apply the owning node's outline.");
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
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
                Assert.AreEqual(0, ahead.CustomData["strobeColorEasing"].AsInt,
                    "Alt+Shift+scroll on a ribbon must author the explicit Linear override on the ahead node.");
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
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1, secondStrobeFade: 0);
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
                    "Shift+scroll on a ribbon must enable the ahead node's strobe fade.");
                Assert.AreEqual((int)EaseType.InOutCircular, ahead.CustomData["strobeEasing"].AsInt,
                    "Shift+scroll on a ribbon must author IOCr as the first strobeEasing on the ahead node.");
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

        // CtrlMiddleClickTogglesGlsColorLerpType proves the authored composite reaches both GLS color-node
        // controllers, toggles in both directions, and suppresses the less-specific middle-click mirror action.
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void CtrlMiddleClickTogglesGlsColorLerpType(bool outerLane, bool startsHsv)
        {
            SetEditingMode(outerLane ? EditingMode.GLS : EditingMode.EventBox);
            var custom = startsHsv ? "\"easingType\":\"HSV\"" : null;
            var group = PlaceColorGroup(0, custom, 1, null);
            var containerObject = new GameObject("GLS color lerp middle-click test container");
            var controllerObject = new GameObject("GLS color lerp middle-click test controller");
            try
            {
                CMInput.IGLSColorObjectsActions controller;
                if (outerLane)
                {
                    var container = CreateOuterContainer(containerObject, group, group.Boxes[0].Events[0]);
                    controller = CreateOuterController(controllerObject, container);
                }
                else
                {
                    var container = CreateInnerContainer(containerObject, group.Boxes[0].Events[0]);
                    controller = CreateInnerController(controllerObject, container);
                }

                SendChordMiddleClick(controller, Key.LeftCtrl);

                var evt = GetOpenColorGroup().Boxes[0].Events[0];
                var expected = startsHsv
                    ? BasicEventColorLerpType.RGB
                    : BasicEventColorLerpType.TrueHSV;
                Assert.AreEqual(expected, evt.CustomLerpType);
                Assert.AreEqual(!startsHsv, evt.CustomData.HasKey("easingType"));
                Assert.AreEqual(0, evt.Color,
                    "The more-specific Ctrl+Middle binding must suppress the plain middle-click color mirror.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(containerObject);
            }
        }

        // OuterPreviewHoverMutationKeepsPhysicalNodeAndOutline reproduces the one-frame outline loss caused by
        // recycling every outer preview while a hover edit replaces its parent GLS group.
        [TestCase(false)]
        [TestCase(true)]
        public void OuterPreviewHoverMutationKeepsPhysicalNodeAndOutline(bool ghost)
        {
            SetEditingMode(EditingMode.GLS);
            var restorePage = ConfigureOuterPreviewPage();
            var group = PlaceThreeNodeColorGroup();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            var controllerObject = new GameObject("Outer preview hover continuity controller");
            try
            {
                Assert.IsTrue(collection.LoadedContainers.TryGetValue(group, out var loaded));
                var owner = loaded as GLSGroupContainer;
                Assert.NotNull(owner);
                var ghosts = GetPreviewGhosts(owner);
                Assert.AreEqual(2, ghosts.Count);
                var hovered = ghost ? ghosts[0] : owner;
                var hoveredOffset = ghost ? 0.75f : 0.5f;
                Assert.That(hovered.PreviewEventData.RelativeJsonTime, Is.EqualTo(hoveredOffset));
                owner.SetGroupHighlighted(true);
                var controller = CreateOuterController(controllerObject, hovered);

                SendChordScroll(controller, 1f, Key.LeftAlt);

                var replacement = GetOpenColorGroup();
                Assert.IsTrue(collection.LoadedContainers.TryGetValue(replacement, out var replacementLoaded));
                var replacementOwner = replacementLoaded as GLSGroupContainer;
                Assert.NotNull(replacementOwner);
                var replacementHovered = ghost
                    ? GetPreviewGhosts(replacementOwner)
                        .Single(preview => Mathf.Approximately(preview.PreviewEventData.RelativeJsonTime, hoveredOffset))
                    : replacementOwner;
                Assert.AreSame(hovered, replacementHovered,
                    "A same-shape hover mutation must preserve the physical preview node under the cursor.");
                Assert.IsTrue(replacementHovered.Highlighted,
                    "The hovered outer preview outline must remain visible through synchronous group replacement.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                restorePage();
            }
        }

        // RapidOuterPreviewScrollKeepsMutatingFrontNode reproduces a stale collider being rebound to the next
        // preview behind the cursor between wheel callbacks in one fast hover-scroll sequence.
        [Test]
        public void RapidOuterPreviewScrollKeepsMutatingFrontNode()
        {
            SetEditingMode(EditingMode.GLS);
            var restorePage = ConfigureOuterPreviewPage();
            var group = PlaceThreeNodeColorGroup();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            var controllerObject = new GameObject("Rapid outer preview scroll controller");
            try
            {
                Assert.IsTrue(collection.LoadedContainers.TryGetValue(group, out var loaded));
                var owner = loaded as GLSGroupContainer;
                Assert.NotNull(owner);
                var hovered = GetPreviewGhosts(owner)[0];
                var controller = CreateOuterController(controllerObject, hovered);

                SendChordScroll(controller, 1f, Key.LeftAlt);
                SendChordScroll(controller, 1f, Key.LeftAlt);

                var events = GetOpenColorGroup().OrderedEvents.Cast<BaseLightColorBase>().ToArray();
                var front = events.Single(evt => Mathf.Approximately(evt.RelativeJsonTime, 0.75f));
                var behind = events.Single(evt => Mathf.Approximately(evt.RelativeJsonTime, 1f));
                Assert.That(front.Brightness, Is.EqualTo(1.2f).Within(0.0001f),
                    "Both rapid wheel callbacks must remain bound to the preview initially under the cursor.");
                Assert.That(behind.Brightness, Is.EqualTo(1f).Within(0.0001f),
                    "The preview behind the hovered node must not receive a stale-collider wheel edit.");
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                restorePage();
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

        // Ribbon hover shortcuts must not turn the empty interval into an occupied node, with or without Alt.
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ColorRibbonHoverKeepsPlacementGhostVisible(bool outerLane, bool holdAlt)
        {
            AssertColorRibbonPlacement(outerLane, holdAlt, click: false);
        }

        // Exercise both orders of the shared left click: inserting a group can invalidate the hover cache before group entry runs.
        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        [TestCase(true, true, true)]
        public void ColorRibbonLeftClickPlacesBetweenNodes(bool outerLane, bool enterGroupFirst, bool afterLateUpdate)
        {
            AssertColorRibbonPlacement(outerLane, holdAlt: false, click: true,
                enterGroupFirst: enterGroupFirst, afterLateUpdate: afterLateUpdate);
        }

        // Existing preview nodes must still block outer group placement, including after the temporary hit cache is cleared.
        [TestCase(false)]
        [TestCase(true)]
        public void OuterColorNodeHoverStillBlocksPlacement(bool afterLateUpdate)
        {
            AssertColorRibbonPlacement(outerLane: true, holdAlt: false, click: true,
                ribbonHit: false, afterLateUpdate: afterLateUpdate);
        }

        // Pooled inner containers can change node type while retaining the same ribbon GameObject between raycasts.
        [Test]
        public void ColorRibbonHitClassificationRefreshesAfterOwnerIsRebound()
        {
            var containerObject = new GameObject("Rebound color ribbon owner");
            try
            {
                var container = CreateInnerContainer(containerObject, new BaseLightColorBase());
                var ribbon = CreateRibbonHitObject(containerObject);
                BeatmapRaycastCache.FirstHit = ribbon;
                BeatmapRaycastCache.HasHit = true;
                Assert.IsTrue(GLSEventCommon.IsColorTransitionRibbonHit());

                BeatmapRaycastCache.Invalidate();
                container.EventData = new BaseLightRotationBase();
                BeatmapRaycastCache.FirstHit = ribbon;
                BeatmapRaycastCache.HasHit = true;
                Assert.IsFalse(GLSEventCommon.IsColorTransitionRibbonHit(),
                    "Reusing the physical hit after invalidation must not exempt a non-color node from placement blocking.");
            }
            finally
            {
                BeatmapRaycastCache.Invalidate();
                Object.DestroyImmediate(containerObject);
            }
        }

        // The default test environment may have no GLS lanes; install an owned color track so failures exercise ribbon placement.
        private static void AssertColorRibbonPlacement(
            bool outerLane, bool holdAlt, bool click, bool ribbonHit = true, bool enterGroupFirst = false,
            bool afterLateUpdate = false)
        {
            var runtime = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var groupProvider = Object.FindAnyObjectByType<GLSGroupGridProvider>();
            var originalTracks = runtime.TrackDefinitions;
            var originalPage = groupProvider.CurrentGroup;
            var testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            testTracks.Copy(originalTracks);
            SetPrivateField(testTracks, "glsEntries", new List<TrackDefinitionGLS>
            {
                new() { ID = 1, Name = "Ribbon placement lane", Group = "Ribbon placement tests", ColorTrack = true }
            });
            testTracks.Initialize();
            try
            {
                runtime.TrackDefinitions = testTracks;
                runtime.NotifyTrackDefinitions();
                groupProvider.SetGroupPage("Ribbon placement tests");
                AssertColorRibbonPlacementOnTrack(outerLane, holdAlt, click, ribbonHit, enterGroupFirst, afterLateUpdate);
            }
            finally
            {
                runtime.TrackDefinitions = originalTracks;
                runtime.NotifyTrackDefinitions();
                groupProvider.SetGroupPage(originalPage);
                Object.DestroyImmediate(testTracks);
            }
        }

        // Use initialized scene placements and authoritative map data, replacing only the frame's physical hit and OS input.
        private static void AssertColorRibbonPlacementOnTrack(
            bool outerLane, bool holdAlt, bool click, bool ribbonHit, bool enterGroupFirst, bool afterLateUpdate)
        {
            SetEditingMode(outerLane ? EditingMode.GLS : EditingMode.EventBox);
            var group = PlaceColorGroup(0, null, 0, null, secondTransition: 1);
            var source = group.Boxes[0].Events[0];
            const float placementBeat = 20.625f;
            var containerObject = new GameObject("Color ribbon placement hit owner");
            var surfaceObject = new GameObject("Color ribbon placement grid surface");
            surfaceObject.transform.SetParent(containerObject.transform);
            var callbackProviderObject = new GameObject("Color ribbon placement callback provider");
            var inputFixture = new InputTestFixture();
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            var enabledMaps = new List<InputActionMap>();
            foreach (var map in sharedInput.asset.actionMaps)
            {
                if (map.enabled)
                    enabledMaps.Add(map);
            }
            sharedInput.Disable();
            inputFixture.Setup();
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var input = new CMInput();
            var keybinds = Object.FindAnyObjectByType<KeybindsController>();
            var hoverModifier = new InputAction("Ribbon placement hover modifier", InputActionType.Button, "<Keyboard>/alt");
            hoverModifier.performed += keybinds.OnHoverModifier;
            hoverModifier.canceled += keybinds.OnHoverModifier;
            hoverModifier.Enable();
            var router = Object.FindAnyObjectByType<PlacementInputSystem>();
            var routerState = new Dictionary<FieldInfo, object>();
            foreach (var name in new[] { "currentProvider", "isOnGrid", "applicationFocus", "applicationFocusChanged", "inputState" })
            {
                var field = typeof(PlacementInputSystem).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);
                routerState.Add(field, field.GetValue(router));
            }
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var previousSnapping = atsc.GridMeasureSnapping;
            // Virtual clicks must not inherit whether the physical cursor last left the editor's Game view.
            var mouseInWindow = typeof(KeybindsController).GetProperty(nameof(KeybindsController.IsMouseInWindow));
            var previousMouseInWindow = KeybindsController.IsMouseInWindow;
            BasePlacement placement = null;
            BeatmapGLSGroupColorInputController outerInput = null;
            GLSGroupContainer previousHoveredObject = null;
            var previousHovering = false;
            var previousBounds = default(Bounds);
            var previousState = PlacementState.Idle;
            var previousAllowPlacement = false;
            try
            {
                ObjectContainer owner;
                ObjectContainer ghost;
                BeatmapRaycastCache.Invalidate();
                atsc.GridMeasureSnapping = 8;
                if (outerLane)
                {
                    var trackProvider = Object.FindAnyObjectByType<GLSGroupGridProvider>();
                    Assert.IsTrue(trackProvider.IdToTracks.TryGetValue(group.ID, out var track),
                        "The test group must have a real outer GLS track.");
                    var provider = track.GetComponent<PlacementProvider>();
                    Assert.NotNull(provider);
                    GLSGroupColorPlacement colorPlacement = null;
                    foreach (var candidate in provider.Placements)
                    {
                        if (candidate is GLSGroupColorPlacement color)
                            colorPlacement = color;
                    }
                    Assert.NotNull(colorPlacement);
                    placement = colorPlacement;
                    outerInput = (BeatmapGLSGroupColorInputController)typeof(GLSGroupColorPlacement)
                        .GetField("groupInputController", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(colorPlacement);
                    previousHovering = outerInput.IsHovering;
                    previousHoveredObject = outerInput.HoveredObject;
                    outerInput.IsHovering = false;
                    colorPlacement.Initialize(provider);
                    // Frame-boundary clicks exercise the real hover lifecycle, which needs the prefab's initialized outline renderers.
                    var outerOwner = (GLSGroupContainer)colorPlacement.ObjectContainerCollection.CreateContainer();
                    outerOwner.transform.SetParent(containerObject.transform, false);
                    outerOwner.ObjectData = group;
                    outerOwner.Setup();
                    outerOwner.PreviewEventData = source;
                    owner = outerOwner;
                    ghost = colorPlacement.PlacementVisualContainer;
                }
                else
                {
                    var colorPlacement = Object.FindAnyObjectByType<GLSEventColorPlacement>();
                    Assert.NotNull(colorPlacement);
                    placement = colorPlacement;
                    colorPlacement.Initialize(null);
                    owner = CreateInnerContainer(containerObject, source);
                    ghost = colorPlacement.PlacementVisualContainer;
                }

                previousBounds = placement.Bounds;
                previousState = placement.State;
                previousAllowPlacement = placement.AllowPlacement;
                placement.AllowPlacement = true;
                placement.Bounds = new Bounds(new Vector3(2f, 0.5f, 0f), new Vector3(4f, 1f, 1f));
                var songTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(placementBeat);
                var worldPoint = placement.PlacementTrack.TransformPoint(
                    new Vector3(0.5f, 0f, songTime * EditorScaleController.EditorScale));
                var gridHit = new Intersections.IntersectionHit(
                    surfaceObject, new Bounds(Vector3.zero, Vector3.one), new Ray(worldPoint, Vector3.forward), 0f);
                placement.UpdateState(gridHit, PlacementInputState.Hover);
                Assert.IsTrue(placement.CanPlace, "The control hover on empty grid space must allow placement.");
                Assert.IsTrue(ghost.gameObject.activeSelf, "The control hover must show the placement ghost.");
                Assert.That(placement.RoundedJsonTime, Is.EqualTo(placementBeat).Within(0.00001f));

                InputSystem.QueueStateEvent(keyboard, holdAlt ? new KeyboardState(Key.LeftAlt) : new KeyboardState());
                InputSystem.Update();
                BeatmapRaycastCache.FirstHit = ribbonHit ? CreateRibbonHitObject(owner.gameObject) : owner.gameObject;
                BeatmapRaycastCache.HasHit = true;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                if (outerInput != null)
                {
                    outerInput.HoveredObject = (GLSGroupContainer)owner;
                    outerInput.IsHovering = true;
                    // Preserve the same physical-hover lifecycle as Update before LateUpdate clears the shared raycast cache.
                    typeof(BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>)
                        .GetMethod("HandleHoverChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(outerInput, new object[] { owner });
                }
                if (ribbonHit)
                {
                    Assert.IsTrue(GLSEventCommon.TryGetColorTransitionTarget(owner, source, out var target),
                        "The physical ribbon hit must still resolve its easing target.");
                    Assert.AreSame(group.Boxes[0].Events[1], target);
                }
                placement.UpdateState(gridHit, PlacementInputState.Hover);
                Debug.Log($"[ColorRibbonPlacement] outer={outerLane} alt={holdAlt} ribbon={ribbonHit} " +
                    $"beat={placement.RoundedJsonTime} visible={ghost.gameObject.activeSelf} canPlace={placement.CanPlace}");
                if (!click)
                {
                    Assert.IsTrue(ghost.gameObject.activeSelf,
                        "Hovering a GLS color transition ribbon must not hide the placement ghost between nodes.");
                    return;
                }

                var callbackProvider = callbackProviderObject.AddComponent<PlacementProvider>();
                callbackProvider.Placements = new[] { placement };
                // OuterColorNodeHoverStillBlocksPlacement enters the group and refreshes bounds; retain the real grid dependency.
                callbackProvider.Lane = outerLane
                    ? ((GLSGroupColorPlacement)placement).GlsGroupTrack.GridLane
                    : (GridLane)typeof(GLSEventGridProvider).GetField("gridLane", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(Object.FindAnyObjectByType<GLSEventGridProvider>());
                SetPrivateField(router, "currentProvider", callbackProvider);
                SetPrivateField(router, "isOnGrid", true);
                SetPrivateField(router, "applicationFocus", true);
                SetPrivateField(router, "applicationFocusChanged", false);
                SetPrivateField(router, "inputState", PlacementInputState.Hover);
                // Fail as a setup error if an unrelated tool or UI guard owns input, not as a ribbon placement regression.
                mouseInWindow.SetValue(null, true);
                Assert.IsTrue((bool)typeof(PlacementInputSystem)
                    .GetProperty("CanInteract", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(router),
                    "The placement input control must be interactive before exercising the ribbon click.");
                var ui = (CustomStandaloneInputModule)typeof(PlacementInputSystem)
                    .GetField("customStandaloneInputModule", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(router);
                Assert.IsFalse(ui.IsPointerOverGameObject<UnityEngine.UI.GraphicRaycaster>(0, true),
                    "The placement input control must not be covered by UI.");
                Assert.IsFalse(PersistentUI.Instance.DialogBoxIsEnabled);
                // Dispatch the real left-button action to both production callbacks in a deterministic order, including after cache invalidation.
                if (outerInput != null && enterGroupFirst)
                    input.PlacementControllers.PlaceObject.performed += outerInput.OnEnterGroup;
                input.PlacementControllers.PlaceObject.performed += router.OnPlaceObject;
                input.PlacementControllers.PlaceObject.Enable();
                // The same outer left click also reaches group entry; ribbon placement must not navigate into its source group.
                if (outerInput != null && !enterGroupFirst)
                    input.PlacementControllers.PlaceObject.performed += outerInput.OnEnterGroup;
                var applied = 0;
                void RecordApplied() => applied++;
                if (outerLane)
                    ((GLSGroupColorPlacement)placement).OnApplied += RecordApplied;
                else
                    ((GLSEventColorPlacement)placement).OnApplied += RecordApplied;
                try
                {
                    // Dynamic input arrives before the next Update; a click must survive the preceding LateUpdate's cache clear.
                    if (afterLateUpdate)
                    {
                        typeof(BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>)
                            .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(outerInput, null);
                        Assert.IsFalse(BeatmapRaycastCache.HasHit);
                    }
                    InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                    InputSystem.Update();
                    Assert.AreEqual(ribbonHit ? 1 : 0, applied,
                        "Left click must place through a ribbon, but must not place over an existing outer node.");
                    // Ribbon hover ownership is for easing, not navigation; real preview nodes must still enter their group.
                    if (outerLane)
                    {
                        Assert.AreEqual(ribbonHit ? EditingMode.GLS : EditingMode.EventBox,
                            Object.FindAnyObjectByType<EditModeContext>().EditingMode,
                            "Only a real preview node, not its color ribbon, should enter the source group.");
                    }
                    if (ribbonHit && !outerLane)
                    {
                        var events = GetOpenColorGroup().Boxes[0].Events;
                        Assert.AreEqual(3, events.Length);
                        Assert.That(events[1].JsonTime, Is.EqualTo(placementBeat).Within(0.00001f));
                        Assert.That(events[0].RelativeJsonTime, Is.EqualTo(0.5f));
                        Assert.That(events[2].RelativeJsonTime, Is.EqualTo(0.75f));
                    }
                    else if (ribbonHit)
                    {
                        var collection = ((GLSGroupColorPlacement)placement).ObjectContainerCollection;
                        var placed = collection.GetBetween(placementBeat, placementBeat);
                        Assert.AreEqual(1, placed.Length);
                        Assert.That(placed[0].JsonTime, Is.EqualTo(placementBeat).Within(0.00001f));
                        Assert.AreEqual(group.ID, ((BaseLightColorEventBoxGroup)placed[0]).ID);
                        Assert.AreEqual(2, group.Boxes[0].Events.Length);
                    }
                }
                finally
                {
                    if (outerLane)
                        ((GLSGroupColorPlacement)placement).OnApplied -= RecordApplied;
                    else
                        ((GLSEventColorPlacement)placement).OnApplied -= RecordApplied;
                }
            }
            finally
            {
                if (placement != null)
                {
                    placement.Exit();
                    placement.Bounds = previousBounds;
                    placement.State = previousState;
                    placement.AllowPlacement = previousAllowPlacement;
                }
                if (outerInput != null)
                {
                    // Retire the test hover before destroying its prefab, then restore the shared precision tracker after simulated LateUpdate.
                    typeof(BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>)
                        .GetMethod("HandleHoverChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(outerInput, new object[] { null });
                    outerInput.IsHovering = previousHovering;
                    outerInput.HoveredObject = previousHoveredObject;
                    if (afterLateUpdate)
                    {
                        typeof(BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>)
                            .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(outerInput, null);
                    }
                }
                foreach (var entry in routerState)
                    entry.Key.SetValue(router, entry.Value);
                atsc.GridMeasureSnapping = previousSnapping;
                // Restore the real editor's cursor boundary state after isolated placement input has finished.
                mouseInWindow.SetValue(null, previousMouseInWindow);
                BeatmapRaycastCache.Invalidate();
                input.Dispose();
                hoverModifier.Dispose();
                keybinds.OnHoverModifier(default);
                inputFixture.TearDown();
                foreach (var map in enabledMaps)
                    map.Enable();
                Object.DestroyImmediate(callbackProviderObject);
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

        // Drive the authored button composite through isolated virtual devices so host mouse state cannot trigger mirroring.
        private static void SendChordMiddleClick(
            CMInput.IGLSColorObjectsActions controller,
            params Key[] modifiers)
        {
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.NotNull(sharedInput);
            var sharedMapWasEnabled = sharedInput.GLSColorObjects.enabled;
            sharedInput.GLSColorObjects.Disable();
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
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Middle));
                InputSystem.Update();
            }
            finally
            {
                input.GLSColorObjects.Disable();
                input.Dispose();
                inputFixture.TearDown();
                if (sharedMapWasEnabled)
                {
                    sharedInput.GLSColorObjects.Enable();
                }
            }
        }

        // ShiftClickingGlsRibbonDoesNotSelectOwningNode drives the authored Shift+Left selection composite in isolation.
        private static void SendShiftLeftClick(CMInput.IBeatmapObjectsActions controller)
        {
            var sharedInput = CMInputCallbackInstaller.InputInstance;
            Assert.NotNull(sharedInput);
            var sharedMapWasEnabled = sharedInput.BeatmapObjects.enabled;
            sharedInput.BeatmapObjects.Disable();
            var inputFixture = new InputTestFixture();
            inputFixture.Setup();
            var input = new CMInput();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            try
            {
                input.BeatmapObjects.SetCallbacks(controller);
                input.BeatmapObjects.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift));
                InputSystem.Update();
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                InputSystem.Update();
            }
            finally
            {
                input.BeatmapObjects.Disable();
                input.Dispose();
                inputFixture.TearDown();
                if (sharedMapWasEnabled)
                {
                    sharedInput.BeatmapObjects.Enable();
                }
            }
        }

        // Place one authoritative group with two events and a valid all-lights filter; f=0 selects nothing in real playback.
        // The first event carries the exercised strobe/custom state.
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
                    {{ ""f"": {{ ""f"": 1, ""p"": 1, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }}, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
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

        // The hover-continuity regressions need two pooled ghosts so a parent rebuild can expose slot reversal.
        private static BaseLightColorEventBoxGroup PlaceThreeNodeColorGroup()
        {
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 20, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 1, ""p"": 1, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 },
                                 { ""b"": 0.75, ""c"": 1, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 },
                                 { ""b"": 1.0, ""c"": 0, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 } ] }
                ] }"));
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            collection.RefreshPool();
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = group;
            return group;
        }

        // Page-aware pooling needs the hover-continuity fixtures to publish and select the lane that owns group ID 1.
        private static System.Action ConfigureOuterPreviewPage()
        {
            var runtime = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var provider = Object.FindAnyObjectByType<GLSGroupGridProvider>();
            var originalTracks = runtime.TrackDefinitions;
            var tracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            tracks.Register(new TrackDefinitionGLS
            {
                ID = 1,
                Group = "GLS hover continuity",
                Name = "GLS hover continuity",
                ColorTrack = true
            });
            runtime.TrackDefinitions = tracks;
            runtime.NotifyTrackDefinitions();
            provider.SetGroupPage("GLS hover continuity");
            return () =>
            {
                runtime.TrackDefinitions = originalTracks;
                runtime.NotifyTrackDefinitions();
                Object.DestroyImmediate(tracks);
            };
        }

        // Read the owner's maintained slot list without discovering preview objects through the whole Unity scene.
        private static List<GLSGroupContainer> GetPreviewGhosts(GLSGroupContainer owner)
        {
            var field = typeof(GLSGroupContainer).GetField("previewGhosts", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (List<GLSGroupContainer>)field.GetValue(owner);
        }

        private static string CustomJson(string custom) =>
            string.IsNullOrEmpty(custom) ? string.Empty : $", \"\"customData\"\": {{{custom}}}";

        // Build a data-only inner container exactly like the strobe-fade regression fixture.
        private static GLSEventContainer CreateInnerContainer(GameObject containerObject, BaseLightColorBase evt)
        {
            var container = containerObject.AddComponent<GLSEventContainer>();
            // Data-only test containers still need the lifecycle dependency that OnDestroy unregisters from.
            container.VisualSettings = GetInitializedVisualSettings();
            // HoveringGlsRibbonDoesNotHighlightOwningNode gives data-only containers their normal outline dependency.
            container.SelectionMpbController = containerObject.AddComponent<MaterialPropertyBlockController>();
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

        // Ribbon selection and hover regressions initialize the normal base-controller dependencies before invoking input.
        private static void ConfigureBaseInputDependencies(
            TestGLSEventColorInputController controller,
            EditingMode editingMode)
        {
            var inputType = typeof(BeatmapInputController<GLSEventContainer>);
            inputType.GetField("CustomStandaloneInputModule", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, Object.FindAnyObjectByType<CustomStandaloneInputModule>());
            inputType.GetField("EditContext", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, Object.FindAnyObjectByType<EditModeContext>());
            inputType.GetField("editMode", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, editingMode);
            inputType.GetField("obstaclePlacement", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, Object.FindAnyObjectByType<ObstaclePlacement>());
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
