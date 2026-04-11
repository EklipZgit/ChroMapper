using System.Linq;
using UnityEngine;

public class
    MaterialPropertyBlockRandomValueSetterData : EnvironmentComponentData<MaterialPropertyBlockRandomValueSetter>
{
    public string[] Renderers;
    public string PropertyName;
    public float MinValue;
    public float MaxValue = 1f;

    public override void SearchAndFillComponents(
        GameObject self,
        MaterialPropertyBlockRandomValueSetter comp,
        CreateContainer container)
    {
        comp.Renderers = Renderers
            .Select(x => container
                .GetGameObjectOrNull(x, self)
                .GetComponent<Renderer>())
            .ToArray();
    }

    public override void CopyTo(MaterialPropertyBlockRandomValueSetter comp)
    {
        comp.PropertyName = PropertyName;
        comp.MinValue = MinValue;
        comp.MaxValue = MaxValue;
    }
}
