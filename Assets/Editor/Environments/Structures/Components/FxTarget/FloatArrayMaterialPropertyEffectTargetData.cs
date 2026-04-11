using System.Linq;
using UnityEngine;

public class FloatArrayMaterialPropertyEffectTargetData : EnvironmentComponentData<MpbArrayFx>
{
    public string[] MaterialPropertyBlockControllers;
    public string PropertyName;

    public Vector2 ValueBounds;
    public float GranularityMultiplier;

    public override void SearchAndFillComponents(GameObject self, MpbArrayFx comp, CreateContainer container)
    {
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(x => container.GetGameObjectOrNull(x, self))
            .Where(x => x != null)
            .Select(x => x.GetComponent<MaterialPropertyBlockController>())
            .Where(x => x != null)
            .ToArray();
    }

    public override void CopyTo(MpbArrayFx comp)
    {
        comp.PropertyName = PropertyName;
        comp.ValueBounds = ValueBounds;
        comp.GranularityMultiplier = GranularityMultiplier;
    }
}
