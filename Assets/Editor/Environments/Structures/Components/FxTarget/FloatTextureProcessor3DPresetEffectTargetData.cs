using UnityEngine;

public class FloatTextureProcessor3DPresetEffectTargetData : EnvironmentComponentData<TextureProcessor3DPresetFx>
{
    public string TextureProcessor3D;
    public Vector2 ValueBounds = new(0f, 1f);

    public override void SearchAndFillComponents(
        GameObject self,
        TextureProcessor3DPresetFx comp,
        CreateContainer container)
    {
        comp.TextureProcessor3D = container
            .GetGameObjectOrNull(TextureProcessor3D, self)
            .GetComponent<TextureProcessor3D>();
    }

    public override void CopyTo(TextureProcessor3DPresetFx comp) => comp.ValueBounds = ValueBounds;
}
