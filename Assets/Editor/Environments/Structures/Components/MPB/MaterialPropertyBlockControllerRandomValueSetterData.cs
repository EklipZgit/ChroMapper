using UnityEngine;

public class
    MaterialPropertyBlockControllerRandomValueSetterData : EnvironmentComponentData<
    MaterialPropertyBlockControllerRandomValueSetter>
{
    public string MaterialPropertyBlockController;
    public string PropertyName;
    public float Min;
    public float Max = 1000f;

    public override void SearchAndFillComponents(
        GameObject self,
        MaterialPropertyBlockControllerRandomValueSetter comp,
        CreateContainer container)
    {
        comp.MpbController = container
            .GetGameObjectOrNull(MaterialPropertyBlockController, self)
            .GetComponent<MaterialPropertyBlockController>();
    }

    public override void CopyTo(MaterialPropertyBlockControllerRandomValueSetter comp)
    {
        comp.PropertyName = PropertyName;
        comp.Min = Min;
        comp.Max = Max;
    }
}
