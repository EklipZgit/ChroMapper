#ifndef BLURS_INCLUDED
#define BLURS_INCLUDED

// Simple kawase blur
float4 kawase(sampler2D blurTex, float2 uv, float radius, float2 texelSize)
{
    float4 blurColor = float4(0, 0, 0, 0);
            
    blurColor.rgb += tex2D(blurTex, uv).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(radius, radius) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(-radius, radius) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(radius, -radius) * texelSize).rgb;
    blurColor.rgb += tex2D(blurTex, uv + float2(-radius, -radius) * texelSize).rgb;
            
    blurColor.rgb /= 5.0;
    
    return blurColor;
}

// Simple box blur
// boxRadius defines how many pixels to sample in each direction
// e.g., boxRadius of 1 samples a 3x3 grid, boxRadius of 2 samples a 5x5 grid, etc.
// (yeah ill admit its not super intuitive but i didnt feel like manually adjusting the loops)
float4 box(sampler2D blurTex, float2 uv, float radius, float2 texelSize, int boxRadius)
{
    float4 blurColor = float4(0, 0, 0, 0);
    
    // TODO(Caeden): Can loops be unrolled if boxRadius is a parameter?
    for (int x = -boxRadius; x < boxRadius; x++)
    {
        for (int y = -boxRadius; y < boxRadius; y++)
        {
            blurColor.rgb += tex2D(blurTex, uv + (float2(x, y) * radius * texelSize)).rgb;
        }
    }

    blurColor.rgb /= pow((boxRadius * 2), 2);
    
    return blurColor;
}

#endif // BLURS_INCLUDED