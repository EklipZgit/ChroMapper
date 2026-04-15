using UnityEngine;
using UnityEngine.Animations;

public class CopyPositionData : EnvironmentComponentData<PositionConstraint>
{
    public string Transform;

    public override void FillComponents(GameObject self, PositionConstraint comp, CreateContainer container)
    {
        if (!container.TryGetGameObjectOrNull(Transform, self, out var t)) return;
        t.GetComponent<ChromaIDMarker>().MarkUse = true;
        comp.AddSource(new ConstraintSource { sourceTransform = t.transform, weight = 1 });
        comp.constraintActive = true;
    }
}
