using UnityEngine;

public class TrackLaneRing : MonoBehaviour
{
    [SerializeField] public Vector3 PositionOffset;
    [SerializeField] public TrackLaneRingsManager ParentManager;

    private float previousRotZ;
    private float rotationZ;
    private float destinationRotationZ;

    private float previousPosZ;
    public float PositionZ;
    private float destinationPosZ;

    private float rotateSpeed;
    private float moveSpeed;

    public Transform CachedTransform;

    public void Start() => CachedTransform = transform;

    public void DoReset()
    {
        rotationZ = 0;
        previousRotZ = 0;
        destinationRotationZ = 0;
        rotateSpeed = 0;
    }

    public void Init(Vector3 pos, Vector3 posOffset)
    {
        CachedTransform = transform; // don't ask why twice
        PositionOffset = posOffset;
        CachedTransform.localPosition = pos + PositionOffset;
        previousPosZ = PositionZ = pos.z + PositionOffset.z;
        rotationZ = destinationRotationZ = CachedTransform.localPosition.z;
    }

    public void FixedUpdateRing(float fixedDeltaTime)
    {
        previousRotZ = rotationZ;
        rotationZ = Mathf.Lerp(rotationZ, destinationRotationZ, fixedDeltaTime * rotateSpeed);

        previousPosZ = PositionZ;
        PositionZ = Mathf.Lerp(PositionZ, PositionOffset.z + destinationPosZ, fixedDeltaTime * moveSpeed);
    }

    public void LateUpdateRing(float interpolationFactor)
    {
        CachedTransform.localEulerAngles = new Vector3(
            0,
            0,
            previousRotZ + ((rotationZ - previousRotZ) * interpolationFactor));
        CachedTransform.localPosition = new Vector3(
            PositionOffset.x,
            PositionOffset.y,
            previousPosZ + ((PositionZ - previousPosZ) * interpolationFactor));
    }

    public void SetRotation(float destinationZ, float rotateSpeed)
    {
        destinationRotationZ = destinationZ;
        this.rotateSpeed = rotateSpeed;
    }

    public float GetRotation() => rotationZ;
    public float GetDestinationRotation() => destinationRotationZ;

    public void SetPosition(float destinationZ, float moveSpeed)
    {
        destinationPosZ = destinationZ;
        this.moveSpeed = moveSpeed;
    }
}
