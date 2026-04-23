#ifndef CUSTOM_LIGHTING_CG_INCLUDED
#define CUSTOM_LIGHTING_CG_INCLUDED

#define MAX_DIRECTIONAL_LIGHTS 5
// don't ask me why, they apparently only use 4
#define MAX_DIRECTIONAL_LIGHTS_ITER 4
uniform float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHTS];
uniform float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHTS];
uniform float4 _DirectionalLightPositions[MAX_DIRECTIONAL_LIGHTS];
uniform float _DirectionalLightRadii[MAX_DIRECTIONAL_LIGHTS];

#define MAX_POINT_LIGHTS 1
uniform float4 _PointLightPositions[MAX_POINT_LIGHTS];
uniform float4 _PointLightColors[MAX_POINT_LIGHTS];

float _PrivatePointLightIntensity;
float4 _PrivatePointLightPosition;

#if defined(UNITY_INSTANCING_ENABLED)
#define GET_PRIVATE_POINT_LIGHT_COLOR UNITY_ACCESS_INSTANCED_PROP(Props, _PrivatePointLightColor) * _PrivatePointLightIntensity
#else
#define GET_PRIVATE_POINT_LIGHT_COLOR _PrivatePointLightColor * _PrivatePointLightIntensity
#endif

#if defined(POINT_LIGHT_IS_LOCAL)
#define GET_PRIVATE_POINT_LIGHT_POSITION(worldPos) mul(unity_ObjectToWorld, float4(_PrivatePointLightPosition.xyz, 1.0)).xyz
#else
#define GET_PRIVATE_POINT_LIGHT_POSITION(worldPos) _PrivatePointLightPosition.xyz
#endif

inline float calculate_falloff(float3 worldPos, float3 lightPos, float lightRad)
{
    float3 dist = worldPos.xyz - lightPos.xyz;
    return 1 / (dot(dist, dist) / (lightRad * lightRad) * 25 + 1);
}

#define __LAMBERT(worldNormal, lightDir) \
    max(0, dot(worldNormal, lightDir))

// twice for nice falloff
#define __HALF_LAMBERT(worldNormal, lightDir) \
    (dot(worldNormal, lightDir) * 0.5 + 0.5) * (dot(worldNormal, lightDir) * 0.5 + 0.5)

#if defined(HALF_LAMBERT)
#define __CALCULATE_DIFFUSE(color, worldNormal, lightDir) \
    color * __HALF_LAMBERT(worldNormal, lightDir)
#else
#define __CALCULATE_DIFFUSE(color, worldNormal, lightDir) \
    color * __LAMBERT(worldNormal, lightDir)
#endif
#define CALCULATE_DIFFUSE(color, worldNormal, lightDir) \
    __CALCULATE_DIFFUSE(color, worldNormal, lightDir)

inline float3 calculate_diffuse_lighting(float3 lightCol, float3 lightDir, float3 worldNormal, float falloff,
                                         float otherDiffuseMul)
{
    float3 accumulated = 0;
    float3 calculated = CALCULATE_DIFFUSE(lightCol, worldNormal, lightDir);
    calculated *= falloff;
    accumulated += calculated;

    #if defined(BOTH_SIDES_DIFFUSE)
    calculated = CALCULATE_DIFFUSE(lightCol, -worldNormal, lightDir);
    calculated *= falloff;
    calculated *= otherDiffuseMul;
    accumulated += calculated;
    #endif

    return accumulated;
}

inline float3 calculate_global_diffuse_lighting(float3 worldPos, float3 worldNormal, float3 pvtPointLightPos, float3 pvtPointLightCol, float otherDiffuse)
{
    #if !defined(DIFFUSE)
    return 0;
    #endif
    float3 accumulated = 0;
    float3 lightCol, lightVec, lightDir, lightPos;
    float lightRadii, distSq, falloff = 1;
    int i;

    // unused
    // [unroll]
    // for (i = 0; i < MAX_POINT_LIGHTS; i++)
    // {
    //     lightCol = _PointLightColors[i].rgb;
    //     lightPos = _PointLightPositions[i].xyz;
    //     lightVec = lightPos - worldPos;
    //     lightDir = pointLightVec / sqrt(pointLightDistSq);
    //
    //     #if defined(LIGHT_FALLOFF)
    //     distSq = max(dot(pointLightVec, pointLightVec), 0.00001);
    //     falloff = 1 / distSq;
    //     #endif
    //     accumulated.rgb += calculate_diffuse_lighting(lightCol, lightDir, worldNormal, falloff, otherDiffuse);
    // }

    #if defined(PRIVATE_POINT_LIGHT)
    lightCol = pvtPointLightCol;
    lightPos = pvtPointLightPos;
    lightVec = lightPos - worldPos;
    distSq = max(dot(lightVec, lightVec), 0.00001);
    lightDir = lightVec / sqrt(distSq);

    #if defined(LIGHT_FALLOFF)
    falloff = 1 / distSq;
    #endif
    accumulated.rgb += calculate_diffuse_lighting(lightCol, lightDir, worldNormal, falloff, otherDiffuse);
    #endif

    [unroll]
    for (i = 0; i < MAX_DIRECTIONAL_LIGHTS_ITER; i++)
    {
        lightCol = _DirectionalLightColors[i].rgb;
        lightDir = _DirectionalLightDirections[i].xyz;
        lightPos = _DirectionalLightPositions[i];
        lightRadii = _DirectionalLightRadii[i];
        #if defined(LIGHT_FALLOFF)
        falloff = calculate_falloff(worldPos, lightPos, lightRadii);
        #endif
        accumulated.rgb += calculate_diffuse_lighting(lightCol, lightDir, worldNormal, falloff, otherDiffuse);
    }

    return accumulated;
}

