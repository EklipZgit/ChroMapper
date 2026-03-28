using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class
    GLSEventPlacement<TGroup, TEvent> : BasePlacement<TEvent, GLSEventContainer, GLSEventGridContainer>
    where TEvent : BaseGLSEvent
{
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] protected GLSEventAppearanceSO GlsEventAppearance;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] protected BeatmapEasingsSelectionInputController EasingInputController;

    public override bool CanPlace => base.CanPlace && glsEventGridProvider.GroupContext.GetType() == typeof(TGroup);

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a GLS Event.");

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        PlacementVisualContainer.EventData = QueuedData;
        PlacementVisualContainer.SafeSetActive(CanPlace);
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState) =>
        PlacementVisualContainer.SafeSetActive(CanPlace);

    public override void HandleApply()
    {
        base.HandleApply();
        PlacementVisualContainer.EventData = QueuedData;
    }

    public override void FinishDrag()
    {
        base.FinishDrag();
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void TransferQueuedToDraggedObject(ref TEvent dragged, TEvent queued) =>
        dragged.JsonTime = queued.JsonTime;
}
