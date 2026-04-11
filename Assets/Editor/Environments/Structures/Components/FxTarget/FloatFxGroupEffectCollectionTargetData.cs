using System.Linq;
using UnityEngine;

public class FloatFxGroupEffectCollectionTargetData : EnvironmentComponentData<CollectionFx>
{
    public string[] FloatFxGroupEffectTargets;

    public override void SearchAndFillComponents(GameObject self, CollectionFx comp, CreateContainer container)
    {
        comp.Targets = FloatFxGroupEffectTargets
            .Select(x => container.GetGameObjectOrNull(x, self))
            .Where(x => x != null)
            .Select(x => x.GetComponent<FxTarget>())
            .Where(x => x != null)
            .ToArray();
    }

    public override void CopyTo(CollectionFx comp) { }
}
