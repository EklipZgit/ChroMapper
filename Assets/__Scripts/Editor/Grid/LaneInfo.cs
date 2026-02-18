using System;

public class LaneInfo : IComparable
{
    public readonly int Type;
    public string Name;

    public LaneInfo(int index, int type)
    {
        Type = type;
        Index = index;
    }

    public int Index { get; }

    public int CompareTo(object obj)
    {
        if (obj is LaneInfo other) return Type - other.Type;
        return 0;
    }
}
