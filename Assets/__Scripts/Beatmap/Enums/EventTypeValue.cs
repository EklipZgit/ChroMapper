namespace Beatmap.Enums
{
    /// <summary>
    /// Basic Event numbers are environment-defined, so use neutral names instead of legacy component assumptions.
    /// These dont have consistent meaning except ColorBoost, LegacyBpmEvent, EarlyRotationEvent, LateRotatationEvent.
    /// This is the exact terminology used by the game in CLR.
    /// </summary>
    public enum EventTypeValue
    {
        Event0 = 0,
        Event1 = 1,
        Event2 = 2,
        Event3 = 3,
        Event4 = 4,
        Event5 = 5,
        Event6 = 6,
        Event7 = 7,
        Event8 = 8,
        Event9 = 9,
        Event10 = 10,
        Event11 = 11,
        Event12 = 12,
        Event13 = 13,
        Event14 = 14,
        Event15 = 15,
        Event16 = 16,
        Event17 = 17,
        VoidEvent = -1,
        Special0 = 40,
        Special1 = 41,
        Special2 = 42,
        Special3 = 43,
        BpmChange = 100,
        ColorBoostEventType = Event5,
        LegacyBpmEventType = Event10,
        EarlyRotationEventType = Event14,
        LateRotationEventType = Event15
    }
}
