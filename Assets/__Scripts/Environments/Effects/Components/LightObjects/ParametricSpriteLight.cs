using System;
using UnityEngine;

public class ParametricSpriteLight : MonoBehaviour
{
    public Renderer Renderer;

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

    [NonSerialized] public bool UseCollision;
    [NonSerialized] public float CollisionLength;

    private MaterialPropertyBlock mpb;
    private bool hasInitialized;
    private Color color;
    private static readonly int colorId = Shader.PropertyToID("_Color");
    private static readonly int sizeParamsId = Shader.PropertyToID("_SizeParams");
    private static readonly int alphaWidthId = Shader.PropertyToID("_AlphaWidth");

    private void OnValidate()
    {
        color = new(0f, 0.5f, 1f);
        Start();
    }

    private void Start()
    {
        mpb = new MaterialPropertyBlock();
        hasInitialized = Renderer != null;
        SetColor(color);
    }

    public void SetColor(Color col)
    {
        color = col;
        if (!hasInitialized) return;

        var length = UseCollision ? Mathf.Min(CollisionLength, Length) : Length;
        var alphaEnd = Mathf.Lerp(AlphaStart, AlphaEnd, Mathf.InverseLerp(0f, Length, length));

        color.a *= AlphaMultiplier;
        color.a = Mathf.Max(color.a, MinAlpha);
        mpb.SetColor(colorId, color);
        mpb.SetVector(alphaWidthId, new Vector4(AlphaStart, alphaEnd, WidthStart, WidthEnd));
        mpb.SetVector(sizeParamsId, new Vector4(Width * WidthMultiplier, length, Center, Width * 2f * WidthMultiplier));
        Renderer.SetPropertyBlock(mpb);
    }
}
