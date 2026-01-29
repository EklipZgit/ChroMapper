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

#define __LAMBERT(worldNormal, lightDir) \
    max(0, dot(worldNormal, lightDir))

// twice for nice falloff
#define __HALF_LAMBERT(worldNormal, lightDir) \
    (dot(worldNormal, lightDir) * 0.5 + 0.5) * (dot(worldNormal, lightDir) * 0.5 + 0.5)

#ifdef DIFFUSE
#ifdef HALF_LAMBERT
#define __CALCULATE_DIFFUSE(albedo, worldNormal, lDir, lCol) \
    albedo * lCol * __HALF_LAMBERT(worldNormal, lDir)
#else
#define __CALCULATE_DIFFUSE(albedo, worldNormal, lDir, lCol) \
    albedo * lCol * __LAMBERT(worldNormal, lDir)
#endif
#ifdef BOTH_SIDES_DIFFUSE
#define CALCULATE_DIFFUSE(albedo, otherDiffuse, worldNormal, lDir, lCol) \
    __CALCULATE_DIFFUSE(albedo, worldNormal, lDir, lCol) + ((__CALCULATE_DIFFUSE(albedo, worldNormal, -lDir, lCol)) * otherDiffuse)
#else
#define CALCULATE_DIFFUSE(albedo, otherDiffuse, worldNormal, lDir, lCol) \
    __CALCULATE_DIFFUSE(albedo, worldNormal, lDir, lCol)
#endif
#else
#define CALCULATE_DIFFUSE(albedo, otherDiffuse, worldNormal, lDir, lCol) \
    0
#endif

#ifdef SPECULAR
#define CALCULATE_SPECULAR(result, albedo, metallic, smoothness, specIntensity, lDir, lCol, worldPos, worldNormal) \
    0; \
    float specN = smoothness * 128; \
    float specBase = pow(saturate(dot(worldNormal, normalize(lDir + normalize(_WorldSpaceCameraPos - worldPos)))), specN); \
    float specNormal = (specN + 8) / (8 * 3.14159); \
    float4 specColor = lerp(1, albedo, metallic); \
    result = lCol * specBase * specNormal * specColor * specIntensity
#else
#define CALCULATE_SPECULAR(result, albedo, metallic, smoothness, specIntensity, lDir, lCol, worldPos, worldNormal) \
    0
#endif

#define CALCULATE_DIRECTIONAL_LIGHTING(result, albedo, metallic, smoothness, specularIntensity, otherDiffuse, lPos, lRad, lDir, lCol, worldPos, worldNormal) \
    float attenuation = GET_LIGHT_FALLOFF_ATTENUATION(worldPos, lPos, lRad); \
    float4 diffuse = CALCULATE_DIFFUSE(albedo, otherDiffuse, worldNormal, lDir, lCol); \
    float4 specular = CALCULATE_SPECULAR(specular, albedo, metallic, smoothness, specularIntensity, lDir, lCol, worldPos, worldNormal); \
    specular = lerp(specular, specular * albedo, metallic); \
    diffuse = diffuse * (1 - metallic); \
    result = diffuse * attenuation + specular * attenuation

#define CALCULATE_POINT_LIGHTING(result, albedo, metallic, smoothness, specularIntensity, otherDiffuse, lPos, lCol, worldPos, worldNormal) \
    float3 lDir = normalize(lPos - worldPos); \
    float4 diffuse = CALCULATE_DIFFUSE(albedo, otherDiffuse, worldNormal, lDir, lCol); \
    float4 specular = CALCULATE_SPECULAR(specular, albedo, metallic, smoothness, specularIntensity, lDir, lCol, worldPos, worldNormal); \
    specular = lerp(specular, specular * albedo, metallic); \
    diffuse = diffuse * (1 - metallic); \
    result = diffuse + specular

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
#define CALCULATE_PRIVATE_POINT_LIGHT(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal) \
    float4 plCol = GET_PRIVATE_POINT_LIGHT_COLOR(); \
    CALCULATE_POINT_LIGHTING(LIGHT_CALCULATE_NAME, color, metallic, smoothness, specularIntensity, otherDiffuse, _PrivatePointLightPosition, plCol * _PrivatePointLightIntensity, worldPos, worldNormal); \
    result += LIGHT_CALCULATE_NAME
#define CALCULATE_AVERAGE(result) \
    result /= MAX_DIRECTIONAL_LIGHTS + MAX_POINT_LIGHTS + 1
#else
#define CALCULATE_PRIVATE_POINT_LIGHT(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal) \
    0
#define CALCULATE_AVERAGE(result) \
    result /= MAX_DIRECTIONAL_LIGHTS + MAX_POINT_LIGHTS
#endif

#define CUSTOM_LIGHTING_APPLY(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal) \
    int LIGHT_ITERATOR_NAME; \
    float4 LIGHT_CALCULATE_NAME = 0; \
    [unroll(MAX_DIRECTIONAL_LIGHTS)] \
    for (LIGHT_ITERATOR_NAME = 0; LIGHT_ITERATOR_NAME < MAX_DIRECTIONAL_LIGHTS; LIGHT_ITERATOR_NAME++) \
    { \
        GET_LIGHT_FALLOFF_PROP(_DirectionalLightPositions[LIGHT_ITERATOR_NAME], _DirectionalLightRadii[LIGHT_ITERATOR_NAME]); \
        float3 lDir = normalize(_DirectionalLightDirections[LIGHT_ITERATOR_NAME].xyz); \
        float4 lCol = _DirectionalLightColors[LIGHT_ITERATOR_NAME]; \
        CALCULATE_DIRECTIONAL_LIGHTING(LIGHT_CALCULATE_NAME, color, metallic, smoothness, specularIntensity, otherDiffuse, lPos, lRad, lDir, lCol, worldPos, worldNormal); \
        result += LIGHT_CALCULATE_NAME; \
    } \
    [unroll(MAX_POINT_LIGHTS)] \
    for (LIGHT_ITERATOR_NAME = 0; LIGHT_ITERATOR_NAME < MAX_POINT_LIGHTS; LIGHT_ITERATOR_NAME++) \
    { \
        float3 lPos = _PointLightPositions[LIGHT_ITERATOR_NAME].xyz; \
        float4 lCol = _PointLightColors[LIGHT_ITERATOR_NAME]; \
        CALCULATE_POINT_LIGHTING(LIGHT_CALCULATE_NAME, color, metallic, smoothness, specularIntensity, otherDiffuse, lPos, lCol, worldPos, worldNormal); \
        result += LIGHT_CALCULATE_NAME; \
    } \
    CALCULATE_PRIVATE_POINT_LIGHT(result, color, metallic, smoothness, specularIntensity, otherDiffuse, worldPos, worldNormal); \
    CALCULATE_AVERAGE(result)
