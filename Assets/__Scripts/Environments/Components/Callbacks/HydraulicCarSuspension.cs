using UnityEngine;

public class HydraulicCarSuspension : MonoBehaviour
{
    public int ContractEventType = 16;
    public int[] ContractEventValues;
    public int ExpandEventType = 17;
    public int[] ExpandEventValues;
    public SpringJoint SpringJoint;
    public float ContractDistance = 0.3f;
    public float ExpandDistance = 0.4f;
    public Rigidbody Rigidbody;
}
