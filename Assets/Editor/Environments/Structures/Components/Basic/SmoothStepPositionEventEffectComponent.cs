using UnityEngine;

public class SmoothStepPositionEventEffectComponent
{
    public bool IsEnabled;

    public string EventType;
    public bool ClampValue;
    public int MinY;
    public int MaxY;
    public Vector3 MovementVector;
    public float StepSize;
    public string EaseType;

    public void CopyTo(SmoothStepPositionEventEffect target)
    {
        target.ClampValue = ClampValue;
        target.MinY = MinY;
        target.MaxY = MaxY;
        target.MovementVector = MovementVector;
        target.StepSize = StepSize;
        target.EaseType = EaseType;
    }
}
