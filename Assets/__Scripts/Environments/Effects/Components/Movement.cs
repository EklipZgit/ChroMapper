using UnityEngine;

public class Movement : MonoBehaviour
{
    public GenericCallbackEventEffect Effect;

    public Transform[] Transforms;
    public Vector3[] MovementData;
    public float TransitionSpeed;

    private int currMovementIndex;
    private Vector3 currPositionOffset;
    private Vector3 prevPositionOffset;
    private Vector3[] startLocalPositions;

    private void Start()
    {
        currPositionOffset = MovementData[0];
        prevPositionOffset = currPositionOffset;
        startLocalPositions = new Vector3[Transforms.Length];
        for (var i = 0; i < Transforms.Length; i++) startLocalPositions[i] = Transforms[i].localPosition;
        SetLocalPositionOffsetsForAllObjects(currPositionOffset);
        Effect.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    protected void FixedUpdate()
    {
        prevPositionOffset = currPositionOffset;
        currPositionOffset = Vector3.LerpUnclamped(
            currPositionOffset,
            MovementData[currMovementIndex],
            Time.fixedDeltaTime * TransitionSpeed);
        if ((currPositionOffset - MovementData[currMovementIndex]).sqrMagnitude < 0.01f) enabled = false;
    }

    protected void LateUpdate() =>
        SetLocalPositionOffsetsForAllObjects(
            Vector3.LerpUnclamped(prevPositionOffset, currPositionOffset, TimeHelper.InterpolationFactor));

    private void HandleStateChanged((int index, BasicEventStateData state) data)
    {
        currMovementIndex = data.index % MovementData.Length;
        enabled = true;
    }

    private void SetLocalPositionOffsetsForAllObjects(Vector3 localPositionOffset)
    {
        for (var i = 0; i < Transforms.Length; i++)
            Transforms[i].localPosition = startLocalPositions[i] + localPositionOffset;
    }
}
