using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GeometryGridContainer : BeatmapObjectContainerCollection<BaseEnvironmentEnhancement>
{
    private sealed class InstructionOrderComparer : IComparer<BaseEnvironmentEnhancement>
    {
        public int Compare(BaseEnvironmentEnhancement x, BaseEnvironmentEnhancement y) =>
            x.FileOrder.CompareTo(y.FileOrder);
    }

    private static readonly InstructionOrderComparer InstructionOrder = new();

    [SerializeField] private GameObject geometryPrefab;
    [SerializeField] private GeometryAppearanceSO geometryAppearanceSo;
    [SerializeField] private TracksManager tracksManager;

    private int nextInstructionOrder;
    private List<BaseEnvironmentEnhancement> orderedInstructions;

    public override ObjectType ContainerType => ObjectType.EnvironmentEnhancement;

    public override IComparer<BaseEnvironmentEnhancement> SortComparer => InstructionOrder;

    protected override void OnMapObjectsAssigned()
    {
        if (ReferenceEquals(orderedInstructions, MapObjects))
            return;

        if (orderedInstructions == null)
            LoadedContainers = new Dictionary<BaseObject, ObjectContainer>(
                LoadedContainers,
                ReferenceIdentityComparer<BaseObject>.Instance);

        orderedInstructions = MapObjects;
        for (var i = 0; i < MapObjects.Count; i++)
            MapObjects[i].FileOrder = i;

        nextInstructionOrder = MapObjects.Count;
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        var eh = obj as BaseEnvironmentEnhancement;
        if (eh.FileOrder == int.MaxValue)
            eh.FileOrder = nextInstructionOrder++;

        // Native legacy removal returns before processing enhancement instructions.
        if (BeatmapContext.HasLegacyEnvironmentRemoval)
            return;

        var container = GeometryContainer.SpawnGeometry(
            eh,
            ref geometryPrefab,
            BeatmapContext,
            tracksManager);
        if (container == null) return;
        container.Setup();
        if (LoadedContainers.TryAdd(eh, container)) ObjectsWithContainers.Add(eh);
        geometryAppearanceSo.SetGeometryAppearance(container);
        container.Selected = SelectionController.IsObjectSelected(obj);
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        if (!inCollection)
            RefreshPool(true);

    }

    public override void DoPostObjectsDeleteWorkflow() => RefreshPool(true);

    public override void RefreshPool(bool force)
    {
        if (!force || BeatmapContext.Descriptor == null)
            return;

        var descriptor = BeatmapContext.Descriptor;
        var refreshing = descriptor.EnhancementState != null;
        tracksManager.ClearEnvironmentBindings();
        ClearContainers();
        if (refreshing)
            descriptor.EnhancementState.Restore();
        else
            descriptor.EnhancementState = new EnvironmentEnhancementState(descriptor);

        foreach (var to_spawn in MapObjects)
        {
            if (to_spawn.HasMatchingTrack(TrackFilterID))
                HandleObjectSpawned(to_spawn);

        }

        if (refreshing)
        {
            FindAnyObjectByType<LightshowController>().RefreshEnvironmentEnhancements();
            tracksManager.RefreshEnvironmentBindings();
            foreach (GeometryContainer container in LoadedContainers.Values)
            {
                container.Animator.RefreshEnvironmentAnimation();
                container.MaterialAnimator.RefreshEnvironmentAnimation();
            }
        }

        BeatmapContext.NotifyBloomFogParamsChanged();
    }

    // A map reload unloads the native scene too, so its rollback state must not survive into the next environment.
    public void ClearSpawnedGeometry()
    {
        tracksManager.ClearEnvironmentBindings();
        ClearContainers();
        if (BeatmapContext.Descriptor != null)
            BeatmapContext.Descriptor.EnhancementState = null;

    }

    private void ClearContainers()
    {
        foreach (var container in LoadedContainers.Values)
        {
            if (container != null)
                GameObject.DestroyImmediate(container.gameObject);

        }

        LoadedContainers.Clear();
        ObjectsWithContainers.Clear();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
    }

    internal override void SubscribeToCallbacks()
    {
    }

    internal override void UnsubscribeToCallbacks()
    {
    }

    public override ObjectContainer CreateContainer() => null;
};
