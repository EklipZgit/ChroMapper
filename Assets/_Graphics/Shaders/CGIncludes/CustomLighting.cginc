#ifndef CUSTOM_LIGHTING_CG_INCLUDED
#define CUSTOM_LIGHTING_CG_INCLUDED

#define MAX_DIRECTIONAL_LIGHTS 5
#define MAX_POINT_LIGHTS 1
#define LIGHT_CALCULATE_NAME __light_calculate
#define LIGHT_ITERATOR_NAME __light_id

uniform float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];
uniform float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
#ifdef LIGHT_FALLOFF
uniform float4 _DirectionalLightPositions[MAX_DIRECTIONAL_LIGHTS];
uniform float _DirectionalLightRadii[MAX_DIRECTIONAL_LIGHTS];
#endif
uniform float4 _PointLightPositions[MAX_POINT_LIGHTS];
uniform float4 _PointLightColors[MAX_POINT_LIGHTS];

#ifdef PRIVATE_POINT_LIGHT
#ifndef UNITY_INSTANCING_ENABLED
float4 _PrivatePointLightColor;
#endif
float _PrivatePointLightIntensity;
float4 _PrivatePointLightPosition;
#endif

#ifdef LIGHT_FALLOFF
#define GET_LIGHT_FALLOFF_ATTENUATION(worldPos, lightPos, lightRad) \
    saturate(1 - distance(worldPos, lPos) / lRad)
#else
#define GET_LIGHT_FALLOFF_ATTENUATION(worldPos, lightPos, lightRad) \
    1
#endif

#ifdef DIFFUSE
#define __CALCULATE_DIFFUSE(worldNormal, lDir, lCol, attenuation) \
    albedo * lCol * max(0, dot(worldNormal, lDir)) * attenuation
#ifdef BOTH_SIDES_DIFFUSE
#define CALCULATE_DIFFUSE(worldNormal, lDir, lCol, attenuation) \
    __CALCULATE_DIFFUSE(worldNormal, lDir, lCol, attenuation) + __CALCULATE_DIFFUSE(worldNormal, -lDir, lCol, attenuation)
#else
#define CALCULATE_DIFFUSE(worldNormal, lDir, lCol, attenuation) \
    __CALCULATE_DIFFUSE(worldNormal, lDir, lCol, attenuation)
#endif
#else
#define CALCULATE_DIFFUSE(worldNormal, lDir, lCol, attenuation) \
    0
#endif

#ifdef SPECULAR
#define CALCULATE_SPECULAR(specular, albedo, metallic, smoothness, lDir, lCol, worldPos, worldNormal, attenuation) \
    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos); \
    float4 specColor = lerp(0.04, albedo, metallic); \
    float3 halfDir = normalize(lDir + viewDir); \
    float ndoth = max(0, dot(worldNormal, halfDir)); \
    float specIntensity = pow(ndoth, smoothness); \
    specular = specColor * lCol * specIntensity * attenuation
#else
#define CALCULATE_SPECULAR(specular, albedo, metallic, smoothness, lDir, lCol, worldPos, worldNormal, attenuation)
#endif

#define CALCULATE_DIRECTIONAL_LIGHTING(calculated, albedo, metallic, smoothness, lPos, lRad, lDir, lCol, worldPos, worldNormal) \
    float attenuation = GET_LIGHT_FALLOFF_ATTENUATION(worldPos, lPos, lRad); \
    float4 diffuse = CALCULATE_DIFFUSE(worldNormal, lDir, lCol, attenuation); \
    float4 specular = 0; \
    CALCULATE_SPECULAR(specular, albedo, metallic, smoothness, lDir, lCol, worldPos, worldNormal, attenuation); \
    calculated = diffuse + specular

#define CALCULATE_POINT_LIGHTING(calculated, lPos, lCol, worldPos, worldNormal) \
    float3 lightDir = lPos - worldPos; \
    lightDir = normalize(lightDir); \
    float diff = max(0, dot(worldNormal, lightDir)); \
    calculated = lCol * diff

#ifdef LIGHT_FALLOFF
#define GET_LIGHT_FALLOFF_PROP(lightPosition, lightRadii)\
    float3 lPos = lightPosition.xyz; \
    float lRad = lightRadii
#else
#define GET_LIGHT_FALLOFF_PROP(lightPosition, lightRadii) \
    float3 lPos = 0; \
    float lRad = 0
#endif

#ifdef UNITY_INSTANCING_ENABLED
#define GET_PRIVATE_POINT_LIGHT_COLOR() \
    UNITY_ACCESS_INSTANCED_PROP(Props, _PrivatePointLightColor)
#else
#define GET_PRIVATE_POINT_LIGHT_COLOR() \
    _PrivatePointLightColor
#endif
#endif

#ifdef PRIVATE_POINT_LIGHT
#define CALCULATE_PRIVATE_POINT_LIGHT(result, worldPos, worldNormal) \
    float4 plCol = GET_PRIVATE_POINT_LIGHT_COLOR(); \
    CALCULATE_POINT_LIGHTING(LIGHT_CALCULATE_NAME, _PrivatePointLightPosition, plCol * _PrivatePointLightIntensity, worldPos, worldNormal); \
    result += color * LIGHT_CALCULATE_NAME
#define CALCULATE_AVERAGE(result) \
    result /= MAX_DIRECTIONAL_LIGHTS + MAX_POINT_LIGHTS + 1
#else
#define CALCULATE_PRIVATE_POINT_LIGHT(result, worldPos, worldNormal) \
    result
#define CALCULATE_AVERAGE(result) \
    result /= MAX_DIRECTIONAL_LIGHTS + MAX_POINT_LIGHTS
#endif

#define CUSTOM_LIGHTING_APPLY(result, color, metallic, smoothness, worldPos, worldNormal) \
    int LIGHT_ITERATOR_NAME; \
    float4 LIGHT_CALCULATE_NAME = 0; \
    [unroll(MAX_DIRECTIONAL_LIGHTS)] \
    for (LIGHT_ITERATOR_NAME = 0; LIGHT_ITERATOR_NAME < MAX_DIRECTIONAL_LIGHTS; LIGHT_ITERATOR_NAME++) \
    { \
        GET_LIGHT_FALLOFF_PROP(_DirectionalLightPositions[LIGHT_ITERATOR_NAME], _DirectionalLightRadii[LIGHT_ITERATOR_NAME]); \
        float3 lDir = normalize(_DirectionalLightDirections[LIGHT_ITERATOR_NAME].xyz); \
        float4 lCol = _DirectionalLightColors[LIGHT_ITERATOR_NAME]; \
        CALCULATE_DIRECTIONAL_LIGHTING(LIGHT_CALCULATE_NAME, color, metallic, smoothness, lPos, lRad, lDir, lCol, worldPos, worldNormal); \
        result += LIGHT_CALCULATE_NAME; \
    } \
    [unroll(MAX_POINT_LIGHTS)] \
    for (LIGHT_ITERATOR_NAME = 0; LIGHT_ITERATOR_NAME < MAX_POINT_LIGHTS; LIGHT_ITERATOR_NAME++) \
    { \
        float3 lPos = _PointLightPositions[LIGHT_ITERATOR_NAME].xyz; \
        float4 lCol = _PointLightColors[LIGHT_ITERATOR_NAME]; \
        CALCULATE_POINT_LIGHTING(LIGHT_CALCULATE_NAME, lPos, lCol, worldPos, worldNormal); \
        result += color * LIGHT_CALCULATE_NAME; \
    } \
    CALCULATE_PRIVATE_POINT_LIGHT(result, worldPos, worldNormal); \
    CALCULATE_AVERAGE(result)
