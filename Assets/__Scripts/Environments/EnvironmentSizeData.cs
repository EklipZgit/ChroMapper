using System;

[Serializable]
public sealed class EnvironmentSizeData
{
    public FloorType FloorType;
    public CeilingType CeilingType;
    public TrackLaneType TrackLaneType;
}

public enum FloorType
{
    NoFloor,
    CloseTo0
}

public enum CeilingType
{
    NoCeiling,
    LowCeiling
}

public enum TrackLaneType
{
    None,
    Normal
}
