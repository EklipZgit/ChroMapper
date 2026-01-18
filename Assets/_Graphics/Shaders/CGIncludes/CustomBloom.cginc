#ifndef CUSTOM_BLOOM_CG_INCLUDED
#define CUSTOM_BLOOM_CG_INCLUDED

#include "CustomTonemapping.cginc"

fixed4 ApplyCustomBloom(fixed4 color, fixed boost)
{
    #if _ACESTONEMAP_BEFORE_EMISSIVE
    ACES_TONE_MAPPING_APPLY(color);
    #endif

    if (color.a > 1.0) color.rgb *= color.a;
    color.a = saturate(color.a);
    color.rgb *= color.a;
    
    #if _BLOOMWHITE_FRAG
    fixed whiteness = saturate(log(1 + max(0, (length(color.rgb * color.a) - 0.75) * boost)));
    color.rgb = lerp(color.rgb, 1, whiteness);
    #endif
    
    #if _BLOOMWHITE_NONE
    color.a = 0;
    #endif
    
    #if _ACESTONEMAP_AFTER_EMISSIVE
    ACES_TONE_MAPPING_APPLY(color);
    #endif

    return color;
}

#endif
