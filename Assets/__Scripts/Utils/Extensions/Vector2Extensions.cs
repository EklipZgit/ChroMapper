using UnityEngine;

internal static class Vector2Extensions
{
    public static Vector2 Repeat(this Vector2 vector, float value)
    {
        vector.x = Mathf.Repeat(vector.x, value);
        vector.y = Mathf.Repeat(vector.y, value);
        return vector;
    }
}
