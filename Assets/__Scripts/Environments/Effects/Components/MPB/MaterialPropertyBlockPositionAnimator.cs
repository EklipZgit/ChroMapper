using UnityEngine;

public class MaterialPropertyBlockPositionAnimator : MaterialPropertyBlockAnimator
{
    public Transform TargetTransform;

    protected override void SetProperty()
    {
        if (TargetTransform != null) Controller.Mpb.SetVector(PropertyId, TargetTransform.position);
    }
}
