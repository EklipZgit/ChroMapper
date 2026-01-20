using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
        library.Sprites.MarkForChange();
        foreach (var s in library.Shaders) s.keywords.Clear();

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
                    library.Shaders.Add(new ShaderEntry { name = m.Shader });
                var keywords = library.Shaders.Find(x => x.name == m.Shader).keywords;
                keywords.AddRange(m.Keywords.Where(x => !keywords.Contains(x)));
            }

            foreach (var o in data.Objects.Where(x => x.Components.SpriteLightWithId != null))
            {
                var t = o.Components.SpriteLightWithId;
                foreach (var r in t)
                {
                    if (r.Sprite == null)
                    {
                        Debug.LogWarning($"Could not get sprite in {o.ChromaID}");
                        continue;
                    }

                    library.Sprites.AddEntry(r.Sprite.TextureName, data.Data.ID);
                }
            }

            foreach (var layerName in data.Objects.Select(x => x.Layer))
                library.LayerMaskLookup.TryAdd(layerName, LayerMask.GetMask("Default"));
        }

        library.Meshes.RemoveUnused();
        library.Materials.RemoveUnused();
        library.Sprites.RemoveUnused();

        library.Meshes.Sort();
        library.Materials.Sort();
        library.Sprites.Sort();
        foreach (var s in library.Shaders)
            s.keywords.Sort((a, b) => string.Compare(a.Replace("_", ""), b.Replace("_", ""), StringComparison.Ordinal));
        library.Shaders.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        library.layerMaskRemap =
            library
                .LayerMaskLookup
                .Select(x => new LayerMaskEntry { name = x.Key, layerMask = x.Value })
                .OrderBy(x => x.name)
                .ToList();

        var shaderPropRemap = new Dictionary<string, string>()
        {
            { "_BlendSrcFactor", "_BlendModeSrc" },
            { "_BlendDstFactor", "_BlendModeDst" },
            { "_BlendSrcFactorA", "_BlendModeSrcA" },
            { "_BlendDstFactorA", "_BlendModeDstA" },
            { "_WhiteBoostMultiplier", "_BloomWhiteMultiplier" }
        };
        foreach (var matInfo in library.Materials.list)
        {
            if (matInfo.Material == null)
            {
                var shader = Shader.Find("ChroMapper/Missing");
                if (TryGetShader(library.Shaders, matInfo.Shader, out var existingShader)) shader = existingShader;

                // Create new material with gpu instancing enabled
                // Shaders that dont support instancing should ignore the flag, but otherwise this should be free performance
                var mat = new Material(shader) { enableInstancing = true };

                if (matInfo.Environments.Count > 1)
                {
                    var targetPath = Path.Combine(graphicsPath, "Materials", "Environment", $"{matInfo.Name}.mat");
                    if (!AssetDatabase.AssetPathExists(targetPath))
                        AssetDatabase.CreateAsset(mat, targetPath);
                    else
                        mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                }
                else
                {
                    var parentPath = Path.Combine(graphicsPath, "Materials", "Environment");
                    var env = matInfo.Environments[0].Replace("Environment", "");
                    var folderPath = Path.Combine(parentPath, env);
                    if (!AssetDatabase.AssetPathExists(folderPath)) AssetDatabase.CreateFolder(parentPath, env);

                    var targetPath = Path.Combine(folderPath, $"{matInfo.Name}.mat");
                    if (!AssetDatabase.AssetPathExists(targetPath))
                        AssetDatabase.CreateAsset(mat, targetPath);
                    else
                        mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                }

                matInfo.Material = mat;
            }
            else if (matInfo.Material.shader.name == "ChroMapper/Missing")
            {
                if (TryGetShader(library.Shaders, matInfo.Shader, out var shader)) matInfo.Material.shader = shader;
            }

            matInfo.Material.SetColor("_Color", matInfo.Color);

            foreach (var floatProp in matInfo.FloatProps)
            {
                var renamedKey = shaderPropRemap.GetValueOrDefault(floatProp.Key, floatProp.Key);
                matInfo.Material.SetFloat(renamedKey, floatProp.Value);
            }

            matInfo.Material.SetFloat("_EnableDiffuse", matInfo.Keywords.Contains("DIFFUSE") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableSpecular", matInfo.Keywords.Contains("SPECULAR") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableRimDim", matInfo.Keywords.Contains("ENABLE_RIM_DIM") ? 1f : 0f);
            matInfo.Material.SetFloat("_InvertRimDim", matInfo.Keywords.Contains("INVERT_RIM_DIM") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_EnablePrivatePointLight",
                matInfo.Keywords.Contains("PRIVATE_POINT_LIGHT") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_PointLightPositionLocal",
                matInfo.Keywords.Contains("POINT_LIGHT_IS_LOCAL") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_EnableHeightFog",
                matInfo.Keywords.Contains("HEIGHT_FOG") || matInfo.Keywords.Contains("ENABLE_HEIGHT_FOG") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_EnableAlphaWidthScale",
                matInfo.Keywords.Contains("ALPHA_WIDTH_SCALE") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_MultiplyColorWithAlpha",
                matInfo.Keywords.Contains("MULTIPLY_COLOR_WITH_ALPHA") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_EnableYAxisBillboard",
                matInfo.Keywords.Contains("ENABLE_Y_AXIS_BILLBOARD") ? 1f : 0f);
            matInfo.Material.SetFloat("_SquareAlpha", matInfo.Keywords.Contains("SQUARE_ALPHA") ? 1f : 0f);
            matInfo.Material.SetFloat("_UseFogForLights", matInfo.Keywords.Contains("USE_FOR_FOR_LIGHTS") ? 1f : 0f);

            if (matInfo.Keywords.Contains("_BILLBOARD_FULL"))
                matInfo.Material.SetFloat("_Billboard", 1f);
            else if (matInfo.Keywords.Contains("_BILLBOARD_Y_AXIS"))
                matInfo.Material.SetFloat("_Billboard", 2f);
            else if (matInfo.Keywords.Contains("_BILLBOARD_CAMERA_FACING"))
                matInfo.Material.SetFloat("_Billboard", 3f);
            else
                matInfo.Material.SetFloat("_Billboard", 0f);

            if (matInfo.Keywords.Contains("_ACES_APPROACH_AFTER_EMISSIVE"))
                matInfo.Material.SetFloat("_AcesTonemap", 1f);
            else if (matInfo.Keywords.Contains("_ACES_APPROACH_BEFORE_EMISSIVE"))
                matInfo.Material.SetFloat("_AcesTonemap", 0f);

            if (matInfo.Keywords.Contains("_WHITEBOOSTTYPE_MAINEFFECT")
                || matInfo.Keywords.Contains("_ENABLE_MAIN_EFFECT_WHITE_BOOST"))
                matInfo.Material.SetFloat("_BloomWhite", 1f);
            else if (matInfo.Keywords.Contains("_WHITEBOOSTTYPE_ALWAYS"))
                matInfo.Material.SetFloat("_BloomWhite", 2f);
            else
                matInfo.Material.SetFloat("_BloomWhite", 0f);
        }

        foreach (var obj in library
            .Materials.list.Select(x => x.Material)
            .Cast<Object>()
            .Append(library)
            .Append(library.Materials)
            .Append(library.Meshes)
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
