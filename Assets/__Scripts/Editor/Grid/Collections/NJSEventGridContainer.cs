using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class NJSEventGridContainer : BeatmapObjectContainerCollection<BaseNJSEvent>
{
    [SerializeField] private GameObject njsEventPrefab;

    public override ObjectType ContainerType => ObjectType.NJSEvent;

    internal override void SubscribeToCallbacks()
    {
        EditorScaleController.OnEditorScaleChanged += HandleEditorScaleChanged;
        Context.Atsc.OnPlayToggled += OnPlayToggle;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.OnEditorScaleChanged -= HandleEditorScaleChanged;
        Context.Atsc.OnPlayToggled -= OnPlayToggle;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;
    }

    private void HandleEditorScaleChanged(float obj) => RefreshPool(true);

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
