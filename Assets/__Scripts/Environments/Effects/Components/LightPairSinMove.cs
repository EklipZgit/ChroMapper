using System;
using UnityEngine;

public class LightPairSinMove : MonoBehaviour
{
    // Retained solely to migrate already-generated environment scenes at runtime.
    public LightRotationEffect LeftEffect;
    public LightRotationEffect RightEffect;
    public GenericCallbackEventEffect SwitchEffect;

    public TransformContainer[] Transforms = new TransformContainer[2];

    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    private bool initialized;

    private void Awake() => InitializeTransforms();

    public void Apply(float leftPhase, float rightPhase)
    {
        InitializeTransforms();
        if (!initialized)
            return;

        // Direct phase evaluation makes pause, rewind, and arbitrary playhead jumps
        // agree with continuous song-time playback.
        Apply(Transforms[0], leftPhase);
        Apply(Transforms[1], rightPhase);
    }

    private void InitializeTransforms()
    {
        if (initialized || Transforms == null || Transforms.Length < 2)
            return;
        if (Transforms[0] == null || Transforms[0].Transform == null
            || Transforms[1] == null || Transforms[1].Transform == null)
        {
            return;
        }

        for (var i = 0; i < 2; i++)
        {
            var container = Transforms[i];
            container.Side = i == 0 ? 1f : -1f;
            container.StartPosition = container.Transform.localPosition;
        }

        initialized = true;
    }

    private void Apply(TransformContainer container, float phase)
    {
        var vector = Vector3.LerpUnclamped(
            StartPositionOffset,
            EndPositionOffset,
            (Mathf.Sin(phase) * 0.5f) + 0.5f);
        vector.x *= container.Side;
        container.Transform.localPosition = container.StartPosition + vector;
    }

    [Serializable]
    public class TransformContainer
    {
        public Transform Transform;

        [NonSerialized] public Vector3 StartPosition;
        [NonSerialized] public float Side;
    }
}
