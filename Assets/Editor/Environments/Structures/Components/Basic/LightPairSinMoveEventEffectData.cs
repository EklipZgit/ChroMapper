using UnityEngine;

public class LightPairSinMoveEventEffectData : EnvironmentComponentData<LightPairSinMove>
{
    public string EventTypeL;
    public int TransformL;
    public string EventTypeR;
    public int TransformR;
    public string SwitchOverrideRandomValuesEvent;
    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    public override void FillComponents(GameObject self, LightPairSinMove comp, CreateContainer container)
    {
        comp.enabled = true;
        if (ConvertUtils.ToEventType(EventTypeL, out var type) && type != -1)
            comp.LeftEffect = container.Descriptor.BasicEventEffectManager.GetOrRegister<LightRotationEffect>(type);
        if (ConvertUtils.ToEventType(EventTypeR, out type) && type != -1)
            comp.RightEffect = container.Descriptor.BasicEventEffectManager.GetOrRegister<LightRotationEffect>(type);
        if (ConvertUtils.ToEventType(SwitchOverrideRandomValuesEvent, out type) && type != -1)
        {
            comp.SwitchEffect =
                container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(type);
        }

        var lT = container.GetComponentOrNull<Transform>(TransformL);
        lT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        var rT = container.GetComponentOrNull<Transform>(TransformR);
        rT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.Transforms =
            new LightPairSinMove.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
        comp.OverrideRandomValues = OverrideRandomValues;
        comp.StartValueOffset = StartValueOffset;
        comp.StartPositionOffset = StartPositionOffset;
        comp.EndPositionOffset = EndPositionOffset;
    }
}
