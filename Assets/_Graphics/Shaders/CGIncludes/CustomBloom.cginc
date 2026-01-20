#ifndef CUSTOM_BLOOM_CG_INCLUDED
#define CUSTOM_BLOOM_CG_INCLUDED

#include "CustomTonemapping.cginc"

fixed4 ApplyCustomBloom(fixed4 color, fixed alphaFactor, fixed whiteMult)
{
    #if _ACESTONEMAP_BEFORE_EMISSIVE
    ACES_TONE_MAPPING_APPLY(color);
    #endif

    #if _BLOOMWHITE_NONE
    color.rgb *= color.a * alphaFactor;
    color.a = 0;
    #endif
    
    #if _BLOOMWHITE_PP
    color.a = saturate(color.a * alphaFactor);
    color.rgb *= color.a;
    #endif

    #if _BLOOMWHITE_FRAG
    // make it white
    color.rgb += color.a * alphaFactor * whiteMult;
    // color.rgb = saturate(color.rgb);
    
    color.a = saturate(color.a * alphaFactor);
    color.rgb *= color.a;
    color.a = 0;
    #endif

    #if _ACESTONEMAP_AFTER_EMISSIVE
    ACES_TONE_MAPPING_APPLY(color);
    #endif

    return color;
}

fixed4 ApplyCustomBloom(fixed4 color, fixed whiteMult)
{
    return ApplyCustomBloom(color, 1, whiteMult);
}

#endif
