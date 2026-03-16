using UnityEngine;

public static class GameObjectExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }

    public static void SetLayerRecursively(this GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform) child.gameObject.SetLayerRecursively(layer);
    }

    public static void DestroySafe(Object go)
    {
    #if UNITY_EDITOR
        if (Application.isPlaying)
            Object.Destroy(go);
        else
            Object.DestroyImmediate(go);
    #else
        Object.Destroy(go);
    #endif
    }
}
