using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class EnvironmentListUpdate
{
    private const string environmentPath = "Assets/__Scenes/Environments";
    private const string scriptPath = "Assets/__Scripts/Environments";

    [MenuItem("Environment/Update Environment List", false, 800)]
    private static void PopulateBuildData()
    {
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Path.Combine(environmentPath, "Data")) && x.EndsWith(".json"));

        var listSo =
            AssetDatabase.LoadAssetAtPath<EnvironmentListSO>(Path.Combine(scriptPath, "EnvironmentListSO.asset"));

        listSo.list.Clear();
        
        foreach (var dataPath in envDataPaths)
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            var data = JsonConvert.DeserializeObject<EnvironmentData>(
                dataAsset.text,
                new Vector3ArrayConverter());
            
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                Path.Combine(environmentPath, data.Data.ID + ".unity"));
            
            if (scene == null) continue;
            
            listSo.list.Add(new EnvironmentListInfo
            {
                Name = data.Data.Title,
                ID = data.Data.ID
            });
        }

        AssetDatabase.ForceReserializeAssets(
            new[] { AssetDatabase.GetAssetPath(listSo) },
            ForceReserializeAssetsOptions.ReserializeAssets);
    }
}
