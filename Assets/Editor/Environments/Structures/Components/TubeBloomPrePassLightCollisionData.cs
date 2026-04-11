using UnityEngine;

public class TubeBloomPrePassLightCollisionData : EnvironmentComponentData<LightCollision>
{
    public string TubeBloomPrePassLightId;
    public string HitPointLightWithId;
    public AnimationCurveData HitPointDistanceToAlphaCurve;
    public bool UseScale;
    public string ScaleTransform;
    public string HitPointGameObject;
    public string HitPointTransform;
    public string[] EnvironmentLayerMask;
    public bool ShowHitPoint;

    public override void SearchAndFillComponents(
        GameObject self,
        LightCollision comp,
        CreateContainer container)
    {
        comp.ParametricLight = container
            .GetGameObjectOrNull(TubeBloomPrePassLightId, self)
            .GetComponent<ParametricBloomFogLightController>();
        container
            .GetGameObjectOrNull(TubeBloomPrePassLightId, self)
            .GetComponent<ChromaIDMarker>()
            .MarkUse = true;
        container
            .GetGameObjectOrNull(TubeBloomPrePassLightId, self)
            .GetComponent<ChromaIDMarker>()
            .MarkActivator = true;

        comp.HitPointLightWithId = container
            .GetGameObjectOrNull(HitPointLightWithId, self)
            .GetComponent<InstancedMaterialLightController>();
        container.GetGameObjectOrNull(HitPointLightWithId, self).GetComponent<ChromaIDMarker>().MarkUse =
            true;
        container
            .GetGameObjectOrNull(HitPointLightWithId, self)
            .GetComponent<ChromaIDMarker>()
            .MarkActivator = true;

        comp.HitPointGameObject = container.GetGameObjectOrNull(HitPointGameObject, self);
        comp.HitPointTransform = container.GetGameObjectOrNull(HitPointTransform, self).transform;
        if (container.TryGetGameObjectOrNull(ScaleTransform, self, out var o)) comp.ScaleTransform = o.transform;
        comp.EnvironmentLayerMask = container.Library.LayerMaskLookup[EnvironmentLayerMask[0]];
    }

    public override void CopyTo(LightCollision comp)
    {
        comp.HitPointDistanceToAlphaCurve = HitPointDistanceToAlphaCurve.Create();
        comp.UseScale = UseScale;
        comp.ShowHitPoint = ShowHitPoint;
    }
}
