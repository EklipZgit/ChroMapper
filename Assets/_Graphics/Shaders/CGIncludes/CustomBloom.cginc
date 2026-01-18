#ifndef CUSTOM_BLOOM_CG_INCLUDED
#define CUSTOM_BLOOM_CG_INCLUDED

#include "CustomTonemapping.cginc"

fixed4 ApplyCustomBloom(fixed4 color, fixed alphaFactor, fixed whiteMult)
{
    #if _ACESTONEMAP_BEFORE_EMISSIVE
    ACES_TONE_MAPPING_APPLY(color);
    #endif

    if (color.a > 1.0) color.rgb *= color.a;
    color.a = saturate(color.a) * alphaFactor;
    color.rgb *= color.a * alphaFactor;
    
    #if _BLOOMWHITE_FRAG
    fixed whiteness = saturate(max(0, saturate(length(color.rgb)) * saturate(color.a) * whiteMult));
    color.rgb = lerp(color.rgb, 1, whiteness);
    #endif
    
    #if _BLOOMWHITE_PP
    color.a *= length(color.rgb);
    #else
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
