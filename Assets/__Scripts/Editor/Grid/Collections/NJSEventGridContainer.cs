using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class NJSEventGridContainer : BeatmapObjectContainerCollection<BaseNJSEvent>
{
    [SerializeField] private GameObject njsEventPrefab;
    // [SerializeField] private NJSEventAppearanceSO njsEventAppearanceSo;

    [SerializeField] private CountersPlusController countersPlus;

    public override ObjectType ContainerType => ObjectType.NJSEvent;

    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.OnPlayToggled += OnPlayToggle;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.OnPlayToggled -= OnPlayToggle;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;
    }

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public override ObjectContainer CreateContainer() => NJSEventContainer.SpawnNJSEvent(null, ref njsEventPrefab);

    // protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    // {
    //     var njsEvent = con as NJSEventContainer;
    //     var njsEventData = obj as BaseNJSEvent;
    //     NJSEventAppearanceSo.SetNJSEventAppearance(njsEvent);
    //     njsEvent.Setup();
    // }
}
