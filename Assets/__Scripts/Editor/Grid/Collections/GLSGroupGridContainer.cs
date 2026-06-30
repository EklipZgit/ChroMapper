using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class GLSGroupGridContainer<TGroup> : BeatmapObjectContainerCollection<TGroup>
    where TGroup : BaseEventBoxGroup
{
    [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    internal override void SubscribeToCallbacks() => BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
    internal override void UnsubscribeToCallbacks() => BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    public override ObjectContainer CreateContainer() =>
        GLSGroupContainer.SpawnGLSGroup(
            null,
            BeatmapContext.TrackDefinitions,
            ref eventPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var e = obj as BaseEventBoxGroup;
        con.transform.SetParent(
            glsGroupGridProvider.IdToTracks.TryGetValue(e.ID, out var track)
                ? track.Track.ObjectParentTransform
                : TargetTransform,
            false);

        var pos = con.transform.localPosition;
        pos.x = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(BeatmapContext.TrackDefinitions, e);
        pos.y = 0.5f;
        con.transform.localPosition = pos;

        glsGroupAppearance.SetAppearance(con as GLSGroupContainer, true, eventGridContainer.IsBoostAt(obj.JsonTime));
    }
}