inline float3 calculate_global_diffuse_lighting(float3 worldPos, float3 worldNormal, float otherDiffuse)
{
    return calculate_global_diffuse_lighting(worldPos, worldNormal, 0, 0, otherDiffuse);
}

inline float3 calculate_global_diffuse_lighting(float3 worldPos, float3 worldNormal)
{
    return calculate_global_diffuse_lighting(worldPos, worldNormal, 0, 0, 0);
}

// GGX Specular
inline float3 calculate_specular_lighting(float3 lightCol, float3 lightDir, float3 reflDir, float smoothness,
                                          float falloff)
{
    float smoothnessSq = smoothness * smoothness;
    float specPower = smoothnessSq * smoothnessSq * 500;

    float3 dist = lightDir - reflDir;
    float distSq = dot(dist, dist);

    float specTerm = saturate(1 - distSq * specPower * 0.5);
    specTerm *= specTerm;
    specTerm *= specTerm;
    specTerm *= specTerm;

    return specTerm * lightCol * smoothnessSq * 500.0 * falloff;
}

inline float3 calculate_reflection_direction(float3 worldPos, float3 worldNormal)
{
    float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos);
    float VdotN = dot(viewDir, worldNormal);
    return worldNormal * (-2 * VdotN) + viewDir;
}

inline float3 calculate_global_specular_lighting(float3 worldPos, float3 worldNormal, float smoothness)
{
    #if !defined(SPECULAR)
    return 0;
    #endif
    float3 reflDir = calculate_reflection_direction(worldPos, worldNormal);

    float3 accumulated = 0;
    float falloff = 1;
    int i;
    [unroll]
    for (i = 0; i < MAX_DIRECTIONAL_LIGHTS_ITER; i++)
    {
        float3 lightCol = _DirectionalLightColors[i].rgb;
        float3 lightDir = _DirectionalLightDirections[i].xyz;

        #if defined(LIGHT_FALLOFF)
        float3 lightPos = _DirectionalLightPositions[i].xyz;
        float3 lightRadii = _DirectionalLightRadii[i];
        falloff = calculate_falloff(worldPos, lightPos, lightRadii);
        #endif

        accumulated += calculate_specular_lighting(lightCol, lightDir, reflDir, smoothness, falloff);
    }

    return accumulated;
}

inline float3 get_one_minus_reflectivity(float metallic)
{
    return 0.96 * (1 - metallic);
}

inline float3 calculate_final_lighting(float3 diffuseColor, float3 specularLighting, float metallic)
{
    return diffuseColor * get_one_minus_reflectivity(metallic) + specularLighting;
}

inline float3 calculate_global_lighting(float3 worldPos, float3 worldNormal, float3 baseColor, float3 pvtPointLightPos,
                                        float3 pvtPointLightCol, float metallic,
                                        float smoothness, float otherDiffuseMul, float specIntensity)
{
    float3 diffuseLighting = calculate_global_diffuse_lighting(
        worldPos, worldNormal, pvtPointLightPos, pvtPointLightCol, otherDiffuseMul);
    float3 specularLighting = calculate_global_specular_lighting(worldPos, worldNormal, smoothness);

    float3 diffuseColor = diffuseLighting * baseColor;
    float3 f0 = metallic * (diffuseColor - 0.04) + 0.04;
    specularLighting *= f0 * specIntensity;

    return calculate_final_lighting(diffuseColor, specularLighting, metallic);
}

inline float3 calculate_global_lighting(float3 worldPos, float3 worldNormal, float3 baseColor, float metallic,
                                        float smoothness, float otherDiffuseMul, float specIntensity)
{
    return calculate_global_lighting(worldPos, worldNormal, baseColor, metallic,
                                     smoothness, otherDiffuseMul, specIntensity);
}

inline float3 calculate_camera_lighting(float3 worldPos, float3 worldNormal, float3 baseColor, float metallic,
                                        float smoothness, float falloff, float otherDiffuseMul, float specIntensity)
{
    float3 lightDir = normalize(_WorldSpaceCameraPos - worldPos);
    float3 reflDir = calculate_reflection_direction(worldPos, worldNormal);

    float3 diffuseLighting = calculate_diffuse_lighting(1, lightDir, worldNormal, falloff, otherDiffuseMul);
    float3 specularLighting = calculate_specular_lighting(1, lightDir, reflDir, smoothness, falloff);

    float3 diffuseColor = diffuseLighting * baseColor;
    float3 f0 = metallic * (diffuseColor - 0.04) + 0.04;
    specularLighting *= f0 * specIntensity;

    return calculate_final_lighting(diffuseColor, specularLighting, metallic);
}

inline float3 calculate_lighting(float3 worldPos, float3 worldNormal, float3 lightColor, float3 lightPos,
                                 float3 baseColor, float metallic, float smoothness, float falloff,
                                 float otherDiffuseMul, float specIntensity)
{
    float3 reflDir = calculate_reflection_direction(worldPos, worldNormal);
    float3 lightVec = lightPos - _WorldSpaceCameraPos;
    float distSq = max(dot(lightVec, lightVec), 0.00001);
    float3 lightDir = lightVec / sqrt(distSq);

    float3 diffuseLighting = calculate_diffuse_lighting(baseColor, lightDir, worldNormal, falloff, otherDiffuseMul);
    float3 specularLighting = calculate_specular_lighting(lightColor, lightDir, reflDir, smoothness, falloff);

    float3 diffuseColor = diffuseLighting * baseColor;
    float3 f0 = metallic * (diffuseColor - 0.04) + 0.04;
    specularLighting *= f0 * specIntensity;

    return calculate_final_lighting(diffuseColor, specularLighting, metallic);
}

#endif
