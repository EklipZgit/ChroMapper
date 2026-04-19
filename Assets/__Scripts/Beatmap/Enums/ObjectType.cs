using System;

namespace Beatmap.Enums
{
    [Flags]
    public enum ObjectType
    {
        Note = 1 << 0,
        Event = 1 << 1,
        Obstacle = 1 << 2,
        CustomNote = 1 << 3,
        CustomEvent = 1 << 4,
        BpmChange = 1 << 5,
        Arc = 1 << 6,
        Chain = 1 << 7,
        Bookmark = 1 << 8,
        Waypoint = 1 << 9,
        NJSEvent = 1 << 10,
        EnvironmentEnhancement = 1 << 11,
        GLSColor = 1 << 12,
        GLSRotation = 1 << 13,
        GLSTranslation = 1 << 14,
        GLSFloatFx = 1 << 15,
        GLSEvent = 1 << 16,

        // Filters for bitmask operations
        // (Can add or remove these as needed)
        FilterNone = 0,
        FilterAll = ~0,
    }
}
