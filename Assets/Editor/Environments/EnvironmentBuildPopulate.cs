using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class EnvironmentBuildPopulate
{
    [MenuItem("Environment/Populate Build Data", false, 800)]
    private static void PopulateBuildData()
    {
        var libraryPath = $"{Constants.EditorPath}/EnvironmentLibrarySO.asset";
        var library = AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(libraryPath);
        if (library == null)
        {
            Debug.LogError($"[EnvironmentTools] EnvironmentLibrarySO not found at '{libraryPath}'.");
            return;
        }

        library.MarkForChange();

        foreach (var data in CreateUtils.GetEnvironmentData())
        {
            Debug.Log($"Populating data from {data.Data.ID}");

            foreach (var m in data.Data.UniqueMeshes) library.Meshes.AddEntry(m, data.Data.ID);
            foreach (var m in data.Data.UniqueMaterials)
            {
                if (m.Shader == "Hidden/InternalErrorShader") continue;
                library.Materials.AddEntry(m, data.Data.ID);
                if (library.Shaders.All(s => s.name != m.Shader))
                    library.Shaders.Add(new ShaderEntry { name = m.Shader });
                var keywords = library.Shaders.Find(x => x.name == m.Shader).keywords;
                keywords.AddRange(m.Keywords.Where(x => !keywords.Contains(x)));
            }

            foreach (var m in data.Data.UniqueTextures) library.Textures.AddEntry(m.Hash, m.Name, data.Data.ID);

            foreach (var layerName in data.Objects.Select(x => x.Layer))
                library.LayerMaskLookup.TryAdd(layerName, LayerMask.GetMask("Default"));
        }

        library.RemoveUnused();
        library.Sort();

        foreach (var entry in library.Shaders)
        {
            if (entry.shader != null) continue;
            entry.shader = Shader.Find(entry.name);
            if (entry.shader == null)
                Debug.LogWarning($"[EnvironmentTools] Shader.Find('{entry.name}') returned null. This shader is not compiled into the project. Materials using it will show as purple until a Shader is assigned manually in EnvironmentLibrarySO.");
            else
                Debug.Log($"[EnvironmentTools] Auto-assigned shader '{entry.name}'.");
        }

        library.Initialize();

        var usedMaterialName = new Dictionary<string, int>();
        foreach (var matInfo in library.Materials.list)
        {
            if (matInfo.Material == null)
            {
                var shader = Shader.Find("ChroMapper/Missing");
                if (TryGetShader(library.Shaders, matInfo.Shader, out var existingShader))
                    shader = existingShader;
                else
                    Debug.LogWarning($"[EnvironmentTools] No Shader mapped for '{matInfo.Shader}' in EnvironmentLibrarySO.Shaders — material '{matInfo.Name}' will use ChroMapper/Missing (purple). Assign a Shader to this entry in the Inspector.");

                // Create new material with gpu instancing enabled
                // Shaders that dont support instancing should ignore the flag, but otherwise this should be free performance
                var mat = new Material(shader) { enableInstancing = true };

                var name = usedMaterialName.TryGetValue(matInfo.Name, out var n) && n > 0
                    ? matInfo.Name + n
                    : matInfo.Name;
                if (matInfo.Environments.Count > 1)
                {
                    var targetPath = $"{Constants.MaterialsPath}/{name}.mat";
                    mat = CreateUtils.CreateOrReplace(mat, targetPath);
                }
                else
                {
                    var environmentName = matInfo.Environments[0].Replace("Environment", "");
                    var targetPath = $"{Constants.MaterialsPath}/{environmentName}/{name}.mat";
                    mat = CreateUtils.CreateOrReplace(mat, targetPath);
                }

                usedMaterialName.TryAdd(name, 0);
                usedMaterialName[name]++;

                matInfo.Material = mat;
            }
            else if (matInfo.Material.shader.name == "ChroMapper/Missing")
            {
                if (TryGetShader(library.Shaders, matInfo.Shader, out var shader)) matInfo.Material.shader = shader;
            }

            MaterialProcessor.HandleProp(library, matInfo);
        }

        foreach (var unusedMaterial in AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Constants.MaterialsPath) && !x.Contains("Custom/"))
            .Select(AssetDatabase.LoadAssetAtPath<Material>)
            .Where(x => x != null)
            .Where(x => !library.Materials.list.Exists(y => y.Material == x)))
            AssetDatabase.RemoveObjectFromAsset(unusedMaterial);

        foreach (var obj in library
            .Materials.list.Select(x => x.Material)
            .Where(x => x != null)
            .Cast<Object>()
            .Concat(library.Textures.list.Select(x => x.Texture))
            .Where(x => x != null)
            .Append(library)
            .Append(library.Materials)
            .Append(library.Meshes)
            .Append(library.Textures)
            .Append(library.Sprites))
            EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssets();
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
