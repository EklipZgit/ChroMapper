using UnityEngine;
using UnityEngine.Animations;

public class ParametricBloomFogLightController : LightController
{
    public float Width = 0.5f;
    public bool OverrideChildrenLength = true;
    public float Length = 1f;
    public float Center = 0.5f;
    public float ColorAlphaMultiplier = 1f;
    public float BloomFogIntensityMultiplier = 1f;
    public float FakeBloomIntensityMultiplier = 1f;
    public float BoostToWhite;
    public float LightWidthMultiplier = 1f;
    public bool AddWidthToLength;
    public bool ThickenWithDistance;
    public AnimationCurve ThickenCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public float MinDistance = 30f;
    public float MaxDistance = 200f;
    public float MinWidthMultiplier = 1f;
    public float MaxWidthMultiplier = 10f;
    public bool DisableRenderersOnZeroAlpha;

    public float BakedGlowWidthScale = 1f;

    public bool MultiplyLengthByAlpha;
    public AnimationCurve AlphaToLengthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public AnimationCurve AlphaToLengthBloomFogCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public bool UpdateAlways;
    public bool LimitAlpha;
    public float MinAlpha;
    public float MaxAlpha = 1f;

    public bool OverrideChildrenAlpha = true;
    public float StartAlpha = 1f;
    public float EndAlpha = 1f;

    public bool OverrideChildrenWidth;
    public float StartWidth = 1f;
    public float EndWidth = 1f;

    public bool EnabledRenderers;

    public ParametricBoxLight BoxLight;
    public ParametricSpriteLight SpriteLight;
    public BloomFogObject BloomFog;

    private Transform tr;
    private bool hasBloomFog;
    private bool hasBoxLight;
    private bool hasSpriteLight;

    public float MultiplyLengthByAlphaBloomFogMultiplier = 1f;
    private float multiplyLengthByAlphaMultiplier = 1f;

    private bool useCollision;

    public bool UseCollision
    {
        get => useCollision;
        set
        {
            useCollision = value;
            if (hasBoxLight) BoxLight.UseCollision = value;
            if (hasSpriteLight) SpriteLight.UseCollision = value;
        }
    }

    public float CollisionLength = float.MaxValue;

    public float CollisionEndAlpha =>
        (UseCollision
            ? Mathf.Lerp(StartAlpha, EndAlpha, Mathf.InverseLerp(0f, Length, CalculatedCollisionLength))
            : EndAlpha)
        * multiplyLengthByAlphaMultiplier;

    private float CalculatedCollisionLength => !UseCollision ? Length : Mathf.Min(CollisionLength, Length);

    public override bool IsPhysical => hasBoxLight || hasSpriteLight;

    protected override bool Initialize()
    {
        tr = transform;

        hasBloomFog = BloomFog != null; // TODO: ideally this shouldn't be needed, it's a requirement
        hasBoxLight = BoxLight != null;
        hasSpriteLight = SpriteLight != null;

        if (hasBloomFog) BloomFog.CachedTransform = tr;

        if (hasBoxLight)
        {
            BoxLight.Center = Center;
            BoxLight.AlphaMultiplier = ColorAlphaMultiplier;
            BoxLight.MinAlpha = MinAlpha;
            if (OverrideChildrenWidth)
            {
                BoxLight.WidthStart = StartWidth;
                BoxLight.WidthEnd = EndWidth;
            }

            BoxLight.InitIfNeeded();
        }

        if (hasSpriteLight)
        {
            SpriteLight.Center = Center;
            SpriteLight.MinAlpha = MinAlpha;
            SpriteLight.AlphaMultiplier = ColorAlphaMultiplier * FakeBloomIntensityMultiplier;
            if (OverrideChildrenWidth)
            {
                SpriteLight.WidthStart = StartWidth;
                SpriteLight.WidthEnd = EndWidth;
            }

            SpriteLight.InitIfNeeded();
        }

        return true;
    }

    private bool shouldRefresh;

    private void OnEnable() => shouldRefresh = true;
    private void OnDisable() => shouldRefresh = false;

    public override bool ShouldInclude => UpdateAlways;
    public override bool ShouldRefresh => shouldRefresh;

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized || UpdateAlways) return;
        Refresh();
    }

    public override void Refresh()
    {
        var rendered = !DisableRenderersOnZeroAlpha || Color.a > 0.01f;
        if (!rendered && !EnabledRenderers) return;

        if (EnabledRenderers != rendered)
        {
            EnabledRenderers = rendered;
            if (hasBoxLight) BoxLight.Renderer.enabled = rendered;
            if (hasSpriteLight) SpriteLight.Renderer.enabled = rendered;
        }

        if (hasBloomFog)
        {
            BloomFog.LightWidthMultiplier = LightWidthMultiplier;
            BloomFog.EndWidth = EndWidth;
            BloomFog.StartWidth = StartWidth;
            BloomFog.StartAlpha = StartAlpha;
            BloomFog.EndAlpha = CollisionEndAlpha;
            BloomFog.LimitAlpha = LimitAlpha;
            BloomFog.MinAlpha = MinAlpha;
            BloomFog.MaxAlpha = MaxAlpha;
            BloomFog.MultiplyLengthByAlphaBloomFogMultiplier = MultiplyLengthByAlphaBloomFogMultiplier;
            BloomFog.MultiplyLengthByAlphaMultiplier = multiplyLengthByAlphaMultiplier;
            BloomFog.BoostToWhite = BoostToWhite;
            BloomFog.IntensityMultiplier = BloomFogIntensityMultiplier;
            BloomFog.Length = CalculatedCollisionLength;
            BloomFog.Center = Center;

            BloomFog.SetColor(Color);
        }

        var widthFactor = 1f;
        if (ThickenWithDistance)
        {
            var time = Mathf.InverseLerp(MinDistance, MaxDistance, transform.position.z);
            widthFactor = Mathf.Lerp(MinWidthMultiplier, MaxWidthMultiplier, ThickenCurve.Evaluate(time));
        }

        var lengthFactor = 1f;
        if (MultiplyLengthByAlpha)
        {
            multiplyLengthByAlphaMultiplier = AlphaToLengthCurve.Evaluate(Color.a);
            MultiplyLengthByAlphaBloomFogMultiplier = AlphaToLengthBloomFogCurve.Evaluate(Color.a);
            lengthFactor = multiplyLengthByAlphaMultiplier;
        }

        if (hasBoxLight)
        {
            if (OverrideChildrenAlpha)
            {
                BoxLight.AlphaStart = StartAlpha;
                BoxLight.AlphaEnd = EndAlpha;
            }

            var width = ThickenWithDistance ? Width * widthFactor : Width;
            BoxLight.Width = width;
            if (OverrideChildrenLength) BoxLight.Height = (Length + (AddWidthToLength ? Width : 0f)) * lengthFactor;

            BoxLight.Length = width;
            if (useCollision) BoxLight.CollisionHeight = CollisionLength;

            BoxLight.SetColor(Color);
        }

        if (hasSpriteLight)
        {
            if (OverrideChildrenAlpha)
            {
                SpriteLight.AlphaStart = StartAlpha;
                SpriteLight.AlphaEnd = EndAlpha;
            }

            var width = ThickenWithDistance
                ? Width * BakedGlowWidthScale * widthFactor
                : Width * BakedGlowWidthScale;
            SpriteLight.Width = width;
            if (OverrideChildrenLength) SpriteLight.Length = (Length + (AddWidthToLength ? width : 0f)) * lengthFactor;

            if (useCollision) SpriteLight.CollisionLength = CollisionLength;

            SpriteLight.SetColor(Color);
        }
    }
}
