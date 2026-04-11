using UnityEngine;

public class FloatMaterialPropertyEffectTargetData : EnvironmentComponentData<MpbFx>
{
    public string MaterialPropertyBlockController;
    public string PropertyName;
    public Vector2 ValueBounds;
    public float GranularityMultiplier;

    public override void SearchAndFillComponents(GameObject self, MpbFx comp, CreateContainer container)
    {
        comp.MpbController = container
            .GetGameObjectOrNull(MaterialPropertyBlockController, self)
            .GetComponent<MaterialPropertyBlockController>();
    }

    public override void CopyTo(MpbFx comp)
    {
        comp.PropertyName = PropertyName;
        comp.ValueBounds = ValueBounds;
        comp.GranularityMultiplier = GranularityMultiplier;
    }
}
