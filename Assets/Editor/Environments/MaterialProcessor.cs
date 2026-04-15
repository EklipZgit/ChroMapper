using System.Collections.Generic;
using System.Linq;

public static class MaterialProcessor
{
    private static readonly Dictionary<string, string> shaderPropRemap = new()
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

    public static void HandleProp(EnvironmentLibrarySO library, MaterialInfo matInfo)
    {
        var mat = matInfo.Material;

        // mat.SetColor("_Color", matInfo.Color);

        foreach (var floatProp in matInfo.FloatProps)
        {
            var renamedKey = shaderPropRemap.GetValueOrDefault(floatProp.Key, floatProp.Key);
            mat.SetFloat(renamedKey, floatProp.Value);
        }

        foreach (var vectorProp in matInfo.VectorProps)
        {
            var renamedKey = shaderPropRemap.GetValueOrDefault(vectorProp.Key, vectorProp.Key);
            mat.SetVector(renamedKey, vectorProp.Value);
        }

        foreach (var textureProp in matInfo.TextureProps)
        {
            if (textureProp.Value == "null") continue;
            var renamedKey = shaderPropRemap.GetValueOrDefault(textureProp.Key, textureProp.Key);
            mat.SetTexture(renamedKey, library.Textures.Lookup[textureProp.Value]);
        }

        // keyword section, welcome to hell
        KeywordToggle(matInfo, "_EnableSecondaryColor", "SECONDARY_COLOR");
        KeywordToggle(matInfo, "_UseColorGradient", "COLOR_GRADIENT");
        KeywordToggle(matInfo, "_UseSpectrogram", "SPECTROGRAM_COLOR");

        KeywordId(
            matInfo,
            "_Secondary_UVs",
            "_SECONDARY_UVS_IMPORT",
            "_SECONDARY_UVS_EXTERNAL_SCALE",
            "_SECONDARY_UVS_OBJECT_SPACE",
            "_SECONDARY_UVS_ADDITIVE_OFFSET");

        KeywordToggle(matInfo, "_EnableMetalSmoothnessTex", "METAL_SMOOTHNESS_TEXTURE");
        KeywordId(
            matInfo,
            "_Metallic_Texture_Source",
            "_METALLIC_TEXTURE_MPM_R",
            "_METALLIC_TEXTURE_MPM_A");
        KeywordId(
            matInfo,
            "_Smoothness_Texture_Source",
            "_SMOOTHNESS_TEXTURE_MPM_A",
            "_SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS");
        KeywordToggle(matInfo, "_PreciseNormal", "PRECISE_NORMAL");

        KeywordToggle(matInfo, "_EnableVertexColor", "VERTEX_COLOR");
        KeywordToggle(matInfo, "_SquareVertexAlpha", "VERTEX_SQUARE_ALPHA");
        KeywordToggle(matInfo, "_RedIsVertexAlpha", "VERTEX_RED_IS_ALPHA");
        KeywordId(matInfo, "_VertexChannels", "_VERTEXCHANNELS_A", "_VERTEXCHANNELS_RGB");

        KeywordToggle(matInfo, "_VertexDisplacement", "VERTEX_DISPLACEMENT");
        KeywordToggle(matInfo, "_3DDisplacement", "SPATIAL_DISPLACEMENT");
        KeywordToggle(matInfo, "_DisplacementSpatial", "DISPLACEMENT_SPATIAL");
        KeywordToggle(matInfo, "_DisplacementBidirectional", "DISPLACEMENT_BIDIRECTIONAL");
        KeywordId(matInfo, "_Spectrogram", "_SPECTROGRAM_FLAT", "_SPECTROGRAM_FULL");

        KeywordId(
            matInfo,
            "_Curve_Vertices",
            "_CURVE_VERTICES_AROUND_X",
            "_CURVE_VERTICES_AROUND_Y",
            "_CURVE_VERTICES_AROUND_Z");

        KeywordId(
            matInfo,
            "_Vertex",
            "_VERTEXMODE_COLOR",
            "_VERTEXMODE_EMISSION",
            "_VERTEXMODE_METALSMOOTHNESS",
            "_VERTEXMODE_SPECIAL",
            "_VERTEXMODE_DISPLACEMENT",
            "_VERTEXMODE_EMISSIVE_MULT_ADD");

        KeywordId(
            matInfo,
            "_Vertex_BloomType",
            "_VERTEX_WHITEBOOSTTYPE_MAINEFFECT",
            "_VERTEX_WHITEBOOSTTYPE_ALWAYS");

        KeywordToggle(matInfo, "_UseMainTex", "MAIN_TEXTURE");

        KeywordToggle(matInfo, "_ZFade", "Z_FADE");
        KeywordToggle(matInfo, "_Pixelate", "PIXELATE");

        KeywordToggle(matInfo, "_EnableTextureColor", "TEXTURE_COLOR");
        KeywordToggle(matInfo, "_AlphaChannel", "_ALPHACHANNEL_RED");

        KeywordToggle(matInfo, "_EnableCustomPadding", "CUSTOM_WRAPPING");

        KeywordToggle(matInfo, "_UseTextureFlipbook", "TEXTURE_FLIPBOOK");
        KeywordToggle(matInfo, "_FlipbookBlendingOff", "FLIPBOOK_BLENDING_OFF");

        KeywordId(
            matInfo,
            "_EmissionTexture",
            "_EMISSIONTEXTURE_SIMPLE",
            "_EMISSIONTEXTURE_PULSE",
            "_EMISSIONTEXTURE_FLIPBOOK");
        KeywordId(matInfo, "_Emission_Texture_Source", "_EMISSION_TEXTURE_SOURCE_MPM_G");
        KeywordToggle(matInfo, "_SecondaryUVsEmissionTex", "SECONDARY_UVS_EMISSION");

        KeywordId(
            matInfo,
            "_EmissionBloomType",
            "_EMISSIONCOLORTYPE_WHITEBOOST",
            "_EMISSIONCOLORTYPE_GRADIENT",
            "_EMISSIONCOLORTYPE_MAINEFFECT");
        KeywordToggle(matInfo, "_EnableEmissionAngleDisappear", "EMISSION_ANGLE_DISAPPEAR");
        KeywordId(
            matInfo,
            "_Emission_Alpha_Source",
            "_EMISSION_ALPHA_SOURCE_COPY_EMISSION",
            "_EMISSION_ALPHA_SOURCE_MPM_R");

        KeywordToggle(matInfo, "_EnableEmissionMask", "EMISSION_MASK");
        KeywordId(matInfo, "_MaskBlend", "_MASKBLEND_ADD", "_MASKBLEND_MASKED_ADD");
        KeywordToggle(matInfo, "_SecondaryUVsMask", "SECONDARY_UVS_EMISSION_MASK");

        KeywordToggle(matInfo, "_EnableSecondaryEmissionMask", "SECONDARY_EMISSION_MASK");
        KeywordId(
            matInfo,
            "_Secondary_MaskBlend",
            "_SECONDARY_MASK_BLEND_ADD",
            "_SECONDARY_MASK_BLEND_MASKED_ADD");
        KeywordToggle(matInfo, "_SecondaryUVsMask2", "SECONDARY_UVS_EMISSION_MASK2");

        KeywordToggle(matInfo, "_EnableMask", "MASK");
        KeywordToggle(matInfo, "_MaskSecondaryUVs", "SECONDARY_UVS_MASK");
        KeywordToggle(matInfo, "_MaskRedIsAlpha", "MASK_RED_IS_ALPHA");
        KeywordId(matInfo, "_MaskBlend", "_MASKBLEND_ADD", "_MASKBLEND_MASKED_ADD");

        KeywordToggle(matInfo, "_EnableMask2", "MASK2");
        KeywordToggle(matInfo, "_Mask2SecondaryUVs", "SECONDARY_UVS_MASK2");
        KeywordToggle(matInfo, "_Mask2RedIsAlpha", "MASK2_RED_IS_ALPHA");
        KeywordId(matInfo, "_Mask2Blend", "_MASK2BLEND_ADD", "_MASK2BLEND_MASKED_ADD");

        KeywordToggle(matInfo, "_CutoutType", "_CUTOUTTYPE_ALPHA_CLIP");

        KeywordToggle(matInfo, "_EnablePrivatePointLight", "PRIVATE_POINT_LIGHT");

        KeywordToggle(matInfo, "_EnableViewAlignDisappear", "VIEW_ALIGN_DISAPPEAR");
        KeywordToggle(matInfo, "_PointLightPositionLocal", "POINT_LIGHT_IS_LOCAL");
        KeywordToggle(matInfo, "_EnableDirt", "ENABLE_DIRT");
        KeywordToggle(matInfo, "_EnableNormalMap", "NORMAL_MAP");
        KeywordToggle(matInfo, "_DetailNormalMap", "DETAIL_NORMAL_MAP");
        KeywordToggle(matInfo, "_EnableLightmap", "LIGHTMAP");
        KeywordToggle(matInfo, "_EnableDiffuse", "DIFFUSE");
        KeywordToggle(matInfo, "_EnableDiffuseTexture", "DIFFUSE_TEXTURE");
        KeywordId(
            matInfo,
            "_Diffuse_Texture_Source",
            "_DIFFUSE_TEXTURE_SOURCE_MPM_R",
            "_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS");
        KeywordToggle(matInfo, "_EnableSpecular", "SPECULAR");
        KeywordToggle(matInfo, "_EnableLightFalloff", "LIGHT_FALLOFF");
        KeywordToggle(matInfo, "_EnableBothSidesDiffuse", "BOTH_SIDES_DIFFUSE");

        KeywordToggle(matInfo, "_EnableRimDim", "ENABLE_RIM_DIM");
        KeywordToggle(matInfo, "_InvertRimDim", "INVERT_RIM_DIM");

        KeywordToggle(matInfo, "_EnableGroundFade", "GROUND_FADE");

        KeywordToggle(matInfo, "_EnableRemapWhiteBoostStart", "REMAP_WHITEBOOST_START");

        KeywordToggle(matInfo, "_EnableAlphaWidthScale", "ALPHA_WIDTH_SCALE");

        KeywordToggle(matInfo, "_MultiplyColorWithAlpha", "MULTIPLY_COLOR_WITH_ALPHA");
        KeywordToggle(matInfo, "_EnableYAxisBillboard", "ENABLE_Y_AXIS_BILLBOARD");
        KeywordToggle(matInfo, "_SquareAlpha", "SQUARE_ALPHA");
        KeywordToggle(matInfo, "_EnableAngleDisappear", "ENABLE_ANGLE_DISAPPEAR");
        KeywordToggle(matInfo, "_UseFogForLights", "USE_FOR_FOR_LIGHTS");

        KeywordId(
            matInfo,
            "_BloomType",
            new[] { "_WHITEBOOSTTYPE_MAINEFFECT", "_ENABLE_MAIN_EFFECT_WHITE_BOOST" },
            new[] { "_WHITEBOOSTTYPE_ALWAYS" });

        KeywordId(matInfo, "_ACES_Approach", "_ACES_APPROACH_BEFORE_EMISSIVE");

        KeywordToggle(matInfo, "_UseColorArray", "COLOR_ARRAY");
        KeywordToggle(matInfo, "_MeshPacking", "MESH_PACKING");
        KeywordId(matInfo, "_Custom_Time", "_CUSTOM_TIME_SONG_TIME", "_CUSTOM_TIME_FREEZE");


        KeywordId(
            matInfo,
            "_Billboard",
            "_BILLBOARD_FULL",
            "_BILLBOARD_Y_AXIS",
            "_BILLBOARD_CAMERA_FACING");

        KeywordToggle(matInfo, "_EnableFog", "ENABLE_FOG", "FOG");
        KeywordToggle(matInfo, "_EnableHeightFog", "ENABLE_HEIGHT_FOG", "HEIGHT_FOG");

        KeywordId(matInfo, "_FogType", "_FOGTYPE_LERP", "_FOGTYPE_COLOR", "_FOGTYPE_ALPHA");
        KeywordToggle(matInfo, "_EnableDistanceDarkening", "DISTANCE_DARKENING");
    }

