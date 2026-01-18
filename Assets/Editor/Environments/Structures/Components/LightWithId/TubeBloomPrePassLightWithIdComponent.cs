using JetBrains.Annotations;
using Newtonsoft.Json;

public class TubeBloomPrePassLightWithIdComponent : EnvDataComponent<ParametricBloomFogLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    [JsonProperty("tubeBloomPrePassLight")] [CanBeNull]
    public TubeBloomPrePassLightComponent TubeBloomPrePassLight;

    public override void CopyTo(ParametricBloomFogLightController target)
    {
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

    public void CopyTo(ParametricBloomFogLightController target)
    {
        target.ColorAlphaMultiplier = ColorAlphaMultiplier;
        target.BloomFogIntensityMultiplier = BloomFogIntensityMultiplier;
        target.Length = TubeLength;
        target.Width = TubeWidth;
        target.Center = Center;
        target.StartAlpha = StartAlpha;
        target.EndAlpha = EndAlpha;
        target.StartWidth = StartWidth;
        target.EndWidth = EndWidth;
        target.BoostToWhite = BoostToWhite;
        target.LimitAlpha = LimitAlpha;
        target.MinAlpha = MinAlpha;
        target.MaxAlpha = MaxAlpha;
        target.LightWidthMultiplier = LightWidthMultiplier;
        target.MultiplyLengthByAlphaBloomFogMultiplier = MultiplyLengthByAlphaBloomFogMultiplier;
        target.UseCollision = UseCollision;
        target.OverrideChildrenLength = OverrideChildrenLength;
        target.FakeBloomIntensityMultiplier = FakeBloomIntensityMultiplier;
        target.AddWidthToLength = AddWidthToLength;
        target.ThickenWithDistance = ThickenWithDistance;
        target.MinDistance = MinDistance;
        target.MaxDistance = MaxDistance;
        target.MinWidthMultiplier = MinWidthMultiplier;
        target.MaxWidthMultiplier = MaxWidthMultiplier;
        target.DisableRenderersOnZeroAlpha = DisableRenderersOnZeroAlpha;
        target.BakedGlowWidthScale = BakedGlowWidthScale;
        target.MultiplyLengthByAlpha = MultiplyLengthByAlpha;
        target.UpdateAlways = UpdateAlways;
        target.OverrideChildrenWidth = OverrideChildrenWidth;
        target.OverrideChildrenAlpha = OverrideChildrenAlpha;

        target.ThickenCurve = ThickenCurve.Create();
        target.AlphaToLengthBloomFogCurve = AlphaToLengthBloomFogCurve.Create();
        target.AlphaToLengthCurve = AlphaToLengthCurve.Create();
    }
}
