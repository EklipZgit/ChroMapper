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

    // keyword section, welcome to hell
    private static readonly Dictionary<string, ShaderKeywordParams> shaderPropParamsMap = new()
    {
        { "_EnableSecondaryColor", new KeyToggle("SECONDARY_COLOR") },
        { "_UseColorGradient", new KeyToggle("COLOR_GRADIENT") },
        { "_UseSpectrogram", new KeyToggle("SPECTROGRAM_COLOR") },
        {
            "_Secondary_UVs", new KeyId(
                "_SECONDARY_UVS_IMPORT",
                "_SECONDARY_UVS_EXTERNAL_SCALE",
                "_SECONDARY_UVS_OBJECT_SPACE",
                "_SECONDARY_UVS_ADDITIVE_OFFSET")
        },
        { "_Metallic_Texture_Source", new KeyId("_METALLIC_TEXTURE_MPM_R", "_METALLIC_TEXTURE_MPM_A") },
        { "_Smoothness_Texture_Source", new KeyId("_SMOOTHNESS_TEXTURE_MPM_A", "_SMOOTHNESS_TEXTURE_MPM_G_ROUGHNESS") },
        { "_EnableMetalSmoothnessTex", new KeyToggle("METAL_SMOOTHNESS_TEXTURE") },
        { "_PreciseNormal", new KeyToggle("PRECISE_NORMAL") },
        { "_EnableVertexColor", new KeyToggle("VERTEX_COLOR") },
        { "_SquareVertexAlpha", new KeyToggle("VERTEX_SQUARE_ALPHA") },
        { "_RedIsVertexAlpha", new KeyToggle("VERTEX_RED_IS_ALPHA") },
        { "_VertexChannels", new KeyId("_VERTEXCHANNELS_A", "_VERTEXCHANNELS_RGB") },
        { "_VertexDisplacement", new KeyToggle("VERTEX_DISPLACEMENT") },
        { "_3DDisplacement", new KeyToggle("SPATIAL_DISPLACEMENT") },
        { "_DisplacementSpatial", new KeyToggle("DISPLACEMENT_SPATIAL") },
        { "_DisplacementBidirectional", new KeyToggle("DISPLACEMENT_BIDIRECTIONAL") },
        { "_Spectrogram", new KeyId("_SPECTROGRAM_FLAT", "_SPECTROGRAM_FULL") },
        {
            "_Curve_Vertices",
            new KeyId("_CURVE_VERTICES_AROUND_X", "_CURVE_VERTICES_AROUND_Y", "_CURVE_VERTICES_AROUND_Z")
        },
        {
            "_Vertex", new KeyId(
                "_VERTEXMODE_COLOR",
                "_VERTEXMODE_EMISSION",
                "_VERTEXMODE_METALSMOOTHNESS",
                "_VERTEXMODE_SPECIAL",
                "_VERTEXMODE_DISPLACEMENT",
                "_VERTEXMODE_EMISSIVE_MULT_ADD")
        },
        { "_Vertex_BloomType", new KeyId("_VERTEX_WHITEBOOSTTYPE_MAINEFFECT", "_VERTEX_WHITEBOOSTTYPE_ALWAYS") },
        { "_UseMainTex", new KeyToggle("MAIN_TEXTURE") },
        { "_ZFade", new KeyToggle("Z_FADE") },
        { "_Pixelate", new KeyToggle("PIXELATE") },
        { "_EnableTextureColor", new KeyToggle("TEXTURE_COLOR") },
        { "_AlphaChannel", new KeyToggle("_ALPHACHANNEL_RED") },
        { "_EnableCustomPadding", new KeyToggle("CUSTOM_WRAPPING") },
        { "_UseTextureFlipbook", new KeyToggle("TEXTURE_FLIPBOOK") },
        { "_FlipbookBlendingOff", new KeyToggle("FLIPBOOK_BLENDING_OFF") },
        {
            "_EmissionTexture",
            new KeyId("_EMISSIONTEXTURE_SIMPLE", "_EMISSIONTEXTURE_PULSE", "_EMISSIONTEXTURE_FLIPBOOK")
        },
        { "_Emission_Texture_Source", new KeyId("_EMISSION_TEXTURE_SOURCE_MPM_G") },
        { "_SecondaryUVsEmissionTex", new KeyToggle("SECONDARY_UVS_EMISSION") },
        {
            "_EmissionBloomType",
            new KeyId("_EMISSIONCOLORTYPE_WHITEBOOST", "_EMISSIONCOLORTYPE_GRADIENT", "_EMISSIONCOLORTYPE_MAINEFFECT")
        },
        { "_EnableEmissionAngleDisappear", new KeyToggle("EMISSION_ANGLE_DISAPPEAR") },
        { "_Emission_Alpha_Source", new KeyId("_EMISSION_ALPHA_SOURCE_COPY_EMISSION", "_EMISSION_ALPHA_SOURCE_MPM_R") },
        { "_EnableEmissionMask", new KeyToggle("EMISSION_MASK") },
        { "_SecondaryUVsMask", new KeyToggle("SECONDARY_UVS_EMISSION_MASK") },
        { "_EnableSecondaryEmissionMask", new KeyToggle("SECONDARY_EMISSION_MASK") },
        { "_Secondary_MaskBlend", new KeyId("_SECONDARY_MASK_BLEND_ADD", "_SECONDARY_MASK_BLEND_MASKED_ADD") },
        { "_SecondaryUVsMask2", new KeyToggle("SECONDARY_UVS_EMISSION_MASK2") },
        { "_EnableMask", new KeyToggle("MASK") },
        { "_MaskSecondaryUVs", new KeyToggle("SECONDARY_UVS_MASK") },
        { "_MaskRedIsAlpha", new KeyToggle("MASK_RED_IS_ALPHA") },
        { "_MaskBlend", new KeyId("_MASKBLEND_ADD", "_MASKBLEND_MASKED_ADD") },
        { "_EnableMask2", new KeyToggle("MASK2") },
        { "_Mask2SecondaryUVs", new KeyToggle("SECONDARY_UVS_MASK2") },
        { "_Mask2RedIsAlpha", new KeyToggle("MASK2_RED_IS_ALPHA") },
        { "_Mask2Blend", new KeyId("_MASK2BLEND_ADD", "_MASK2BLEND_MASKED_ADD") },
        { "_CutoutType", new KeyToggle("_CUTOUTTYPE_ALPHA_CLIP") },
        { "_EnablePrivatePointLight", new KeyToggle("PRIVATE_POINT_LIGHT") },
        { "_EnableViewAlignDisappear", new KeyToggle("VIEW_ALIGN_DISAPPEAR") },
        { "_PointLightPositionLocal", new KeyToggle("POINT_LIGHT_IS_LOCAL") },
        { "_EnableDirt", new KeyToggle("ENABLE_DIRT") },
        { "_EnableNormalMap", new KeyToggle("NORMAL_MAP") },
        { "_DetailNormalMap", new KeyToggle("DETAIL_NORMAL_MAP") },
        { "_EnableLightmap", new KeyToggle("LIGHTMAP") },
        { "_EnableDiffuse", new KeyToggle("DIFFUSE") },
        { "_EnableDiffuseTexture", new KeyToggle("DIFFUSE_TEXTURE") },
        {
            "_Diffuse_Texture_Source",
            new KeyId("_DIFFUSE_TEXTURE_SOURCE_MPM_R", "_DIFFUSE_TEXTURE_SOURCE_MPM_A_SMOOTHNESS")
        },
        { "_EnableSpecular", new KeyToggle("SPECULAR") },
        { "_EnableLightFalloff", new KeyToggle("LIGHT_FALLOFF") },
        { "_EnableBothSidesDiffuse", new KeyToggle("BOTH_SIDES_DIFFUSE") },
        { "_EnableRimDim", new KeyToggle("ENABLE_RIM_DIM") },
        { "_InvertRimDim", new KeyToggle("INVERT_RIM_DIM") },
        { "_EnableGroundFade", new KeyToggle("GROUND_FADE") },
        { "_EnableRemapWhiteBoostStart", new KeyToggle("REMAP_WHITEBOOST_START") },
        { "_EnableAlphaWidthScale", new KeyToggle("ALPHA_WIDTH_SCALE") },
        { "_MultiplyColorWithAlpha", new KeyToggle("MULTIPLY_COLOR_WITH_ALPHA") },
        { "_EnableYAxisBillboard", new KeyToggle("ENABLE_Y_AXIS_BILLBOARD") },
        { "_SquareAlpha", new KeyToggle("SQUARE_ALPHA") },
        { "_EnableAngleDisappear", new KeyToggle("ENABLE_ANGLE_DISAPPEAR") },
        { "_UseFogForLights", new KeyToggle("USE_FOR_FOR_LIGHTS") },
        {
            "_BloomType", new KeyId(
                new[] { "_WHITEBOOSTTYPE_MAINEFFECT", "_ENABLE_MAIN_EFFECT_WHITE_BOOST" },
                new[] { "_WHITEBOOSTTYPE_ALWAYS" })
        },
        { "_ACES_Approach", new KeyId("_ACES_APPROACH_BEFORE_EMISSIVE") },
        { "_UseColorArray", new KeyToggle("COLOR_ARRAY") },
        { "_MeshPacking", new KeyToggle("MESH_PACKING") },
        { "_Custom_Time", new KeyId("_CUSTOM_TIME_SONG_TIME", "_CUSTOM_TIME_FREEZE") },
        { "_Billboard", new KeyId("_BILLBOARD_FULL", "_BILLBOARD_Y_AXIS", "_BILLBOARD_CAMERA_FACING") },
        { "_EnableFog", new KeyToggle("ENABLE_FOG", "FOG") },
        { "_EnableHeightFog", new KeyToggle("ENABLE_HEIGHT_FOG", "HEIGHT_FOG") },
        { "_FogType", new KeyId("_FOGTYPE_LERP", "_FOGTYPE_COLOR", "_FOGTYPE_ALPHA") },
        { "_EnableDistanceDarkening", new KeyToggle("DISTANCE_DARKENING") },
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
            mat.SetTexture(renamedKey, library.Textures.Lookup[textureProp.Value.ToLower()]);
        }

        foreach (var (propName, shaderPropParams) in shaderPropParamsMap) shaderPropParams.Apply(matInfo, propName);
    }

    private abstract class ShaderKeywordParams
    {
        public abstract void Apply(MaterialInfo matInfo, string propName);
    }

    private class KeyToggle : ShaderKeywordParams
    {
        private readonly string[] values;

        public KeyToggle(params string[] k) => values = k;

        public override void Apply(MaterialInfo matInfo, string propName) =>
            matInfo.Material.SetFloat(propName, matInfo.Keywords.Any(values.Contains) ? 1f : 0f);
    }

    private class KeyId : ShaderKeywordParams
    {
        private readonly string[][] values;

        public KeyId(params string[] k) => values = k.Select(x => new[] { x }).ToArray();
        public KeyId(params string[][] k) => values = k;

        public override void Apply(MaterialInfo matInfo, string propName) =>
            matInfo.Material.SetFloat(propName, GetIndex(matInfo));

        private float GetIndex(MaterialInfo matInfo)
        {
            foreach (var (keyword, index) in values.Select((s, i) => (s, i)))
            {
                if (matInfo.Keywords.Any(keyword.Contains)) return index + 1f;
            }

            return 0f;
        }
    }
}
