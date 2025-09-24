using System;
using UnityEngine;
using UnityEngine.Serialization;

public class LightingObject : MonoBehaviour
{
    public bool OverrideLightGroup;
    public int OverrideLightGroupID;
    public bool UseInvertedPlatformColors;
    public bool CanBeTurnedOff = true;

    [SerializeField] private float multiplyAlpha = 1;

    [FormerlySerializedAs("lightID")] public int LightID;
    [FormerlySerializedAs("propGroup")] public int PropGroup;


    private float startTimeAlpha;
    private float startTimeColor;
    private Color startColor = Color.white;
    private float startAlpha;
    private float endTimeAlpha;
    private float endTimeColor;
    private Color endColor = Color.white;
    private float endAlpha;
    private bool useHSV;
    private Func<float, float> easing = Easing.ByName["easeLinear"];

    private MaterialPropertyBlock lightPropertyBlock;
    private Renderer lightRenderer;

    private BoostSprite boostSprite;


    private static readonly int mainTex = Shader.PropertyToID("_MainTex");
    private static readonly int baseColor = Shader.PropertyToID("_Color");

    private void Start()
    {
        lightPropertyBlock = new MaterialPropertyBlock();
        lightRenderer = GetComponentInChildren<Renderer>();
        boostSprite = GetComponent<BoostSprite>();

        if (lightRenderer is SpriteRenderer spriteRenderer)
        {
            if (boostSprite != null) boostSprite.Setup(spriteRenderer.sprite);

            lightPropertyBlock.SetTexture(mainTex, spriteRenderer.sprite.texture);
        }

        if (!OverrideLightGroup) return;
        var descriptor = LoadInitialMap.Platform;

        // TODO: Add types?
        if (descriptor != null
            && OverrideLightGroupID >= 0
            && OverrideLightGroupID < descriptor.LightingManagers.Length)
        {
            var lm = descriptor.LightingManagers[OverrideLightGroupID];
            while (lm.LightIDPlacementMapReverse?.ContainsKey(LightID) ?? false)
            {
                ++LightID;
            }

            lm.ControllingLights.Add(this);
            lm.LoadOldLightOrder();
        }
    }

    private void OnDestroy()
    {
        if (OverrideLightGroup)
        {
            var descriptor = LoadInitialMap.Platform;

            if (descriptor != null
                && OverrideLightGroupID >= 0
                && OverrideLightGroupID < descriptor.LightingManagers.Length)
            {
                var lm = descriptor.LightingManagers[OverrideLightGroupID];
                lm.ControllingLights.Remove(this);
                lm.LightIDPlacementMapReverse?.Remove(LightID);
            }
        }
    }

    private void UpdateLighting(Color color, float alpha)
    {
        lightPropertyBlock.SetColor(baseColor, color * alpha);
        lightRenderer.SetPropertyBlock(lightPropertyBlock);
    }

    public void UpdateTime(float time)
    {
        var nTimeAlpha = Mathf.Clamp01((time - startTimeAlpha) / (endTimeAlpha - startTimeAlpha));
        var nTimeColor = Mathf.Clamp01((time - startTimeColor) / (endTimeColor - startTimeColor));
        var color = useHSV
            ? LerpHSV(startColor, endColor, easing(nTimeColor))
            : Color.Lerp(startColor, endColor, easing(nTimeColor));
        var alpha = Mathf.Lerp(startAlpha, endAlpha, easing(nTimeAlpha)) * color.a;

        UpdateLighting(color, alpha);
    }

    private static Color LerpHSV(Color start, Color end, float t)
    {
        Color.RGBToHSV(start, out var sH, out var sS, out var sV);
        Color.RGBToHSV(end, out var eH, out var eS, out var eV);
        var hue = Mathf.LerpAngle(sH * 360f, eH * 360f, t);
        return Color
            .HSVToRGB(
                Mathf.Repeat(hue, 360f) / 360f,
                Mathf.Lerp(sS, eS, t),
                Mathf.Lerp(sV, eV, t))
            .WithAlpha(Mathf.Lerp(start.a, end.a, t));
    }

    public void UpdateFromState(BasicLightState state)
    {
        startTimeAlpha = state.StartTime;
        startTimeColor = state.StartTimeColor;
        startAlpha = state.StartAlpha;
        startColor = BasicLightManager.GetStartColorFromState(this, state);

        endTimeAlpha = state.EndTimeAlpha;
        endTimeColor = state.EndTimeColor;
        endAlpha = state.EndAlpha;
        endColor = BasicLightManager.GetEndColorFromState(this, state);

        useHSV = state.UseHSV;
        easing = state.Easing;
    }

    public void UpdateStartAndEndColor(Color start, Color end)
    {
        startColor = start;
        endColor = end;
    }

    public void UpdateBoostState(bool boost)
    {
        if (boostSprite != null) lightPropertyBlock.SetTexture(mainTex, boostSprite.GetSprite(boost).texture);
    }
}
