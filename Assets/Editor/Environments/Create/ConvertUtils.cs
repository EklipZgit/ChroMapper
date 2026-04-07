using System;
using UnityEngine;

public static class ConvertUtils
{
    public static int ToEventType(string val)
    {
        return val switch
        {
            "Event0" => 0,
            "Event1" => 1,
            "Event2" => 2,
            "Event3" => 3,
            "Event4" => 4,
            "Event5" => 5,
            "Event6" => 6,
            "Event7" => 7,
            "Event8" => 8,
            "Event9" => 9,
            "Event10" => 10,
            "Event11" => 11,
            "Event12" => 12,
            "Event13" => 13,
            "Event14" => 14,
            "Event15" => 15,
            "Event16" => 16,
            "Event17" => 17,
            "Event18" => 18,
            "Event19" => 19,
            "Event20" => 20,
            "Event21" => 21,
            "VoidEvent" => -1,
            "Special0" => 40,
            "Special1" => 41,
            "Special2" => 42,
            "Special3" => 43,
            "BpmChange" => 100,
            _ => throw new Exception("Unknown event or new?: " + val)
        };
    }

    public static bool ToEventType(string val, out int res)
    {
        try
        {
            res = ToEventType(val);
        }
        catch
        {
            res = -1;
            return false;
        }

        return true;
    }

    public static BasicEventKind ToEventKind(string val)
    {
        return val switch
        {
            "None" => BasicEventKind.None,
            "Lights" => BasicEventKind.Lights,
            "Toggle" => BasicEventKind.Toggle,
            "FloatValue" => BasicEventKind.FloatValue,
            "IntValue" => BasicEventKind.IntValue,
            "BtsCharacterSelection" => BasicEventKind.BtsCharacter,
            "CarSelection" => BasicEventKind.Car,
            _ => throw new Exception("Unknown toolbar type: " + val)
        };
    }

    public static RotationStepType ToRotationStepType(string val) =>
        Enum.TryParse<RotationStepType>(val, out var result) ? result : RotationStepType.Range;

    public static Vector2 ToVector2(float[] ary) => new(ary[0], ary[1]);
    public static Vector4 ToVector4(float[] ary) => new(ary[0], ary[1], ary[2], ary[3]);
    public static Color ToColor(float[] ary) => new(ary[0], ary[1], ary[2], ary[3]);
}
