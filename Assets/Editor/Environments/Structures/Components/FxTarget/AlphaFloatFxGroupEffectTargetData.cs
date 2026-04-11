using System.Linq;
using UnityEngine;

public class AlphaFloatFxGroupEffectTargetData : EnvironmentComponentData<AlphaFx>
{
    public string[] MaterialPropertyBlockControllers;
    public string Property;
    public float[] StaticColor;

    public override void SearchAndFillComponents(GameObject self, AlphaFx comp, CreateContainer container)
    {
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(x => container.GetGameObjectOrNull(x, self))
            .Where(x => x != null)
            .Select(x => x.GetComponent<MaterialPropertyBlockController>())
            .Where(x => x != null)
            .ToArray();
    }

    public override void CopyTo(AlphaFx comp)
    {
        comp.Property = Property;
        comp.StaticColor = new Color(StaticColor[0], StaticColor[1], StaticColor[2], StaticColor[3]);
    }
}
