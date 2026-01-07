using UnityEngine;

public class ParametricBloomFogLightController : LightController
{
    public ParametricBoxLight BoxLight;
    public ParametricSpriteLight SpriteLight;
    public BloomFogObject BloomFog;

    private bool useBoxLight;
    private bool useSpriteLight;
    private bool useBloomFog;

    protected override bool Initialize()
    {
        useBoxLight = BoxLight != null;
        useSpriteLight = SpriteLight != null;
        useBloomFog = BloomFog != null;
        return true;
    }

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;

        // These are basically cached null checks to avoid doing them every frame
        if (useBoxLight) BoxLight.SetColor(color);
        if (useSpriteLight) SpriteLight.SetColor(color);
        if (useBloomFog) BloomFog.SetColor(color);
    }
}
