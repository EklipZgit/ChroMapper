// yes, i yoink this from Swifter's vivify template,
// although noise used by beat saber is different
// https://github.com/Swifter1243/VivifyTemplate/blob/master/Assets/VivifyTemplate/Utilities/Shader%20Functions/Noise.cginc

// https://www.shadertoy.com/view/XsX3zB
float3 random3(float3 c)
{
    float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
    float3 r;
    r.z = frac(512.0 * j);
    j *= .125;
    r.x = frac(512.0 * j);
    j *= .125;
    r.y = frac(512.0 * j);
    return r - 0.5;
}

/* skew constants for 3d simplex functions */
static float F3 = 0.3333333;
static float G3 = 0.1666667;

// https://www.shadertoy.com/view/Msf3WH
float simplex(float3 p)
{
    /* 1. find current tetrahedron T and it's four vertices */
    /* s, s+i1, s+i2, s+1.0 - absolute skewed (integer) coordinates of T vertices */
    /* x, x1, x2, x3 - unskewed coordinates of p relative to each of T vertices*/

    /* calculate s and x */
    float3 s = floor(p + dot(p, F3));
    float3 x = p - s + dot(s, G3);

    /* calculate i1 and i2 */
    float3 e = step(0, x - x.yzx);
    float3 i1 = e * (1.0 - e.zxy);
    float3 i2 = 1.0 - e.zxy * (1.0 - e);

    /* x1, x2, x3 */
    float3 x1 = x - i1 + G3;
    float3 x2 = x - i2 + 2.0 * G3;
    float3 x3 = x - 1.0 + 3.0 * G3;

    /* 2. find four surflets and store them in d */
    float4 w, d;

    /* calculate surflet weights */
    w.x = dot(x, x);
    w.y = dot(x1, x1);
    w.z = dot(x2, x2);
    w.w = dot(x3, x3);

    /* w fades from 0.6 at the center of the surflet to 0.0 at the margin */
    w = max(0.6 - w, 0.0);

    /* calculate surflet components */
    d.x = dot(random3(s), x);
    d.y = dot(random3(s + i1), x1);
    d.z = dot(random3(s + i2), x2);
    d.w = dot(random3(s + 1.0), x3);

    /* multiply d by w^4 */
    w *= w;
    w *= w;
    d *= w;

    /* 3. return the sum of the four surflets */
    return dot(d, 52) * 0.5 + 0.5;
}

// I dont fucking know where this came from lol
float2 hash(float2 p) // replace this by something better
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
}

// https://www.shadertoy.com/view/ldl3Dl
float3 hash(float3 x)
{
    x = float3(dot(x, float3(127.1, 311.7, 74.7)),
               dot(x, float3(269.5, 183.3, 246.1)),
               dot(x, float3(113.5, 271.9, 124.6)));

    return frac(sin(x) * 43758.5453123);
}

// https://www.shadertoy.com/view/Msf3WH
float simplex(in float2 p)
{
    const float K1 = 0.366025404; // (sqrt(3)-1)/2;
    const float K2 = 0.211324865; // (3-sqrt(3))/6;

    float2 i = floor(p + (p.x + p.y) * K1);
    float2 a = p - i + (i.x + i.y) * K2;
    float m = step(a.y, a.x);
    float2 o = float2(m, 1.0 - m);
    float2 b = a - o + K2;
    float2 c = a - 1.0 + 2.0 * K2;
    float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
    float3 n = h * h * h * h * float3(dot(a, hash(i + 0.0)), dot(b, hash(i + o)), dot(c, hash(i + 1.0)));
    return dot(n, 70) * 0.5 + 0.5;
}
