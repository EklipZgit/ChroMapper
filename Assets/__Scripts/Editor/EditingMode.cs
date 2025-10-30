using System;

[Flags]
public enum EditingMode : byte
{
    None = 0,
    Gameplay = 1 << 0,
    GLS = 1 << 2,
    BasicEvent = 1 << 3,
}
