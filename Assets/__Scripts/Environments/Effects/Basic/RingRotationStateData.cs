using Beatmap.Base;

public class RingRotationStateData : BasicEventStateData
{
    // unfortunately, you cannot modulo this out, so there's a chance this can overflow
    public float RotationInitial;
    public float RotationChange;
    public bool Direction;

    public RingRotationStateData(BaseEvent data) : base(data)
    {
    }
}
