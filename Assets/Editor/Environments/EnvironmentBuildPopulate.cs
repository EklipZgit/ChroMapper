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

        library.Meshes.MarkForChange();
        library.Materials.MarkForChange();

        foreach (var dataPath in envDataPaths)
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            var data = JsonConvert.DeserializeObject<EnvData>(
                dataAsset.text,
                new Vector3ArrayConverter());

            Debug.Log($"Populating data from {data.Data.ID}");

            foreach (var m in data.Data.UniqueMeshes) library.Meshes.AddEntry(m, data.Data.ID);
            foreach (var m in data.Data.UniqueMaterials)
            {
                library.Materials.AddEntry(m, data.Data.ID);
                if (library.Shaders.All(s => s.name != m.Shader))
                    library.Shaders.Add(new ShaderEntry() { name = m.Shader });
            }

            foreach (var layerName in data.Objects.Select(x => x.Layer))
                library.layerMaskLookup.TryAdd(layerName, LayerMask.GetMask("Default"));
        }

        library.Meshes.RemoveUnused();
        library.Materials.RemoveUnused();
        
        library.Meshes.Sort();
        library.Materials.Sort();

        library.layerMaskRemap =
            library
                .layerMaskLookup
                .Select(x => new LayerMaskEntry { name = x.Key, layerMask = x.Value })
                .OrderBy(x => x.name)
                .ToList();


        foreach (var matInfo in library.Materials.list)
        {
            if (matInfo.Material == null)
            {
                var shader = Shader.Find("ChroMapper/Missing");
                if (TryGetShader(library.Shaders, matInfo.Shader, out var existingShader)) shader = existingShader;
                var mat = new Material(shader);

                if (matInfo.Environments.Count > 1)
                {
                    var targetPath = Path.Combine(graphicsPath, "Materials", "Environment", $"{matInfo.Name}.mat");
                    if (!AssetDatabase.AssetPathExists(targetPath)) AssetDatabase.CreateAsset(mat, targetPath);
                    else mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                }
                else
                {
                    var parentPath = Path.Combine(graphicsPath, "Materials", "Environment");
                    var env = matInfo.Environments[0].Replace("Environment", "");
                    var folderPath = Path.Combine(parentPath, env);
                    if (!AssetDatabase.AssetPathExists(folderPath)) AssetDatabase.CreateFolder(parentPath, env);

                    var targetPath = Path.Combine(folderPath, $"{matInfo.Name}.mat");
                    if (!AssetDatabase.AssetPathExists(targetPath)) AssetDatabase.CreateAsset(mat, targetPath);
                    else mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                }
                
                matInfo.Material = mat;
            }
            else if (matInfo.Material.shader.name == "ChroMapper/Missing")
            {
                if (TryGetShader(library.Shaders, matInfo.Shader, out var shader)) matInfo.Material.shader = shader;
            }
            
            matInfo.Material.SetColor("_Color", matInfo.Color);
        }
        
        AssetDatabase.ForceReserializeAssets(
            AssetDatabase
                .GetAllAssetPaths()
                .Where(x => x.StartsWith(Path.Combine(editorPath)) && x.EndsWith(".asset")),
            ForceReserializeAssetsOptions.ReserializeAssets);
    }

    private static bool TryGetShader(List<ShaderEntry> list, string shaderName, out Shader shader)
    {
        var entry = list.FirstOrDefault(x => x.name == shaderName);
        if (entry.shader == null)
        {
            shader = null;
            return false;
        }

        shader = entry.shader;
        return true;
    }
}
