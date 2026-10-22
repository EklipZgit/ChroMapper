using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // The GLS easings menu is a serialized family dropdown plus a direction dropdown that only exists
    // for directional families. These tests drive the real TMP_Dropdown events on the mapper scene
    // controls so a missing reference or index-as-easing shortcut fails here instead of at runtime.
    public class InputEasingDropdownTest : GLSExtensionTestBase
    {
        // Family order is curated, not enum order. Each entry emits the family's "In" member, with
        // None and Linear keeping their dedicated ids and the Catmull-Rom modes their spline ids.
        private static readonly int[] FamilyIds =
        {
            (int)EaseType.None,
            (int)EaseType.Linear,
            (int)EaseType.InQuadratic,
            (int)EaseType.InSinusoidal,
            (int)EaseType.InCubic,
            (int)EaseType.InQuartic,
            (int)EaseType.InQuintic,
            (int)EaseType.InExponential,
            (int)EaseType.InCircular,
            (int)EaseType.InBack,
            (int)EaseType.InElastic,
            (int)EaseType.InBounce,
            (int)EaseType.CatmullRom,
            (int)EaseType.CatmullRomCentripetal,
            (int)EaseType.CatmullRomChordal
        };

        private static readonly string[] FamilyLabels =
        {
            "None",
            "Linear",
            "Quadratic",
            "Sine",
            "Cubic",
            "Quartic",
            "Quintic",
            "Exponential",
            "Circular",
            "Back",
            "Elastic",
            "Bounce",
            "Catmull-Rom (Uniform)",
            "Catmull-Rom (Centripetal)",
            "Catmull-Rom (Chordal)"
        };

        // Back, Elastic, and Bounce own the fourth direction entry.
        private static readonly int[] AlternativeIndices = { 9, 10, 11 };

        private InputEasingViewController view;
        private BeatmapEasingsSelectionInputController input;
        private GLSEventGridProvider innerProvider;
        private EditModeContext editModeContext;

        private DropdownComponent Family => Field<DropdownComponent>(view, "easingDropdown");
        private DropdownComponent Direction => Field<DropdownComponent>(view, "curveDropdown");
        private TMP_Dropdown FamilyControl => Field<TMP_Dropdown>(Family, "dropdown");
        private TMP_Dropdown DirectionControl => Field<TMP_Dropdown>(Direction, "dropdown");
        private ToggleComponent Extension => Field<ToggleComponent>(view, "extensionToggle");
        private GameObject ControlsPanel => Field<GameObject>(view, "controlsPanel");

        // Nondirectional families keep the row rendered but unclickable; context visibility owns
        // whether the row is active at all.
        private bool DirectionEnabled => Direction.Selectable.interactable;

        [SetUp]
        public void ResolveEasingMenu()
        {
            view = Object.FindAnyObjectByType<InputEasingViewController>(FindObjectsInactive.Include);
            Assert.That(view, Is.Not.Null, "The mapper scene must contain the easing view controller.");
            input = Field<BeatmapEasingsSelectionInputController>(view, "inputController");
            innerProvider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            editModeContext = Object.FindAnyObjectByType<EditModeContext>();
        }

        // Each case starts from a known directional selection so the remembered direction and the
        // visible options cannot leak between parameterized siblings.
        [TearDown]
        public void RestoreEasingMenu()
        {
            if (innerProvider != null)
                innerProvider.GroupContext = null;
            if (input != null)
                input.RestoreMenuState((int)EaseType.Linear, 0);
        }

        private void ResetToDirectionalDefaults()
        {
            input.RestoreMenuState((int)EaseType.InQuadratic, 0);
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.InQuadratic));
            Assert.That(input.CurrentExtension, Is.Zero);
        }

        private static int AlternativeBeatSaberId(int familyIndex) => familyIndex switch
        {
            9 => (int)EaseType.BeatSaberInOutBack,
            10 => (int)EaseType.BeatSaberInOutElastic,
            _ => (int)EaseType.BeatSaberInOutBounce
        };

        // Spawn a real group through its collection so GLSInputViewController owns the easing tab
        // visibility exactly like authored gameplay context changes do.
        private IEnumerator OpenPanelFor(BaseEventBoxGroup group)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, out _);
            editModeContext.EditingMode = EditingMode.EventBox;
            innerProvider.GroupContext = group;
            Assert.That(ControlsPanel.activeInHierarchy, Is.True, "EventBox transform contexts must show the easing tab.");
            yield return null; // DropdownComponent.Start attaches its TMP_Dropdown listener on first activation.
            // Batchmode never renders frames, so force the layout pass before any bounds assertions.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ControlsPanel.transform.parent);
        }

        private static BaseEventBoxGroup CloneTimed(BaseEventBoxGroup group)
        {
            group = BeatmapFactory.Clone(group);
            group.RecomputeSongBpmTime();
            return group;
        }

        private static BaseEventBoxGroup CreateRotationGroup() => CloneTimed(
            new BaseLightRotationEventBoxGroup
            {
                ID = 0,
                JsonTime = 4f,
                Boxes = new()
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = (int)Axis.Y,
                        Events = new[] { new BaseLightRotationBase { Rotation = 30f } }
                    }
                }
            });

        private static BaseEventBoxGroup CreateGroup(Kind kind) => kind switch
        {
            Kind.Translation => CloneTimed(new BaseLightTranslationEventBoxGroup
            {
                ID = 0,
                JsonTime = 4f,
                Boxes = new()
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = (int)Axis.X,
                        Events = new[] { new BaseLightTranslationBase() }
                    }
                }
            }),
            Kind.FloatFX => CloneTimed(new BaseVfxEventEventBoxGroup
            {
                ID = 0,
                JsonTime = 4f,
                Boxes = new()
                {
                    new BaseVfxEventEventBox
                    {
                        Events = new[] { new BaseFxEventFloat() }
                    }
                }
            }),
            Kind.Color => CloneTimed(new BaseLightColorEventBoxGroup
            {
                ID = 0,
                JsonTime = 4f,
                Boxes = new()
                {
                    new BaseLightColorEventBox
                    {
                        Events = new[] { new BaseLightColorBase() }
                    }
                }
            }),
            _ => CreateRotationGroup()
        };

        private static Type InnerPlacementType(Kind kind) => kind switch
        {
            Kind.Translation => typeof(GLSEventTranslationPlacement),
            Kind.FloatFX => typeof(GLSEventFloatFXPlacement),
            _ => typeof(GLSEventRotationPlacement)
        };

        public enum Kind { Rotation, Translation, FloatFX, Color }

        [UnityTest]
        public IEnumerator EasingMenuUsesSerializedDropdowns()
        {
            var family = Family;
            var direction = Direction;
            Assert.That(FamilyControl, Is.Not.Null, "The family dropdown needs a real TMP_Dropdown.");
            Assert.That(DirectionControl, Is.Not.Null, "The direction dropdown needs a real TMP_Dropdown.");
            Assert.That(family.Selectable, Is.Not.Null);
            Assert.That(direction.Selectable, Is.Not.Null);
            Assert.That(family.transform.IsChildOf(ControlsPanel.transform), Is.True,
                "Tab visibility must own the family dropdown.");
            Assert.That(direction.transform.IsChildOf(ControlsPanel.transform), Is.True,
                "Tab visibility must own the direction dropdown.");
            Assert.That(Family.GetComponent<Tooltip>(), Is.Not.Null,
                "The family dropdown carries the extended-family warning tooltip.");

            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());
            // The column stacks family, direction, then the labeled extension row in one VLG; the
            // obsolete rotated headers are removed, so nothing can crowd the row captions.
            var familyRow = (RectTransform)family.transform;
            var directionRow = (RectTransform)direction.transform;
            var extensionRow = (RectTransform)Extension.transform;
            Assert.That(familyRow.parent, Is.SameAs(directionRow.parent));
            Assert.That(extensionRow.parent, Is.SameAs(directionRow.parent),
                "The extension row must stack inside the same easing column.");
            Assert.That(ControlsPanel.transform.Find("Header"), Is.Null,
                "The Easings header must not crowd the dropdown labels.");
            Assert.That(ControlsPanel.transform.parent.Find("GLS Others Tab"), Is.Null,
                "The Others header wrapper must be removed once Extension joins the column.");
            Assert.That(((RectTransform)ControlsPanel.transform).rect.width,
                Is.EqualTo(136f).Within(0.5f), "The easing column must reserve real width.");

            Assert.That(familyRow.rect.height, Is.EqualTo(20f).Within(0.1f),
                "The closed family row must be 20 units tall.");
            Assert.That(directionRow.rect.height, Is.EqualTo(20f).Within(0.1f),
                "The closed direction row must be 20 units tall.");
            Assert.That(extensionRow.rect.height, Is.EqualTo(14f).Within(0.1f),
                "The extension row keeps its compact 14 unit height.");
            Assert.That(directionRow.anchoredPosition.y, Is.LessThan(familyRow.anchoredPosition.y),
                "The direction row must render below the family row.");
            Assert.That(extensionRow.anchoredPosition.y, Is.LessThan(directionRow.anchoredPosition.y),
                "The extension row must render below both dropdown rows.");
            Assert.That(familyRow.anchoredPosition.y - directionRow.anchoredPosition.y,
                Is.EqualTo(23f).Within(0.1f), "Rows stack with a 3 unit gap.");
            Assert.That(directionRow.anchoredPosition.y - extensionRow.anchoredPosition.y,
                Is.EqualTo(20f).Within(0.1f), "The extension row keeps the same 3 unit gap.");
            var extensionLabel = Extension.GetComponentsInChildren<TMP_Text>(false)
                .FirstOrDefault(text => !string.IsNullOrEmpty(text.text));
            Assert.That(extensionLabel, Is.Not.Null,
                "The extension row keeps a visible, non-empty label under the dropdowns.");

            // The nested dropdown prefab carries its own minimum heights, so the row roots being
            // 20 units is not enough; the actual clickable strips must fill the padded row height
            // and stay inside their rows in every family state. The inactive popup templates
            // are skipped by the active-only label enumeration.
            var familySelectable = (RectTransform)family.Selectable.transform;
            var directionSelectable = (RectTransform)direction.Selectable.transform;
            foreach (var familyIndex in new[] { 1, 13, 3 })
            {
                FamilyControl.value = familyIndex;
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    (RectTransform)ControlsPanel.transform.parent);
                Assert.That(familyRow.rect.height, Is.EqualTo(20f).Within(0.1f),
                    $"{FamilyLabels[familyIndex]} must not change the family row height.");
                Assert.That(directionRow.rect.height, Is.EqualTo(20f).Within(0.1f),
                    $"{FamilyLabels[familyIndex]} must not change the direction row height.");
                Assert.That(familySelectable.rect.height,
                    Is.EqualTo(20f).Within(0.1f),
                    $"{FamilyLabels[familyIndex]} must keep the clickable family strip at the row height.");
                Assert.That(directionSelectable.rect.height,
                    Is.EqualTo(20f).Within(0.1f),
                    $"{FamilyLabels[familyIndex]} must keep the clickable direction strip at the row height.");
                AssertRowContains(familyRow, familySelectable);
                AssertRowContains(directionRow, directionSelectable);
                foreach (var label in familyRow.GetComponentsInChildren<TMP_Text>(false))
                {
                    AssertRowContains(familyRow, (RectTransform)label.transform);
                }

                foreach (var label in directionRow.GetComponentsInChildren<TMP_Text>(false))
                {
                    AssertRowContains(directionRow, (RectTransform)label.transform);
                }
            }

            // The extension row follows the same label-left, control-right pattern as the
            // dropdowns: its label sits directly left of the checkbox, and the checkbox lines
            // up with the dropdown selectables above it.
            var checkbox = (RectTransform)Extension.Selectable.transform;
            var extensionLabelBounds = WorldBounds((RectTransform)extensionLabel.transform);
            var checkboxBounds = WorldBounds(checkbox);
            Assert.That(extensionLabelBounds.max.x, Is.LessThanOrEqualTo(checkboxBounds.min.x + 0.01f),
                "The extension label must sit to the left of its checkbox.");
            Assert.That(checkboxBounds.min.x - extensionLabelBounds.max.x, Is.LessThanOrEqualTo(6f),
                "The extension label must stay attached to its checkbox.");
            Assert.That(Mathf.Abs(checkboxBounds.min.x - WorldBounds(familySelectable).min.x),
                Is.LessThanOrEqualTo(2f),
                "The checkbox must align with the dropdown controls above it.");
            Assert.That(checkboxBounds.size.y, Is.GreaterThan(0f).And.LessThanOrEqualTo(14.1f),
                "The checkbox must fit the compact row height.");
            Assert.That(checkboxBounds.size.x, Is.GreaterThan(0f),
                "The checkbox needs a real clickable width.");
            extensionLabel.ForceMeshUpdate();
            Assert.That(extensionLabel.isTextOverflowing, Is.False,
                "The extension label must fit its container.");

            // The longest family name must stay readable in the closed caption box while the
            // direction row is grayed out, not clipped or shrunk past legibility.
            FamilyControl.value = 13; // Catmull-Rom (Centripetal)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ControlsPanel.transform.parent);
            Assert.That(DirectionEnabled, Is.False, "The caption check needs a grayed direction row.");
            var caption = FamilyControl.captionText;
            caption.ForceMeshUpdate();
            Assert.That(caption.isTextOverflowing, Is.False,
                "The longest family name must fit the closed caption box.");
            Assert.That(caption.fontSize, Is.GreaterThanOrEqualTo(7f),
                "The closed caption must not rely on an unreadably small font.");
            var captionRect = ((RectTransform)caption.transform).rect;
            var captionText = caption.textBounds;
            Assert.That(captionRect.Contains(new Vector2(captionText.min.x, captionText.min.y)), Is.True,
                "The closed caption must paint inside its caption box.");
            Assert.That(captionRect.Contains(new Vector2(captionText.max.x, captionText.max.y)), Is.True,
                "The closed caption must paint inside its caption box.");

            // The taller closed box must actually pad the caption: at least 4 units of air above
            // and 2 below the text area, measured inside each selectable's own rect space.
            AssertCaptionPadding(familySelectable, (RectTransform)FamilyControl.captionText.transform);
            AssertCaptionPadding(directionSelectable, (RectTransform)DirectionControl.captionText.transform);

            // The down arrow shrank to 70% so the wider caption box never collides with it.
            AssertArrowSize(familySelectable);
            AssertArrowSize(directionSelectable);

            // The easing column must reserve real width in every EventBox context: the dropdown
            // selectables may not paint over the Extension toggle or the sibling tab controls next
            // to it, and every clickable control and label in the topbar must stay inside the
            // Center Panel. Only the controls' own rects count, so the hidden TMP popup templates
            // cannot taint the measurement.
            var extension = Extension.Selectable;
            foreach (var kind in new[] { Kind.Rotation, Kind.Translation, Kind.FloatFX, Kind.Color })
            {
                innerProvider.GroupContext = CreateGroup(kind);
                yield return null;
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ControlsPanel.transform.parent.parent);
                AssertClickableFree((RectTransform)family.Selectable.transform, extension, kind.ToString());
                AssertClickableFree((RectTransform)direction.Selectable.transform, extension, kind.ToString());
                AssertTopBarContained(kind);
            }

            // Open the real TMP popup and prove every family choice is fully visible inside the
            // viewport without a scrollbar, then confirm the popup stays on screen.
            innerProvider.GroupContext = CreateGroup(Kind.Rotation);
            yield return null;
            FamilyControl.Show();
            try
            {
                var popup = Field<GameObject>(FamilyControl, "m_Dropdown");
                Assert.That(popup, Is.Not.Null, "The family dropdown must open its option list.");
                var popupRect = (RectTransform)popup.transform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);

                var scroll = popup.GetComponent<ScrollRect>();
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.vertical, Is.False, "The family list must not scroll vertically.");
                Assert.That(scroll.horizontal, Is.False, "The family list must not scroll.");
                Assert.That(scroll.verticalScrollbar, Is.Null,
                    "The family list must not carry a scrollbar.");
                var scrollbar = popup.transform.Find("Scrollbar");
                Assert.That(scrollbar == null || !scrollbar.gameObject.activeSelf, Is.True,
                    "The family popup must not show a scrollbar.");

                var viewport = (RectTransform)popup.transform.Find("Viewport");
                var content = (RectTransform)viewport.Find("Content");
                var viewportBounds = WorldBounds(viewport);
                var contentBounds = WorldBounds(content);
                Assert.That(contentBounds.size.y, Is.LessThanOrEqualTo(viewportBounds.size.y + 0.01f),
                    "Every family item must fit inside the popup viewport without scrolling.");

                // Each cloned item carries a Toggle on its row root; the deactivated item template
                // stays invisible to this active-only enumeration.
                var items = content.GetComponentsInChildren<Toggle>();
                Assert.That(items, Has.Length.EqualTo(FamilyIds.Length),
                    "The popup must instantiate one item per family option.");
                var paddedViewport = viewportBounds;
                paddedViewport.Expand(0.05f); // rows share the viewport edge, so allow rounding slack
                foreach (var item in items)
                {
                    var itemBounds = WorldBounds((RectTransform)item.transform);
                    Assert.That(
                        paddedViewport.Contains(itemBounds.min) && paddedViewport.Contains(itemBounds.max),
                        Is.True, $"{item.name} must paint fully inside the popup viewport " +
                            $"(item {itemBounds.min}..{itemBounds.max} vs viewport {viewportBounds.min}..{viewportBounds.max}).");
                }

                var labels = popup.GetComponentsInChildren<TMP_Text>();
                Assert.That(labels, Has.Length.GreaterThanOrEqualTo(FamilyIds.Length),
                    "The popup must render one label per family option.");
                foreach (var label in labels)
                {
                    label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False,
                        $"Popup item '{label.text}' overflows its item.");
                    var labelRect = ((RectTransform)label.transform).rect;
                    var textRect = label.textBounds;
                    Assert.That(labelRect.Contains(new Vector2(textRect.min.x, textRect.min.y)), Is.True,
                        $"Popup item '{label.text}' paints outside its item row.");
                    Assert.That(labelRect.Contains(new Vector2(textRect.max.x, textRect.max.y)), Is.True,
                        $"Popup item '{label.text}' paints outside its item row.");
                }

                var popupBounds = WorldBounds(popupRect);
                var screenBounds = WorldBounds((RectTransform)popupRect.GetComponentInParent<Canvas>()
                    .rootCanvas.transform);
                Assert.That(screenBounds.Contains(popupBounds.min), Is.True,
                    $"The family popup must open inside the canvas (popup {popupBounds.min}..{popupBounds.max} vs canvas {screenBounds.min}..{screenBounds.max}).");
                Assert.That(screenBounds.Contains(popupBounds.max), Is.True,
                    $"The family popup must open inside the canvas (popup {popupBounds.min}..{popupBounds.max} vs canvas {screenBounds.min}..{screenBounds.max}).");
            }
            finally
            {
                FamilyControl.Hide();
            }

            // The popup must never change the closed-box heights.
            Assert.That(familyRow.rect.height, Is.EqualTo(20f).Within(0.1f),
                "Opening the popup must not stretch the family row.");
            Assert.That(directionRow.rect.height, Is.EqualTo(20f).Within(0.1f),
                "Opening the popup must not stretch the direction row.");
        }

        // Convert the caption's world corners into the selectable's local rect space so the
        // vertical padding is measured in the same logical units the layout uses. The closed box
        // pads the caption 4 units on top and 2 on the bottom.
        private static void AssertCaptionPadding(RectTransform selectable, RectTransform caption)
        {
            var corners = new Vector3[4];
            caption.GetWorldCorners(corners);
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            foreach (var corner in corners)
            {
                var local = selectable.InverseTransformPoint(corner);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }

            var rowRect = selectable.rect;
            Assert.That(minY - rowRect.yMin, Is.GreaterThanOrEqualTo(1.9f),
                $"The {selectable.name} caption must keep at least 2 units of bottom padding " +
                $"(caption {minY}..{maxY} inside {rowRect.yMin}..{rowRect.yMax}).");
            Assert.That(rowRect.yMax - maxY, Is.GreaterThanOrEqualTo(3.9f),
                $"The {selectable.name} caption must keep at least 4 units of top padding " +
                $"(caption {minY}..{maxY} inside {rowRect.yMin}..{rowRect.yMax}).");
        }

        private static void AssertArrowSize(RectTransform selectable)
        {
            var arrow = selectable.Find("Arrow") as RectTransform;
            Assert.That(arrow, Is.Not.Null, $"{selectable.name} must keep its arrow child.");
            Assert.That(arrow.rect.width, Is.EqualTo(10.5f).Within(0.1f),
                $"The {selectable.name} arrow must render at 70% width.");
            Assert.That(arrow.rect.height, Is.EqualTo(6.72f).Within(0.1f),
                $"The {selectable.name} arrow must render at 70% height.");
        }

        // Every clickable control and label under the Center Panel must stay inside the panel in
        // this context, measured in the panel's own logical units so a world-space slack cannot
        // mask the reported overhang. The mode/action button set is named explicitly so an empty
        // enumeration cannot pass silently.
        private void AssertTopBarContained(Kind kind)
        {
            var centerPanel = ControlsPanel.transform;
            while (centerPanel != null && centerPanel.name != "Center Panel")
            {
                centerPanel = centerPanel.parent;
            }
            Assert.That(centerPanel, Is.Not.Null, "Center Panel must own the GLS controls.");
            var panel = (RectTransform)centerPanel;
            var panelRect = panel.rect;
            // 0.1 logical units of slack only; the frozen layout had a real 3-unit overhang.

            var names = new HashSet<string>();
            foreach (var selectable in centerPanel.GetComponentsInChildren<Selectable>(false))
            {
                var bounds = BoundsInSpace(panel, (RectTransform)selectable.transform);
                AssertContained(kind, panelRect, bounds, selectable.name);
                names.Add(selectable.name);
            }
            foreach (var expected in new[] { "Delete", "paintProperties", "MirrorUi", "Gameplay", "GLS", "Basic Event" })
            {
                Assert.That(names, Does.Contain(expected),
                    $"{kind}: the {expected} mode button must be part of the measured set.");
            }
            foreach (var text in centerPanel.GetComponentsInChildren<TMP_Text>(false))
            {
                var bounds = BoundsInSpace(panel, (RectTransform)text.transform);
                AssertContained(kind, panelRect, bounds, "label " + text.name);
            }

            static void AssertContained(Kind kind, Rect panelRect, Bounds bounds, string name)
            {
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(panelRect.xMin - 0.1f),
                    $"{kind}: {name} overflows the topbar left edge " +
                    $"({bounds.min.x}..{bounds.max.x} vs panel {panelRect.xMin}..{panelRect.xMax}).");
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(panelRect.xMax + 0.1f),
                    $"{kind}: {name} overflows the topbar right edge " +
                    $"({bounds.min.x}..{bounds.max.x} vs panel {panelRect.xMin}..{panelRect.xMax}).");
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(panelRect.yMin - 0.1f),
                    $"{kind}: {name} overflows the topbar bottom edge " +
                    $"({bounds.min.y}..{bounds.max.y} vs panel {panelRect.yMin}..{panelRect.yMax}).");
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(panelRect.yMax + 0.1f),
                    $"{kind}: {name} overflows the topbar top edge " +
                    $"({bounds.min.y}..{bounds.max.y} vs panel {panelRect.yMin}..{panelRect.yMax}).");
            }

            // The left-packed toolbar must pull the easing column left of its measured position.
            var easingsTab = centerPanel.GetComponentsInChildren<RectTransform>(false)
                .FirstOrDefault(candidate => candidate.name == "GLS Easings Tab");
            Assert.That(easingsTab, Is.Not.Null, "The easing tab must exist under the Center Panel.");
            var tabLeft = BoundsInSpace((RectTransform)centerPanel, easingsTab).min.x;
            var frozenLeft = kind switch
            {
                Kind.Translation => -21f,
                Kind.Color => 37f,
                _ => tabLeft
            };
            Assert.That(tabLeft, Is.LessThanOrEqualTo(frozenLeft),
                $"{kind}: the easing column must sit at or left of its previous position " +
                $"(got {tabLeft}, previously {frozenLeft}).");
        }

        private static Bounds BoundsInSpace(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var bounds = new Bounds(space.InverseTransformPoint(corners[0]), Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                bounds.Encapsulate(space.InverseTransformPoint(corners[i]));
            }
            return bounds;
        }

        // The color EventBox exposes the same easing menu: the shared dropdown writes the exact
        // curve into the inner color queue, and placement serializes it into customData.easing.
        [UnityTest]
        public IEnumerator ColorEasingMenuWritesExactCurve(
            [Values(EaseType.None, EaseType.Linear, EaseType.OutSinusoidal, EaseType.InCubic,
                EaseType.CatmullRom, EaseType.CatmullRomCentripetal,
                EaseType.CatmullRomChordal)] EaseType easing)
        {
            ResetToDirectionalDefaults();
            var group = CreateGroup(Kind.Color);
            yield return OpenPanelFor(group);

            Assert.That(Family.gameObject.activeInHierarchy, Is.True,
                "The color context must expose the family dropdown.");
            Assert.That(Direction.gameObject.activeInHierarchy, Is.True,
                "The color context must expose the direction row.");

            var placement = Object.FindObjectsByType<BasePlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<GLSEventColorPlacement>().Single();
            placement.Initialize(null);
            var queued = Field<BaseGLSEvent>(placement, "QueuedData");

            // Existing color and strobe overrides must survive alongside the new easing key. The
            // strobe easing only serializes while strobe fade is on, so arm it for this fixture.
            ((BaseLightColorBase)queued).ChromaColorEasing = (int)EaseType.InCubic;
            ((BaseLightColorBase)queued).ChromaStrobeEasing = (int)EaseType.OutBounce;
            ((BaseLightColorBase)queued).StrobeFade = 1;
            ((BaseLightColorBase)queued).WriteCustom();

            var family = (int)BeatmapEasingsSelectionInputController.GetEaseFamily(easing);
            var curve = BeatmapEasingsSelectionInputController.GetEaseCurve(easing);
            FamilyControl.value = Array.IndexOf(FamilyIds, family);
            yield return null;
            if (BeatmapEasingsSelectionInputController.HasEaseCurve(easing))
            {
                DirectionControl.value = (int)curve;
                yield return null;
            }

            Assert.That(input.CurrentEasing, Is.EqualTo((int)easing));
            Assert.That(queued.Easing, Is.EqualTo((int)easing),
                "The color queue must take the exact selected curve, not Linear/None.");

            queued.EventBoxGroupData = group;
            queued.EventBoxData = group.ReadOnlyBoxes[0];
            queued.BoxIndex = 0;
            queued.RelativeJsonTime = 4f;
            queued.RecomputeSongBpmTime();
            queued.UsePrevious = 0;
            placement.GetType().GetMethod("HandleApply").Invoke(placement, null);

            var placed = (BaseLightColorBase)FindPlacedGroup(group.ObjectType)
                .ReadOnlyBoxes[0].ReadOnlyEvents.Last();
            Assert.That(placed.Easing, Is.EqualTo((int)easing));
            var json = placed.ToJson();
            Assert.That(json["i"].AsInt,
                Is.EqualTo(easing == EaseType.None ? 0 : 1));
            if (Beatmap.V3.V3LightColorBase.RequiresCustomEasing((int)easing))
            {
                Assert.That(json["customData"]["easing"].AsInt, Is.EqualTo((int)easing));
            }
            else
            {
                Assert.That(json["customData"].HasKey("easing"), Is.False,
                    "None must not leave a stale customData.easing behind.");
            }
            // An Instant node has no interval for colorEasing to drive, so None strips only that key.
            if (easing != EaseType.None)
            {
                Assert.That(json["customData"]["colorEasing"].AsInt, Is.EqualTo((int)EaseType.InCubic),
                    "The explicit colorEasing override must survive.");
            }
            else
            {
                Assert.That(json["customData"].HasKey("colorEasing"), Is.False,
                    "An Instant node cannot keep a colorEasing override.");
            }
            Assert.That(json["customData"]["strobeEasing"].AsInt, Is.EqualTo((int)EaseType.OutBounce),
                "The strobe easing override must survive.");
        }

        // The fade checkbox is a real UI control wired to the shared menu: on writes Linear, off
        // writes None, and the shared dropdown must repaint both ways.
        [UnityTest]
        public IEnumerator ColorFadeToggleSyncsSharedMenu()
        {
            yield return OpenPanelFor(CreateGroup(Kind.Color));
            var colorView = Object.FindAnyObjectByType<GLSInputColorViewController>();
            var fade = Field<ToggleComponent>(colorView, "fadeToggle");

            fade.Value = true;
            yield return null;
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.Linear));
            Assert.That(FamilyControl.captionText.text, Is.EqualTo("Linear"));

            fade.Value = false;
            yield return null;
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.None));
            Assert.That(FamilyControl.captionText.text, Is.EqualTo("None"));
        }

        // Selecting a positive curve must keep the fade checkbox on without losing the chosen id.
        [UnityTest]
        public IEnumerator ColorCurveSelectionKeepsFadeOn()
        {
            yield return OpenPanelFor(CreateGroup(Kind.Color));
            var colorView = Object.FindAnyObjectByType<GLSInputColorViewController>();
            var fade = Field<ToggleComponent>(colorView, "fadeToggle");

            input.NotifyEasingChanged(EaseType.CatmullRomChordal);
            yield return null;
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.CatmullRomChordal));
            Assert.That(fade.Value, Is.True,
                "A positive curve keeps fade enabled instead of bouncing back to a flag.");
            Assert.That(FamilyControl.captionText.text, Is.EqualTo("Catmull-Rom (Chordal)"));
        }

        // Replaying the color view must repeat the shared menu's real easing, not the stale
        // cached fade flag that used to flatten a selected curve back to Linear.
        [UnityTest]
        public IEnumerator ColorRefreshViewsKeepsSelectedCurve()
        {
            yield return OpenPanelFor(CreateGroup(Kind.Color));
            var placement = Object.FindObjectsByType<BasePlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<GLSEventColorPlacement>().Single();
            var queued = Field<BaseGLSEvent>(placement, "QueuedData");

            input.NotifyEasingChanged(EaseType.CatmullRom);
            var colorInput = Object.FindAnyObjectByType<BeatmapGLSEventColorInputController>();
            colorInput.RefreshViews();

            Assert.That(queued.Easing, Is.EqualTo((int)EaseType.CatmullRom),
                "RefreshViews must replay the actual easing, not the cached fade value.");
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.CatmullRom));
        }

        // NotifyFadeChanged(0) must still reach an active queue that currently holds None even
        // when the shared menu already shows Linear and its value callback suppresses a repeat.
        [UnityTest]
        public IEnumerator ColorFadeNotifyUpdatesQueueWhenMenuAlreadyLinear()
        {
            yield return OpenPanelFor(CreateGroup(Kind.Color));
            var placement = Object.FindObjectsByType<BasePlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<GLSEventColorPlacement>().Single();
            var queued = Field<BaseGLSEvent>(placement, "QueuedData");

            input.NotifyEasingChanged(EaseType.Linear);
            queued.Easing = (int)EaseType.None;

            Object.FindAnyObjectByType<BeatmapGLSEventColorInputController>().NotifyFadeChanged(0);
            Assert.That(queued.Easing, Is.EqualTo((int)EaseType.Linear));
        }

        // Editor state keeps the exact easing id so a color queue restored with a curve repaints
        // the shared dropdown and the fade checkbox coherently.
        [UnityTest]
        public IEnumerator ColorEditorStateRestoresCustomCurve()
        {
            yield return OpenPanelFor(CreateGroup(Kind.Color));
            var placement = Object.FindObjectsByType<BasePlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<GLSEventColorPlacement>().Single();
            var colorView = Object.FindAnyObjectByType<GLSInputColorViewController>();
            var fade = Field<ToggleComponent>(colorView, "fadeToggle");

            input.NotifyEasingChanged(EaseType.CatmullRomCentripetal);
            var data = new JSONObject();
            placement.CaptureEditorState(data);
            input.RestoreMenuState((int)EaseType.Linear, 0);
            placement.LoadEditorState(data);

            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.CatmullRomCentripetal),
                "Editor state must restore the exact curve, not the old Linear/None fade flag.");
            Assert.That(FamilyControl.captionText.text, Is.EqualTo("Catmull-Rom (Centripetal)"));
            Assert.That(fade.Value, Is.True);
        }

        private static void AssertRowContains(RectTransform row, RectTransform child)
        {
            var rowBounds = WorldBounds(row);
            rowBounds.Expand(0.05f); // shared row edges need rounding slack
            var childBounds = WorldBounds(child);
            Assert.That(rowBounds.Contains(childBounds.min) && rowBounds.Contains(childBounds.max),
                Is.True,
                $"{child.name} must render inside its {row.name} row " +
                    $"(child {childBounds.min}..{childBounds.max} vs row {rowBounds.min}..{rowBounds.max}).");
        }

        private void AssertClickableFree(RectTransform control, Selectable extension, string context)
        {
            var rect = WorldBounds(control);
            var placementModes = ControlsPanel.transform.parent;
            var neighbors = placementModes.GetComponentsInChildren<Selectable>();
            Assert.That(neighbors, Does.Contain(extension),
                $"{context}: the visible Extension toggle must be measured as a neighbor.");
            var extensionBounds = WorldBounds((RectTransform)extension.transform);
            Assert.That(extensionBounds.size.x, Is.GreaterThan(0f),
                $"{context}: the Extension toggle needs a measured clickable area.");
            foreach (var other in neighbors)
            {
                if (other.transform.IsChildOf(control) || control.IsChildOf(other.transform))
                {
                    continue;
                }
                var otherRect = WorldBounds((RectTransform)other.transform);
                Assert.That(rect.Intersects(otherRect), Is.False,
                    $"{context}: {control.name} overlaps clickable {other.name}.");
            }

            var extensionRect = WorldBounds((RectTransform)extension.transform);
            Assert.That(rect.Intersects(extensionRect), Is.False,
                $"{context}: {control.name} overlaps the visible Extension toggle.");
        }

        private static Bounds WorldBounds(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var bounds = new Bounds(new Vector3(corners[0].x, corners[0].y), Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                bounds.Encapsulate(new Vector3(corners[i].x, corners[i].y));
            }
            return bounds;
        }

        [UnityTest]
        public IEnumerator FamilyDropdownListsEveryFamilyInOrder()
        {
            yield return OpenPanelFor(CreateRotationGroup());
            Assert.That(FamilyControl.options.Count, Is.EqualTo(FamilyIds.Length),
                "Every menu family needs one explicit dropdown option.");
            for (var i = 0; i < FamilyLabels.Length; i++)
            {
                Assert.That(FamilyControl.options[i].text, Is.EqualTo(FamilyLabels[i]),
                    $"Family option {i} must be {FamilyLabels[i]}.");
            }
        }

        [UnityTest]
        public IEnumerator FamilyOptionSelectsExactEasing([NUnit.Framework.Range(0, 14)] int option)
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = option;

            Assert.That(input.CurrentEasing, Is.EqualTo(FamilyIds[option]),
                $"Family option {option} ({FamilyLabels[option]}) must emit easing {FamilyIds[option]}.");
        }

        // Every ordinary family cycles In, Out, and In+Out through the shared row. The dropdown
        // index is a direction, never an easing id.
        [UnityTest]
        public IEnumerator DirectionOptionAppliesCurve(
            [Values(2, 3, 4, 5, 6, 7, 8)] int family,
            [NUnit.Framework.Range(0, 2)] int direction)
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = family;
            DirectionControl.value = direction;

            Assert.That(input.CurrentEasing, Is.EqualTo(FamilyIds[family] + direction));
        }

        // Back, Elastic, and Bounce additionally offer the Beat Saber-specific In+Out variant.
        [UnityTest]
        public IEnumerator DirectionOptionAppliesCurveForAlternativeFamilies(
            [Values(9, 10, 11)] int family,
            [NUnit.Framework.Range(0, 3)] int direction)
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = family;
            DirectionControl.value = direction;

            var expected = direction == 3 ? AlternativeBeatSaberId(family) : FamilyIds[family] + direction;
            Assert.That(input.CurrentEasing, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator DirectionRowDisablesForNondirectionalFamilies(
            [Values(0, 1, 12, 13, 14)] int family)
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());
            Assert.That(Direction.gameObject.activeSelf, Is.True, "Directional defaults must show the row.");
            Assert.That(DirectionEnabled, Is.True, "Directional defaults must enable the row.");

            FamilyControl.value = family;

            Assert.That(Direction.gameObject.activeSelf, Is.True,
                $"{FamilyLabels[family]} keeps the direction row rendered.");
            Assert.That(DirectionEnabled, Is.False,
                $"{FamilyLabels[family]} has no direction; the row must be grayed out.");
            Assert.That(input.CurrentEasing, Is.EqualTo(FamilyIds[family]));
        }

        [UnityTest]
        public IEnumerator BeatSaberOptionOnlyAppearsForAlternativeFamilies([NUnit.Framework.Range(2, 11)] int family)
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = family;

            var expected = Array.IndexOf(AlternativeIndices, family) >= 0 ? 4 : 3;
            Assert.That(DirectionControl.options.Count, Is.EqualTo(expected),
                $"{FamilyLabels[family]} must expose {expected} direction options.");
        }

        [UnityTest]
        public IEnumerator SwitchingDirectionalFamiliesPreservesDirection()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = 4; // Cubic
            DirectionControl.value = (int)EaseCurve.Out;
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.OutCubic));

            FamilyControl.value = 3; // Sine
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.OutSinusoidal));
            Assert.That(DirectionControl.value, Is.EqualTo((int)EaseCurve.Out));
        }

        [UnityTest]
        public IEnumerator BeatSaberDirectionConvertsToInOutOutsideAlternatives()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = 11; // Bounce
            DirectionControl.value = (int)EaseCurve.BeatSaberInOut;
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.BeatSaberInOutBounce));

            FamilyControl.value = 2; // Quadratic has no fourth option
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.InOutQuadratic));
            Assert.That(DirectionControl.value, Is.EqualTo((int)EaseCurve.InOut));
        }

        [UnityTest]
        public IEnumerator BeatSaberDirectionPreservedAcrossAlternativeFamilies()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = 9; // Back
            DirectionControl.value = (int)EaseCurve.BeatSaberInOut;
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.BeatSaberInOutBack));

            FamilyControl.value = 10; // Elastic
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.BeatSaberInOutElastic));
            Assert.That(DirectionControl.options.Count, Is.EqualTo(4));
            Assert.That(DirectionControl.value, Is.EqualTo((int)EaseCurve.BeatSaberInOut));
        }

        // Hidden families must not flush the remembered direction, including the Beat Saber variant.
        [UnityTest]
        public IEnumerator NondirectionalFamiliesPreserveRememberedDirection()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = 4;
            DirectionControl.value = (int)EaseCurve.Out;
            FamilyControl.value = 12; // Catmull-Rom (Uniform)
            Assert.That(DirectionEnabled, Is.False);
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.CatmullRom));

            FamilyControl.value = 5; // Quartic
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.OutQuartic),
                "Returning to a directional family must restore the last direction.");

            FamilyControl.value = 11;
            DirectionControl.value = (int)EaseCurve.BeatSaberInOut;
            FamilyControl.value = 0; // None
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.None));

            FamilyControl.value = 9; // Back
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.BeatSaberInOutBack),
                "The Beat Saber variant survives while the row is hidden.");
        }

        // Hotkeys, shortcuts, and editor-state restores must resync both rows without producing a
        // second easing notification back into the input controller.
        [UnityTest]
        public IEnumerator ExternalEasingChangeSyncsBothDropdowns()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            input.NotifyEasingChanged(EaseType.BeatSaberInOutElastic);
            Assert.That(FamilyControl.value, Is.EqualTo(10));
            Assert.That(DirectionControl.options.Count, Is.EqualTo(4));
            Assert.That(DirectionControl.value, Is.EqualTo((int)EaseCurve.BeatSaberInOut));
            Assert.That(DirectionEnabled, Is.True);

            input.NotifyEasingChanged(EaseType.OutSinusoidal);
            Assert.That(FamilyControl.value, Is.EqualTo(3));
            Assert.That(DirectionControl.options.Count, Is.EqualTo(3));
            Assert.That(DirectionControl.value, Is.EqualTo((int)EaseCurve.Out));

            input.NotifyEasingChanged(EaseType.CatmullRomCentripetal);
            Assert.That(FamilyControl.value, Is.EqualTo(13));
            Assert.That(DirectionEnabled, Is.False);
            Assert.That(input.CurrentExtension, Is.Zero, "An easing change still clears the extension toggle.");
        }

        [UnityTest]
        public IEnumerator ApplyEditorStateSyncsWithoutEmittingEasing()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());
            var notifications = 0;
            input.OnEasingChanged += CountNotification;
            try
            {
                view.ApplyEditorState((int)EaseType.CatmullRomChordal, 1);

                Assert.That(notifications, Is.Zero,
                    "Applying editor state must not feed easing changes back into placements.");
                Assert.That(FamilyControl.value, Is.EqualTo(14));
                Assert.That(DirectionEnabled, Is.False);
                Assert.That(Extension.Value, Is.True);
            }
            finally
            {
                input.OnEasingChanged -= CountNotification;
            }

            void CountNotification(int _) => notifications++;
        }

        [UnityTest]
        public IEnumerator LoadEditorStateRestoresDropdownsAndExtension()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            view.LoadEditorState(JSON.Parse("{\"easing\": 104, \"extension\": 0}"));

            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.CatmullRomCentripetal));
            Assert.That(input.CurrentExtension, Is.Zero);
            Assert.That(FamilyControl.value, Is.EqualTo(13));
            Assert.That(DirectionEnabled, Is.False);
            Assert.That(Extension.Value, Is.False);

            // Restoring Extension makes every active placement rewrite its queue to a fresh extension
            // node, which resets the menu easing to Linear; the dropdowns must follow that result.
            view.LoadEditorState(JSON.Parse("{\"easing\": 104, \"extension\": 1}"));
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.Linear));
            Assert.That(input.CurrentExtension, Is.EqualTo(1));
            Assert.That(FamilyControl.value, Is.EqualTo(1));
            Assert.That(Extension.Value, Is.True);

            // A document missing the keys keeps the controller's current values.
            view.LoadEditorState(JSON.Parse("{}"));
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.Linear));
            Assert.That(input.CurrentExtension, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ExtendedFamiliesCarryWarningTooltip()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());
            var tooltip = Family.GetComponent<Tooltip>();
            Assert.That(tooltip, Is.Not.Null);

            foreach (var family in new[] { 3, 4, 5, 6, 7 })
            {
                FamilyControl.value = family;
                Assert.That(tooltip.enabled, Is.True,
                    $"{FamilyLabels[family]} needs the localized ChromaGLS requirement warning.");
            }

            foreach (var family in new[] { 12, 13, 14 })
            {
                FamilyControl.value = family;
                Assert.That(tooltip.enabled, Is.True,
                    $"{FamilyLabels[family]} needs the Catmull-Rom compatibility warning.");
            }

            FamilyControl.value = 2; // Quadratic
            Assert.That(tooltip.enabled, Is.False, "Native families must not display a warning.");
        }

        [UnityTest]
        public IEnumerator WarningTooltipFollowsExternalChanges()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());
            var tooltip = Family.GetComponent<Tooltip>();

            input.NotifyEasingChanged(EaseType.CatmullRom);
            Assert.That(tooltip.enabled, Is.True, "Restored Catmull-Rom selections keep the warning.");

            input.NotifyEasingChanged(EaseType.InQuadratic);
            Assert.That(tooltip.enabled, Is.False);
        }

        // Direction cycling and family mapping must never mutate the spline ids. SetEaseCurve is the
        // shared seam every caller funnels through.
        [Test]
        public void SetEaseCurveLeavesNondirectionalIdsUnchanged(
            [Values(EaseType.None, EaseType.Linear, EaseType.CatmullRom,
                EaseType.CatmullRomCentripetal, EaseType.CatmullRomChordal)] EaseType easing,
            [Values] EaseCurve curve)
        {
            Assert.That(BeatmapEasingsSelectionInputController.SetEaseCurve(easing, curve),
                Is.EqualTo(easing));
        }

        // HasEaseCurve is the shared seam for the curve row and both curve shortcuts; it is only
        // true for the recognized directional families and the three Beat Saber In+Out ids.
        [Test]
        public void HasEaseCurveMatchesOnlyDirectionalIds()
        {
            foreach (EaseType easing in Enum.GetValues(typeof(EaseType)))
            {
                var directional = (int)easing is >= (int)EaseType.InQuadratic and <= (int)EaseType.InOutBounce
                    or >= (int)EaseType.BeatSaberInOutBack and <= (int)EaseType.BeatSaberInOutBounce;
                Assert.That(BeatmapEasingsSelectionInputController.HasEaseCurve(easing), Is.EqualTo(directional),
                    $"{easing} reported the wrong direction support.");
            }
        }

        // The curve-cycling shortcut previously walked Catmull-Rom into unrelated enum values
        // through plain arithmetic; with no direction it must do nothing.
        [UnityTest]
        public IEnumerator CurveShortcutSkipsCatmullRom()
        {
            var selected = PerformCurveCycle(EaseType.CatmullRomChordal);
            Assert.That(selected, Is.Null,
                "Cycling the direction on a Catmull-Rom selection must not emit an easing.");
            yield break;
        }

        [UnityTest]
        public IEnumerator CurveShortcutSkipsHoveredCatmullRom()
        {
            var fixture = new IsolatedInputTestFixture();
            fixture.Setup();
            var controllerObject = new GameObject("Curve hover test controller");
            var controller = controllerObject.AddComponent<BeatmapEasingsSelectionInputController>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var cycleAction = new InputAction(
                "Cycle hovered easing curve", InputActionType.Button, "<Keyboard>/e");
            var hovered = new GameObject("Hovered Catmull-Rom event").AddComponent<GLSEventContainer>();
            // Unspawned containers still unsubscribe their serialized settings asset during OnDestroy,
            // and a fresh ScriptableObject cannot satisfy that (VisualSettingsSO.OnEnable needs its
            // repository), so reuse the already-loaded project asset.
            hovered.VisualSettings = Resources.FindObjectsOfTypeAll<VisualSettingsSO>()[0];
            hovered.EventData = new BaseLightRotationBase { Easing = (int)EaseType.CatmullRomCentripetal };
            try
            {
                controller.IsHovering = true;
                controller.HoveredObject = hovered;
                cycleAction.performed += controller.OnEasingCurveHover;
                cycleAction.Enable();

                fixture.Press(keyboard.eKey, queueEventOnly: true);
                InputSystem.Update();

                Assert.That(hovered.EventData.Easing, Is.EqualTo((int)EaseType.CatmullRomCentripetal),
                    "Hover cycling must leave Catmull-Rom events untouched.");
            }
            finally
            {
                cycleAction.Disable();
                cycleAction.Dispose();
                Object.DestroyImmediate(hovered.gameObject);
                Object.DestroyImmediate(controllerObject);
                fixture.TearDown();
            }

            yield break;
        }

        // Real inner placements consume the shared easing notifications, so a dropdown selection must
        // reach the committed event for every easing-capable EventBox lane.
        [UnityTest]
        public IEnumerator PlacementCommitsDropdownCatmullRom(
            [Values(Kind.Rotation, Kind.Translation, Kind.FloatFX)] Kind kind,
            [Values(EaseType.CatmullRom, EaseType.CatmullRomCentripetal, EaseType.CatmullRomChordal)] EaseType easing)
        {
            ResetToDirectionalDefaults();
            var group = CreateGroup(kind);
            yield return OpenPanelFor(group);

            FamilyControl.value = Array.IndexOf(FamilyIds, (int)easing);
            Assert.That(input.CurrentEasing, Is.EqualTo((int)easing));

            var placement = Array.Find(
                Object.FindObjectsByType<BasePlacement>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None),
                candidate => candidate.GetType() == InnerPlacementType(kind));
            Assert.That(placement, Is.Not.Null, $"{kind} inner placement must exist in the scene.");
            placement.Initialize(null);
            var queued = Field<BaseGLSEvent>(placement, "QueuedData");
            queued.EventBoxGroupData = group;
            queued.EventBoxData = group.ReadOnlyBoxes[0];
            queued.BoxIndex = 0;
            queued.RelativeJsonTime = 4f;
            queued.RecomputeSongBpmTime();
            queued.UsePrevious = 0;

            Assert.That(queued.Easing, Is.EqualTo((int)easing),
                "The placement queue must follow the dropdown easing.");

            placement.GetType().GetMethod("HandleApply").Invoke(placement, null);

            var placed = FindPlacedGroup(group.ObjectType);
            Assert.That(placed.ReadOnlyBoxes[0].ReadOnlyEvents[^1].Easing, Is.EqualTo((int)easing),
                $"{kind} must commit the dropdown-selected {easing} id.");
        }

        // The easing column belongs to every EventBox context including color; other editing
        // modes hide the column entirely.
        [UnityTest]
        public IEnumerator EasingTabVisibilityFollowsGroupContext()
        {
            editModeContext.EditingMode = EditingMode.EventBox;
            Assert.That(ControlsPanel.activeInHierarchy, Is.False, "No group context keeps the easing column hidden.");

            var rotation = CreateGroup(Kind.Rotation);
            BeatmapObjectContainerCollection.GetCollectionForType(rotation.ObjectType).SpawnObject(rotation, out _);
            innerProvider.GroupContext = rotation;
            Assert.That(ControlsPanel.activeInHierarchy, Is.True, "Rotation groups expose the easing column.");
            Assert.That(Family.gameObject.activeSelf, Is.True, "Rotation groups expose the family row.");
            Assert.That(Direction.gameObject.activeSelf, Is.True, "Rotation groups expose the direction row.");
            Assert.That(Extension.gameObject.activeSelf, Is.True, "Rotation groups expose the extension row.");

            var color = CreateGroup(Kind.Color);
            BeatmapObjectContainerCollection.GetCollectionForType(color.ObjectType).SpawnObject(color, out _);
            innerProvider.GroupContext = color;
            Assert.That(ControlsPanel.activeInHierarchy, Is.True,
                "Color groups expose the easing column.");
            Assert.That(Family.gameObject.activeSelf, Is.True, "Color groups expose the family row.");
            Assert.That(Direction.gameObject.activeSelf, Is.True,
                "Color groups expose the direction row.");
            Assert.That(Extension.gameObject.activeSelf, Is.True,
                "The labeled extension row stays available for color groups.");
            Assert.That(Extension.GetComponentsInChildren<TMP_Text>(), Is.Not.Empty,
                "The color-context extension row keeps its label.");

            innerProvider.GroupContext = null;
            editModeContext.EditingMode = EditingMode.GLS;
            Assert.That(ControlsPanel.activeInHierarchy, Is.False,
                "Non-EventBox modes hide the whole easing column.");
            yield break;
        }

        // A disabled row still fires its value callback when the option list is rewritten or a
        // programmatic value lands; the guard must keep stale callbacks from mutating easing or
        // clearing the extension state.
        [UnityTest]
        public IEnumerator DisabledDirectionCallbackCannotMutateState()
        {
            ResetToDirectionalDefaults();
            yield return OpenPanelFor(CreateRotationGroup());

            FamilyControl.value = 12; // Catmull-Rom (Uniform)
            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.CatmullRom));
            Assert.That(DirectionEnabled, Is.False);

            DirectionControl.value = (int)EaseCurve.InOut;

            Assert.That(input.CurrentEasing, Is.EqualTo((int)EaseType.CatmullRom),
                "A disabled direction row must not rewrite the easing.");
            Assert.That(input.CurrentExtension, Is.Zero,
                "A disabled direction row must not clear the extension state.");

            // Enabling the extension rewrites the queue and resets the menu easing through the
            // placement callbacks, so compare against whatever the controller lands on.
            input.NotifyExtensionChanged(1);
            Assert.That(input.CurrentExtension, Is.EqualTo(1));
            Assert.That(DirectionEnabled, Is.False);
            var settled = input.CurrentEasing;

            DirectionControl.value = (int)EaseCurve.Out;

            Assert.That(input.CurrentEasing, Is.EqualTo(settled),
                "A disabled direction row must not rewrite the easing.");
            Assert.That(input.CurrentExtension, Is.EqualTo(1),
                "A disabled direction row must not clear the extension state.");
        }

        private static BaseEventBoxGroup FindPlacedGroup(ObjectType type)
        {
            BaseEventBoxGroup result = null;
            BeatmapObjectContainerCollection.GetCollectionForType(type).ForEachObjectBetweenSongBpmTime(
                0f,
                (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(16f),
                (_, obj) =>
                {
                    if (obj is BaseEventBoxGroup group && group.ID == 0)
                        result = group;
                });
            Assert.That(result, Is.Not.Null);
            return result;
        }

        // A dedicated virtual keyboard drives production callbacks while the fixture owns the isolated Input System runtime.
        private static EaseType? PerformCurveCycle(EaseType start)
        {
            var fixture = new IsolatedInputTestFixture();
            fixture.Setup();
            var controllerObject = new GameObject("Catmull-Rom curve cycle test controller");
            var controller = controllerObject.AddComponent<BeatmapEasingsSelectionInputController>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var cycleAction = new InputAction(
                "Cycle easing curve", InputActionType.Button, "<Keyboard>/e");
            EaseType? selected = null;
            try
            {
                controller.NotifyEasingChanged(start);
                controller.OnEasingChanged += value => selected = (EaseType)value;
                cycleAction.performed += controller.OnEasingCurve;
                cycleAction.Enable();

                fixture.Press(keyboard.eKey, queueEventOnly: true);
                InputSystem.Update();

                return selected;
            }
            finally
            {
                cycleAction.Disable();
                cycleAction.Dispose();
                Object.DestroyImmediate(controllerObject);
                fixture.TearDown();
            }
        }
    }
}
