using System;
using Beatmap.Base;

public abstract class StateData<T> : IEquatable<StateData<T>> where T : BaseObject
{
    private static int ID;
    private readonly int id = ID++; // maybe reference equality is better, idk

    protected StateData(T @base) => Base = @base;

    public readonly T Base;
    public float StartTime = float.MinValue;
    public float EndTime = float.MaxValue;

    public bool Equals(StateData<T> other) => id == other!.id;
    public bool IsWithinRange(float value) => StartTime <= value && value < EndTime;
}
