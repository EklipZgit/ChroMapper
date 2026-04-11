using UnityEngine;

public class HydraulicCarSuspensionEffectData : EnvironmentComponentData<HydraulicCarSuspension>
{
    public string ContractEvent;
    public int[] ContractEventValues;
    public string ExpandEvent;
    public int[] ExpandEventValues;
    public string SpringJoint;
    public float ContractDistance = 0.3f;
    public float ExpandDistance = 0.4f;
    public string Rigidbody;

    public override void SearchAndFillComponents(GameObject self, HydraulicCarSuspension comp, CreateContainer container)
    {
        comp.Rigidbody = container.GetGameObjectOrNull(Rigidbody, self).GetComponent<Rigidbody>();
        comp.SpringJoint = container.GetGameObjectOrNull(SpringJoint, self).GetComponent<SpringJoint>();
    }

    public override void CopyTo(HydraulicCarSuspension comp)
    {
        comp.ContractEventValues = ContractEventValues;
        comp.ExpandEventValues = ExpandEventValues;
        comp.ContractDistance = ContractDistance;
        comp.ExpandDistance = ExpandDistance;
    }
}
