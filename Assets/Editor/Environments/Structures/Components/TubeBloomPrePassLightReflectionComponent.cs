public class TubeBloomPrePassLightReflectionComponent : EnvDataComponent<ParametricBloomFogLightController>
{
    public bool IsEnabled;

    public TubeBloomPrePassLightWithHitPoint MainTubeBloomPrePassLight;
    public TubeBloomPrePassLightWithHitPoint[] TubeBloomPrePassLightBounces;
    public string[] EnvironmentLayerMask;

    public override void CopyTo(ParametricBloomFogLightController target)
    {
    }
}

public class TubeBloomPrePassLightWithHitPoint
{
    public string TubeBloomPrePassLightId;
    public string HitPointLightWithId;
    public AnimationCurveComponent HitPointDistanceToAlphaCurve;
    public string HitPointGameObject;
    public string HitPointTransform;
    public bool ShowHitPoint;
}
