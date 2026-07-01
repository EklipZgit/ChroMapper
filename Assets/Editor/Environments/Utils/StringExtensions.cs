using UnityEditor;
using UnityEngine;

public static class StringExtensions
{
    public static T GetOrCreateScriptableObject<T>(this string path) where T : ScriptableObject
    {
        T asset;
        if (AssetDatabase.AssetPathExists(path))
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
        else
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }

        return asset;
    }
}
