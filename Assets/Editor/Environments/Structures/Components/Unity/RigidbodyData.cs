using System;
using UnityEngine;

public class RigidbodyData : EnvironmentComponentData<Rigidbody>
{
    public float Mass;
    public float LinearDamping;
    public float AngularDamping;
    public bool UseGravity;
    public bool IsKinematic;
    public string Interpolation;
    public string CollisionDetectionMode;
    public string Constraints;

    public override void SearchAndFillComponents(GameObject self, Rigidbody comp, CreateContainer container) { }

    public override void CopyTo(Rigidbody comp)
    {
        comp.mass = Mass;
        comp.linearDamping = LinearDamping;
        comp.angularDamping = AngularDamping;
        comp.useGravity = UseGravity;
        comp.isKinematic = IsKinematic;
        comp.interpolation = Enum.Parse<RigidbodyInterpolation>(Interpolation);
        comp.collisionDetectionMode = Enum.Parse<CollisionDetectionMode>(CollisionDetectionMode);
        comp.constraints = Enum.Parse<RigidbodyConstraints>(Constraints);
    }
}
