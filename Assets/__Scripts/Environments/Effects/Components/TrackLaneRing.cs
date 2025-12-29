using UnityEngine;

public class TrackLaneRing : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset;

    private float previousRotZ;
    private float rotationZ;
    private float destinationRotationZ;

    private float previousPosZ;
    private float positionZ;
    private float destinationPosZ;

    private float rotateSpeed;
    private float moveSpeed;

    public void DoReset()
    {
        rotationZ = 0;
        previousRotZ = 0;
        destinationRotationZ = 0;
        rotateSpeed = 0;
    }

    public void Init(Vector3 pos, Vector3 posOffset)
    {
        positionOffset = posOffset;
        transform.localPosition = pos + positionOffset;
        previousPosZ = positionZ = pos.z + positionOffset.z;
        rotationZ = destinationRotationZ = transform.localPosition.z;
    }

    public void FixedUpdateRing(float fixedDeltaTime)
    {
        previousRotZ = rotationZ;
        rotationZ = Mathf.Lerp(rotationZ, destinationRotationZ, fixedDeltaTime * rotateSpeed);

        previousPosZ = positionZ;
        positionZ = Mathf.Lerp(positionZ, positionOffset.z + destinationPosZ, fixedDeltaTime * moveSpeed);
    }

    public void LateUpdateRing(float interpolationFactor)
    {
        transform.localEulerAngles = new Vector3(
            0,
            0,
            previousRotZ + ((rotationZ - previousRotZ) * interpolationFactor));
        transform.localPosition = new Vector3(
            positionOffset.x,
            positionOffset.y,
            previousPosZ + ((positionZ - previousPosZ) * interpolationFactor));
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
