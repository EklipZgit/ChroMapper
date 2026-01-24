using JetBrains.Annotations;
using Newtonsoft.Json;

public class TubeBloomPrePassLightWithIdComponent : EnvDataComponent<ParametricBloomFogLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    [CanBeNull] public TubeBloomPrePassLightComponent TubeBloomPrePassLight;
    public bool SetOnlyOnce;
    public bool SetColorOnly;

    public override void CopyTo(ParametricBloomFogLightController target)
    {
        // target.SetOnlyOnce = SetOnlyOnce;
        // target.SetColorOnly = SetColorOnly;

        if (TubeBloomPrePassLight is null) return;
        
        target.ColorAlphaMultiplier = TubeBloomPrePassLight.ColorAlphaMultiplier;
        target.BloomFogIntensityMultiplier = TubeBloomPrePassLight.BloomFogIntensityMultiplier;
        target.Length = TubeBloomPrePassLight.TubeLength;
        target.Width = TubeBloomPrePassLight.TubeWidth;
        target.Center = TubeBloomPrePassLight.Center;
        target.StartAlpha = TubeBloomPrePassLight.StartAlpha;
        target.EndAlpha = TubeBloomPrePassLight.EndAlpha;
        target.StartWidth = TubeBloomPrePassLight.StartWidth;
        target.EndWidth = TubeBloomPrePassLight.EndWidth;
        target.BoostToWhite = TubeBloomPrePassLight.BoostToWhite;
        target.LimitAlpha = TubeBloomPrePassLight.LimitAlpha;
        target.MinAlpha = TubeBloomPrePassLight.MinAlpha;
        target.MaxAlpha = TubeBloomPrePassLight.MaxAlpha;
        target.LightWidthMultiplier = TubeBloomPrePassLight.LightWidthMultiplier;
        target.MultiplyLengthByAlphaBloomFogMultiplier = TubeBloomPrePassLight.MultiplyLengthByAlphaBloomFogMultiplier;
        target.UseCollision = TubeBloomPrePassLight.UseCollision;
        target.OverrideChildrenLength = TubeBloomPrePassLight.OverrideChildrenLength;
        target.FakeBloomIntensityMultiplier = TubeBloomPrePassLight.FakeBloomIntensityMultiplier;
        target.AddWidthToLength = TubeBloomPrePassLight.AddWidthToLength;
        target.ThickenWithDistance = TubeBloomPrePassLight.ThickenWithDistance;
        target.MinDistance = TubeBloomPrePassLight.MinDistance;
        target.MaxDistance = TubeBloomPrePassLight.MaxDistance;
        target.MinWidthMultiplier = TubeBloomPrePassLight.MinWidthMultiplier;
        target.MaxWidthMultiplier = TubeBloomPrePassLight.MaxWidthMultiplier;
        target.DisableRenderersOnZeroAlpha = TubeBloomPrePassLight.DisableRenderersOnZeroAlpha;
        target.BakedGlowWidthScale = TubeBloomPrePassLight.BakedGlowWidthScale;
        target.MultiplyLengthByAlpha = TubeBloomPrePassLight.MultiplyLengthByAlpha;
        target.UpdateAlways = TubeBloomPrePassLight.UpdateAlways;
        target.OverrideChildrenWidth = TubeBloomPrePassLight.OverrideChildrenWidth;
        target.OverrideChildrenAlpha = TubeBloomPrePassLight.OverrideChildrenAlpha;

        target.ThickenCurve = TubeBloomPrePassLight.ThickenCurve.Create();
        target.AlphaToLengthBloomFogCurve = TubeBloomPrePassLight.AlphaToLengthBloomFogCurve.Create();
        target.AlphaToLengthCurve = TubeBloomPrePassLight.AlphaToLengthCurve.Create();
    }
}

public class TubeBloomPrePassLightComponent
{
    public float ColorAlphaMultiplier;
    public float BloomFogIntensityMultiplier;
    public float TubeLength;
    public float TubeWidth;
    public float Center;
    public float StartAlpha;
    public float EndAlpha;
    public float StartWidth;
    public float EndWidth;
    public float BoostToWhite;
    public bool LimitAlpha;
    public float MinAlpha;
    public float MaxAlpha;
    public float LightWidthMultiplier;
    public float MultiplyLengthByAlphaBloomFogMultiplier;
    public bool UseCollision;
    public bool OverrideChildrenLength;
    public float FakeBloomIntensityMultiplier;
    public bool AddWidthToLength;
    public bool ThickenWithDistance;
    public float MinDistance;
    public float MaxDistance;
    public float MinWidthMultiplier;
    public float MaxWidthMultiplier;
    public bool DisableRenderersOnZeroAlpha;
    public float BakedGlowWidthScale;
    public bool MultiplyLengthByAlpha;
    public bool UpdateAlways;
    public bool OverrideChildrenWidth;
    public bool OverrideChildrenAlpha;

    public AnimationCurveComponent ThickenCurve;
    public AnimationCurveComponent AlphaToLengthBloomFogCurve;
    public AnimationCurveComponent AlphaToLengthCurve;

    public string ParametricBoxId = "";
    public string SliceSpriteControllerId = "";
}
