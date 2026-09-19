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
            "RenderType"="Transparent"
        }

        Cull Off
        ZTest LEqual
        ZWrite Off
        Lighting Off
        // The Zero Zero alpha factors write a zero framebuffer alpha at icon pixels, matching 
        // the no-bloom mask the old Blend Off + CUSTOM_BLOOM_NONE_APPLY path produced.
        Blend SrcAlpha OneMinusSrcAlpha, Zero Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"
            #include "ShaderLibrary/CustomBloom.hlsl"

            float _CutoutThreshold;
            float4 _MainTex_TexelSize;

            half4 frag(v2f i) : SV_Target
            {
                half4 tex = SampleSpriteTexture(i.texcoord);
                // The old (a-0.5)*sqrt(footprint)+0.5 pivot re-hardened the smooth
                // mip-averaged alpha ramp into a binary silhouette, aka the stair-stepping and scattered
                // dark border pixels on distant icons. A pure multiplicative lift keeps minified
                // strokes legible while leaving the filtered gradient shape untouched; footprint <= 1 is a
                // no-op so near-field pixels are unchanged.
                float footprint = max(
                    fwidth(i.texcoord.x) * _MainTex_TexelSize.z,
                    fwidth(i.texcoord.y) * _MainTex_TexelSize.w);
                tex.a = saturate(tex.a * sqrt(max(footprint, 1.0)));
                half4 color = tex * i.color;
                // keeps the sampled coverage in color.a: SrcAlpha blending
                // needs it to feather edges, and the Zero Zero alpha factors write the bloom mask instead, so
                // CUSTOM_BLOOM_NONE_APPLY's a=0 overwrite is replaced rather than applied on top.
                clip(color.a - _CutoutThreshold);
                return color;
            }
            ENDHLSL
        }
    }
}
