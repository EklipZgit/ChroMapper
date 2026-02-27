#ifndef BLOOM_FOG_CG_INCLUDED
#define BLOOM_FOG_CG_INCLUDED

float _StereoCameraEyeOffset;

// These are the global variable names the game uses by default,
// certain mods might want to use their own attenuation/offset variable names.
#ifndef CUSTOM_FOG_ATTENUATION_NAME
#define CUSTOM_FOG_ATTENUATION_NAME _CustomFogAttenuation
#endif
#ifndef CUSTOM_FOG_OFFSET_NAME
#define CUSTOM_FOG_OFFSET_NAME _CustomFogOffset
#endif

float CUSTOM_FOG_ATTENUATION_NAME;
float CUSTOM_FOG_OFFSET_NAME;

inline float distanceSquared(float3 pos)
{
    float3 distance = pos - _WorldSpaceCameraPos;
    return dot(distance, distance);
}

#ifndef CUSTOM_FOG_COMPUTE_FACTOR
#define CUSTOM_FOG_COMPUTE_FACTOR(result, distanceSq, fogStartOffset, fogScale) \
  result = max(distanceSq + -fogStartOffset, 0); \
  result = max(result * fogScale + -CUSTOM_FOG_OFFSET_NAME, 0); \
  result = 1 / (result * CUSTOM_FOG_ATTENUATION_NAME + 1); \
  result = -result + 1

#endif

#ifndef CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME
#define CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME _CustomFogHeightFogStartY
#endif
#ifndef CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME
#define CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME _CustomFogHeightFogHeight
#endif

float CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME;
float CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME;

#ifndef CUSTOM_HEIGHT_FOG_COMPUTE_FACTOR
#define CUSTOM_HEIGHT_FOG_COMPUTE_FACTOR(result, worldPos, fogHeightOffset, fogHeightScale) \
  result = CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME + CUSTOM_FOG_HEIGHT_FOG_START_Y_NAME; \
  result = ((worldPos.y * fogHeightScale) + fogHeightOffset) + -result; \
  result = clamp(result / CUSTOM_FOG_HEIGHT_FOG_HEIGHT_NAME, 0, 1); \
  result = (-result * 2 + 3) * (result * result)
#endif

inline float4 ComputeScreenPosCustom(float4 pos)
{
    float4 screenPos = ComputeNonStereoScreenPos(pos);
    #if defined(UNITY_SINGLE_PASS_STEREO) || defined(STEREO_INSTANCING_ON) || defined(STEREO_MULTIVIEW_ON)
    float eyeOffset = (unity_StereoEyeIndex * (_StereoCameraEyeOffset + _StereoCameraEyeOffset)) + -
        _StereoCameraEyeOffset;
    screenPos.x = pos.w * eyeOffset + screenPos.x;
    #if !UNITY_UV_STARTS_AT_TOP
    screenPos.y = -screenPos.y + pos.w;
    #endif
    #endif
    return screenPos;
}

#ifdef BLOOM_FOG

float2 _CustomFogTextureToScreenRatio;
sampler2D _BloomPrePassTexture;

#define CUSTOM_FOG_COMPUTE_UV(screenPos) \
  float2 customFogUV = screenPos.xy / screenPos.w; \
  customFogUV = (customFogUV + -0.5) * _CustomFogTextureToScreenRatio + 0.5

#define BLOOM_PREPASS_SAMPLE(screenPos) \
  CUSTOM_FOG_COMPUTE_UV(screenPos); \
  float4 bloomPrepassCol = float4(tex2D(_BloomPrePassTexture, customFogUV).rgb, 0)

#else

#define BLOOM_PREPASS_SAMPLE(screenPos) \
  float4 bloomPrepassCol = 0

#endif

#if defined(_FOGTYPE_LERP) || (!defined(_FOGTYPE_LERP) && !defined(_FOGTYPE_COLOR) && !defined(_FOGTYPE_ALPHA))
#define BLOOM_FOG_BLEND(col, bloomfogCol) \
  col = bloomfogCol;
#endif
#ifdef _FOGTYPE_COLOR
#define BLOOM_FOG_BLEND(col, bloomfogCol) \
  col.rgb = bloomfogCol.rgb;
#endif
#ifdef _FOGTYPE_ALPHA
#define BLOOM_FOG_BLEND(col, bloomfogCol) \
  col.a = bloomfogCol.a;
#endif

#define BLOOM_FOG_APPLY_CALCULATED_FACTOR(col, screenPos, fogFactor) \
  BLOOM_PREPASS_SAMPLE(screenPos); \
  float4 bloomfogCol = fogFactor * (-col + bloomPrepassCol) + col; \
  BLOOM_FOG_BLEND(col, bloomfogCol)

#define BLOOM_FOG_APPLY(col, screenPos, worldPos, fogStartOffset, fogScale) \
  float customFogFactor; \
  CUSTOM_FOG_COMPUTE_FACTOR(customFogFactor, distanceSquared(worldPos), fogStartOffset, fogScale); \
  BLOOM_FOG_APPLY_CALCULATED_FACTOR(col, screenPos, customFogFactor)

#define BLOOM_FOG_HEIGHT_APPLY_CALCULATED_FACTOR(col, screenPos, fogFactor, heightFogFactor) \
  BLOOM_PREPASS_SAMPLE(screenPos); \
  fogFactor = -fogFactor + 1; \
  float4 bloomfogCol = (heightFogFactor * -fogFactor + 1) * (-col + bloomPrepassCol) + col; \
  BLOOM_FOG_BLEND(col, bloomfogCol)

#define BLOOM_FOG_HEIGHT_APPLY(col, screenPos, worldPos, fogStartOffset, fogScale, fogHeightOffset, fogHeightScale) \
  float customHeightFogFactor; \
  float customFogFactor; \
  CUSTOM_HEIGHT_FOG_COMPUTE_FACTOR(customHeightFogFactor, worldPos, fogHeightOffset, fogHeightScale); \
  CUSTOM_FOG_COMPUTE_FACTOR(customFogFactor, distanceSquared(worldPos), fogStartOffset, fogScale); \
  BLOOM_FOG_HEIGHT_APPLY_CALCULATED_FACTOR(col, screenPos, customFogFactor, customHeightFogFactor)

#define BLOOM_FOG_APPLY_TRANSPARENT_CALCULATED_FACTOR(col, fogFactor) \
  fogFactor = (-fogFactor + 1) * col.a; \
  float4 bloomfogCol = float4(fogFactor * col.rgb, fogFactor); \
  BLOOM_FOG_BLEND(col, bloomfogCol)

#define BLOOM_FOG_APPLY_TRANSPARENT(col, worldPos, fogStartOffset, fogScale) \
  float customFogFactor; \
  CUSTOM_FOG_COMPUTE_FACTOR(customFogFactor, distanceSquared(worldPos), fogStartOffset, fogScale); \
  BLOOM_FOG_APPLY_TRANSPARENT_CALCULATED_FACTOR(col, customFogFactor)

#endif // BLOOM_FOG_CG_INCLUDED
