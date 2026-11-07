using UnityEngine;

// The inactive scene template contains only car physics, without renderers or live event callbacks.
public class HydraulicCarPhysicsRig : MonoBehaviour
{
    public Rigidbody[] Bodies;
    public SpringJoint[] Joints;
    public Vector3[] StartupPositions;
    public Quaternion[] StartupRotations;
    public Vector3[] StartupVelocities;
    public Vector3[] StartupAngularVelocities;
    public Vector3 Gravity = new(0f, -9.81f, 0f);
    public float FixedDeltaTime = 0.02f;
}
