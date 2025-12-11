using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment List", fileName = "EnvironmentListSO")]
public class EnvironmentListSO : ScriptableObject
{
    private const string environmentPath = "Assets/__Scenes/Environments";
    private const string editorPath = "Assets/Editor/Environments";

    [SerializeField] public List<EnvironmentListInfo> list = new();

    public readonly Dictionary<string, EnvironmentListInfo> LookupID = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        LookupID.Clear();
        foreach (var entry in list) LookupID[entry.ID] = entry;
    }


    [MenuItem("Environment/Update Environment List", false, 800)]
    private static void PopulateBuildData()
    {
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Path.Combine(environmentPath, "Data")) && x.EndsWith(".json"));

        var listSo =
            AssetDatabase.LoadAssetAtPath<EnvironmentListSO>(Path.Combine(editorPath, "EnvironmentListSO.asset"));

        listSo.list.Clear();
        
        foreach (var dataPath in envDataPaths)
        {
            // why does it have to be in 2 different assembly
            // where my ref
            // var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            // var data = JsonConvert.DeserializeObject<EnvironmentData>(
            //     dataAsset.text,
            //     new Vector3ArrayConverter());
            //
            // var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
            //     Path.Combine(environmentPath, data.Data.ID + ".unity"));
            //
            // if (scene == null) continue;
            //
            // listSo.list.Add(new EnvironmentListInfo
            // {
            //     Name = data.Data.Title,
            //     ID = data.Data.ID
            // });
        }

        AssetDatabase.ForceReserializeAssets(
            new[] { AssetDatabase.GetAssetPath(listSo) },
            ForceReserializeAssetsOptions.ReserializeAssets);
    }
}

// either this goes into same file
[Serializable]
public class EnvironmentListInfo
{
    public string Name;
    public string ID;
    public PlatformColorScheme ColorScheme;
}
