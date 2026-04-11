using UnityEngine;

public class SmoothStepPositionEventEffectData : EnvironmentComponentData<SmoothStepPositionEventEffect>
{
    public string EventType;
    public bool ClampValue;
    public int MinY;
    public int MaxY;
    public Vector3 MovementVector;
    public float StepSize;

    public override void SearchAndFillComponents(
        GameObject self,
        SmoothStepPositionEventEffect comp,
        CreateContainer container)
    {
    }

    public override void CopyTo(SmoothStepPositionEventEffect comp)
    {
        comp.ClampValue = ClampValue;
        comp.MinY = MinY;
        comp.MaxY = MaxY;
        comp.MovementVector = MovementVector;
        comp.StepSize = StepSize;
    }
}
