// ETAN dropped great piece of information and gave us this
// Tonemapping: https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
// Bloom: Reinhard Tone Mapping
#ifndef CUSTOM_TONEMAPPING_CG_INCLUDED
#define CUSTOM_TONEMAPPING_CG_INCLUDED

#define ACES_TONE_MAPPING_APPLY(col) \
col.rgb = saturate((col.rgb * (2.51 * col.rgb + 0.03)) / (col.rgb * (2.43 * col.rgb + 0.59) + 0.14))

#define REINHARD_TONE_MAPPING_APPLY(col) \
col.rgb = col.rgb / (col.rgb + 1)

#endif // CUSTOM_TONEMAPPING_CG_INCLUDED
