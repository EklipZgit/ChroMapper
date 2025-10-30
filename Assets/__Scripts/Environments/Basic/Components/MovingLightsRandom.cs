using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class MovingLightsRandom : MonoBehaviour
{
    [FormerlySerializedAs("startOffset")] public float StartOffset;
    internal float movementSpeed;

    protected bool OverrideRandomValues;
    protected int RandomGenerationFrameNum = -1;
    internal float randomStartOffset;

    protected bool UseZPositionForAngleOffset = false;
    protected float ZPositionAngleOffsetScale = 1f;

    public event Action OnStyleSwitched;

    public void SwitchStyle(bool b)
    {
        OverrideRandomValues = b;
        RandomUpdate(false);
        OnStyleSwitched?.Invoke();
    }

    public void RandomUpdate(bool leftEvent)
    {
        var frameCount = Time.frameCount;
        if (RandomGenerationFrameNum != frameCount)
        {
            if (OverrideRandomValues)
                randomStartOffset = 0f;
            else
                randomStartOffset = Random.Range(0.0f, 2 * (float)Math.PI);
            RandomGenerationFrameNum = Time.frameCount;
        }
    }
}
