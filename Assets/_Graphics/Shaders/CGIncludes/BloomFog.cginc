float _StereoCameraEyeOffset;

// These are the global variable names the game uses by default,
// certain mods might want to use their own attenuation/offset variable names.
float _CustomFogAttenuation;
float _CustomFogOffset;

#define CUSTOM_FOG_COMPUTE_FACTOR(distance, fogStartOffset, fogScale) \
  float customFogFactor = max(dot(distance, distance) + -fogStartOffset, 0); \
  customFogFactor = max(customFogFactor * fogScale + -_CustomFogOffset, 0); \
  customFogFactor = 1 / (customFogFactor * _CustomFogAttenuation + 1); \
  customFogFactor = -customFogFactor + 1

float _CustomFogHeightFogStartY;
float _CustomFogHeightFogHeight;

#define CUSTOM_FOG_HEIGHT_FOG_COMPUTE_FACTOR(worldPos, fogHeightOffset, fogHeightScale) \
  float customFogHeightFogFactor = _CustomFogHeightFogHeight + _CustomFogHeightFogStartY; \
  customFogHeightFogFactor = ((worldPos.y * fogHeightScale) + fogHeightOffset) + -customFogHeightFogFactor; \
  customFogHeightFogFactor = clamp(customFogHeightFogFactor / _CustomFogHeightFogHeight, 0, 1); \
  customFogHeightFogFactor = (-customFogHeightFogFactor * 2 + 3) * (customFogHeightFogFactor * customFogHeightFogFactor)

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

float2 _CustomFogTextureToScreenRatio;
sampler2D _BloomPrePassTexture;

#define CUSTOM_FOG_COMPUTE_UV(screenPos) \
  float2 customFogUV = screenPos.xy / screenPos.w; \
  customFogUV = (customFogUV + -0.5) * _CustomFogTextureToScreenRatio + 0.5

#define BLOOM_PREPASS_SAMPLE(screenPos) \
  CUSTOM_FOG_COMPUTE_UV(screenPos); \
  float4 bloomPrepassCol = float4(tex2D(_BloomPrePassTexture, customFogUV).rgb, 0)

#define BLOOM_PREPASS_SAMPLE(screenPos) \
  float4 bloomPrepassCol = float4(0,0,0,0)

#define BLOOM_FOG_APPLY(col, screenPos, worldPos, fogStartOffset, fogScale) \
  float3 bloomFogDistance = worldPos - _WorldSpaceCameraPos; \
  CUSTOM_FOG_COMPUTE_FACTOR(bloomFogDistance, fogStartOffset, fogScale); \
  BLOOM_PREPASS_SAMPLE(screenPos); \
  col = customFogFactor * (-col + bloomPrepassCol) + col

#define BLOOM_FOG_HEIGHT_FOG_APPLY(col, screenPos, worldPos, fogStartOffset, fogScale, fogHeightOffset, fogHeightScale) \
  float3 bloomFogDistance = worldPos - _WorldSpaceCameraPos; \
  CUSTOM_FOG_HEIGHT_FOG_COMPUTE_FACTOR(worldPos, fogHeightOffset, fogHeightScale); \
  CUSTOM_FOG_COMPUTE_FACTOR(bloomFogDistance, fogStartOffset, fogScale); \
  BLOOM_PREPASS_SAMPLE(screenPos); \
  customFogFactor = -customFogFactor + 1; \
  col = (customFogHeightFogFactor * -customFogFactor + 1) * (-col + bloomPrepassCol) + col

#define BLOOM_FOG_APPLY_TRANSPARENT(col, worldPos, fogStartOffset, fogScale) \
  float3 bloomFogDistance = worldPos - _WorldSpaceCameraPos; \
  CUSTOM_FOG_COMPUTE_FACTOR(bloomFogDistance, fogStartOffset, fogScale); \
  customFogFactor = (-customFogFactor + 1) * col.a; \
  col = float4(customFogFactor * col.rgb, customFogFactor)