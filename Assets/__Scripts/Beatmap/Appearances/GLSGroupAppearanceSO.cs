using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public void SetAppearance(
            GLSGroupContainer container,
            bool final = true,
            bool boost = false)
        {
            container.transform.localScale = Vector3.one * (final ? 0.75f : 0.6f);
            switch (container.EventBoxGroupData)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    var colorEvt = lcebg.Boxes.SelectMany(x => x.Events).FirstOrDefault();
                    if (colorEvt == null || colorEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(
                            colorId,
                            colorEvt.Color == (int)LightColor.Red
                                ? boost ? eventAppearance.RedBoostColor : eventAppearance.RedColor
                                : colorEvt.Color == (int)LightColor.Blue
                                    ? boost ? eventAppearance.BlueBoostColor : eventAppearance.BlueColor
                                    : boost
                                        ? eventAppearance.WhiteBoostColor
                                        : eventAppearance.WhiteColor);
                        container.SetText(GLSEventCommon.GetColorInfo(colorEvt));
                        container.SetText(true);
                    }

                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    var rotationEvt = lrebg.Boxes.SelectMany(x => x.Events).FirstOrDefault();
                    if (rotationEvt == null || rotationEvt.UsePrevious == 1)
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
                case BaseLightTranslationEventBoxGroup ltebg:
                    var translationEvt = ltebg.Boxes.SelectMany(x => x.Events).FirstOrDefault();
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
                case BaseVfxEventEventBoxGroup ffbg:
                    var fxEvt = ffbg.Boxes.SelectMany(x => x.Events).FirstOrDefault();
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
