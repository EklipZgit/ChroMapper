using UnityEngine;

public class Parametric3SliceSpriteWidthEndFloatFxEffectTargetData : EnvironmentComponentData<ParametricSliceEndWidthFx>
{
    public string Parametric3SliceSpriteController;

    public Vector2 ValueBounds;
    public float ValueMultiplier = 1f;

    public override void SearchAndFillComponents(
        GameObject self,
        ParametricSliceEndWidthFx comp,
        CreateContainer container)
    {
        comp.SpriteLight = container
            .GetGameObjectOrNull(Parametric3SliceSpriteController, self)
            .GetComponent<ParametricSpriteLight>();
    }

    public override void CopyTo(ParametricSliceEndWidthFx comp)
    {
        comp.ValueBounds = ValueBounds;
        comp.ValueMultiplier = ValueMultiplier;
    }
}
