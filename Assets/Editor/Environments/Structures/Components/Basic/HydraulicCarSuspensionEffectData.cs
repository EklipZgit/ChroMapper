using UnityEngine;

public class HydraulicCarSuspensionEffectData : EnvironmentComponentData<HydraulicCarSuspension>
{
    public string ContractEventType;
    public int[] ContractEventValues;
    public string ExpandEventType;
    public int[] ExpandEventValues;
    public int SpringJoint;
    public float ContractDistance = 0.3f;
    public float ExpandDistance = 0.4f;
    public int Rigidbody;

    public override void FillComponents(
        GameObject self,
        HydraulicCarSuspension comp,
        CreateContainer container)
    {
        comp.ContractEffect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
                ConvertUtils.ToEventType(ContractEventType));
        comp.ExpandEffect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
                ConvertUtils.ToEventType(ExpandEventType));

        comp.Rigidbody = container.GetComponentOrNull<Rigidbody>(Rigidbody);
        comp.SpringJoint = container.GetComponentOrNull<SpringJoint>(SpringJoint);
        comp.ContractEventValues = ContractEventValues;
        comp.ExpandEventValues = ExpandEventValues;
        comp.ContractDistance = ContractDistance;
        comp.ExpandDistance = ExpandDistance;
    }
}
