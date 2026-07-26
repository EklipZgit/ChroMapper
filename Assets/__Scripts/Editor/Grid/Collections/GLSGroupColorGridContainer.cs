using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GLSGroupColorGridContainer : GLSGroupGridContainer<BaseLightColorEventBoxGroup>
{
    public override ObjectType ContainerType => ObjectType.GLSColor;

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        base.HandleObjectSpawned(obj, inCollection);
        // A newly inserted transition target changes the forward ribbon owned by an already-loaded prior node.
        if (!inCollection)
            RefreshLoadedTransitionRibbons();
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        base.HandleObjectDelete(obj, inCollection);
        // Removing a group must immediately clear any loaded source ribbon that previously ended inside it.
        if (!inCollection)
            RefreshLoadedTransitionRibbons();
    }

    public override void DoPostObjectsSpawnedWorkflow()
    {
        base.DoPostObjectsSpawnedWorkflow();
        // Consolidate ribbon invalidation after bulk color-group insertion.
        RefreshLoadedTransitionRibbons();
    }

    public override void DoPostObjectsDeleteWorkflow()
    {
        base.DoPostObjectsDeleteWorkflow();
        // Consolidate ribbon invalidation after bulk color-group deletion.
        RefreshLoadedTransitionRibbons();
    }

    private void RefreshLoadedTransitionRibbons()
    {
        foreach (var container in LoadedContainers.Values)
        {
            // Unity-owned GLS containers need explicit null checks before refreshing their ribbon ghosts.
            var glsGroupContainer = container as GLSGroupContainer;
            if (glsGroupContainer != null)
            {
                glsGroupContainer.RefreshTransitionRibbons();
            }
        }
    }
}
