using Beatmap.Base;
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

        // PrefabWiresBothFacesToTheSharedSpriteAtlas keeps artwork replaceable while serialized renderers share one alpha-blended material.
        [SerializeField] private Sprite[] icons;

        // OE duplicates its marker sprites across the top and side faces; paired arrays preserve that behavior with fixed pooled objects.
        [SerializeField] private SpriteRenderer[] primaryRenderers;
        [SerializeField] private SpriteRenderer[] secondaryRenderers;
        // ColorNodeTwoColumnLayout needs a third prefab-wired face pair for the bottom-left strobeColorEasing icon.
        [SerializeField] private SpriteRenderer[] tertiaryRenderers;

        // Sprite assignment changes only atlas UVs, while fixed pooled transforms avoid mesh, material, and object recreation.
        public void SetIcons(GLSEventIconState state, BaseGLSEvent evt, TextMeshPro[] valueDisplays)
        {
            // Pass the container-owned text pair into layout so the hot path uses its established dependency directly.
            ApplyLayout(evt, valueDisplays);
            // ColorNodeTwoColumnLayout shrinks every color marker so three slots fit where two did before.
            var iconScale = evt is BaseLightColorBase ? ColorIconScale : 1f;
            SetIcon(primaryRenderers, state.Primary, iconScale);
            SetIcon(secondaryRenderers, state.Secondary, iconScale);
            SetIcon(tertiaryRenderers, state.Tertiary, iconScale);
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

            // ColorNodeLayoutUsesRequestedVerticalOffsets moves both color TMP faces down one-twelfth before per-row voffsets apply, while transform and fallback layouts retain their own baselines.
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
                ? icons[(int)iconType - 1]
                : null;
            // CircularEasingsKeepOriginalWidth and RotationDirectionIconsAreOutlinedAndLarger keep per-family scale changes on the pooled renderers.
            var scale = iconScale * (IsEasing(iconType)
                ? new Vector3(
                    IsCircularEasing(iconType)
                        ? CircularEasingIconWidth
                        : EasingIconWidth,
                    EasingIconHeight,
                    DefaultIconSize)
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

        // Only easing glyphs receive the requested wider, shorter aspect; direction and state icons retain OE proportions.
        private static bool IsEasing(GLSEventIconType iconType)
        {
            var value = (int)iconType;
            return value >= (int)GLSEventIconType.EaseLinear
                && value <= (int)GLSEventIconType.EaseBeatSaberInOutBounce;
        }

        // CircularEasingsKeepOriginalWidth names the only easing family exempt from horizontal stretching without changing its center position.
        private static bool IsCircularEasing(GLSEventIconType iconType)
        {
            return iconType is GLSEventIconType.EaseInCircular
                or GLSEventIconType.EaseOutCircular
                or GLSEventIconType.EaseInOutCircular;
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
