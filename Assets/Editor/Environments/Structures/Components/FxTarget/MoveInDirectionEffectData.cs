using UnityEngine;

public class MoveInDirectionEffectData : EnvironmentComponentData<MoveInDirectionFx>
{
    public string Transform;
    public Vector3 MoveOrigin;
    public float MoveScale = 1f;

    public override void SearchAndFillComponents(GameObject self, MoveInDirectionFx comp, CreateContainer container) =>
        comp.TargetTransform = container.GetGameObjectOrNull(Transform, self).transform;

    public override void CopyTo(MoveInDirectionFx comp)
    {
        comp.MoveOrigin = MoveOrigin;
        comp.MoveScale = MoveScale;
    }
}
