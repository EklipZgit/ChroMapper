using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public abstract class
    GLSEventPlacement<TGroup, TEvent> : BasePlacement<TEvent, GLSEventContainer, GLSEventGridContainer>
    where TEvent : BaseGLSEvent
{
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] protected GLSEventAppearanceSO GlsEventAppearance;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] protected BeatmapEasingsSelectionInputController EasingInputController;

    public override bool CanPlace =>
        base.CanPlace
        && glsEventGridProvider.GroupContext != null
        && glsEventGridProvider.GroupContext.GetType() == typeof(TGroup)
        && QueuedData.EventBoxGroupData.ReadOnlyBoxes.Count > 0
        // GLS event times are offsets from their group and cannot precede its beat.
        && QueuedData.RelativeJsonTime >= 0f;

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        throw new ArgumentException("If you triggered this, you tried to use add object where it couldn't");

    public override void Start()
    {
        base.Start();
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
    }

    public void OnDestroy() => glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;

    private void HandleGroupChanged(BaseEventBoxGroup group) => QueuedData.EventBoxGroupData = group;

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        PlacementVisualContainer.EventData = QueuedData;
        PlacementVisualContainer.SafeSetActive(CanPlace);
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        var i = (int)(PlacementVisualContainer.transform.localPosition.x - 0.5f);
        QueuedData.RelativeJsonTime = RoundedJsonTime - QueuedData.EventBoxGroupData.JsonTime;
        QueuedData.RecomputeSongBpmTime();
        // Re-evaluate after updating the offset so the hover node immediately hides before the group.
        PlacementVisualContainer.SafeSetActive(CanPlace);
        if (QueuedData.EventBoxGroupData.ReadOnlyBoxes.Count == 0) return;
        // Clamp to the final valid list index; Count itself is out of range and caused repeated placement exceptions.
        var boxIndex = Math.Clamp(i, 0, QueuedData.EventBoxGroupData.ReadOnlyBoxes.Count - 1);
        QueuedData.EventBoxData = QueuedData.EventBoxGroupData.ReadOnlyBoxes[boxIndex];
        QueuedData.BoxIndex = boxIndex;
    }

    public override void HandleApply()
    {
        // we omit the action here, the same otherwise
        ObjectContainerCollection.SpawnObject(QueuedData, out _);
        QueuedData = BeatmapFactory.Clone(QueuedData);
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        PlacementVisualContainer.EventData = QueuedData;
    }

    public override void Apply()
    {
        // Guard direct placement calls as well as the normal input-system CanPlace filter.
        if (CanPlace)
            base.Apply();
    }

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = base.StartDrag(draggedObject);
        if (con == null) return null;

        // imagine having to assign this bullshit again and agian
        DraggedObjectData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        OriginalQueued.EventBoxGroupData = glsEventGridProvider.GroupContext;
        OriginalDraggedObjectData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;

        return con;
    }

    public override void FinishDrag()
    {
        // Restore the original node when a drag would move it before its group's beat.
        if (DraggedObjectData.RelativeJsonTime < 0f)
        {
            ObjectContainerCollection.SpawnObject(OriginalDraggedObjectData, out _);
            QueuedData = BeatmapFactory.Clone(OriginalQueued);
            QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;

            DraggedObjectContainer.Dragged = false;
            DraggedObjectContainer = null;
            IsDragging = false;

            PlacementVisualContainer.EventData = QueuedData;
            return;
        }

        // slightly different, just no action
        ObjectContainerCollection.SpawnObject(DraggedObjectData, out _);

        QueuedData = BeatmapFactory.Clone(OriginalQueued);
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;

        DraggedObjectContainer.Dragged = false;
        DraggedObjectContainer = null;
        HandleDragged();
        IsDragging = false;

        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void TransferQueuedToDraggedObject(ref TEvent dragged, TEvent queued)
    {
        dragged.RelativeJsonTime = queued.RelativeJsonTime;
        dragged.JsonTime = queued.JsonTime;
        dragged.EventBoxData = queued.EventBoxData;
        dragged.BoxIndex = queued.BoxIndex;
    }
}
