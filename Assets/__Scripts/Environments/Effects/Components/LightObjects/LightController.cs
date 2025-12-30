using UnityEngine;

public class LightController : BaseLightController
{
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);
    public LightObject BoxLight;
    public LightObject SpriteLight;
    public LightObjectBloomFog BloomFog;

    private bool useBoxLight;
    private bool useSpriteLight;
    private bool useBloomFog;

    private void Start()
    {
        useBoxLight = BoxLight != null;
        useSpriteLight = SpriteLight != null;
        useBloomFog = BloomFog != null;
    }

    public override void SetColor(Color color)
    {
        // These are basically cached null checks to avoid doing them every frame
        if (useBoxLight) BoxLight.SetColor(color);
        if (useSpriteLight) SpriteLight.SetColor(color);
        if (useBloomFog) BloomFog.SetColor(color);
    }
}
