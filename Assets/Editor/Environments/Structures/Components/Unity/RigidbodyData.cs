using System;
using UnityEngine;

public class RigidbodyData : EnvironmentComponentData<Rigidbody>
{
    public override int Priority => -1;

    public float Mass;
    public float LinearDamping;
    public float AngularDamping;
    public bool UseGravity;
    public bool IsKinematic;
    public string Interpolation;
    public string CollisionDetectionMode;
    public string Constraints;
    public RigidbodyStartupData StartupState;

    public override void FillComponents(GameObject self, Rigidbody comp, CreateContainer container)
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

public class RigidbodyStartupData
{
    public Vector3 Position;
    public Vector4 Rotation;
    public Vector3 LinearVelocity;
    public Vector3 AngularVelocity;
}
