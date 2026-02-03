#ifndef CUSTOM_BLOOM_CG_INCLUDED
#define CUSTOM_BLOOM_CG_INCLUDED

#define CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(color) \
    color.rgb *= abs(color.a); \
    color.a = 0

#define CUSTOM_BLOOM_NONE_APPLY(color) \
    color.a = 0

#define CUSTOM_BLOOM_PP_APPLY(color, multiplier) \
    color.a = abs(color.a); \
    color.rgb *= color.a

#define CUSTOM_BLOOM_FRAG_APPLY(color, multiplier) \
    color.a = abs(color.a); \
    color.rgb += color.a * multiplier; \
    color.rgb *= color.a; \
    color.a = 0

#endif
