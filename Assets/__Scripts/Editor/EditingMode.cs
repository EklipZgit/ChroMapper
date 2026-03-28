using System;

[Flags]
public enum EditingMode : byte
{
    Gameplay = 1 << 0,
    GLS = 1 << 1,
    BasicEvent = 1 << 2,
    EventBox = 1 << 3
}

public enum EditingModeNoFlag
{
    Gameplay,
    GLS,
    BasicEvent,
    EventBox
}
