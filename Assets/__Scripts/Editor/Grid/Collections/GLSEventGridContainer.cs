using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GLSEventGridContainer : BeatmapObjectContainerCollection<BaseGLSEvent>
{
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSEventAppearanceSO glsEventAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    public override ObjectType ContainerType => ObjectType.GLSEvent;

    public override ObjectContainer CreateContainer() =>
        GLSEventContainer.SpawnGLSEvent(null, BeatmapContext.TracksDefinition, ref eventPrefab);

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        // TODO: giga bad, do it somewhere else in factory
        MapObjects = group
            .AbstractBoxes.SelectMany(box =>
            {
                return box switch
                {
                    BaseLightColorEventBox lceb => lceb.Events.Select(evt =>
                    {
                        evt.EventBoxGroupData = group;
                        evt.EventBoxData = box;
                        return evt;
                    }),
                    BaseLightRotationEventBox lreb => lreb.Events.Select(evt =>
                    {
                        evt.EventBoxGroupData = group;
                        evt.EventBoxData = box;
                        return evt;
                    }),
                    BaseLightTranslationEventBox lteb => lteb.Events.Select(evt =>
                    {
                        evt.EventBoxGroupData = group;
                        evt.EventBoxData = box;
                        return evt;
                    }),
                    BaseVfxEventEventBox veeb => veeb.Events.Select(evt =>
                    {
                        evt.EventBoxGroupData = group;
                        evt.EventBoxData = box;
                        return evt;
                    }),
                    _ => Enumerable.Empty<BaseGLSEvent>()
                };
            })
            .Select(evt =>
            {
                evt.SetMap(BeatSaberSongContainer.Instance.Map);
                evt.RecomputeSongBpmTime();
                return evt;
            })
            .ToList();
        MapObjects.Sort();
        RefreshPool(true);
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var c = con as GLSEventContainer;
        if (obj is BaseGLSEvent { EventBoxData: not null, EventBoxGroupData: not null } eventData)
            c.BoxIndex = eventData.EventBoxGroupData.AbstractBoxes.FindIndex(x => x == eventData.EventBoxData);
        else
            c.BoxIndex = -1;
        con.UpdateGridPosition();

        glsEventAppearance.SetAppearance(
            c,
            true,
            eventGridContainer.AllBoostEvents.FindLast(x => x.JsonTime <= obj.JsonTime)?.Value == 1);
    }
}
