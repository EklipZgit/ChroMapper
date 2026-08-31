using UnityEngine;

public class Movement : MonoBehaviour
{
    public Transform[] Transforms;
    public Vector3[] MovementData;
    public float TransitionSpeed;

    private Vector3[] startLocalPositions;

    private void Start()
    {
        startLocalPositions = new Vector3[Transforms.Length];
        for (var i = 0; i < Transforms.Length; i++)
            startLocalPositions[i] = Transforms[i].localPosition;

        SetLocalPositionOffsetsForAllObjects(MovementData[0]);
    }

    public void Apply(Vector3 localPositionOffset)
    {
        SetLocalPositionOffsetsForAllObjects(localPositionOffset);
    }

    private void SetLocalPositionOffsetsForAllObjects(Vector3 localPositionOffset)
    {
        for (var i = 0; i < Transforms.Length; i++)
            Transforms[i].localPosition = startLocalPositions[i] + localPositionOffset;
    }
}
