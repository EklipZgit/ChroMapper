#ifndef CUSTOM_LIGHTING_CG_INCLUDED
#define CUSTOM_LIGHTING_CG_INCLUDED

uniform float4 _DirectionalLightDirections[5];
uniform float4 _DirectionalLightPositionsRadii[5];
uniform float4 _DirectionalLightColors[5];
uniform float4 _PointLightPositions[1];
uniform float4 _PointLightColors[1];

#if PRIVATE_POINT_LIGHT
float4 _PrivatePointLightColor;
float _PrivatePointLightIntensity;
float4 _PrivatePointLightPosition;
#endif

float3 directionalLighting(float3 albedo, float metallic, float smoothness, float3 lPos, float lRad, float3 lDir,
                           float4 lCol, float3 worldPos, float normal)
{
    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

    // idk why position is provided so maybe figure it out?
    // float dist = distance(worldPos, lPos);
    // float atten = saturate(1 - dist / lRad);
    float atten = 1;

    float3 diffuse = 0;
    float3 specular = 0;

    #if DIFFUSE
    float ndotl = max(0, dot(normal, lDir));
    diffuse = albedo * lCol * ndotl * atten;
    #endif

    #if SPECULAR
    // technically should be texture but whatever
    float3 specColor = lerp(0.04, albedo, metallic);
    float3 halfDir = normalize(lDir + viewDir);
    float ndoth = max(0, dot(normal, halfDir));
    float specIntensity = pow(ndoth, smoothness);
    specular = specColor * lCol * specIntensity * atten;
    #endif

    return diffuse + specular;
}

float3 pointLighting(float3 lPos, float4 lCol, float3 worldPos, float worldNormal)
{
    float3 lightDir = lPos - worldPos;
    lightDir = normalize(lightDir);

    float diff = max(0, dot(worldNormal, lightDir));

    return lCol * diff;
}

float3 applyCustomLighting(float3 color, float metallic, float smoothness, float3 worldPos, float worldNormal)
{
    worldNormal = normalize(worldNormal);
    // TODO: not sure if it will be 0 regardless if any light or diffuse enabled at all
    float3 calculated = 0;

    int l;
    for (l = 0; l < 5; l++)
    {
        float3 lPos = _DirectionalLightPositionsRadii[l].xyz;
        float lRad = _DirectionalLightPositionsRadii[l].w;
        float3 lDir = normalize(_DirectionalLightDirections[l].xyz);
        float4 lCol = _DirectionalLightColors[l];

        calculated += directionalLighting(color, metallic, smoothness, lPos, lRad, lDir, lCol, worldPos, worldNormal);
    }

    for (l = 0; l < 1; l++)
    {
        float3 lPos = _PointLightPositions[l].xyz;
        float4 lCol = _PointLightColors[l];

        calculated += color * pointLighting(lPos, lCol, worldPos, worldNormal);
    }

    #if PRIVATE_POINT_LIGHT
    float4 plCol = UNITY_ACCESS_INSTANCED_PROP(Props, _PrivatePointLightColor);
    calculated += color * pointLighting(_PrivatePointLightPosition, plCol * _PrivatePointLightIntensity,
                                        worldPos, normal);
    calculated /= 7;
    #else
    calculated /= 6;
    #endif

    return calculated;
}


#endif
