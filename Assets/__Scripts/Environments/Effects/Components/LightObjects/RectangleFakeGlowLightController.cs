using UnityEngine;

public class RectangleFakeGlowLightController : LightController
{
    public Renderer Renderer;

    public float MinAlpha;
    public float AlphaMultiplier = 1f;

    public Vector2 Size = Vector2.one;
    public float EdgeSize = 0.1f;

    private static readonly int sizeParamsId = Shader.PropertyToID("_SizeParams");

    protected override bool Initialize() => Renderer != null;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;

        color.a *= AlphaMultiplier;
        if (color.a < MinAlpha) color.a = MinAlpha;

        var size = new Vector4(Size.x * 0.5f, Size.y * 0.5f, 1f, EdgeSize * 0.5f);
        transform.localScale = size;
        Mpb.SetColor(ColorId, color);
        Mpb.SetVector(sizeParamsId, size);
        Renderer.SetPropertyBlock(Mpb);
    }
}
