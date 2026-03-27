using System;

[Flags]
public enum EditingMode : byte
{
    None = 0,
    Gameplay = 1 << 0,
    GLS = 1 << 1,
    BasicEvent = 1 << 2,
    EventBox = 1 << 3,
}

public enum EditingModeNoFlag
{
    None,
    Gameplay,
    GLS,
    BasicEvent,
    EventBox,
}
