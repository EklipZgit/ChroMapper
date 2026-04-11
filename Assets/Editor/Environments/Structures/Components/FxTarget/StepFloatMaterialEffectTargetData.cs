using UnityEngine;

public class StepFloatMaterialEffectTargetData : EnvironmentComponentData<MpbStepFx>
{
    public string MaterialPropertyBlockController;
    public string PropertyName;

    public float StepFactor;
    public float StepSize;

    public override void SearchAndFillComponents(GameObject self, MpbStepFx comp, CreateContainer container)
    {
        comp.MpbController = container
            .GetGameObjectOrNull(MaterialPropertyBlockController, self)
            .GetComponent<MaterialPropertyBlockController>();
    }

    public override void CopyTo(MpbStepFx comp)
    {
        comp.PropertyName = PropertyName;
        comp.StepFactor = StepFactor;
        comp.StepSize = StepSize;
    }
}
