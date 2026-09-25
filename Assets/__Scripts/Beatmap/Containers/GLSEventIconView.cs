using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSEventIconView : MonoBehaviour
    {
        // avoid z-fighting.
        public const float SurfaceOffset = 0.505f;
        public const float StateIconHorizontalPosition = 0.25f;
        public const float EasingIconWidth = 0.3168f * 1.15f;
        public const float CircularEasingIconWidth = 0.22f * 1.15f;
        public const float EasingIconHeight = 0.198f * 1.15f;
        public const float PreviousRotationDirectionIconSize = 0.264f;
        public const float RotationDirectionIconSize = PreviousRotationDirectionIconSize * 1.3f;
        public const float ColorIconScale = 0.7f;

        private const float DefaultIconSize = 0.22f;
        private const float DefaultIconHeight = 0.29f;
        private const float TextSurfaceOffset = 0.506f;

        public const float HoverLabelHorizontalPosition = 0.9f;
        private const float HoverLabelWidth = 3f;
        private const float HoverLabelHeight = 1.5f;
        private const string HoverLineHeightTag = "<line-height=70%>";
        private const string HoverTitleSizeTag = "<size=50%>";
        private const string HoverAbbreviationSizeTag = "<size=70%>";
        private TextMeshPro[] fadeHoverDisplays;
        private TextMeshPro[] strobeHoverDisplays;
        private TextMeshPro[] strobeColorHoverDisplays;
        private BaseLightColorBase currentColorEvent;
        private bool colorHoverVisible;
        private Transform previewVisualParent;

        // Only the authored set is serialized per node: the dormant parallel NoOutline array doubled every
        // pooled node's sprite references for an outline-less trigger setting that does not exist. If one
        // lands, resolve it through a shared lookup rather than a second serialized array.
        [SerializeField] private Sprite[] icons;

        [SerializeField] private SpriteRenderer[] primaryRenderers;
        [SerializeField] private SpriteRenderer[] secondaryRenderers;
        [SerializeField] private SpriteRenderer[] tertiaryRenderers;

        public void SetPreviewVisualParent(Transform parent) => previewVisualParent = parent;

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

            ApplyLayout(evt, valueDisplays);
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

        // Cloning happens once per pooled view on its first valid color hover
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
                var display = Instantiate(
                    valueDisplays[i],
                    previewVisualParent != null ? previewVisualParent : transform,
                    false);
                display.name = $"{name} Hover {(i == 0 ? "Top" : "Side")}";
                display.enabled = true;
                display.rectTransform.sizeDelta = new Vector2(HoverLabelWidth, HoverLabelHeight);
                display.textWrappingMode = TextWrappingModes.NoWrap;
                display.overflowMode = TextOverflowModes.Overflow;
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

        private void RefreshColorHoverLabels()
        {
            var valid = colorHoverVisible && currentColorEvent != null && currentColorEvent.UsePrevious == 0;
            if (!valid)
            {
                DeactivateColorHoverPairs();
                return;
            }

            var isHsv = currentColorEvent.CustomLerpType == BasicEventColorLerpType.TrueHSV;
            var fadeEasing = currentColorEvent.Easing == (int)EaseType.None
                ? (int)EaseType.None
                : currentColorEvent.ChromaColorEasing ?? currentColorEvent.Easing;
            SetHoverPair(fadeHoverDisplays, "Fade Ease", fadeEasing, true, isHsv, true);
            var strobeEasing = currentColorEvent.StrobeFade == 1
                ? currentColorEvent.ChromaStrobeEasing ?? (int)EaseType.InOutCubic
                : (int)EaseType.None;
            SetHoverPair(strobeHoverDisplays, "Strobe Ease", strobeEasing, GLSEventCommon.IsStrobing(currentColorEvent), isHsv, false);
            SetHoverPair(
                strobeColorHoverDisplays,
                "Strobe Color Ease",
                currentColorEvent.ChromaStrobeColorEasing ?? (int)EaseType.None,
                currentColorEvent.ChromaStrobeColorEasing.HasValue,
                isHsv,
                true);
        }

        private static void SetHoverPair(
            TextMeshPro[] pair,
            string title,
            int easing,
            bool active,
            bool hsv,
            bool hsvTagOnLeft)
        {
            if (pair == null)
            {
                return;
            }

            var abbreviation = GetHoverAbbreviation(easing);
            if (hsv)
            {
                abbreviation = hsvTagOnLeft ? $"HSV {abbreviation}" : $"{abbreviation} HSV";
            }

            var text = $"{HoverLineHeightTag}{HoverTitleSizeTag}{title}</size>\n{HoverAbbreviationSizeTag}{abbreviation}</size></line-height>";
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

        private static string GetHoverAbbreviation(int easing)
        {
            var abbreviation = Easing.IDToShortName.GetValueOrDefault(easing);
            return easing is (int)EaseType.InQuadratic
                or (int)EaseType.OutQuadratic
                or (int)EaseType.InOutQuadratic
                ? $"{abbreviation} (Qd)"  // Reduce confusion for mappers coming from OE. Because we support all the other exponentials, we go with a common ^# format, but OE has ^2=Qd.
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
            SetFacePositions(
                tertiaryRenderers,
                -StateIconHorizontalPosition,
                GLSEventCommon.ColorTertiaryIconHeight);

            var textVerticalOffset = evt is BaseLightRotationBase or BaseLightTranslationBase or BaseFxEventFloat
                ? GLSEventCommon.TransformTextVerticalOffset
                : evt is BaseLightColorBase
                    ? GLSEventCommon.ColorFaceVerticalOffset
                    : 0f;
            valueDisplays[0].rectTransform.localPosition = new Vector3(0f, TextSurfaceOffset, textVerticalOffset);
            valueDisplays[1].rectTransform.localPosition = new Vector3(0f, textVerticalOffset, -TextSurfaceOffset);
        }

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

        // Each icon enum maps directly to one serialized atlas-packed sprite. None disables its preallocated renderer pair
        private void SetIcon(SpriteRenderer[] renderers, GLSEventIconType iconType, float iconScale)
        {
            var enabled = iconType != GLSEventIconType.None;
            var sprite = enabled
                ? icons[(int)iconType - 1]
                : null;
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

        private static bool IsEasing(GLSEventIconType iconType)
        {
            var value = (int)iconType;
            return (value >= (int)GLSEventIconType.EaseLinear
                    && value <= (int)GLSEventIconType.EaseBeatSaberInOutBounce)
                || iconType == GLSEventIconType.NoEasingStep;
        }

        private static bool IsRotationDirection(GLSEventIconType iconType)
        {
            return iconType is GLSEventIconType.RotationAutomatic
                or GLSEventIconType.RotationClockwise
                or GLSEventIconType.RotationCounterClockwise;
        }
    }
}
