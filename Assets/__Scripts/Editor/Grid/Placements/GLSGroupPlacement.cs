using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class GLSGroupPlacement<TGroup, TCollection> : BasePlacement<TGroup, GLSGroupContainer, TCollection>
    where TGroup : BaseEventBoxGroup where TCollection : GLSGroupGridContainer<TGroup>
{
    [SerializeField] public GLSEventTrack GlsEventTrack;

    [SerializeField] protected GLSEventAppearanceSO glsEventAppearance;
    [SerializeField] private BeatmapRuntimeContext context;

    public override bool CanPlace => base.CanPlace && IsInPosition();

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a GLS Group.");

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        GlsEventTrack = provider.GetComponent<GLSEventTrack>();
        PlacementTrack = GlsEventTrack.Track.ObjectParentTransform;
        QueuedData.ID = GlsEventTrack.TrackDefinition.ID;
        PlacementVisualContainer.EventBoxGroupData = QueuedData;
        PlacementVisualContainer.transform.SetParent(PlacementTrack, false);
        PlacementVisualContainer.SafeSetActive(CanPlace);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState) =>
        PlacementVisualContainer.SafeSetActive(CanPlace);

    protected bool IsInPosition() =>
        Mathf.Approximately(
            Mathf.Floor(PlacementVisualContainer.transform.localPosition.x),
            GLSGroupContainer.GetPositionFromTrackDefinition(context.TracksDefinition, QueuedData));

    public override void HandleApply()
    {
        base.HandleApply();
        PlacementVisualContainer.EventBoxGroupData = QueuedData;
    }

    public override void FinishDrag()
    {
        base.FinishDrag();
        PlacementVisualContainer.EventBoxGroupData = QueuedData;
    }

    protected override void TransferQueuedToDraggedObject(ref TGroup dragged, TGroup queued) =>
        dragged.JsonTime = queued.JsonTime;
}
