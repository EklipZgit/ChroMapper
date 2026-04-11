using System.Linq;
using UnityEngine;

public class
    MaterialPropertyBlockControllerArrayRandomValueSetterData : EnvironmentComponentData<
    MaterialPropertyBlockControllerArrayRandomValueSetter>
{
    public string[] MaterialPropertyBlockControllers;
    public string PropertyName;
    public Vector3 Min;
    public Vector3 Max;

    public override void SearchAndFillComponents(
        GameObject self,
        MaterialPropertyBlockControllerArrayRandomValueSetter comp,
        CreateContainer container)
    {
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(x => container
                .GetGameObjectOrNull(x, self)
                .GetComponent<MaterialPropertyBlockController>())
            .ToArray();
    }

    public override void CopyTo(MaterialPropertyBlockControllerArrayRandomValueSetter comp)
    {
        comp.PropertyName = PropertyName;
        comp.Min = Min;
        comp.Max = Max;
    }
}