    private static void KeywordToggle(MaterialInfo matInfo, string prop, string keyword) =>
        matInfo.Material.SetFloat(prop, matInfo.Keywords.Contains(keyword) ? 1f : 0f);

    private static void KeywordToggle(MaterialInfo matInfo, string prop, params string[] keywords) =>
        matInfo.Material.SetFloat(prop, matInfo.Keywords.Any(keywords.Contains) ? 1f : 0f);

    private static void KeywordId(MaterialInfo matInfo, string prop, params string[] keywords) =>
        matInfo.Material.SetFloat(prop, KeywordId(matInfo, keywords));

    private static float KeywordId(MaterialInfo matInfo, params string[] keywords)
    {
        foreach (var (keyword, index) in keywords.Select((s, i) => (s, i)))
        {
            if (matInfo.Keywords.Contains(keyword)) return index + 1f;
        }

        return 0f;
    }

    private static void KeywordId(MaterialInfo matInfo, string prop, params string[][] keywords) =>
        matInfo.Material.SetFloat(prop, KeywordId(matInfo, keywords));

    private static float KeywordId(MaterialInfo matInfo, params string[][] keywords)
    {
        foreach (var (keyword, index) in keywords.Select((s, i) => (s, i)))
        {
            if (matInfo.Keywords.Any(keyword.Contains)) return index + 1f;
        }

        return 0f;
    }
}
