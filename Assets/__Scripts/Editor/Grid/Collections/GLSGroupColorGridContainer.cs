using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GLSGroupColorGridContainer : GLSGroupGridContainer<BaseLightColorEventBoxGroup>
{
    // Reuse indexed source groups because color pool refreshes run for every viewport movement.
    private readonly System.Collections.Generic.HashSet<BaseLightColorEventBoxGroup> retainedTransitionGroups = new();

    public override ObjectType ContainerType => ObjectType.GLSColor;

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        base.HandleObjectSpawned(obj, inCollection);
        // A newly inserted transition target changes the forward ribbon owned by an already-loaded prior node.
        GLSEventCommon.AddColorTransitionGroup((BaseLightColorEventBoxGroup)obj);
        if (!inCollection)
        {
            RefreshLoadedTransitionRibbons();
        }
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        base.HandleObjectDelete(obj, inCollection);
        // Removing a group must immediately clear any loaded source ribbon that previously ended inside it.
        GLSEventCommon.RemoveColorTransitionGroup((BaseLightColorEventBoxGroup)obj);
        if (!inCollection)
        {
            RefreshLoadedTransitionRibbons();
        }
    }

    public override void DoPostObjectsSpawnedWorkflow()
    {
        base.DoPostObjectsSpawnedWorkflow();
        // Consolidate ribbon refresh after bulk color-group insertion.
        RefreshLoadedTransitionRibbons();
    }

    public override void DoPostObjectsDeleteWorkflow()
    {
        base.DoPostObjectsDeleteWorkflow();
        // Consolidate ribbon refresh after bulk color-group deletion.
        RefreshLoadedTransitionRibbons();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        // Query only transition intervals crossing the viewport boundary before parent pooling recycles their sources.
        retainedTransitionGroups.Clear();
        GLSEventCommon.GetColorTransitionSourceGroupsAt(lowerBound, TrackFilterID, retainedTransitionGroups);

        base.RefreshPool(lowerBound, upperBound, forceRefresh);

        // Recreate a recycled parent so its represented source ghost keeps drawing the ribbon.
        foreach (var group in retainedTransitionGroups)
        {
            if (!LoadedContainers.ContainsKey(group))
            {
                CreateContainerFromPool(group);
            }
        }
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
