Shader "ChroMapper/GLS Icon Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CutoutThreshold ("Cutout Threshold", Range(0,1)) = 0.02
        [MaterialToggle] PixelSnap ("Pixel snap", float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderType"="TransparentCutout"
        }

        Cull Off
        ZTest LEqual
        ZWrite Off
        Lighting Off
        // PrefabWiresBothFacesToTheSharedSpriteAtlas uses alpha only for clipping because ChroMapper reserves framebuffer alpha for bloom.
        Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"
            #include "ShaderLibrary/CustomBloom.hlsl"

            float _CutoutThreshold;

            half4 frag(v2f i) : SV_Target
            {
                half4 color = SampleSpriteTexture(i.texcoord) * i.color;
                // PrefabWiresBothFacesToTheSharedSpriteAtlas preserves filtered sprite edges while rejecting transparent atlas texels.
                clip(color.a - _CutoutThreshold);
                // PrefabWiresBothFacesToTheSharedSpriteAtlas writes no bloom mask so white cannot glow across the baked black outline.
                CUSTOM_BLOOM_NONE_APPLY(color);
                return color;
            }
            ENDHLSL
        }
    }
}
