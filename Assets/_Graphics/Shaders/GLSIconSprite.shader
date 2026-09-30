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

            // GLSIconShaderSurvivesBloomLibraryMoves: this pass writes its no-bloom mask through Blend and must not depend on unused bloom helpers.
            #include "UnitySprites.cginc"

            float _CutoutThreshold;

            half4 frag(v2f i) : SV_Target
            {
                // MinifiedIconPreservesSampledAlpha: filtered alpha already
                // represents coverage; boosting it thickened distant strokes
                // and hardened their edges instead of preserving the mip filter.
                half4 color = SampleSpriteTexture(i.texcoord) * i.color;
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
