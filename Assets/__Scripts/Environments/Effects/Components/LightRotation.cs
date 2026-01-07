using UnityEngine;
using Random = UnityEngine.Random;

public class LightRotation : MonoBehaviour
{
    public LightRotationEffect Effect;

    public Transform Transform;
    public Quaternion StartRotation;
    public Vector3 RotationVector;
    public float SpeedMultiplier;

    private float speed;

    private void Start()
    {
        Effect.OnStateChanged += HandleStateChanged;
        enabled = false;
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;
    private void Update() => Transform.Rotate(RotationVector, Time.deltaTime * speed, Space.Self);

    private void HandleStateChanged(LightRotationStateData state)
    {
        var evt = state.Base;
        float value = evt.Value;

        var direction = Random.value < 0.5f ? 1f : -1f;
        var lockRotation = false;
        if (evt.CustomData != null)
        {
            if (evt.CustomLockRotation.HasValue) lockRotation = evt.CustomLockRotation.Value;

            if (value > 0)
            {
                if (evt.CustomPreciseSpeed.HasValue)
                    value = evt.CustomPreciseSpeed.Value;
                else if (evt.CustomSpeed.HasValue) value = evt.CustomSpeed.Value;
            }

            if (evt.CustomDirection.HasValue) direction = evt.CustomDirection.Value == 0 ? 1f : -1f;
        }

        switch (value)
        {
            case 0:
                enabled = false;
                if (lockRotation) return;
                Transform.localRotation = StartRotation;
                break;
            case > 0:
                Transform.localRotation = StartRotation;
                Transform.Rotate(RotationVector, Random.Range(0f, 180f), Space.Self);
                speed = value * SpeedMultiplier * 20f * direction;

                enabled = !evt.CustomLockRotation.HasValue || lockRotation;
                break;
        }
    }
}
