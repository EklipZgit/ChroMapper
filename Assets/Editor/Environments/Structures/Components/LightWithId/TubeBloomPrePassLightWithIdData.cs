using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class TubeBloomPrePassLightWithIdData : EnvironmentComponentData<ParametricBloomFogLightController>
{
    private const string ParametricBoxControllerKey = "parametricBoxController";
    private const string Dynamic3SliceSpriteKey = "dynamic3SliceSprite";

    [JsonProperty("lightId")] public int Id;

    [CanBeNull] public TubeBloomPrePassLightComponent TubeBloomPrePassLight;

    public override void FillComponents(
        GameObject self,
        ParametricBloomFogLightController comp,
        CreateContainer container)
    {
        if (TubeBloomPrePassLight is null)
        {
            throw new JsonSerializationException(
                $"Tube bloom pre-pass light '{ChromaId(self)}' in environment " +
                $"'{container.EnvironmentId}' is missing its tubeBloomPrePassLight data; the export " +
                "omitted the serialized component. Re-export the environment data.");
        }

        var tube = TubeBloomPrePassLight;
        var missingFields = MissingReferenceFields(tube);
        if (missingFields != null)
        {
            Debug.LogWarning(
                $"Tube bloom pre-pass light '{ChromaId(self)}' (instance {tube.InstanceId}) " +
                $"in environment '{container.EnvironmentId}' is missing exported {missingFields}; " +
                "emit integer 0 for a null or intentionally excluded reference. References are " +
                "not inferred from the object hierarchy.");
        }

        // Resolve before mutating so an invalid reference cannot leave a partial import.
        var boxLight = ResolveReference<ParametricBoxLight>(
            self, tube.ParametricBoxController, ParametricBoxControllerKey, container);
        var spriteLight = ResolveReference<ParametricSpriteLight>(
            self, tube.Dynamic3SliceSprite, Dynamic3SliceSpriteKey, container);

        comp.BloomFog = self.AddComponent<BloomFogObject>();
        comp.BoxLight = boxLight;
        comp.SpriteLight = spriteLight;

        tube.FillComponents(self, comp, container);
    }

    // An omitted or JSON-null field is nullable null; an explicit integer 0 is the
    // canonical null-or-intentionally-excluded reference and resolves silently.
    private static string MissingReferenceFields(TubeBloomPrePassLightComponent tube)
    {
        var boxMissing = tube.ParametricBoxController is null;
        var spriteMissing = tube.Dynamic3SliceSprite is null;
        if (boxMissing && spriteMissing)
            return ParametricBoxControllerKey + " and " + Dynamic3SliceSpriteKey;
        if (boxMissing) return ParametricBoxControllerKey;
        if (spriteMissing) return Dynamic3SliceSpriteKey;
        return null;
    }

    private static string ChromaId(GameObject self)
    {
        var marker = self.GetComponent<ChromaIDMarker>();
        return marker != null ? marker.ChromaID : self.name;
    }

    private static T ResolveReference<T>(
        GameObject self,
        int? instanceId,
        string fieldKey,
        CreateContainer container) where T : Component
    {
        if (instanceId is null || instanceId.Value == 0)
            return null;

        var resolved = container.GetComponentOrNull<T>(instanceId.Value);
        if (resolved == null)
        {
            throw new JsonSerializationException(
                $"Tube bloom pre-pass light '{ChromaId(self)}' in environment " +
                $"'{container.EnvironmentId}' has '{fieldKey}' instance {instanceId.Value} that is " +
                $"missing from the export or is not a {typeof(T).Name}. Fix the exported reference " +
                "or re-export the environment data.");
        }

        return resolved;
    }
}

public class TubeBloomPrePassLightComponent : EnvironmentComponentData<ParametricBloomFogLightController>
{
    public float ColorAlphaMultiplier;
    public float BloomFogIntensityMultiplier;
    public BloomPrePassLightTypeData LightType;
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

    public int? ParametricBoxController;
    public int? Dynamic3SliceSprite;

    public override void FillComponents(
        GameObject self,
        ParametricBloomFogLightController comp,
        CreateContainer container)
    {
        comp.ColorAlphaMultiplier = ColorAlphaMultiplier;
        comp.BloomFogIntensityMultiplier = BloomFogIntensityMultiplier;
        comp.LightType = LightType?.Create(container);
        comp.Length = TubeLength;
        comp.Width = TubeWidth;
        comp.Center = Center;
        comp.StartAlpha = StartAlpha;
        comp.EndAlpha = EndAlpha;
        comp.StartWidth = StartWidth;
        comp.EndWidth = EndWidth;
        comp.BoostToWhite = BoostToWhite;
        comp.LimitAlpha = LimitAlpha;
        comp.MinAlpha = MinAlpha;
        comp.MaxAlpha = MaxAlpha;
        comp.LightWidthMultiplier = LightWidthMultiplier;
        comp.MultiplyLengthByAlphaBloomFogMultiplier = MultiplyLengthByAlphaBloomFogMultiplier;
        comp.UseCollision = UseCollision;
        comp.OverrideChildrenLength = OverrideChildrenLength;
        comp.FakeBloomIntensityMultiplier = FakeBloomIntensityMultiplier;
        comp.AddWidthToLength = AddWidthToLength;
        comp.ThickenWithDistance = ThickenWithDistance;
        comp.MinDistance = MinDistance;
        comp.MaxDistance = MaxDistance;
        comp.MinWidthMultiplier = MinWidthMultiplier;
        comp.MaxWidthMultiplier = MaxWidthMultiplier;
        comp.DisableRenderersOnZeroAlpha = DisableRenderersOnZeroAlpha;
        comp.BakedGlowWidthScale = BakedGlowWidthScale;
        comp.MultiplyLengthByAlpha = MultiplyLengthByAlpha;
        comp.UpdateAlways = UpdateAlways;
        comp.OverrideChildrenWidth = OverrideChildrenWidth;
        comp.OverrideChildrenAlpha = OverrideChildrenAlpha;

        comp.ThickenCurve = ThickenCurve.Create();
        comp.AlphaToLengthBloomFogCurve = AlphaToLengthBloomFogCurve.Create();
        comp.AlphaToLengthCurve = AlphaToLengthCurve.Create();
    }
}

/// <summary>Exported bloom-prepass material identity and draw order used by Environment Creator.</summary>
public class BloomPrePassLightTypeData
{
    [JsonProperty("material", Required = Required.Always)] public string Material;
    [JsonProperty("renderingPriority", Required = Required.Always)] public int RenderingPriority;

    /// <summary>Creates the runtime light type from the exported material and draw order.</summary>
    /// <exception cref="JsonSerializationException">
    /// The exported material is absent from the environment material catalog or resolves to the missing-material shader.
    /// </exception>
    public BloomFogLightType Create(CreateContainer container)
    {
        var material = container.GetMaterialSafe(Material);
        if (material == null || material.shader == null || material.shader.name == "ChroMapper/Missing")
        {
            throw new JsonSerializationException(
                $"Bloom-prepass light material '{Material}' for environment '{container.EnvironmentId}' " +
                "is unresolved or uses the ChroMapper/Missing shader. Correct this hash's environment material " +
                "catalog entry before creating the environment.");
        }

        return new BloomFogLightType(material, RenderingPriority);
    }
}
