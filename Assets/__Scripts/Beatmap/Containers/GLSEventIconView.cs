using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSEventIconView : MonoBehaviour
    {
        // TransformNodeLayoutMatchesOe leaves a five-thousandths physical clearance to avoid z-fighting without visible hovering.
        public const float SurfaceOffset = 0.505f;
        public const float StateIconHorizontalPosition = 0.25f;
        // TransformNodeLayoutMatchesOe scales easing art 15% around its center and direction art 30% around its fixed top edge.
        // EasingIconsBakeHorizontalStretchIntoArtwork keeps these as the nominal displayed footprint the generator
        // targets; the renderer scale itself is now uniform because each glyph's texture carries the aspect, so
        // stroke thickness can no longer be stretched ~60% fatter horizontally than vertically.
        public const float EasingIconWidth = 0.3168f * 1.15f;
        public const float CircularEasingIconWidth = 0.22f * 1.15f;
        public const float EasingIconHeight = 0.198f * 1.15f;
        public const float PreviousRotationDirectionIconSize = 0.264f;
        public const float RotationDirectionIconSize = PreviousRotationDirectionIconSize * 1.3f;
        // ColorNodeTwoColumnLayout fits three icon slots on one face, so color markers render below the transform size.
        public const float ColorIconScale = 0.7f;

        private const float DefaultIconSize = 0.22f;
        private const float DefaultIconHeight = 0.29f;
        // TransformNodeLayoutMatchesOe keeps text one additional thousandth forward so it remains clear of both node and icon planes.
        private const float TextSurfaceOffset = 0.506f;

        // ColorHoverLabelsExplainEasingsOutsideNode centers a three-unit rect one unit out so its aligned inner edge reaches the half-node face edge.
        public const float HoverLabelHorizontalPosition = 0.9f;
        private const float HoverLabelWidth = 3f;
        private const float HoverLabelHeight = 1.5f;
        // CompressedHoverEasingText halves the title, reduces the abbreviation thirty percent, and collapses the label gap to about a tenth node.
        private const string HoverLineHeightTag = "<line-height=70%>";
        private const string HoverTitleSizeTag = "<size=50%>";
        private const string HoverAbbreviationSizeTag = "<size=70%>";
        private TextMeshPro[] fadeHoverDisplays;
        private TextMeshPro[] strobeHoverDisplays;
        private TextMeshPro[] strobeColorHoverDisplays;
        private BaseLightColorBase currentColorEvent;
        private bool colorHoverVisible;

        // PrefabWiresBothFacesToTheSharedSpriteAtlas keeps artwork replaceable while serialized renderers share one alpha-blended material.
        [SerializeField] private Sprite[] icons;
        // OutlineLessGlyphSetReadyForSettingSwap: parallel generated set wired index-for-index with icons;
        // dormant until the real outline-less trigger setting is identified.
        [SerializeField] private Sprite[] noOutlineIcons;

        // ThinnerBorderSameGlyphFootprint ends the borderless evaluation: the authored outlined set is back now
        // that Generate-GlsEasingIcons emits a thinner black ring; the no-outline set stays wired for the real
        // outline-less setting when one is identified.
        private const bool UseOutlineLessGlyphSet = false;

        // OE duplicates its marker sprites across the top and side faces; paired arrays preserve that behavior with fixed pooled objects.
        [SerializeField] private SpriteRenderer[] primaryRenderers;
        [SerializeField] private SpriteRenderer[] secondaryRenderers;
        // ColorNodeTwoColumnLayout needs a third prefab-wired face pair for the bottom-left strobeColorEasing icon.
        [SerializeField] private SpriteRenderer[] tertiaryRenderers;

        // Sprite assignment changes only atlas UVs, while fixed pooled transforms avoid mesh, material, and object recreation.
        public void SetIcons(GLSEventIconState state, BaseGLSEvent evt, TextMeshPro[] valueDisplays)
        {
            // ColorHoverLabelsExplainEasingsOutsideNode retains the represented color event so stationary-cursor easing edits refresh hover text.
            currentColorEvent = evt as BaseLightColorBase;
            // A pooled rebind cannot retain hover ownership after becoming non-color or inherited; retiring the flag prevents a later off-cursor color reuse from reopening stale labels.
            if (currentColorEvent == null || currentColorEvent.UsePrevious == 1)
            {
                colorHoverVisible = false;
                DeactivateColorHoverPairs();
            }

            // Pass the container-owned text pair into layout so the hot path uses its established dependency directly.
            ApplyLayout(evt, valueDisplays);
            // ColorNodeTwoColumnLayout shrinks every color marker so three slots fit where two did before.
            var iconScale = evt is BaseLightColorBase ? ColorIconScale : 1f;
            SetIcon(primaryRenderers, state.Primary, iconScale);
            SetIcon(secondaryRenderers, state.Secondary, iconScale);
            SetIcon(tertiaryRenderers, state.Tertiary, iconScale);
            // A pooled node rebound under a stationary cursor must refresh or retire its existing hover labels.
            if (colorHoverVisible)
            {
                RefreshColorHoverLabels();
            }
        }

        // ColorHoverLabelsExplainEasingsOutsideNode gates labels on a valid color node so ribbon hover, inherited
        // nodes, and non-color nodes never allocate clones; unchanged per-frame calls return before rebuilding strings or touching SetText.
        public void SetColorHover(bool visible, TextMeshPro[] valueDisplays)
        {
            visible = visible && currentColorEvent != null && currentColorEvent.UsePrevious == 0;
            if (colorHoverVisible == visible)
            {
                return;
            }

            colorHoverVisible = visible;
            if (visible)
            {
                EnsureColorHoverDisplays(valueDisplays);
                RefreshColorHoverLabels();
            }
            else
            {
                DeactivateColorHoverPairs();
            }
        }

        // Cloning happens once per pooled view on its first valid color hover; these two requested nudges move
        // only the Fade/Strobe Color label centers while their icons remain anchored to the shared layout.
        private void EnsureColorHoverDisplays(TextMeshPro[] valueDisplays)
        {
            fadeHoverDisplays ??= CreateHoverDisplayPair(
                valueDisplays,
                "Fade Ease",
                -HoverLabelHorizontalPosition,
                GLSEventCommon.ColorEasingIconHeight + (1f / 20f));
            strobeHoverDisplays ??= CreateHoverDisplayPair(
                valueDisplays,
                "Strobe Ease",
                HoverLabelHorizontalPosition,
                GLSEventCommon.ColorStrobeIconHeight);
            strobeColorHoverDisplays ??= CreateHoverDisplayPair(
                valueDisplays,
                "Strobe Color Ease",
                -HoverLabelHorizontalPosition,
                GLSEventCommon.ColorTertiaryIconHeight - (1f / 15f));
        }

        // Each label pair shares its icon's plane and height while the three-unit rect puts its aligned inner edge at the half-node face edge.
        private TextMeshPro[] CreateHoverDisplayPair(
            TextMeshPro[] valueDisplays,
            string name,
            float horizontalPosition,
            float iconHeight)
        {
            var pair = new TextMeshPro[2];
            for (var i = 0; i < pair.Length; i++)
            {
                var display = Instantiate(valueDisplays[i], transform, false);
                display.name = $"{name} Hover {(i == 0 ? "Top" : "Side")}";
                display.enabled = true;
                display.rectTransform.sizeDelta = new Vector2(HoverLabelWidth, HoverLabelHeight);
                display.textWrappingMode = TextWrappingModes.NoWrap;
                display.overflowMode = TextOverflowModes.Overflow;
                // CompressedHoverEasingText keeps each label's inner edge beside the node instead of centered far outside it.
                display.alignment = horizontalPosition < 0f
                    ? TextAlignmentOptions.Right
                    : TextAlignmentOptions.Left;
                display.rectTransform.localPosition = i == 0
                    ? new Vector3(horizontalPosition, TextSurfaceOffset, iconHeight)
                    : new Vector3(horizontalPosition, iconHeight, -TextSurfaceOffset);
                display.gameObject.SetActive(false);
                pair[i] = display;
            }

            return pair;
        }

        // Refresh mirrors GLSEventIconResolver's slot semantics so a label never disagrees with its icon.
        private void RefreshColorHoverLabels()
        {
            var valid = colorHoverVisible && currentColorEvent != null && currentColorEvent.UsePrevious == 0;
            if (!valid)
            {
                DeactivateColorHoverPairs();
                return;
            }

            var fadeEasing = currentColorEvent.Easing == (int)EaseType.None
                ? (int)EaseType.None
                : currentColorEvent.ChromaColorEasing ?? currentColorEvent.Easing;
            SetHoverPair(fadeHoverDisplays, "Fade Ease", fadeEasing, true);
            var strobeEasing = currentColorEvent.StrobeFade == 1
                ? currentColorEvent.ChromaStrobeEasing ?? (int)EaseType.InOutCubic
                : (int)EaseType.None;
            SetHoverPair(strobeHoverDisplays, "Strobe Ease", strobeEasing, GLSEventCommon.IsStrobing(currentColorEvent));
            SetHoverPair(
                strobeColorHoverDisplays,
                "Strobe Color Ease",
                currentColorEvent.ChromaStrobeColorEasing ?? (int)EaseType.None,
                currentColorEvent.ChromaStrobeColorEasing.HasValue);
        }

        private static void SetHoverPair(TextMeshPro[] pair, string title, int easing, bool active)
        {
            if (pair == null)
            {
                return;
            }

            // CompressedHoverEasingText keeps the title and abbreviation visually paired instead of vertically overlapping adjacent labels.
            var text = $"{HoverLineHeightTag}{HoverTitleSizeTag}{title}</size>\n{HoverAbbreviationSizeTag}{GetHoverAbbreviation(easing)}</size></line-height>";
            for (var i = 0; i < pair.Length; i++)
            {
                if (pair[i] == null)
                {
                    continue;
                }

                if (active)
                {
                    pair[i].SetText(text);
                }

                pair[i].gameObject.SetActive(active);
            }
        }

        // QuadraticHoverLabelsNameOeFamily marks only the color node's outside labels so other GLS displays retain their compact ^2 abbreviation.
        private static string GetHoverAbbreviation(int easing)
        {
            var abbreviation = Easing.IDToShortName.GetValueOrDefault(easing);
            return easing is (int)EaseType.InQuadratic
                or (int)EaseType.OutQuadratic
                or (int)EaseType.InOutQuadratic
                ? $"{abbreviation} (Qd)"
                : abbreviation;
        }

        private void DeactivateColorHoverPairs()
        {
            DeactivateHoverPair(fadeHoverDisplays);
            DeactivateHoverPair(strobeHoverDisplays);
            DeactivateHoverPair(strobeColorHoverDisplays);
        }

        private static void DeactivateHoverPair(TextMeshPro[] pair)
        {
            if (pair == null)
            {
                return;
            }

            for (var i = 0; i < pair.Length; i++)
            {
                if (pair[i] != null)
                {
                    pair[i].gameObject.SetActive(false);
                }
            }
        }

        // TransformNodeLayoutMatchesOe mirrors rotation icon centers from the same single offset used to derive the TMP columns.
        private void ApplyLayout(BaseGLSEvent evt, TextMeshPro[] valueDisplays)
        {
            var primaryX = evt is BaseLightRotationBase
                ? GLSEventCommon.RotationColumnHorizontalOffset
                : evt is BaseLightTranslationBase or BaseFxEventFloat
                    ? 0f
                    : -StateIconHorizontalPosition;
            var secondaryX = evt is BaseLightRotationBase
                ? -GLSEventCommon.RotationColumnHorizontalOffset
                : StateIconHorizontalPosition;

            // TransformNodeLayoutMatchesOe routes every transform easing through one shared height so node types cannot drift apart.
            var primaryIconHeight = evt is BaseLightRotationBase or BaseLightTranslationBase or BaseFxEventFloat
                ? GLSEventCommon.TransformEasingIconHeight
                : evt is BaseLightColorBase
                    ? GLSEventCommon.ColorEasingIconHeight
                    : DefaultIconHeight;
            var secondaryIconHeight = evt is BaseLightRotationBase
                ? GLSEventCommon.RotationDirectionIconHeight
                : evt is BaseLightColorBase
                    ? GLSEventCommon.ColorStrobeIconHeight
                    : DefaultIconHeight;
            SetFacePositions(primaryRenderers, primaryX, primaryIconHeight);
            SetFacePositions(secondaryRenderers, secondaryX, secondaryIconHeight);
            // ColorNodeTwoColumnLayout parks the pooled bottom-left pair on non-color nodes where it stays disabled.
            SetFacePositions(
                tertiaryRenderers,
                -StateIconHorizontalPosition,
                GLSEventCommon.ColorTertiaryIconHeight);

            // ColorNodeLayoutUsesRequestedVerticalOffsets moves both color TMP faces with the shared corrected baseline before per-row voffsets apply, while transform and fallback layouts retain their own baselines.
            var textVerticalOffset = evt is BaseLightRotationBase or BaseLightTranslationBase or BaseFxEventFloat
                ? GLSEventCommon.TransformTextVerticalOffset
                : evt is BaseLightColorBase
                    ? GLSEventCommon.ColorFaceVerticalOffset
                    : 0f;
            // The top and side TMP objects use different face axes; both retain the physical depth offset that prevents surface clipping.
            valueDisplays[0].rectTransform.localPosition = new Vector3(0f, TextSurfaceOffset, textVerticalOffset);
            valueDisplays[1].rectTransform.localPosition = new Vector3(0f, textVerticalOffset, -TextSurfaceOffset);
        }

        // Renderer order is fixed as top then side, matching the prefab arrays and keeping both faces driven by one layout constant.
        private static void SetFacePositions(
            SpriteRenderer[] renderers,
            float horizontalPosition,
            float iconHeight)
        {
            renderers[0].transform.localPosition = new Vector3(
                horizontalPosition,
                SurfaceOffset,
                iconHeight);
            renderers[1].transform.localPosition = new Vector3(
                horizontalPosition,
                iconHeight,
                -SurfaceOffset);
        }

        // Each icon enum maps directly to one serialized atlas-packed sprite; None disables its preallocated renderer pair.
        private void SetIcon(SpriteRenderer[] renderers, GLSEventIconType iconType, float iconScale)
        {
            var enabled = iconType != GLSEventIconType.None;
            var sprite = enabled
                ? SelectSprite(iconType)
                : null;
            // EasingIconsBakeHorizontalStretchIntoArtwork applies one uniform scale because the generated glyph
            // texture already carries the display aspect; CircularEasingsKeepOriginalWidth keeps its narrower
            // footprint through a narrower canvas, and RotationDirectionIconsAreOutlinedAndLarger keeps its square scale.
            var scale = iconScale * (IsEasing(iconType)
                ? new Vector3(EasingIconHeight, EasingIconHeight, DefaultIconSize)
                : IsRotationDirection(iconType)
                    ? Vector3.one * RotationDirectionIconSize
                    : Vector3.one * DefaultIconSize);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sprite = sprite;
                renderers[i].enabled = enabled;
                renderers[i].transform.localScale = scale;
            }
        }

        // OutlineLessGlyphSetReadyForSettingSwap: the mechanism stays wired but gated off; flipping
        // UseOutlineLessGlyphSet to the real setting makes only generated easing glyphs swap, while rotation,
        // instant, and transition markers keep their authored sprite under either theme.
        private Sprite SelectSprite(GLSEventIconType iconType)
        {
            var set = UseOutlineLessGlyphSet
                && noOutlineIcons != null
                && noOutlineIcons.Length == icons.Length
                && IsEasing(iconType)
                ? noOutlineIcons
                : icons;
            return set[(int)iconType - 1];
        }

        // Only generated easing glyphs share the pre-stretched artwork and the outline-less variant set; direction and state icons retain OE proportions.
        private static bool IsEasing(GLSEventIconType iconType)
        {
            var value = (int)iconType;
            // NoEasingStepMarkerIsAGeneratedRightAngle shares the curve family canvas, so it takes the same
            // wide aspect rather than the replaced OE block's square footprint.
            return (value >= (int)GLSEventIconType.EaseLinear
                    && value <= (int)GLSEventIconType.EaseBeatSaberInOutBounce)
                || iconType == GLSEventIconType.NoEasingStep;
        }

        // RotationDirectionIconsAreOutlinedAndLarger scopes the larger square scale to Auto/CW/CCW without affecting color-state sprites.
        private static bool IsRotationDirection(GLSEventIconType iconType)
        {
            return iconType is GLSEventIconType.RotationAutomatic
                or GLSEventIconType.RotationClockwise
                or GLSEventIconType.RotationCounterClockwise;
        }
    }
}
