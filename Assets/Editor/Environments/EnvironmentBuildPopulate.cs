using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class EnvironmentBuildPopulate
{
    private const string editorPath = "Assets/Editor/Environments";
    private const string graphicsPath = "Assets/_Graphics";
    private const string environmentPath = "Assets/__Scenes/Environments";

    [MenuItem("Environment/Populate Build Data", false, 800)]
    private static void PopulateBuildData()
    {
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Path.Combine(environmentPath, "Data")) && x.EndsWith(".json"));

        var library =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(Path.Combine(editorPath, "EnvironmentLibrarySO.asset"));
        var materialsToAdd = new Dictionary<string, List<string>>();

        var pairs = GetOrCreateSOs(envDataPaths);
        foreach (var (data, build) in pairs)
        {
            Debug.Log($"Populating data for {data.Data.ID}");

            foreach (var m in data.Data.UniqueMeshes) build.AddMeshEntry(m);
            foreach (var m in data.Data.UniqueMaterials) build.AddMaterialEntry(m.Name);
            build.Sort();

            foreach (var se in build
                .materials
                .Where(x => x.Value == null)
                .Select(x => x.Name))
            {
                materialsToAdd.TryAdd(se, new());
                materialsToAdd[se].Add(data.Data.ID.Replace("Environment", ""));
            }

            foreach (var se in data.Objects.Select(x => x.Layer))
                library.layerMaskLookup.TryAdd(se, LayerMask.GetMask("Default"));
        }

        library.layerMaskRemap =
            library
                .layerMaskLookup
                .Select(x => new LayerMaskEntry { name = x.Key, layerMask = x.Value })
                .OrderBy(x => x.name)
                .ToList();

        // Create missing materials
        var materialNameToAsset = new Dictionary<string, Material>();
        foreach (var (materialName, paths) in materialsToAdd)
        {
            var mat = new Material(Shader.Find("ChroMapper/Missing"));
            if (paths.Count > 1)
            {
                var targetPath = Path.Combine(graphicsPath, "Materials", "Environment", $"{materialName}.mat");
                if (!AssetDatabase.AssetPathExists(targetPath)) AssetDatabase.CreateAsset(mat, targetPath);
                materialNameToAsset.Add(materialName, AssetDatabase.LoadAssetAtPath<Material>(targetPath));
            }
            else
            {
                var parentPath = Path.Combine(graphicsPath, "Materials", "Environment");
                var folderPath = Path.Combine(parentPath, paths[0]);
                if (!AssetDatabase.AssetPathExists(folderPath)) AssetDatabase.CreateFolder(parentPath, paths[0]);

                var targetPath = Path.Combine(folderPath, $"{materialName}.mat");
                if (!AssetDatabase.AssetPathExists(targetPath)) AssetDatabase.CreateAsset(mat, targetPath);
                materialNameToAsset.Add(materialName, AssetDatabase.LoadAssetAtPath<Material>(targetPath));
            }
        }

        // Apply missing materials to build
        foreach (var (_, build) in pairs)
        {
            foreach (var entry in build.materials)
            {
                if (materialNameToAsset.TryGetValue(entry.Name, out var value)) entry.Value = value;
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static List<(EnvironmentData data, EnvironmentBuildSO build)> GetOrCreateSOs(IEnumerable<string> dataPaths)
    {
        Debug.Log("Creating build SO from data folder");
        var list = new List<(EnvironmentData data, EnvironmentBuildSO build)>();
        foreach (var dataPath in dataPaths)
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            var data = JsonConvert.DeserializeObject<EnvironmentData>(
                dataAsset.text,
                new Vector3ArrayConverter());
            var targetPath = Path.Combine(environmentPath, "Data", $"{dataAsset.name}BuildSO.asset");

            var exist = AssetDatabase.AssetPathExists(targetPath);
            var build = exist
                ? AssetDatabase.LoadAssetAtPath<EnvironmentBuildSO>(targetPath)
                : ScriptableObject.CreateInstance<EnvironmentBuildSO>();

            if (!exist) AssetDatabase.CreateAsset(build, targetPath);
            list.Add((data, build));
        }

        return list;
    }
}
