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

    public override bool CanPlace => base.CanPlace && glsEventGridProvider.GroupContext.GetType() == typeof(TGroup);

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
        PlacementVisualContainer.SafeSetActive(CanPlace);
        var i = (int)(PlacementVisualContainer.transform.localPosition.x - 0.5f);
        QueuedData.RelativeJsonTime = RoundedJsonTime - QueuedData.EventBoxGroupData.JsonTime;
        QueuedData.RecomputeSongBpmTime();
        (QueuedData.EventBoxData, QueuedData.BoxIndex) = QueuedData.EventBoxGroupData switch
        {
            BaseLightColorEventBoxGroup lcebg => ((BaseEventBox)lcebg.Boxes[Math.Clamp(i, 0, lcebg.Boxes.Count)],
                Math.Clamp(i, 0, lcebg.Boxes.Count)),
            BaseLightRotationEventBoxGroup lrebg => (lrebg.Boxes[Math.Clamp(i, 0, lrebg.Boxes.Count)],
                Math.Clamp(i, 0, lrebg.Boxes.Count)),
            BaseLightTranslationEventBoxGroup ltebg => (ltebg.Boxes[Math.Clamp(i, 0, ltebg.Boxes.Count)],
                Math.Clamp(i, 0, ltebg.Boxes.Count)),
            BaseVfxEventEventBoxGroup ffebg => (ffebg.Boxes[Math.Clamp(i, 0, ffebg.Boxes.Count)],
                Math.Clamp(i, 0, ffebg.Boxes.Count)),
            _ => throw new ArgumentException("Something went wrong.")
        };
    }

    public override void HandleApply()
    {
        // we omit the action here, the same otherwise
        ObjectContainerCollection.SpawnObject(QueuedData, out _);
        QueuedData = BeatmapFactory.Clone(QueuedData);
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        PlacementVisualContainer.EventData = QueuedData;
    }

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = base.StartDrag(draggedObject);
        if (con == null) return null;

        // imagine having to assign this bullshit again and agian
        DraggedObjectData = BeatmapFactory.Clone(DraggedObjectData);
        DraggedObjectData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        OriginalQueued.EventBoxGroupData = glsEventGridProvider.GroupContext;
        OriginalDraggedObjectData.EventBoxGroupData = glsEventGridProvider.GroupContext;
        QueuedData.EventBoxGroupData = glsEventGridProvider.GroupContext;

        return con;
    }

    public override void FinishDrag()
    {
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
