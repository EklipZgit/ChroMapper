using System.Linq;
using UnityEngine;

public class TextureProcessor3DData : EnvironmentComponentData<TextureProcessor3D>
{
    public string TextureGenCompute;
    public string WriteTexturesCompute;
    public string[] InputTextures;

    public int RowSize;
    public int ColumnSize;
    public int DepthSize;

    public MotionPreset[] PresetArray = new MotionPreset[10];

    public int ActivePresetIndex;

    public string[] MaterialsUsingOutput;

    public override void SearchAndFillComponents(GameObject self, TextureProcessor3D comp, CreateContainer container)
    {
        comp.TextureGenCompute = container.Library.ComputeShaders.Find(x => x.name == TextureGenCompute).computeShader;
        comp.WriteTexturesCompute =
            container.Library.ComputeShaders.Find(x => x.name == WriteTexturesCompute).computeShader;
        comp.InputTextures = InputTextures.Select(x => container.Library.Textures.Lookup[x] as Texture2D).ToArray();
        // comp.MaterialsUsingOutput = MaterialsUsingOutput.Select(x => container.Library.Materials.Lookup[x]).ToArray();
    }

    public override void CopyTo(TextureProcessor3D comp)
    {
        comp.PresetArray = PresetArray.Select(x => x.Create()).ToArray();

        comp.RowSize = RowSize;
        comp.ColumnSize = ColumnSize;
        comp.DepthSize = DepthSize;
        comp.ActivePresetIndex = ActivePresetIndex;
    }

    public class ChannelParams
    {
        public int ComputeKernel;
        public int InputTextureIndex;
        public float Speed;
        public float SpatialScale;
        public float Phase;
        public float Param1;
        public float Param2;
        public float OutputOffset;

        public TextureProcessor3D.ChannelParams Create()
        {
            return new TextureProcessor3D.ChannelParams
            {
                ComputeKernel = (TextureProcessor3D.ComputeKernel)ComputeKernel,
                InputTextureIndex = InputTextureIndex,
                Speed = Speed,
                SpatialScale = SpatialScale,
                Phase = Phase,
                Param1 = Param1,
                Param2 = Param2,
                OutputOffset = OutputOffset
            };
        }
    }

    public class MotionPreset
    {
        public ChannelParams ChannelA;
        public ChannelParams ChannelB;
        public ChannelParams ChannelC;
        public ChannelParams ChannelD;

        public TextureProcessor3D.MotionPreset Create()
        {
            return new TextureProcessor3D.MotionPreset
            {
                ChannelA = ChannelA.Create(),
                ChannelB = ChannelB.Create(),
                ChannelC = ChannelC.Create(),
                ChannelD = ChannelD.Create()
            };
        }
    }
}
