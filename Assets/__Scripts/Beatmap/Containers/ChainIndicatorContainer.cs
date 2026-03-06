using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ChainIndicatorContainer : ObjectContainer
    {
        [Header("Others")] public IndicatorType IndicatorType;
        public ChainContainer ParentChain;

        private static readonly int translucentAlphaId = Shader.PropertyToID("_TranslucentAlpha");
        private static readonly int opaqueAlphaId = Shader.PropertyToID("_OpaqueAlpha");

        public override BaseObject ObjectData
        {
            get => ParentChain.ChainData;
            set => ParentChain.ChainData = (BaseChain)value;
        }

        public override void UpdateGridPosition()
        {
            var chainData = (BaseChain)ObjectData;
            switch (IndicatorType)
            {
                case IndicatorType.Head:
                    transform.localPosition = Vector3.zero;
                    transform.localEulerAngles = new Vector3(
                        NoteContainer.Directionalize(ParentChain.ChainData.CutDirection).z + 90,
                        -90,
                        0);
                    break;
                case IndicatorType.Tail:
                    {
                        var zOffset = (chainData.TailSongBpmTime - chainData.SongBpmTime)
                            * EditorScaleController.EditorScale
                            * BeatmapConstant.LaneSize;
                        transform.localPosition = (Vector3)(chainData.GetTailPosition() - chainData.GetPosition())
                            + new Vector3(0f, 0f, zOffset);
                        transform.rotation = ParentChain.GetTailNodeRotation();
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateMaterials(MaterialPropertyBlock materialPropertyBlock)
        {
            var c = materialPropertyBlock.GetColor(colorId);
            MpbController.Mpb.SetColor(colorId, c);
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
