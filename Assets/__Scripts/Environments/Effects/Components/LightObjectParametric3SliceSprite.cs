using UnityEngine;

public class LightObjectParametric3SliceSprite : LightObject
{
    public float WidthMultiplier;
    public float AlphaStart;
    public float AlphaEnd;
    public float AlphaMultiplier;
    public float Width;
    public float WidthStart;
    public float WidthEnd;
    public float Center;
    public float Length;
    public float MinAlpha;

    public bool UseCollision;
    public float CollisionLength;

    private static readonly int sizeParamsId = Shader.PropertyToID("_SizeParams");
    private static readonly int alphaWidthId = Shader.PropertyToID("_AlphaWidth");

    public void OnValidate()
    {
        if (Renderer != null) Start();
    }

    protected override void Start()
    {
        base.Start();
        UpdateLighting(Color.blue);
    }

    public override void UpdateLighting(Color color)
    {
        var length = UseCollision ? Mathf.Min(CollisionLength, Length) : Length;
        var alphaEnd = Mathf.Lerp(AlphaStart, AlphaEnd, Mathf.InverseLerp(0f, Length, length));

        Mpb.SetColor(colorId, ModifyColor(color));
        Mpb.SetVector(alphaWidthId, new Vector4(AlphaStart, alphaEnd, WidthStart, WidthEnd));
        Mpb.SetVector(sizeParamsId, new Vector4(Width * WidthMultiplier, length, Center, Width * 2f * WidthMultiplier));
        Renderer.SetPropertyBlock(Mpb);
    }

    protected override Color ModifyColor(Color color)
    {
        color.a *= AlphaMultiplier;
        color.a = Mathf.Max(color.a, MinAlpha);
        return color;
    }
}
