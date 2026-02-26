using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ChainIndicatorContainer : ObjectContainer
    {
        public IndicatorType IndicatorType;
        public ChainContainer ParentChain;

        private static readonly int lit = Shader.PropertyToID("_Lit");
        private static readonly int translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");
        private static readonly int opaqueAlpha = Shader.PropertyToID("_OpaqueAlpha");

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
            MaterialPropertyBlock.SetColor(colorId, c);
            UpdateMaterials();
        }

        public override void Setup()
        {
            base.Setup();
            MaterialPropertyBlock.SetFloat(lit, Settings.Instance.SimpleBlocks ? 0 : 1);
            MaterialPropertyBlock.SetFloat(translucentAlpha, 0.6f);
            MaterialPropertyBlock.SetFloat(opaqueAlpha, 0.6f);

            UpdateMaterials();
        }
    }
}
