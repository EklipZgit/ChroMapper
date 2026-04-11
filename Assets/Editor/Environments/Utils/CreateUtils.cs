using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class CreateUtils
{
    public static IEnumerable<EnvironmentData> GetEnvironmentData()
    {
        return GetEnvironmentDataRaw()
            .Select(JsonToEnvironmentData);
    }

    public static IEnumerable<TextAsset> GetEnvironmentDataRaw()
    {
        return AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Constants.EnvironmentDataPath) && x.EndsWith(".json"))
            .Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
            .Where(x => x != null);
    }

    public static EnvironmentData JsonToEnvironmentData(TextAsset textAsset) =>
        JsonConvert.DeserializeObject<EnvironmentData>(
            textAsset.text,
            new Vector2ArrayConverter(),
            new Vector3ArrayConverter(),
            new Vector4ArrayConverter(),
            new ColorArrayConverter());

    public static T CreateOrReplace<T>(T obj, string path) where T : Object
    {
        if (!AssetDatabase.AssetPathExists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetDatabase.CreateAsset(new Mesh(), path);
        }
        else
            obj = AssetDatabase.LoadAssetAtPath<T>(path);

        return obj;
    }

    public static GameObject GetGameObjectOrNull(
        Dictionary<string, GameObject> chromaIdObjects,
        string id,
        GameObject go)
    {
        if (id == "self") return go;
        return string.IsNullOrEmpty(id) ? null : chromaIdObjects.GetValueOrDefault(id);
    }

    public static bool TryGetGameObjectOrNull(
        Dictionary<string, GameObject> chromaIdObjects,
        string id,
        GameObject dgo,
        out GameObject go)
    {
        if (id == "self")
        {
            go = dgo;
            return true;
        }

        if (!string.IsNullOrEmpty(id)) return chromaIdObjects.TryGetValue(id, out go);
        go = null;
        return false;
    }
}

public class CreateContainer
{
    public EnvironmentLibrarySO Library;
    public Dictionary<string, GameObject> ChromaIdObjects;

    public GameObject GetGameObjectOrNull(string n) => CreateUtils.GetGameObjectOrNull(ChromaIdObjects, n, null);

    public GameObject GetGameObjectOrNull(string n, GameObject self) =>
        CreateUtils.GetGameObjectOrNull(ChromaIdObjects, n, self);

    public bool TryGetGameObjectOrNull(string n, GameObject self, out GameObject go) =>
        CreateUtils.TryGetGameObjectOrNull(ChromaIdObjects, n, self, out go);
}
