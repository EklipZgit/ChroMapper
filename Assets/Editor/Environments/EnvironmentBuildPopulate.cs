using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class EnvironmentBuildPopulate
{
    private const string environmentPath = "Assets/__Scenes/Environments";

    [MenuItem("Environment/Populate Build Data", false, 800)]
    private static void PopulateBuildData()
    {
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Path.Combine(environmentPath, "Data")) && x.EndsWith(".json"));

        foreach (var envDataPath in envDataPaths)
        {
            var envDataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(envDataPath);
            var envData = JsonConvert.DeserializeObject<EnvironmentData>(
                envDataAsset.text,
                new Vector3ArrayConverter());

            var targetPath = Path.Combine(environmentPath, "Data", $"{envDataAsset.name}BuildSO.asset");
            Debug.Log($"Populating data from {envDataPath} to {targetPath}");

            var exist = AssetDatabase.AssetPathExists(targetPath);
            var envBuild = exist
                ? AssetDatabase.LoadAssetAtPath<EnvironmentBuildSO>(targetPath)
                : ScriptableObject.CreateInstance<EnvironmentBuildSO>();

            PopulateData(envData, envBuild);

            if (!exist) AssetDatabase.CreateAsset(envBuild, targetPath);
        }
        AssetDatabase.SaveAssets();
    }

    private static void PopulateData(EnvironmentData data, EnvironmentBuildSO envBuild)
    {
        foreach (var m in data.Data.UniqueMeshes) envBuild.AddMeshEntry(m);
        foreach (var m in data.Data.UniqueMaterials) envBuild.AddMaterialEntry(m.Name);
    }
}
