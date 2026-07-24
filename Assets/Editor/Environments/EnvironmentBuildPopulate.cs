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
        // AssetDatabase always reports forward-slash paths, including on Windows.
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(PathUtils.Combine(environmentPath, "Data")) && x.EndsWith(".json"))
            .ToList();

        // Abort before marking entries unused so a path regression cannot silently empty the generated libraries.
        if (envDataPaths.Count == 0)
        {
            const string message = "Populate Build Data found no environment JSON assets; generated libraries were not changed.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        // Unity asset loading requires normalized project-relative paths on every host platform.
        var library =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(PathUtils.Combine(editorPath, "EnvironmentLibrarySO.asset"));

        // Fail explicitly instead of producing a partial refresh when the library asset cannot be resolved.
        if (library == null)
        {
            const string message = "Populate Build Data could not load EnvironmentLibrarySO.asset.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        // Validate every source before mutating generated assets so one unreadable file cannot leave a partial refresh.
        var environmentData = new List<EnvData>(envDataPaths.Count);
        foreach (var dataPath in envDataPaths)
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            if (dataAsset == null)
            {
                var message = $"Populate Build Data could not load '{dataPath}'.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            var data = JsonConvert.DeserializeObject<EnvData>(
                dataAsset.text,
                new Vector3ArrayConverter());
            if (data?.Data == null)
            {
                var message = $"Populate Build Data could not deserialize '{dataPath}'.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            environmentData.Add(data);
        }

        library.Meshes.MarkForChange();
        library.Materials.MarkForChange();
        library.Sprites.MarkForChange();
        foreach (var s in library.Shaders)
            s.keywords.Clear();

        foreach (var data in environmentData)
        {
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
            { "_WhiteBoostMultiplier", "_BloomWhiteMultiplier" },
            { "_ThresholdAngle", "_EmissionThresholdAngle" },
            { "_Rotate_UV", "_RotateUV" },
            { "_RimCameraDistanceOffset", "_RimDistanceOffset" },
            { "_RimCameraDistanceScale", "_RimDistanceScale" }
        };

        var usedMaterialName = new Dictionary<string, int>();
        foreach (var matInfo in library.Materials.list)
        {
            if (matInfo.Material == null)
            {
                var shader = Shader.Find("ChroMapper/Missing");
                if (TryGetShader(library.Shaders, matInfo.Shader, out var existingShader)) shader = existingShader;

                // Create new material with gpu instancing enabled
                // Shaders that dont support instancing should ignore the flag, but otherwise this should be free performance
                var mat = new Material(shader) { enableInstancing = true };

                var name = usedMaterialName.TryGetValue(matInfo.Name, out var n) && n > 0
                    ? matInfo.Name + n
                    : matInfo.Name;
                if (matInfo.Environments.Count > 1)
                {
                    // Asset creation and lookup paths must use Unity's forward-slash convention.
                    var targetPath = PathUtils.Combine(graphicsPath, "Materials", "Environment", $"{name}.mat");
                    if (!AssetDatabase.AssetPathExists(targetPath))
                        AssetDatabase.CreateAsset(mat, targetPath);
                    else
                        mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                }
                else
                {
                    // Keep every folder and material path compatible with AssetDatabase on Windows.
                    var parentPath = PathUtils.Combine(graphicsPath, "Materials", "Environment");
                    var env = matInfo.Environments[0].Replace("Environment", "");
                    var folderPath = PathUtils.Combine(parentPath, env);
                    if (!AssetDatabase.AssetPathExists(folderPath)) AssetDatabase.CreateFolder(parentPath, env);

                    var targetPath = PathUtils.Combine(folderPath, $"{name}.mat");
                    if (!AssetDatabase.AssetPathExists(targetPath))
                        AssetDatabase.CreateAsset(mat, targetPath);
                    else
                        mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                }

                usedMaterialName.TryAdd(name, 0);
                usedMaterialName[name]++;

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

            foreach (var vectorProp in matInfo.VectorProps)
            {
                var renamedKey = shaderPropRemap.GetValueOrDefault(vectorProp.Key, vectorProp.Key);
                matInfo.Material.SetVector(renamedKey, vectorProp.Value);
            }

            matInfo.Material.SetFloat(
                "_EnableSecondaryColor",
                matInfo.Keywords.Contains("SECONDARY_COLOR") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_UseColorGradient",
                matInfo.Keywords.Contains("COLOR_GRADIENT") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_UseSpectrogram",
                matInfo.Keywords.Contains("SPECTROGRAM_COLOR") ? 1f : 0f);

            if (matInfo.Keywords.Contains("_SECONDARY_UVS_IMPORT"))
                matInfo.Material.SetFloat("_Secondary_UVs", 1f);
            else if (matInfo.Keywords.Contains("_SECONDARY_UVS_EXTERNAL_SCALE"))
                matInfo.Material.SetFloat("_Secondary_UVs", 2f);
            else if (matInfo.Keywords.Contains("_SECONDARY_UVS_OBJECT_SPACE"))
                matInfo.Material.SetFloat("_Secondary_UVs", 3f);
            else if (matInfo.Keywords.Contains("_SECONDARY_UVS_ADDITIVE_OFFSET"))
                matInfo.Material.SetFloat("_Secondary_UVs", 4f);
            else
                matInfo.Material.SetFloat("_Secondary_UVs", 0f);

            matInfo.Material.SetFloat(
                "_EnableMetalSmoothnessTex",
                matInfo.Keywords.Contains("METAL_SMOOTHNESS_TEXTURE") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_METALLIC_TEXTURE_MPM_R"))
                matInfo.Material.SetFloat("_Metallic_Texture_Source", 1f);
            else if (matInfo.Keywords.Contains("_METALLIC_TEXTURE_MPM_A"))
                matInfo.Material.SetFloat("_Metallic_Texture_Source", 2f);
            else
                matInfo.Material.SetFloat("_Metallic_Texture_Source", 0f);
            if (matInfo.Keywords.Contains("_SMOOTHNESS_TEXTURE_MPM_A"))
                matInfo.Material.SetFloat("_Smoothness_Texture_Source", 1f);
            else if (matInfo.Keywords.Contains("_SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS"))
                matInfo.Material.SetFloat("_Smoothness_Texture_Source", 2f);
            else
                matInfo.Material.SetFloat("_Smoothness_Texture_Source", 0f);
            matInfo.Material.SetFloat("_PreciseNormal", matInfo.Keywords.Contains("PRECISE_NORMAL") ? 1f : 0f);

            matInfo.Material.SetFloat("_EnableVertexColor", matInfo.Keywords.Contains("VERTEX_COLOR") ? 1f : 0f);
            matInfo.Material.SetFloat("_SquareVertexAlpha", matInfo.Keywords.Contains("VERTEX_SQUARE_ALPHA") ? 1f : 0f);
            matInfo.Material.SetFloat("_RedIsVertexAlpha", matInfo.Keywords.Contains("VERTEX_RED_IS_ALPHA") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_VERTEXCHANNELS_A"))
                matInfo.Material.SetFloat("_VertexChannels", 1f);
            else if (matInfo.Keywords.Contains("_VERTEXCHANNELS_RGB"))
                matInfo.Material.SetFloat("_VertexChannels", 2f);
            else
                matInfo.Material.SetFloat("_VertexChannels", 0f);

            matInfo.Material.SetFloat(
                "_VertexDisplacement",
                matInfo.Keywords.Contains("VERTEX_DISPLACEMENT") ? 1f : 0f);
            matInfo.Material.SetFloat("_3DDisplacement", matInfo.Keywords.Contains("SPATIAL_DISPLACEMENT") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_DisplacementSpatial",
                matInfo.Keywords.Contains("DISPLACEMENT_SPATIAL") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_DisplacementBidirectional",
                matInfo.Keywords.Contains("DISPLACEMENT_BIDIRECTIONAL") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_SPECTROGRAM_FLAT"))
                matInfo.Material.SetFloat("_Spectrogram", 1f);
            else if (matInfo.Keywords.Contains("_SPECTROGRAM_FULL"))
                matInfo.Material.SetFloat("_Spectrogram", 2f);
            else
                matInfo.Material.SetFloat("_Spectrogram", 0f);

            if (matInfo.Keywords.Contains("_CURVE_VERTICES_AROUND_X"))
                matInfo.Material.SetFloat("_Curve_Vertices", 1f);
            else if (matInfo.Keywords.Contains("_CURVE_VERTICES_AROUND_Y"))
                matInfo.Material.SetFloat("_Curve_Vertices", 2f);
            else if (matInfo.Keywords.Contains("_CURVE_VERTICES_AROUND_Z"))
                matInfo.Material.SetFloat("_Curve_Vertices", 3f);
            else
                matInfo.Material.SetFloat("_Curve_Vertices", 0f);

            if (matInfo.Keywords.Contains("_VERTEXMODE_COLOR"))
                matInfo.Material.SetFloat("_Vertex", 1f);
            else if (matInfo.Keywords.Contains("_VERTEXMODE_EMISSION"))
                matInfo.Material.SetFloat("_Vertex", 2f);
            else if (matInfo.Keywords.Contains("_VERTEXMODE_METALSMOOTHNESS"))
                matInfo.Material.SetFloat("_Vertex", 3f);
            else if (matInfo.Keywords.Contains("_VERTEXMODE_SPECIAL"))
                matInfo.Material.SetFloat("_Vertex", 4f);
            else if (matInfo.Keywords.Contains("_VERTEXMODE_DISPLACEMENT"))
                matInfo.Material.SetFloat("_Vertex", 5f);
            else if (matInfo.Keywords.Contains("_VERTEXMODE_EMISSIVE_MULT_ADD"))
                matInfo.Material.SetFloat("_Vertex", 6f);
            else
                matInfo.Material.SetFloat("_Vertex", 0f);

            if (matInfo.Keywords.Contains("_VERTEX_WHITEBOOSTTYPE_MAINEFFECT"))
                matInfo.Material.SetFloat("_Vertex_BloomType", 1f);
            else if (matInfo.Keywords.Contains("_VERTEX_WHITEBOOSTTYPE_ALWAYS"))
                matInfo.Material.SetFloat("_Vertex_BloomType", 2f);
            else
                matInfo.Material.SetFloat("_Vertex_BloomType", 0f);

            matInfo.Material.SetFloat("_UseMainTex", matInfo.Keywords.Contains("MAIN_TEXTURE") ? 1f : 0f);

            matInfo.Material.SetFloat("_ZFade", matInfo.Keywords.Contains("Z_FADE") ? 1f : 0f);
            matInfo.Material.SetFloat("_Pixelate", matInfo.Keywords.Contains("PIXELATE") ? 1f : 0f);

            matInfo.Material.SetFloat("_EnableTextureColor", matInfo.Keywords.Contains("TEXTURE_COLOR") ? 1f : 0f);
            matInfo.Material.SetFloat("_AlphaChannel", matInfo.Keywords.Contains("_ALPHACHANNEL_RED") ? 1f : 0f);

            matInfo.Material.SetFloat("_EnableCustomPadding", matInfo.Keywords.Contains("CUSTOM_WRAPPING") ? 1f : 0f);

            matInfo.Material.SetFloat("_UseTextureFlipbook", matInfo.Keywords.Contains("TEXTURE_FLIPBOOK") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_FlipbookBlendingOff",
                matInfo.Keywords.Contains("FLIPBOOK_BLENDING_OFF") ? 1f : 0f);

            if (matInfo.Keywords.Contains("_EMISSIONTEXTURE_SIMPLE"))
                matInfo.Material.SetFloat("_EmissionTexture", 1f);
            else if (matInfo.Keywords.Contains("_EMISSIONTEXTURE_PULSE"))
                matInfo.Material.SetFloat("_EmissionTexture", 2f);
            else if (matInfo.Keywords.Contains("_EMISSIONTEXTURE_FLIPBOOK"))
                matInfo.Material.SetFloat("_EmissionTexture", 3f);
            else
                matInfo.Material.SetFloat("_EmissionTexture", 0f);
            if (matInfo.Keywords.Contains("_EMISSION_TEXTURE_SOURCE_MPM_G"))
                matInfo.Material.SetFloat("_Emission_Texture_Source", 1f);
            else
                matInfo.Material.SetFloat("_Emission_Texture_Source", 0f);
            matInfo.Material.SetFloat(
                "_SecondaryUVsEmissionTex",
                matInfo.Keywords.Contains("SECONDARY_UVS_EMISSION") ? 1f : 0f);

            if (matInfo.Keywords.Contains("_EMISSIONCOLORTYPE_WHITEBOOST"))
                matInfo.Material.SetFloat("_EmissionBloomType", 1f);
            else if (matInfo.Keywords.Contains("_EMISSIONCOLORTYPE_GRADIENT"))
                matInfo.Material.SetFloat("_EmissionBloomType", 2f);
            else if (matInfo.Keywords.Contains("_EMISSIONCOLORTYPE_MAINEFFECT"))
                matInfo.Material.SetFloat("_EmissionBloomType", 3f);
            else
                matInfo.Material.SetFloat("_EmissionBloomType", 0f);
            matInfo.Material.SetFloat(
                "_EnableEmissionAngleDisappear",
                matInfo.Keywords.Contains("EMISSION_ANGLE_DISAPPEAR") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_EMISSION_ALPHA_SOURCE_COPY_EMISSION"))
                matInfo.Material.SetFloat("_Emission_Alpha_Source", 1f);
            else if (matInfo.Keywords.Contains("_EMISSION_ALPHA_SOURCE_MPM_R"))
                matInfo.Material.SetFloat("_Emission_Alpha_Source", 2f);
            else
                matInfo.Material.SetFloat("_Emission_Alpha_Source", 0f);

            matInfo.Material.SetFloat("_EnableEmissionMask", matInfo.Keywords.Contains("EMISSION_MASK") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_MASKBLEND_ADD"))
                matInfo.Material.SetFloat("_MaskBlend", 1f);
            else if (matInfo.Keywords.Contains("_MASKBLEND_MASKED_ADD"))
                matInfo.Material.SetFloat("_MaskBlend", 2f);
            else
                matInfo.Material.SetFloat("_MaskBlend", 0f);
            matInfo.Material.SetFloat(
                "_SecondaryUVsMask",
                matInfo.Keywords.Contains("SECONDARY_UVS_EMISSION_MASK") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_EnableSecondaryEmissionMask",
                matInfo.Keywords.Contains("SECONDARY_EMISSION_MASK") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_SECONDARY_MASK_BLEND_ADD"))
                matInfo.Material.SetFloat("_Secondary_MaskBlend", 1f);
            else if (matInfo.Keywords.Contains("_SECONDARY_MASK_BLEND_MASKED_ADD"))
                matInfo.Material.SetFloat("_Secondary_MaskBlend", 2f);
            else
                matInfo.Material.SetFloat("_Secondary_MaskBlend", 0f);
            matInfo.Material.SetFloat(
                "_SecondaryUVsMask2",
                matInfo.Keywords.Contains("SECONDARY_UVS_EMISSION_MASK2") ? 1f : 0f);

            matInfo.Material.SetFloat("_EnableMask", matInfo.Keywords.Contains("MASK") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_MaskSecondaryUVs",
                matInfo.Keywords.Contains("SECONDARY_UVS_MASK") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_MaskRedIsAlpha",
                matInfo.Keywords.Contains("MASK_RED_IS_ALPHA") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_MASKBLEND_ADD"))
                matInfo.Material.SetFloat("_MaskBlend", 1f);
            else if (matInfo.Keywords.Contains("_MASKBLEND_MASKED_ADD"))
                matInfo.Material.SetFloat("_MaskBlend", 2f);
            else
                matInfo.Material.SetFloat("_MaskBlend", 0f);

            matInfo.Material.SetFloat("_EnableMask2", matInfo.Keywords.Contains("MASK2") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_Mask2SecondaryUVs",
                matInfo.Keywords.Contains("SECONDARY_UVS_MASK2") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_Mask2RedIsAlpha",
                matInfo.Keywords.Contains("MASK2_RED_IS_ALPHA") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_MASK2BLEND_ADD"))
                matInfo.Material.SetFloat("_Mask2Blend", 1f);
            else if (matInfo.Keywords.Contains("_MASK2BLEND_MASKED_ADD"))
                matInfo.Material.SetFloat("_Mask2Blend", 2f);
            else
                matInfo.Material.SetFloat("_Mask2Blend", 0f);

            matInfo.Material.SetFloat(
                "_CutoutType",
                matInfo.Keywords.Contains("_CUTOUTTYPE_ALPHA_CLIP") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_EnablePrivatePointLight",
                matInfo.Keywords.Contains("PRIVATE_POINT_LIGHT") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_EnableViewAlignDisappear",
                matInfo.Keywords.Contains("VIEW_ALIGN_DISAPPEAR") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_PointLightPositionLocal",
                matInfo.Keywords.Contains("POINT_LIGHT_IS_LOCAL") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableDirt", matInfo.Keywords.Contains("ENABLE_DIRT") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableNormalMap", matInfo.Keywords.Contains("NORMAL_MAP") ? 1f : 0f);
            matInfo.Material.SetFloat("_DetailNormalMap", matInfo.Keywords.Contains("DETAIL_NORMAL_MAP") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableLightmap", matInfo.Keywords.Contains("LIGHTMAP") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableDiffuse", matInfo.Keywords.Contains("DIFFUSE") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableDiffuseTexture", matInfo.Keywords.Contains("DIFFUSE_TEXTURE") ? 1f : 0f);
            if (matInfo.Keywords.Contains("_DIFFUSE_TEXTURE_SOURCE_MPM_R"))
                matInfo.Material.SetFloat("_Diffuse_Texture_Source", 1f);
            else if (matInfo.Keywords.Contains("_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS"))
                matInfo.Material.SetFloat("_Diffuse_Texture_Source", 2f);
            else
                matInfo.Material.SetFloat("_Diffuse_Texture_Source", 0f);
            matInfo.Material.SetFloat("_EnableSpecular", matInfo.Keywords.Contains("SPECULAR") ? 1f : 0f);
            matInfo.Material.SetFloat("_EnableLightFalloff", matInfo.Keywords.Contains("LIGHT_FALLOFF") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_EnableBothSidesDiffuse",
                matInfo.Keywords.Contains("BOTH_SIDES_DIFFUSE") ? 1f : 0f);

            matInfo.Material.SetFloat("_EnableRimDim", matInfo.Keywords.Contains("ENABLE_RIM_DIM") ? 1f : 0f);
            matInfo.Material.SetFloat("_InvertRimDim", matInfo.Keywords.Contains("INVERT_RIM_DIM") ? 1f : 0f);

            matInfo.Material.SetFloat("_EnableGroundFade", matInfo.Keywords.Contains("GROUND_FADE") ? 1f : 0f);

            matInfo.Material.SetFloat(
                "_EnableRemapWhiteBoostStart",
                matInfo.Keywords.Contains("REMAP_WHITEBOOST_START") ? 1f : 0f);

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
            matInfo.Material.SetFloat(
                "_EnableAngleDisappear",
                matInfo.Keywords.Contains("ENABLE_ANGLE_DISAPPEAR") ? 1f : 0f);
            matInfo.Material.SetFloat("_UseFogForLights", matInfo.Keywords.Contains("USE_FOR_FOR_LIGHTS") ? 1f : 0f);

            if (matInfo.Keywords.Contains("_WHITEBOOSTTYPE_MAINEFFECT")
                || matInfo.Keywords.Contains("_ENABLE_MAIN_EFFECT_WHITE_BOOST"))
                matInfo.Material.SetFloat("_BloomType", 1f);
            else if (matInfo.Keywords.Contains("_WHITEBOOSTTYPE_ALWAYS"))
                matInfo.Material.SetFloat("_BloomType", 2f);
            else
                matInfo.Material.SetFloat("_BloomType", 0f);

            if (matInfo.Keywords.Contains("_ACES_APPROACH_BEFORE_EMISSIVE"))
                matInfo.Material.SetFloat("_ACES_Approach", 1f);
            else
                matInfo.Material.SetFloat("_ACES_Approach", 0f);

            matInfo.Material.SetFloat(
                "_UseColorArray",
                matInfo.Keywords.Contains("COLOR_ARRAY") ? 1f : 0f);

            if (matInfo.Keywords.Contains("_CUSTOM_TIME_SONG_TIME"))
                matInfo.Material.SetFloat("_Custom_Time", 1f);
            else if (matInfo.Keywords.Contains("_CUSTOM_TIME_FREEZE"))
                matInfo.Material.SetFloat("_Custom_Time", 2f);
            else
                matInfo.Material.SetFloat("_Custom_Time", 0f);


            if (matInfo.Keywords.Contains("_BILLBOARD_FULL"))
                matInfo.Material.SetFloat("_Billboard", 1f);
            else if (matInfo.Keywords.Contains("_BILLBOARD_Y_AXIS"))
                matInfo.Material.SetFloat("_Billboard", 2f);
            else if (matInfo.Keywords.Contains("_BILLBOARD_CAMERA_FACING"))
                matInfo.Material.SetFloat("_Billboard", 3f);
            else
                matInfo.Material.SetFloat("_Billboard", 0f);

            matInfo.Material.SetFloat(
                "_EnableFog",
                matInfo.Keywords.Contains("FOG") || matInfo.Keywords.Contains("ENABLE_FOG") ? 1f : 0f);
            matInfo.Material.SetFloat(
                "_EnableHeightFog",
                matInfo.Keywords.Contains("HEIGHT_FOG") || matInfo.Keywords.Contains("ENABLE_HEIGHT_FOG") ? 1f : 0f);

            if (matInfo.Keywords.Contains("_FOGTYPE_LERP"))
                matInfo.Material.SetFloat("_FogType", 1f);
            else if (matInfo.Keywords.Contains("_FOGTYPE_COLOR"))
                matInfo.Material.SetFloat("_FogType", 2f);
            else if (matInfo.Keywords.Contains("_FOGTYPE_ALPHA"))
                matInfo.Material.SetFloat("_FogType", 3f);
            else
                matInfo.Material.SetFloat("_FogType", 0f);
            matInfo.Material.SetFloat(
                "_EnableDistanceDarkening",
                matInfo.Keywords.Contains("DISTANCE_DARKENING") ? 1f : 0f);
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
