public class TubeBloomPrePassLightCollisionComponent : EnvDataComponent<ParametricBloomFogLightController>
{
    public bool IsEnabled;

    public string TubeBloomPrePassLightId;
    public string HitPointLightWithId;
    public AnimationCurveComponent HitPointDistanceToAlphaCurve;
    public bool UseScale;
    public string ScaleTransform;
    public string HitPointGameObject;
    public string HitPointTransform;
    public string[] EnvironmentLayerMask;
    public bool ShowHitPoint;

    public override void CopyTo(ParametricBloomFogLightController target)
    {
    }
}
