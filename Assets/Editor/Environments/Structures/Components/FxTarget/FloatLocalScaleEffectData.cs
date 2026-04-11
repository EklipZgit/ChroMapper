using System.Linq;
using UnityEngine;

public class FloatLocalScaleEffectData : EnvironmentComponentData<LocalScaleFx>
{
    public string[] Transforms;
    public Vector2 ValueBounds;
    public Vector3 StartScale;

    public override void SearchAndFillComponents(GameObject self, LocalScaleFx comp, CreateContainer container)
    {
        comp.TargetTransforms = Transforms
            .Select(x => container.GetGameObjectOrNull(x, self))
            .Where(x => x != null)
            .Select(x => x.transform)
            .Select(x =>
            {
                x.transform.localScale = StartScale;
                return x;
            })
            .ToArray();
    }

    public override void CopyTo(LocalScaleFx comp) => comp.ValueBounds = ValueBounds;
}
