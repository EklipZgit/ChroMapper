// Use the existing rounded selection sprite's alpha mask while coloring its border as a rainbow.
Shader "UI/Chroma Rainbow Border"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 normalizedTilePosition : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 normalizedTilePosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                output.normalizedTilePosition = input.normalizedTilePosition;
                return output;
            }

            fixed3 HueToRgb(float hue)
            {
                float3 hueOffsets = float3(0.0, 0.66666667, 0.33333333);
                return saturate(abs(frac(hue + hueOffsets) * 6.0 - 3.0) - 1.0);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 maskedSprite = tex2D(_MainTex, input.texcoord) * input.color;
                // Consume normalized mesh coordinates so the rounded border completes exactly one hue rotation around its center.
                float2 fromCenter = input.normalizedTilePosition - 0.5;
                float hue = atan2(fromCenter.y, fromCenter.x) / (2.0 * UNITY_PI) + 0.5;
                // Lift the ring toward white without clipping individual hue channels, which keeps green distinguishable from yellow.
                fixed3 brightHue = lerp(HueToRgb(hue), fixed3(1.0, 1.0, 1.0), 0.25);
                fixed4 color = fixed4(brightHue, maskedSprite.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
