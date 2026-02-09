#ifndef CUSTOM_LIGHTING_CG_INCLUDED
#define CUSTOM_LIGHTING_CG_INCLUDED

#define MAX_DIRECTIONAL_LIGHTS 5
#define MAX_POINT_LIGHTS 1
#define LIGHT_CALCULATE_NAME __light_calculate
#define LIGHT_ITERATOR_NAME __light_id

uniform float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];
uniform float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
uniform float4 _DirectionalLightPositions[MAX_DIRECTIONAL_LIGHTS];
uniform float _DirectionalLightRadii[MAX_DIRECTIONAL_LIGHTS];
uniform float4 _PointLightPositions[MAX_POINT_LIGHTS];
uniform float4 _PointLightColors[MAX_POINT_LIGHTS];

float _PrivatePointLightIntensity;
float4 _PrivatePointLightPosition;

#if defined(LIGHT_FALLOFF)
#define GET_LIGHT_FALLOFF_ATTENUATION(worldPos, lightPos, lightRad) \
    saturate(1 - distance(worldPos, lightPos) / lightRad)
#else
#define GET_LIGHT_FALLOFF_ATTENUATION(worldPos, lightPos, lightRad) \
    1
#endif

#define __LAMBERT(worldNormal, lightDir) \
    max(0, dot(worldNormal, lightDir))

// twice for nice falloff
#define __HALF_LAMBERT(worldNormal, lightDir) \
    (dot(worldNormal, lightDir) * 0.5 + 0.5) * (dot(worldNormal, lightDir) * 0.5 + 0.5)

#if defined(DIFFUSE)
#if defined(HALF_LAMBERT)
#define __CALCULATE_DIFFUSE(albedo, metallic, worldNormal, lightDir) \
    albedo * __HALF_LAMBERT(worldNormal, lightDir) * (1 - metallic)
#else
#define __CALCULATE_DIFFUSE(albedo, metallic, worldNormal, lightDir) \
    albedo * __LAMBERT(worldNormal, lightDir) * (1 - metallic)
#endif
#if defined(BOTH_SIDES_DIFFUSE)
#define CALCULATE_DIFFUSE(albedo, metallic, otherDiffuse, worldNormal, lightDir) \
    __CALCULATE_DIFFUSE(albedo, metallic, worldNormal, lightDir) + ((__CALCULATE_DIFFUSE(albedo, metallic, worldNormal, -lightDir)) * otherDiffuse)
#else
#define CALCULATE_DIFFUSE(albedo, metallic, otherDiffuse, worldNormal, lightDir) \
    __CALCULATE_DIFFUSE(albedo, metallic, worldNormal, lightDir)
#endif
#else
#define CALCULATE_DIFFUSE(albedo, metallic, otherDiffuse, worldNormal, lightDir) \
    0
#endif

// holy fuck i'd rather use function
#if defined(SPECULAR)
#if defined(LOW_QUALITY_SHADER)
// Blinn-Phong
#define CALCULATE_SPECULAR(result, albedo, metallic, smoothness, specIntensity, lightDir, worldPos, worldNormal) \
    0; \
    float3 specViewDir = normalize(_WorldSpaceCameraPos - worldPos); \
    float3 specHalfDir = normalize(lightDir + specViewDir); \
    float specPower = smoothness * 128; \
    float specNDotH = saturate(dot(worldNormal, specHalfDir)); \
    float specVDotH = saturate(dot(specViewDir, specHalfDir)); \
    float specNormal = (specPower + 8) / (8 * 3.14159265); \
    float3 specF0 = lerp(0.04, albedo, metallic); \
    float3 specFresnel = specF0 + (1 - specF0) * pow(1 - specVDotH, 5); \
    result = pow(specNDotH, specPower) * specNormal * specFresnel * specIntensity * dot(worldNormal, lightDir)
#else
// GGX
#define CALCULATE_SPECULAR(result, albedo, metallic, smoothness, specIntensity, lightDir, worldPos, worldNormal) \
    0; \
    float3 specViewDir = normalize(_WorldSpaceCameraPos - worldPos); \
    float3 specHalfDir = normalize(lightDir + specViewDir); \
    float specNDotH = saturate(dot(worldNormal, specHalfDir)); \
    float specNDotV = saturate(dot(worldNormal, specViewDir)); \
    float specNDotL = saturate(dot(worldNormal, lightDir)); \
    float specVDotH = saturate(dot(specViewDir, specHalfDir)); \
    float specRoughness = 1 - smoothness; \
    float specAlpha = specRoughness * specRoughness; \
    float specAlpha2 = specAlpha * specAlpha; \
    float specDenom = (specNDotH * specNDotH) * (specAlpha2 - 1) + 1; \
    float specDistribution = specAlpha2 / (3.14159265 * specDenom * specDenom); \
    float3 specF0 = lerp(0.04, albedo, metallic); \
    float3 specFresnel = specF0 + (1 - specF0) * pow(1 - specVDotH, 5); \
    float specK = (specAlpha + 1) * (specAlpha + 1) / 8; \
    float specGV = specNDotV / (specNDotV * (1 - specK) + specK); \
    float specGL = specNDotL / (specNDotL * (1 - specK) + specK); \
    float specG = specGV * specGL; \
    result = (specDistribution * specFresnel * specG) / (4 * specNDotV * specNDotL + 0.001) * specNDotL * specIntensity
#endif
#else
#define CALCULATE_SPECULAR(result, albedo, metallic, smoothness, specIntensity, lightDir, worldPos, worldNormal) \
    0
#endif

