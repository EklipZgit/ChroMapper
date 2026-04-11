using UnityEngine;

public class
    FloatTextureProcessor3DMappingFloatEffectTargetData : EnvironmentComponentData<TextureProcessor3DMappingFloatFx>
{
    public string Material;
    public bool UseSlave;
    public string SlaveMaterial;

    public int Mapping;

    public Vector2 ValueBounds = new(-1f, 1f);
    public bool InvertAxis;
    public bool InvertAxisSlave;

    public override void SearchAndFillComponents(
        GameObject self,
        TextureProcessor3DMappingFloatFx comp,
        CreateContainer container)
    {
        comp.Material = container.Library.Materials.Lookup[Material];
        comp.SlaveMaterial = container.Library.Materials.Lookup[SlaveMaterial];
    }

    public override void CopyTo(TextureProcessor3DMappingFloatFx comp)
    {
        comp.UseSlave = UseSlave;
        comp.Mapping = (TextureProcessor3DMappingFloatFx.TextureProcessor3DMapping)Mapping;
        comp.ValueBounds = ValueBounds;
        comp.InvertAxis = InvertAxis;
        comp.InvertAxisSlave = InvertAxisSlave;
    }
}
