using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class GLSGroupPlacement<TGroup, TCollection> : BasePlacement<TGroup, GLSGroupContainer, TCollection>
    where TGroup : BaseEventBoxGroup where TCollection : GLSGroupGridContainer<TGroup>
{
    [SerializeField] public GLSEventTrack GlsEventTrack;

    [SerializeField] private GLSEventAppearanceSO glsEventAppearance;
    [SerializeField] private BeatmapRuntimeContext context;

    public override bool CanPlace => base.CanPlace && IsInPosition();

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a GLS Group.");

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        GlsEventTrack = provider.GetComponent<GLSEventTrack>();
        PlacementTrack = GlsEventTrack.Track.ObjectParentTransform;
        PlacementVisualContainer.TracksDefinition = context.TracksDefinition;
        PlacementVisualContainer.transform.SetParent(PlacementTrack, false);
        PlacementVisualContainer.EventBoxGroupData = QueuedData;
        PlacementVisualContainer.EventBoxGroupData.ID = GlsEventTrack.TrackDefinition.ID;
        PlacementVisualContainer.SafeSetActive(CanPlace);
    }
    
    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        PlacementVisualContainer.SafeSetActive(CanPlace);
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }
    
    protected bool IsInPosition() =>
        Mathf.Approximately(
            Mathf.Floor(PlacementVisualContainer.transform.localPosition.x),
            PlacementVisualContainer.GetPositionFromTrackDefinition());
}
