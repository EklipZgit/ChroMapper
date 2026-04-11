using System.Linq;
using UnityEngine;

public class TubeBloomPrePassLightReflectionData : EnvironmentComponentData<LightReflection>
{
    public TubeBloomPrePassLightWithHitPoint MainTubeBloomPrePassLight;
    public TubeBloomPrePassLightWithHitPoint[] TubeBloomPrePassLightBounces;
    public string[] EnvironmentLayerMask;

    public override void SearchAndFillComponents(
        GameObject self,
        LightReflection comp,
        CreateContainer container)
    {
        comp.MainParametricLight = RegisterReflection(MainTubeBloomPrePassLight);
        comp.ParametricLightReflection = TubeBloomPrePassLightBounces.Select(RegisterReflection).ToArray();
        comp.EnvironmentLayerMask = container.Library.LayerMaskLookup[EnvironmentLayerMask[0]];
        return;

        LightReflection.ParametricLightWithHitPoint RegisterReflection(TubeBloomPrePassLightWithHitPoint comp)
        {
            container
                    .GetGameObjectOrNull(comp.TubeBloomPrePassLightId, self)
                    .GetComponent<ChromaIDMarker>()
                    .MarkUse =
                true;
            container
                .GetGameObjectOrNull(comp.TubeBloomPrePassLightId, self)
                .GetComponent<ChromaIDMarker>()
                .MarkActivator = true;
            container.GetGameObjectOrNull(comp.HitPointLightWithId, self).GetComponent<ChromaIDMarker>().MarkUse =
                true;
            container
                .GetGameObjectOrNull(comp.HitPointLightWithId, self)
                .GetComponent<ChromaIDMarker>()
                .MarkActivator = true;
            return new LightReflection.ParametricLightWithHitPoint
            {
                Light =
                    container
                        .GetGameObjectOrNull(comp.TubeBloomPrePassLightId, self)
                        .GetComponent<ParametricBloomFogLightController>(),
                HitPointLightWithId =
                    container
                        .GetGameObjectOrNull(comp.HitPointLightWithId, self)
                        .GetComponent<InstancedMaterialLightController>(),
                HitPointGameObject = container.ChromaIdObjects[comp.HitPointGameObject],
                HitPointTransform = container.ChromaIdObjects[comp.HitPointTransform].transform,
                HitPointDistanceToAlphaCurve = comp.HitPointDistanceToAlphaCurve.Create(),
                ShowHitPoint = comp.ShowHitPoint
            };
        }
    }

    public override void CopyTo(LightReflection comp)
    {
    }

    public class TubeBloomPrePassLightWithHitPoint
    {
        public string TubeBloomPrePassLightId;
        public string HitPointLightWithId;
        public AnimationCurveData HitPointDistanceToAlphaCurve;
        public string HitPointGameObject;
        public string HitPointTransform;
        public bool ShowHitPoint;
    }
}
