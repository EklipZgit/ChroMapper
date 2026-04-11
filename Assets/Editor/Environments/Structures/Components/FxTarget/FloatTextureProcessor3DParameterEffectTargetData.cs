using UnityEngine;

public class FloatTextureProcessor3DParameterEffectTargetData : EnvironmentComponentData<TextureProcessor3DParameterFx>
{
    public string TextureProcessor3D;

    public int Parameter;
    public int Channel;

    public Vector2 ValueBounds = new(0f, 1f);

    public override void SearchAndFillComponents(
        GameObject self,
        TextureProcessor3DParameterFx comp,
        CreateContainer container)
    {
        comp.TextureProcessor3D = container
            .GetGameObjectOrNull(TextureProcessor3D, self)
            .GetComponent<TextureProcessor3D>();
    }

    public override void CopyTo(TextureProcessor3DParameterFx comp)
    {
        comp.Parameter = (TextureProcessor3DParameterFx.TextureProcessor3DParameter)Parameter;
        comp.Channel = (TextureProcessor3DParameterFx.TextureProcessor3DChannel)Channel;
        comp.ValueBounds = ValueBounds;
    }
}
