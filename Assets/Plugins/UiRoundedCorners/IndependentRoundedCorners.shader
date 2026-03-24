Shader "UI/RoundedCorners/IndependentRoundedCorners" {
    
    Properties {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        
        // --- Mask support ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        // Definition in Properties section is required to Mask works properly
        _r ("r", Vector) = (0,0,0,0)
        _halfSize ("halfSize", Vector) = (0,0,0,0)
        _rect2props ("rect2props", Vector) = (0,0,0,0)
        // ---
    }
    
    SubShader {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent" 
        }
        
        // --- Mask support ---
        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }    
        Cull Off
        Lighting Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        // ---
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            CGPROGRAM
            
            #include "UnityCG.cginc"
            #include "SDFUtils.cginc"
            #include "ShaderSetup.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
            
            sampler2D _MainTex;
            
            fixed4 frag (v2f i) : SV_Target {
                // uv1: (r.x, r.y, r.z, r.w)
                // uv2: (halfSize.x, halfSize.y, rect2props.x, rect2props.y)
                // uv3: (rect2props.z, rect2props.w, 0, 0)
                float4 r = i.uv1;
                float2 halfSize = i.uv2.xy;
                float4 rect2props = float4(i.uv2.zw, i.uv3.xy);
                float alpha = CalcAlphaForIndependentCorners(i.uv, halfSize, rect2props, r);
                return mixAlpha(tex2D(_MainTex, i.uv), i.color, alpha);
            }
            
            ENDCG
        }
    }
}
