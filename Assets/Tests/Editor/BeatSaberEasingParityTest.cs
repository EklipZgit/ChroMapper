using System.Reflection;
using Beatmap.Enums;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tests.Editor
{
    public class BeatSaberEasingParityTest : InputTestFixture
    {
        // These samples encode Beat Saber 1.44.1 Tweening.Easing so editor previews cannot drift from runtime curves.
        [TestCase(ElasticCurve.In, 0.25f)]
        [TestCase(ElasticCurve.In, 0.6f)]
        [TestCase(ElasticCurve.Out, 0.25f)]
        [TestCase(ElasticCurve.Out, 0.6f)]
        [TestCase(ElasticCurve.InOut, 0.25f)]
        [TestCase(ElasticCurve.InOut, 0.6f)]
        public void StandardElasticMatchesBeatSaber1441(ElasticCurve curve, float time)
        {
            var expected = EvaluateBeatSaberElastic(curve, time);
            var actual = EvaluateChroMapperElastic(curve, time);

            Assert.AreEqual(expected, actual, 0.000001f);
        }

        // CyclingAlternativeCurveVisitsBothRuntimeVariants proves CM can author every InOut alternative accepted by Beat Saber.
        [TestCase(EaseType.OutBack, EaseType.InOutBack)]
        [TestCase(EaseType.InOutBack, EaseType.BeatSaberInOutBack)]
        [TestCase(EaseType.BeatSaberInOutBack, EaseType.InBack)]
        [TestCase(EaseType.OutElastic, EaseType.InOutElastic)]
        [TestCase(EaseType.InOutElastic, EaseType.BeatSaberInOutElastic)]
        [TestCase(EaseType.BeatSaberInOutElastic, EaseType.InElastic)]
        [TestCase(EaseType.OutBounce, EaseType.InOutBounce)]
        [TestCase(EaseType.InOutBounce, EaseType.BeatSaberInOutBounce)]
        [TestCase(EaseType.BeatSaberInOutBounce, EaseType.InBounce)]
        public void CyclingAlternativeCurveVisitsBothRuntimeVariants(EaseType start, EaseType expected)
        {
            var selected = PerformEasingAction(
                start,
                (controller, action) => action.performed += controller.OnEasingCurve);

            Assert.AreEqual(expected, selected);
        }

        // CyclingAlternativeFamilyPreservesInOutVariant prevents either valid runtime curve from being silently converted.
        [TestCase(EaseType.InOutBounce, EaseType.InOutBack)]
        [TestCase(EaseType.InOutBack, EaseType.InOutElastic)]
        [TestCase(EaseType.InOutElastic, EaseType.InOutBounce)]
        [TestCase(EaseType.BeatSaberInOutBounce, EaseType.BeatSaberInOutBack)]
        [TestCase(EaseType.BeatSaberInOutBack, EaseType.BeatSaberInOutElastic)]
        [TestCase(EaseType.BeatSaberInOutElastic, EaseType.BeatSaberInOutBounce)]
        public void CyclingAlternativeFamilyPreservesInOutVariant(EaseType start, EaseType expected)
        {
            var selected = PerformEasingAction(
                start,
                (controller, action) => action.performed += controller.OnEasingAlternative);

            Assert.AreEqual(expected, selected);
        }

        // A dedicated virtual keyboard drives production callbacks while the fixture owns the isolated Input System runtime.
        private EaseType? PerformEasingAction(
            EaseType start,
            System.Action<BeatmapEasingsSelectionInputController, InputAction> bindAction)
        {
            var controllerObject = new GameObject("Beat Saber easing selection test controller");
            var controller = controllerObject.AddComponent<BeatmapEasingsSelectionInputController>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var cycleAction = new InputAction(
                "Cycle easing curve",
                InputActionType.Button,
                "<Keyboard>/e");
            EaseType? selected = null;
            try
            {
                controller.NotifyEasingChanged(start);
                controller.OnEasingChanged += value => selected = (EaseType)value;
                bindAction(controller, cycleAction);
                cycleAction.Enable();

                Press(keyboard.eKey, queueEventOnly: true);
                InputSystem.Update();

                return selected;
            }
            finally
            {
                // Dispose isolated actions before InputTestFixture restores the original Input System runtime.
                cycleAction.Disable();
                cycleAction.Dispose();
                Object.DestroyImmediate(controllerObject);
            }
        }

        // ExtendedGlsEasingMenuSupportsAllLeads exercises the real menu callbacks and restored state for every ChromaGLS-only family.
        [TestCase(3, EaseType.InSinusoidal)]
        [TestCase(4, EaseType.InCubic)]
        [TestCase(5, EaseType.InQuartic)]
        [TestCase(6, EaseType.InQuintic)]
        [TestCase(7, EaseType.InExponential)]
        public void ExtendedGlsEasingMenuSupportsAllLeads(int familyIndex, EaseType family)
        {
            var menu = new MenuFixture();
            try
            {
                for (var lead = 0; lead < 3; lead++)
                {
                    var easing = family + lead;
                    menu.Controller.RestoreMenuState((int)easing, 0);
                    menu.View.ApplyEditorState((int)easing, 0);
                    Assert.AreEqual(familyIndex, menu.Family.Value, $"Restored {easing} must select its family.");
                    Assert.AreEqual(lead, menu.Direction.Value);

                    for (var targetLead = 0; targetLead < 3; targetLead++)
                    {
                        InvokePrivateMethod(menu.View, "HandleCurveOptionChanged", targetLead);
                        Assert.AreEqual((int)(family + targetLead), menu.Controller.CurrentEasing);
                    }

                    menu.Controller.RestoreMenuState((int)(EaseType.InQuadratic + lead), 0);
                    menu.View.ApplyEditorState(menu.Controller.CurrentEasing, 0);
                    Assert.AreNotEqual(familyIndex, menu.Family.Value, "Selecting a native family clears the extended family.");
                    InvokePrivateMethod(menu.View, "HandleEasingOptionChanged", familyIndex);
                    Assert.AreEqual((int)easing, menu.Controller.CurrentEasing, "Changing family preserves the selected lead.");
                }

                menu.Controller.RestoreMenuState((int)EaseType.BeatSaberInOutBack, 0);
                menu.View.ApplyEditorState(menu.Controller.CurrentEasing, 0);
                InvokePrivateMethod(menu.View, "HandleEasingOptionChanged", familyIndex);
                Assert.AreEqual((int)(family + 2), menu.Controller.CurrentEasing,
                    "Families without a Beat Saber-specific variant use their ordinary InOut curve.");
            }
            finally
            {
                menu.Dispose();
            }
        }

        // EasingMenuDisplaysEitherInOutVariant keeps imported standard and Beat Saber curves visible without rewriting them.
        [TestCase(EaseType.InOutBack, 9, 2)]
        [TestCase(EaseType.InOutElastic, 10, 2)]
        [TestCase(EaseType.InOutBounce, 11, 2)]
        [TestCase(EaseType.BeatSaberInOutBack, 9, 3)]
        [TestCase(EaseType.BeatSaberInOutElastic, 10, 3)]
        [TestCase(EaseType.BeatSaberInOutBounce, 11, 3)]
        public void EasingMenuDisplaysEitherInOutVariant(EaseType easing, int familyIndex, int directionIndex)
        {
            var menu = new MenuFixture();
            try
            {
                menu.Controller.RestoreMenuState((int)easing, 0);
                menu.View.ApplyEditorState((int)easing, 0);

                Assert.AreEqual(directionIndex, menu.Direction.Value);
                Assert.AreEqual(familyIndex, menu.Family.Value);
                Assert.AreEqual((int)easing, menu.Controller.CurrentEasing);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // SelectingInOutLeadEmitsStandardValue keeps the visible three-state menu faithful to its standard InOut label.
        [TestCase(9, EaseType.InOutBack)]
        [TestCase(10, EaseType.InOutElastic)]
        [TestCase(11, EaseType.InOutBounce)]
        public void SelectingInOutLeadEmitsStandardValue(int familyIndex, EaseType expected)
        {
            var menu = new MenuFixture();
            EaseType? selected = null;
            try
            {
                menu.Controller.RestoreMenuState((int)EaseType.InQuadratic, 0);
                menu.View.ApplyEditorState(menu.Controller.CurrentEasing, 0);
                menu.Controller.OnEasingChanged += value => selected = (EaseType)value;

                InvokePrivateMethod(menu.View, "HandleEasingOptionChanged", familyIndex);
                InvokePrivateMethod(menu.View, "HandleCurveOptionChanged", (int)EaseCurve.InOut);

                Assert.AreEqual(expected, selected);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // SelectingAlternativeFamilyWithInOutLeadEmitsStandardValue guards the family switch against OE coercion.
        [TestCase(9, EaseType.InOutBack)]
        [TestCase(10, EaseType.InOutElastic)]
        [TestCase(11, EaseType.InOutBounce)]
        public void SelectingAlternativeFamilyWithInOutLeadEmitsStandardValue(int familyIndex, EaseType expected)
        {
            var menu = new MenuFixture();
            EaseType? selected = null;
            try
            {
                menu.Controller.RestoreMenuState((int)EaseType.InOutQuadratic, 0);
                menu.View.ApplyEditorState(menu.Controller.CurrentEasing, 0);
                menu.Controller.OnEasingChanged += value => selected = (EaseType)value;

                InvokePrivateMethod(menu.View, "HandleEasingOptionChanged", familyIndex);

                Assert.AreEqual(expected, selected);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // SelectingAlternativeFamilyPreservesBeatSaberInOutValue keeps imported custom curves intact during family edits.
        [TestCase(EaseType.BeatSaberInOutBounce, 9, EaseType.BeatSaberInOutBack)]
        [TestCase(EaseType.BeatSaberInOutBack, 10, EaseType.BeatSaberInOutElastic)]
        [TestCase(EaseType.BeatSaberInOutElastic, 11, EaseType.BeatSaberInOutBounce)]
        public void SelectingAlternativeFamilyPreservesBeatSaberInOutValue(
            EaseType start,
            int familyIndex,
            EaseType expected)
        {
            var menu = new MenuFixture();
            EaseType? selected = null;
            try
            {
                menu.Controller.RestoreMenuState((int)start, 0);
                menu.View.ApplyEditorState(menu.Controller.CurrentEasing, 0);
                menu.Controller.OnEasingChanged += value => selected = (EaseType)value;

                InvokePrivateMethod(menu.View, "HandleEasingOptionChanged", familyIndex);

                Assert.AreEqual(expected, selected);
            }
            finally
            {
                menu.Dispose();
            }
        }

        // ChroMapper's public easing delegates are the production preview path exercised by every affected GLS type.
        private static float EvaluateChroMapperElastic(ElasticCurve curve, float time)
        {
            return curve switch
            {
                ElasticCurve.In => Easing.Elastic.In(time),
                ElasticCurve.Out => Easing.Elastic.Out(time),
                _ => Easing.Elastic.InOut(time)
            };
        }

        // This is a direct transcription of Beat Saber 1.44.1 Tweening.Easing's three standard Elastic equations.
        private static float EvaluateBeatSaberElastic(ElasticCurve curve, float time)
        {
            if (time == 0f || time == 1f)
            {
                return time;
            }

            return curve switch
            {
                ElasticCurve.In => -Mathf.Pow(2f, (10f * time) - 10f)
                    * Mathf.Sin(((10f * time) - 10.75f) * (Mathf.PI * 2f / 3f)),
                ElasticCurve.Out => (Mathf.Pow(2f, -10f * time)
                    * Mathf.Sin(((10f * time) - 0.75f) * (Mathf.PI * 2f / 3f))) + 1f,
                _ when time < 0.5f => -(Mathf.Pow(2f, (20f * time) - 10f)
                    * Mathf.Sin(((20f * time) - 11.125f) * (Mathf.PI * 4f / 9f))) / 2f,
                _ => (Mathf.Pow(2f, (-20f * time) + 10f)
                    * Mathf.Sin(((20f * time) - 11.125f) * (Mathf.PI * 4f / 9f)) / 2f) + 1f
            };
        }

        // Reflection sets only serialized test dependencies; production state still flows through ApplyEditorState and ToggleComponent.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Could not find test field {fieldName}.");
            field.SetValue(target, value);
        }

        // Invoke the real private toggle callbacks so the regression covers serialized UI behavior instead of duplicating it.
        private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Could not find test method {methodName}.");
            method.Invoke(target, arguments);
        }

        public enum ElasticCurve
        {
            In,
            Out,
            InOut
        }

        // A reusable menu fixture ensures every UI regression receives the same complete serialized dependency graph.
        private sealed class MenuFixture
        {
            private readonly GameObject root;

            public MenuFixture()
            {
                root = new GameObject("Beat Saber easing menu test");
                Controller = root.AddComponent<BeatmapEasingsSelectionInputController>();
                View = root.AddComponent<InputEasingViewController>();
                Extension = CreateToggle(root.transform, "Extension");
                Family = CreateDropdown(root.transform, "Easing");
                Direction = CreateDropdown(root.transform, "Direction");
                SetPrivateField(View, "inputController", Controller);
                SetPrivateField(View, "extensionToggle", Extension);
                SetPrivateField(View, "easingDropdown", Family);
                SetPrivateField(View, "curveDropdown", Direction);
            }

            public BeatmapEasingsSelectionInputController Controller { get; }
            public InputEasingViewController View { get; }
            public ToggleComponent Extension { get; }
            public DropdownComponent Family { get; }
            public DropdownComponent Direction { get; }

            // Destroy the complete hierarchy together so component and view lifecycle callbacks retain production ordering.
            public void Dispose()
            {
                Object.DestroyImmediate(root);
            }

            // Each ToggleComponent receives the sole dependency its value-update hot path requires.
            private static ToggleComponent CreateToggle(Transform parent, string name)
            {
                var toggleObject = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(ToggleComponent));
                toggleObject.transform.SetParent(parent, false);
                var toggleComponent = toggleObject.GetComponent<ToggleComponent>();
                SetPrivateField(toggleComponent, "toggle", toggleObject.GetComponent<Toggle>());
                return toggleComponent;
            }

            // A bare TMP_Dropdown still records values and options; the tests drive the view's option
            // handlers directly because synchronous tests never reach DropdownComponent.Start.
            private static DropdownComponent CreateDropdown(Transform parent, string name)
            {
                var dropdownObject = new GameObject(name, typeof(RectTransform), typeof(TMP_Dropdown), typeof(DropdownComponent));
                dropdownObject.transform.SetParent(parent, false);
                var component = dropdownObject.GetComponent<DropdownComponent>();
                var dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
                SetPrivateField(component, "dropdown", dropdown);
                component.Selectable = dropdown;
                return component;
            }
        }
    }
}
