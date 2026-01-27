#ifndef CUSTOM_BLOOM_CG_INCLUDED
#define CUSTOM_BLOOM_CG_INCLUDED

#include "CustomTonemapping.cginc"


#if _ACESTONEMAP_BEFORE_EMISSIVE
#define TONEMAP_APPLY_BEFORE(color) \
    ACES_TONE_MAPPING_APPLY(color)
#else
#define TONEMAP_APPLY_BEFORE(color)
#endif

#if _ACESTONEMAP_AFTER_EMISSIVE
#define TONEMAP_APPLY_AFTER(color) \
    ACES_TONE_MAPPING_APPLY(color)
#else
#define TONEMAP_APPLY_AFTER(color)
#endif

#define CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(color) \
    TONEMAP_APPLY_BEFORE(color); \
    color.rgb *= abs(color.a); \
    color.a = 0; \
    TONEMAP_APPLY_AFTER(color)

#define CUSTOM_BLOOM_NONE_APPLY(color) \
    TONEMAP_APPLY_BEFORE(color); \
    color.a = 0; \
    TONEMAP_APPLY_AFTER(color)

#define CUSTOM_BLOOM_PP_APPLY(color, multiplier) \
    TONEMAP_APPLY_BEFORE(color); \
    color.a = abs(color.a); \
    color.rgb *= color.a; \
    TONEMAP_APPLY_AFTER(color)

#define CUSTOM_BLOOM_FRAG_APPLY(color, multiplier) \
    TONEMAP_APPLY_BEFORE(color); \
    color.a = abs(color.a); \
    color.rgb += color.a * multiplier; \
    color.rgb *= color.a; \
    color.a = 0; \
    TONEMAP_APPLY_AFTER(color)

#endif
