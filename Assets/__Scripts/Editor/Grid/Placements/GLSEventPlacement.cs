using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
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
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed a GLS Event.");

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
        // this doesn't affect the actual beatmap data so we're safe here
        ObjectContainerCollection.SpawnObject(QueuedData, out _);

        // convert back collection and replace the group instead
        var newGroup = BeatmapFactory.Clone(QueuedData.EventBoxGroupData);
        // the typa shit i had to pull to amke this work
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                foreach (var boxEvents in ObjectContainerCollection
                    .MapObjects
                    .Select(e =>
                    {
                        var newEvt = BeatmapFactory.Clone(e);
                        newEvt.EventBoxGroupData = newGroup;
                        newEvt.EventBoxData = lcebg.Boxes[e.BoxIndex];
                        newEvt.BoxIndex = e.BoxIndex;
                        return newEvt as BaseLightColorBase;
                    })
                    .GroupBy(e => e.BoxIndex))
                    lcebg.Boxes[boxEvents.Key].Events = boxEvents.ToArray();
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                foreach (var boxEvents in ObjectContainerCollection
                    .MapObjects
                    .Select(e =>
                    {
                        var newEvt = BeatmapFactory.Clone(e);
                        newEvt.EventBoxGroupData = newGroup;
                        newEvt.EventBoxData = lrebg.Boxes[e.BoxIndex];
                        newEvt.BoxIndex = e.BoxIndex;
                        return newEvt as BaseLightRotationBase;
                    })
                    .GroupBy(e => e.BoxIndex))
                    lrebg.Boxes[boxEvents.Key].Events = boxEvents.ToArray();
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                foreach (var boxEvents in ObjectContainerCollection
                    .MapObjects
                    .Select(e =>
                    {
                        var newEvt = BeatmapFactory.Clone(e);
                        newEvt.EventBoxGroupData = newGroup;
                        newEvt.EventBoxData = ltebg.Boxes[e.BoxIndex];
                        newEvt.BoxIndex = e.BoxIndex;
                        return newEvt as BaseLightTranslationBase;
                    })
                    .GroupBy(e => e.BoxIndex))
                    ltebg.Boxes[boxEvents.Key].Events = boxEvents.ToArray();
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                foreach (var boxEvents in ObjectContainerCollection
                    .MapObjects
                    .Select(e =>
                    {
                        var newEvt = BeatmapFactory.Clone(e);
                        newEvt.EventBoxGroupData = newGroup;
                        newEvt.EventBoxData = ffebg.Boxes[e.BoxIndex];
                        newEvt.BoxIndex = e.BoxIndex;
                        return newEvt as BaseFxEventFloat;
                    })
                    .GroupBy(e => e.BoxIndex))
                    ffebg.Boxes[boxEvents.Key].Events = boxEvents.ToArray();
                break;
            default:
                throw new ArgumentException("Something went wrong.");
        }

        BeatmapActionContainer.AddAction(GenerateAction(newGroup, new[] { QueuedData.EventBoxGroupData }));
        glsEventGridProvider.GroupContext = newGroup;

        QueuedData = BeatmapFactory.Clone(QueuedData);
        QueuedData.EventBoxGroupData = newGroup;
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
