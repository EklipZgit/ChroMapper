// ETAN dropped great piece of information and gave us this
// Tonemapping: https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
// Bloom: Reinhard Tone Mapping
#ifndef CUSTOM_TONEMAPPING_CG_INCLUDED
#define CUSTOM_TONEMAPPING_CG_INCLUDED

#if ACES_TONE_MAPPING

#define ACES_TONE_MAPPING_APPLY(col) \
const float a = 2.51; \
const float b = 0.03; \
const float c = 2.43; \
const float d = 0.59; \
const float e = 0.14; \
col = saturate((col*(a*col+b))/(col*(c*col+d)+e))

#else

#define ACES_TONE_MAPPING_APPLY(col)

#endif

#if REINHARD_TONE_MAPPING

#define REINHARD_TONE_MAPPING_APPLY(col) \
col.rgb = col.rgb / (col.rgb + 1.0)

#else

#define REINHARD_TONE_MAPPING_APPLY(col)

#endif

#endif // CUSTOM_TONEMAPPING_CG_INCLUDED