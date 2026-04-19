using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ArcIndicatorContainer : ObjectContainer
    {
        [Header("Others")] public IndicatorType IndicatorType;
        public ArcContainer ParentArc;

        private static readonly int translucentAlphaId = Shader.PropertyToID("_TranslucentAlpha");
        private static readonly int opaqueAlphaId = Shader.PropertyToID("_OpaqueAlpha");

        public override BaseObject ObjectData
        {
            get => ParentArc.ArcData;
            set => ParentArc.ArcData = (BaseArc)value;
        }

        public override void UpdateGridPosition()
        {
            switch (IndicatorType)
            {
                // We're not using p1 and p2 since they're *really* far away
                case IndicatorType.Head:
                    {
                        var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ParentArc.ArcData.CutDirection).z;
                        var headDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);
                        var pos = ParentArc.p0() + (headDirection * BeatmapConstant.LaneSize / 2f);
                        transform.localPosition = pos;
                        transform.localEulerAngles = new Vector3(
                            NoteContainer.Directionalize(ParentArc.ArcData.CutDirection).z + 90,
                            -90,
                            0);
                        break;
                    }
                case IndicatorType.Tail:
                    {
                        var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ParentArc.ArcData.TailCutDirection).z;
                        var tailDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);
                        var pos = ParentArc.p3() - (tailDirection / BeatmapConstant.LaneSize / 2f);
                        pos.z *= ParentArc.ArcData.DurationSongBpmTime
                            * EditorScaleController.EditorScale
                            * BeatmapConstant.LaneSize;
                        transform.localPosition = pos;
                        transform.localEulerAngles = new Vector3(
                            NoteContainer.Directionalize(ParentArc.ArcData.TailCutDirection).z + 90,
                            -90,
                            0);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateMaterials(MaterialPropertyBlock materialPropertyBlock)
        {
            var c = materialPropertyBlock.GetColor(ColorId);
            MpbController.Mpb.SetColor(ColorId, c);
            UpdateMaterials();
        }

        public override void Setup()
        {
            base.Setup();
            MpbController.Mpb.SetFloat(translucentAlphaId, 0.6f);
            MpbController.Mpb.SetFloat(opaqueAlphaId, 0.6f);

            UpdateMaterials();
        }
    }
}