#define CALCULATE_DIRECTIONAL_LIGHTING(result, albedo, metallic, smoothness, specularIntensity, otherDiffuse, lightPos, lightRad, lightDir, lightColor, worldPos, worldNormal) \
    float attenuation = GET_LIGHT_FALLOFF_ATTENUATION(worldPos, lightPos, lightRad); \
    float3 diffuse = CALCULATE_DIFFUSE(albedo, metallic, otherDiffuse, worldNormal, lightDir); \
    float3 specular = CALCULATE_SPECULAR(specular, albedo, metallic, smoothness, specularIntensity, lightDir, worldPos, worldNormal); \
    result = (diffuse * attenuation + specular * attenuation) * lightColor

#define CALCULATE_POINT_LIGHTING(result, albedo, metallic, smoothness, specularIntensity, otherDiffuse, lightPos, lightColor, lightIntensity, worldPos, worldNormal) \
    float3 lightDir = normalize(lightPos - worldPos); \
    float3 diffuse = CALCULATE_DIFFUSE(albedo, metallic, otherDiffuse, worldNormal, lightDir); \
    float3 specular = CALCULATE_SPECULAR(specular, albedo, metallic, smoothness, specularIntensity, lightDir, worldPos, worldNormal); \
    result = (diffuse + specular) * lightColor * lightIntensity

#if defined(LIGHT_FALLOFF)
#define GET_LIGHT_FALLOFF_PROP(lightPosition, lightRadii)\
    float3 lightPos = lightPosition.xyz; \
    float lightRad = lightRadii
#else
#define GET_LIGHT_FALLOFF_PROP(lightPosition, lightRadii) \
    float3 lightPos = 0; \
    float lightRad = 0
#endif

#if defined(PRIVATE_POINT_LIGHT)

#if defined(UNITY_INSTANCING_ENABLED)
#define GET_PRIVATE_POINT_LIGHT_COLOR UNITY_ACCESS_INSTANCED_PROP(Props, _PrivatePointLightColor)
#else
#define GET_PRIVATE_POINT_LIGHT_COLOR _PrivatePointLightColor
#endif

#if defined(POINT_LIGHT_IS_LOCAL)
#define GET_PRIVATE_POINT_LIGHT_POSITION(worldPos) \
    worldPos + _PrivatePointLightPosition
#else
#define GET_PRIVATE_POINT_LIGHT_POSITION(worldPos) \
    _PrivatePointLightPosition
#endif

#define CALCULATE_PRIVATE_POINT_LIGHTING(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal) \
    float3 plightColor = GET_PRIVATE_POINT_LIGHT_COLOR; \
    float3 plightPos = GET_PRIVATE_POINT_LIGHT_POSITION(worldPos); \
    CALCULATE_POINT_LIGHTING(LIGHT_CALCULATE_NAME, color, metallic, smoothness, specularIntensity, otherDiffuse, plightPos, plightColor, _PrivatePointLightIntensity, worldPos, worldNormal); \
    result.rgb += LIGHT_CALCULATE_NAME
#define CALCULATE_AVERAGE(result) \
    result.rgb /= MAX_DIRECTIONAL_LIGHTS + MAX_POINT_LIGHTS + 1
#else
#define CALCULATE_PRIVATE_POINT_LIGHTING(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal)
#define CALCULATE_AVERAGE(result) \
    result.rgb /= MAX_DIRECTIONAL_LIGHTS + MAX_POINT_LIGHTS
#endif

#define CUSTOM_LIGHTING_APPLY(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal) \
    int LIGHT_ITERATOR_NAME; \
    float3 LIGHT_CALCULATE_NAME = 0; \
    [unroll(MAX_DIRECTIONAL_LIGHTS)] \
    for (LIGHT_ITERATOR_NAME = 0; LIGHT_ITERATOR_NAME < MAX_DIRECTIONAL_LIGHTS; LIGHT_ITERATOR_NAME++) \
    { \
        GET_LIGHT_FALLOFF_PROP(_DirectionalLightPositions[LIGHT_ITERATOR_NAME], _DirectionalLightRadii[LIGHT_ITERATOR_NAME]); \
        float3 lightDir = normalize(_DirectionalLightDirections[LIGHT_ITERATOR_NAME].xyz); \
        float3 lightColor = _DirectionalLightColors[LIGHT_ITERATOR_NAME]; \
        CALCULATE_DIRECTIONAL_LIGHTING(LIGHT_CALCULATE_NAME, color, metallic, smoothness, specularIntensity, otherDiffuse, lightPos, lightRad, lightDir, lightColor, worldPos, worldNormal); \
        result.rgb += LIGHT_CALCULATE_NAME; \
    } \
    [unroll(MAX_POINT_LIGHTS)] \
    for (LIGHT_ITERATOR_NAME = 0; LIGHT_ITERATOR_NAME < MAX_POINT_LIGHTS; LIGHT_ITERATOR_NAME++) \
    { \
        float3 lightPos = _PointLightPositions[LIGHT_ITERATOR_NAME].xyz; \
        float3 lightColor = _PointLightColors[LIGHT_ITERATOR_NAME]; \
        CALCULATE_POINT_LIGHTING(LIGHT_CALCULATE_NAME, color, metallic, smoothness, specularIntensity, otherDiffuse, lightPos, lightColor, 1, worldPos, worldNormal); \
        result.rgb += LIGHT_CALCULATE_NAME; \
    } \
    CALCULATE_PRIVATE_POINT_LIGHTING(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal)

// TODO: im not sure whether to average or let as is

#endif
