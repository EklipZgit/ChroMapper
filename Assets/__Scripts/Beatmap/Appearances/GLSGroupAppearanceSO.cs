using System;
using System.Globalization;
using ZLinq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/GLS Group Appearance SO", fileName = "GLSGroupAppearanceSO")]
    public class GLSGroupAppearanceSO : ScriptableObject
    {
        [SerializeField] private EventAppearanceSO eventAppearance;

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int strobeColorId = Shader.PropertyToID("_StrobeColor");
        private static readonly int strobeColorEnabledId = Shader.PropertyToID("_StrobeColorEnabled");
        public void SetAppearance(
            GLSGroupContainer container,
            bool final = true,
            bool boost = false)
        {
            // Reuse the Basic Event scale contract so outer GLS nodes share its grounded-height calculation.
            container.transform.localScale = Vector3.one * (final
                ? EventAppearanceSO.FinalNodeScale
                : EventAppearanceSO.PreviewNodeScale);
            container.MpbController.Mpb.SetFloat(strobeColorEnabledId, 0f);

            // Appearance refreshes are frequent, so only build ordering when the group has not initialized its maintained cache.
            if (container.EventBoxGroupData is not null && !container.EventBoxGroupData.OrderedEventsInitialized)
                container.EventBoxGroupData.ResortOrderedEvents();

            switch (container.EventBoxGroupData)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    var colorEvt = container.PreviewEventData as BaseLightColorBase
                        ?? lcebg.OrderedEvents.AsValueEnumerable().OfType<BaseLightColorBase>().FirstOrDefault();
                    container.SetIcons(colorEvt);
                    if (colorEvt == null || colorEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        // Use first event's full appearance including strobe colors
                        var color = GLSEventCommon.GetColor(colorEvt, boost, eventAppearance);
                        var strobeColor = GLSEventCommon.GetStrobeColor(colorEvt, boost, eventAppearance);
                        container.MpbController.Mpb.SetColor(colorId, color);
                        container.MpbController.Mpb.SetColor(strobeColorId, strobeColor);
                        // Keep an unset strobe dark color from rendering a band on a non-strobing preview node.
                        var strobeBandEnabled = GLSEventCommon.IsStrobing(colorEvt) && color != strobeColor;
                        container.MpbController.Mpb.SetFloat(
                            strobeColorEnabledId,
                            strobeBandEnabled
                                ? 1f
                                : 0f);
                        container.SetText(GLSEventCommon.GetColorInfo(colorEvt));
                        container.SetText(true);
                    }

                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    var rotationEvt = container.PreviewEventData as BaseLightRotationBase
                        ?? lrebg.OrderedEvents.AsValueEnumerable().OfType<BaseLightRotationBase>().FirstOrDefault();
                    container.SetIcons(rotationEvt);
                    if (rotationEvt == null || rotationEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(colorId, GLSEventCommon.GetAxisColor(rotationEvt, eventAppearance));
                        container.SetText(GLSEventCommon.GetRotationInfo(rotationEvt));
                        container.SetText(true);
                    }

                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    var translationEvt = container.PreviewEventData as BaseLightTranslationBase
                        ?? ltebg.OrderedEvents.AsValueEnumerable().OfType<BaseLightTranslationBase>().FirstOrDefault();
                    container.SetIcons(translationEvt);
                    if (translationEvt == null || translationEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(colorId, GLSEventCommon.GetAxisColor(translationEvt, eventAppearance));
                        container.SetText(GLSEventCommon.GetTranslationInfo(translationEvt));
                        container.SetText(true);
                    }

                    break;
                case BaseVfxEventEventBoxGroup ffbg:
                    var fxEvt = container.PreviewEventData as BaseFxEventFloat
                        ?? ffbg.OrderedEvents.AsValueEnumerable().OfType<BaseFxEventFloat>().FirstOrDefault();
                    container.SetIcons(fxEvt);
                    if (fxEvt == null || fxEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.RingEventsColor);
                        container.SetText(GLSEventCommon.GetFloatFXInfo(fxEvt));
                        container.SetText(true);
                    }

                    break;
                default:
                    container.SetIcons(null);
                    container.MpbController.Mpb.SetColor(colorId, Color.gray);
                    container.SetText(false);
                    break;
            }

            container.MpbController.ApplyChanges();
        }

        public void UpdateTransitionRibbon(GLSGroupContainer container, Func<float, bool> isBoostAt)
        {
            if (container.PreviewEventData is BaseLightColorBase colorEvent)
            {
                // each outer preview ribbon exposes the represented box's physical light lanes.
                GLSEventCommon.UpdateColorTransitionRibbon(
                    container.lightGradientController,
                    colorEvent,
                    eventAppearance,
                    isBoostAt,
                    container.GlsLightCount,
                    aggregateSameTimeBoxes: true);
                if (container.IncomingLightGradientController != null)
                {
                    GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                        container.IncomingLightGradientController, colorEvent, eventAppearance, isBoostAt,
                        container.GlsLightCount, aggregateSameTimeBoxes: true);
                }
            }
            else
            {
                container.lightGradientController.SetVisible(false);
                // A recycled non-color body must not keep a former color group's incoming interval visible.
                if (container.IncomingLightGradientController != null)
                {
                    container.IncomingLightGradientController.SetVisible(false);
                }
            }
        }
    }
}
