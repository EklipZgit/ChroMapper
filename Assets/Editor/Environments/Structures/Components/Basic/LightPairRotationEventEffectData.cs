using UnityEngine;

public class LightPairRotationEventEffectData : EnvironmentComponentData<LightPairRotation>
{
    public string EventTypeL;
    public string TransformL;
    public string EventTypeR;
    public string TransformR;
    public string SwitchOverrideRandomValuesEvent;
    public Vector3 RotationVector;
    public bool OverrideRandomValues;
    public bool UseZPositionForAngleOffset;
    public float ZPositionAngleOffsetScale;
    public float StartRotation;

    public override void SearchAndFillComponents(GameObject self, LightPairRotation comp, CreateContainer container)
    {
        var lT = container.GetGameObjectOrNull(TransformL, self).transform;
        lT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        var rT = container.GetGameObjectOrNull(TransformR, self).transform;
        rT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.Transforms =
            new LightPairRotation.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
    }

    public override void CopyTo(LightPairRotation comp)
    {
        comp.RotationVector = RotationVector;
        comp.OverrideRandomValues = OverrideRandomValues;
        comp.UseZPositionForAngleOffset = UseZPositionForAngleOffset;
        comp.ZPositionAngleOffsetScale = ZPositionAngleOffsetScale;
        comp.StartRotation = StartRotation;
    }
}
