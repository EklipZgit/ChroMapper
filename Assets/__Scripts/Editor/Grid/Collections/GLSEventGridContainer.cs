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
        MapObjects.Clear();
        MapObjects.AddRange(
            group switch
            {
                BaseLightColorEventBoxGroup lcebg => lcebg.Boxes.SelectMany(box => box.Events.Select(evt => evt)),
                BaseLightRotationEventBoxGroup lrebg => lrebg.Boxes.SelectMany(box => box.Events.Select(evt => evt)),
                BaseLightTranslationEventBoxGroup ltebg => ltebg.Boxes.SelectMany(box => box.Events.Select(evt => evt)),
                BaseVfxEventEventBoxGroup veebg => veebg.Boxes.SelectMany(box => box.Events.Select(evt => evt)),
                _ => Enumerable.Empty<BaseGLSEvent>()
            });
        MapObjects.Sort();
        RefreshPool(true);
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var c = con as GLSEventContainer;
        con.UpdateGridPosition();

        glsEventAppearance.SetAppearance(
            c,
            true,
            eventGridContainer.AllBoostEvents.FindLast(x => x.JsonTime <= obj.JsonTime)?.Value == 1);
    }
}
