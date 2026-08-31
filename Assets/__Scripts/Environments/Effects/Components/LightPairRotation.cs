using System;
using UnityEngine;

public class LightPairRotation : MonoBehaviour
{
    public TransformContainer[] Transforms = new TransformContainer[2];

    public Vector3 RotationVector;

    public bool OverrideRandomValues;
    public bool UseZPositionForAngleOffset;
    public float ZPositionAngleOffsetScale;

    public float StartRotation;

    private bool initialized;

    private void Awake() => TryInitializeTransforms();

    // LightPairRotationLateWiringIsFinalizedDuringEffectInitialization requires runtime builder wiring to be
    // finalized at manager initialization, while Awake remains tolerant of fields assigned after AddComponent.
    public void Initialize()
    {
        if (!TryInitializeTransforms())
            throw new InvalidOperationException($"Light pair rotation '{name}' requires two initialized transforms.");
    }

    // Applying both sides together prevents separate event managers from observing different
    // states while the editor recomputes or rewinds a shared paired-laser timeline. The lifecycle
    // invariant established by Initialize keeps this render path branch-free.
    public void Apply(float leftAngle, float rightAngle)
    {
        Apply(Transforms[0], leftAngle);
        Apply(Transforms[1], rightAngle);
    }

    private bool TryInitializeTransforms()
    {
        if (initialized)
            return true;
        if (Transforms == null || Transforms.Length < 2)
            return false;

        // Validate the complete pair before caching either start rotation; partial setup would
        // otherwise compound StartRotation when initialization is retried after builder wiring.
        if (Transforms[0] == null || Transforms[0].Transform == null
            || Transforms[1] == null || Transforms[1].Transform == null)
        {
            return false;
        }

        for (var i = 0; i < 2; i++)
        {
            var container = Transforms[i];
            container.StartAngle = i == 0 ? StartRotation : -StartRotation;
            container.Start = container.Transform.rotation;
            container.Transform.localRotation =
                container.Start * Quaternion.Euler(RotationVector * container.StartAngle);
        }

        initialized = true;
        return true;
    }

    private void Apply(TransformContainer container, float angle)
    {
        container.Transform.localRotation = container.Start * Quaternion.Euler(RotationVector * angle);
    }

    [Serializable]
    public class TransformContainer
    {
        public Transform Transform;

        [NonSerialized] public Quaternion Start;
        [NonSerialized] public float StartAngle;
    }
}
