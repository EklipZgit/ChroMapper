using UnityEngine;

public class MaterialPropertyBlockPositionUpdaterData : EnvironmentComponentData<MaterialPropertyBlockPositionAnimator>
{
    public string Property;
    public string TargetTransform;

    public override void SearchAndFillComponents(
        GameObject self,
        MaterialPropertyBlockPositionAnimator comp,
        CreateContainer container)
    {
        comp.Controller = self.GetComponent<MaterialPropertyBlockController>();
        comp.TargetTransform = container.GetGameObjectOrNull(TargetTransform, self).transform;
        comp.TargetTransform.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
    }

    public override void CopyTo(MaterialPropertyBlockPositionAnimator comp) => comp.Property = Property;
}
