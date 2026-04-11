using UnityEngine;

public class LightPairSinMoveEventEffectData : EnvironmentComponentData<LightPairSinMove>
{
    public string EventTypeL;
    public string TransformL;
    public string EventTypeR;
    public string TransformR;
    public string SwitchOverrideRandomValuesEvent;
    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    public override void SearchAndFillComponents(GameObject self, LightPairSinMove comp, CreateContainer container)
    {
        var lT = container.GetGameObjectOrNull(TransformL, self).transform;
        lT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        var rT = container.GetGameObjectOrNull(TransformR, self).transform;
        rT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.Transforms =
            new LightPairSinMove.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
    }

    public override void CopyTo(LightPairSinMove comp)
    {
        comp.OverrideRandomValues = OverrideRandomValues;
        comp.StartValueOffset = StartValueOffset;
        comp.StartPositionOffset = StartPositionOffset;
        comp.EndPositionOffset = EndPositionOffset;
    }
}
