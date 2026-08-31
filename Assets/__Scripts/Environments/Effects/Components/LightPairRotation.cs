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

    private void Awake() => InitializeTransforms();

    // Applying both sides together prevents separate event managers from observing different
    // states while the editor recomputes or rewinds a shared paired-laser timeline.
    public void Apply(float leftAngle, float rightAngle)
    {
        InitializeTransforms();
        Apply(0, leftAngle);
        Apply(1, rightAngle);
    }

    private void InitializeTransforms()
    {
        if (initialized || Transforms == null || Transforms.Length < 2)
            return;

        // Validate the complete pair before caching either start rotation; partial setup would
        // otherwise compound StartRotation when initialization is retried after builder wiring.
        if (Transforms[0] == null || Transforms[0].Transform == null
            || Transforms[1] == null || Transforms[1].Transform == null)
        {
            return;
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
    }

    private void Apply(int index, float angle)
    {
        if (Transforms == null || index < 0 || index >= Transforms.Length)
            return;

        var container = Transforms[index];
        if (container == null || container.Transform == null)
            return;

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
