using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class TubeBloomPrePassLightWithIdData : EnvironmentComponentData<ParametricBloomFogLightController>
{
    [JsonProperty("lightId")] public int Id;

    [CanBeNull] public TubeBloomPrePassLightComponent TubeBloomPrePassLight;

    public override void SearchAndFillComponents(
        GameObject self,
        ParametricBloomFogLightController comp,
        CreateContainer container)
    {
    }

    public override void CopyTo(ParametricBloomFogLightController comp)
    {
        // target.SetOnlyOnce = SetOnlyOnce;
        // target.SetColorOnly = SetColorOnly;

        if (TubeBloomPrePassLight is null) return;

        comp.ColorAlphaMultiplier = TubeBloomPrePassLight.ColorAlphaMultiplier;
        comp.BloomFogIntensityMultiplier = TubeBloomPrePassLight.BloomFogIntensityMultiplier;
        comp.Length = TubeBloomPrePassLight.TubeLength;
        comp.Width = TubeBloomPrePassLight.TubeWidth;
        comp.Center = TubeBloomPrePassLight.Center;
        comp.StartAlpha = TubeBloomPrePassLight.StartAlpha;
        comp.EndAlpha = TubeBloomPrePassLight.EndAlpha;
        comp.StartWidth = TubeBloomPrePassLight.StartWidth;
        comp.EndWidth = TubeBloomPrePassLight.EndWidth;
        comp.BoostToWhite = TubeBloomPrePassLight.BoostToWhite;
        comp.LimitAlpha = TubeBloomPrePassLight.LimitAlpha;
        comp.MinAlpha = TubeBloomPrePassLight.MinAlpha;
        comp.MaxAlpha = TubeBloomPrePassLight.MaxAlpha;
        comp.LightWidthMultiplier = TubeBloomPrePassLight.LightWidthMultiplier;
        comp.MultiplyLengthByAlphaBloomFogMultiplier = TubeBloomPrePassLight.MultiplyLengthByAlphaBloomFogMultiplier;
        comp.UseCollision = TubeBloomPrePassLight.UseCollision;
        comp.OverrideChildrenLength = TubeBloomPrePassLight.OverrideChildrenLength;
        comp.FakeBloomIntensityMultiplier = TubeBloomPrePassLight.FakeBloomIntensityMultiplier;
        comp.AddWidthToLength = TubeBloomPrePassLight.AddWidthToLength;
        comp.ThickenWithDistance = TubeBloomPrePassLight.ThickenWithDistance;
        comp.MinDistance = TubeBloomPrePassLight.MinDistance;
        comp.MaxDistance = TubeBloomPrePassLight.MaxDistance;
        comp.MinWidthMultiplier = TubeBloomPrePassLight.MinWidthMultiplier;
        comp.MaxWidthMultiplier = TubeBloomPrePassLight.MaxWidthMultiplier;
        comp.DisableRenderersOnZeroAlpha = TubeBloomPrePassLight.DisableRenderersOnZeroAlpha;
        comp.BakedGlowWidthScale = TubeBloomPrePassLight.BakedGlowWidthScale;
        comp.MultiplyLengthByAlpha = TubeBloomPrePassLight.MultiplyLengthByAlpha;
        comp.UpdateAlways = TubeBloomPrePassLight.UpdateAlways;
        comp.OverrideChildrenWidth = TubeBloomPrePassLight.OverrideChildrenWidth;
        comp.OverrideChildrenAlpha = TubeBloomPrePassLight.OverrideChildrenAlpha;

        comp.ThickenCurve = TubeBloomPrePassLight.ThickenCurve.Create();
        comp.AlphaToLengthBloomFogCurve = TubeBloomPrePassLight.AlphaToLengthBloomFogCurve.Create();
        comp.AlphaToLengthCurve = TubeBloomPrePassLight.AlphaToLengthCurve.Create();
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

    public AnimationCurveData ThickenCurve;
    public AnimationCurveData AlphaToLengthBloomFogCurve;
    public AnimationCurveData AlphaToLengthCurve;

    public string ParametricBoxId = "";
    public string SliceSpriteControllerId = "";
}
