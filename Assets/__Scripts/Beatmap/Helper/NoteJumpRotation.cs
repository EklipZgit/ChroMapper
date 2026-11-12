using UnityEngine;

public static class NoteJumpRotation
{
    // NoteJump.Init selects from this fixed table using song seconds and the note's floor offset.
    private static readonly Vector3[] middleRotationDirections =
    {
        new(-0.9543871f, -0.1183784f, 0.2741019f),
        new(0.7680854f, -0.08805521f, 0.6342642f),
        new(-0.6780157f, 0.306681f, -0.6680131f),
        new(0.1255014f, 0.9398643f, 0.3176546f),
        new(0.365105f, -0.3664974f, -0.8557909f),
        new(-0.8790653f, -0.06244748f, -0.4725934f),
        new(0.01886305f, -0.8065798f, 0.5908241f),
        new(-0.1455435f, 0.8901445f, 0.4318099f),
        new(0.07651193f, 0.9474725f, -0.3105508f),
        new(0.1306983f, -0.2508438f, -0.9591639f)
    };

    public static Vector3 GetMiddleRotationOffset(float noteSeconds, Vector2 floorOffset)
    {
        var index = Mathf.Abs(Mathf.RoundToInt(noteSeconds * 10f + floorOffset.x * 2f + floorOffset.y * 2f)
            % middleRotationDirections.Length);
        return middleRotationDirections[index] * 20f;
    }

    public static Quaternion Evaluate(Quaternion endRotation, Vector3 middleRotationOffset, float normalTime)
    {
        if (normalTime >= 0.5f)
        {
            return endRotation;
        }
        var middleRotation = Quaternion.Euler(endRotation.eulerAngles + middleRotationOffset);
        return normalTime < 0.125f
            ? Quaternion.Slerp(Quaternion.identity, middleRotation, Mathf.Sin(normalTime * Mathf.PI * 4f))
            : Quaternion.Slerp(middleRotation, endRotation, Mathf.Sin((normalTime - 0.125f) * Mathf.PI * 2f));
    }
}
