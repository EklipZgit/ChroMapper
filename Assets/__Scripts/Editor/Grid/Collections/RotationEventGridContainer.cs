using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class RotationEventGridContainer : BeatmapObjectContainerCollection<BaseRotationEvent>
{
    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;

    public override ObjectType ContainerType => ObjectType.RotationEvent;

    internal override void SubscribeToCallbacks() => BeatmapContext.Atsc.OnPlayToggled += OnPlayToggle;
    internal override void UnsubscribeToCallbacks() => BeatmapContext.Atsc.OnPlayToggled -= OnPlayToggle;

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        tracksManager.RefreshTracks();
        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);

    private void OnPlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    public override ObjectContainer CreateContainer() => RotationEventContainer.SpawnEvent(null, ref eventPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj) =>
        eventAppearance.SetAppearance(con as RotationEventContainer);
}
