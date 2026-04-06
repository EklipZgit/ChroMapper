using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/GLS Event Appearance SO", fileName = "GLSEventAppearanceSO")]
    public class GLSEventAppearanceSO : ScriptableObject
    {
        [SerializeField] private EventAppearanceSO eventAppearance;

        private static readonly int colorId = Shader.PropertyToID("_Color");

        public void SetAppearance(
            GLSEventContainer container,
            bool final = true,
            bool boost = false)
        {
            container.transform.localScale = Vector3.one * (final ? 0.75f : 0.6f);
            switch (container.EventData)
            {
                case BaseLightColorBase colorEvt:
                    if (colorEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(
                            colorId,
                            GLSEventCommon.GetColor(colorEvt, boost, eventAppearance));
                        container.SetText(GLSEventCommon.GetColorInfo(colorEvt));
                        container.SetText(true);
                    }

                    break;
                case BaseLightRotationBase rotationEvt:
                    if (rotationEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.RingEventsColor);
                        container.SetText(GLSEventCommon.GetRotationInfo(rotationEvt));
                        container.SetText(true);
                    }

                    break;
                case BaseLightTranslationBase translationEvt:
                    if (translationEvt == null || translationEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.RingEventsColor);
                        container.SetText(GLSEventCommon.GetTranslationInfo(translationEvt));
                        container.SetText(true);
                    }

                    break;
                case BaseFxEventFloat fxEvt:
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
                    container.MpbController.Mpb.SetColor(colorId, Color.gray);
                    container.SetText(false);
                    break;
            }

            container.MpbController.ApplyChanges();
        }
    }
}
