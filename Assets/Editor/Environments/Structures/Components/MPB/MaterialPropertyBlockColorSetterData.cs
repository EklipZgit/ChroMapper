using UnityEngine;

public class MaterialPropertyBlockColorSetterData : EnvironmentComponentData<MaterialPropertyBlockColorSetter>
{
    public string MaterialPropertyBlockControllerId;
    public string Property;
    public bool InverseAlpha;
    public bool DisableOnZeroAlpha;
    public bool SendAlphaToProperty;
    public string AlphaProperty;
    public bool MultiplyWithAlpha;

    public override void SearchAndFillComponents(
        GameObject self,
        MaterialPropertyBlockColorSetter comp,
        CreateContainer container)
    {
        comp.Controller = container
            .GetGameObjectOrNull(MaterialPropertyBlockControllerId, self)
            .GetComponent<MaterialPropertyBlockController>();
    }

    public override void CopyTo(MaterialPropertyBlockColorSetter comp)
    {
        comp.Property = Property;
        comp.InverseAlpha = InverseAlpha;
        comp.DisableOnZeroAlpha = DisableOnZeroAlpha;
        comp.SendAlphaToProperty = SendAlphaToProperty;
        comp.AlphaProperty = AlphaProperty;
        comp.MultiplyWithAlpha = MultiplyWithAlpha;
    }
}
