using UnityEngine;

internal static class Vector3Extensions
{
    public static Vector3 Repeat(this Vector3 vector, float value)
    {
        vector.x = Mathf.Repeat(vector.x, value);
        vector.y = Mathf.Repeat(vector.y, value);
        vector.z = Mathf.Repeat(vector.z, value);
        return vector;
    }
}
