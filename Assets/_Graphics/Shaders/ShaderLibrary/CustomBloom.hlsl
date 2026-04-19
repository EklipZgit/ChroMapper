#ifndef CUSTOM_BLOOM_CG_INCLUDED
#define CUSTOM_BLOOM_CG_INCLUDED

#define CUSTOM_BLOOM_NONE_TRANSPARENT_APPLY(color) \
    color.rgb *= abs(color.a); \
    color.a = 0


#define CUSTOM_BLOOM_NONE_APPLY(color) \
    color.rgb *= color.a;\
    color.a = 0

#define CUSTOM_BLOOM_PP_APPLY(color, multiplier) \
    float wb = pow(color.a, 2); \
    wb = wb * multiplier; \
    wb = pow(wb, 2); \
    float whiteTerm = wb * (1 - 0.1); \
    color.rgb = color.rgb * color.a + whiteTerm;


#define CUSTOM_BLOOM_FRAG_APPLY(color, multiplier) \
    float x = color.a; \
    float wb = pow(x * multiplier, 2); \
    wb = wb * wb; \
    color.rgb = saturate(color.rgb * x + wb); \
    color.a = x * multiplier;
#endif
