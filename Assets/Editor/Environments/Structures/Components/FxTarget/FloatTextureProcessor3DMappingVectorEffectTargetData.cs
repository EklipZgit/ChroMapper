using UnityEngine;

public class
    FloatTextureProcessor3DMappingVectorEffectTargetData : EnvironmentComponentData<TextureProcessor3DMappingVectorFx>
{
    public string Material;
    public bool UseSlave;
    public string SlaveMaterial;

    public int Mapping;
    public int Channel;

    public Vector2 ValueBounds = new(-1f, 1f);
    public bool InvertAxis;
    public bool InvertAxisSlave;

    public override void SearchAndFillComponents(
        GameObject self,
        TextureProcessor3DMappingVectorFx comp,
        CreateContainer container)
    {
        comp.Material = container.Library.Materials.Lookup[Material];
        comp.SlaveMaterial = container.Library.Materials.Lookup[SlaveMaterial];
    }

    public override void CopyTo(TextureProcessor3DMappingVectorFx comp)
    {
        comp.UseSlave = UseSlave;
        comp.Mapping = (TextureProcessor3DMappingVectorFx.TextureProcessor3DMapping)Mapping;
        comp.Channel = (TextureProcessor3DMappingVectorFx.TextureProcessor3DChannel)Channel;
        comp.ValueBounds = ValueBounds;
        comp.InvertAxis = InvertAxis;
        comp.InvertAxisSlave = InvertAxisSlave;
    }
}
