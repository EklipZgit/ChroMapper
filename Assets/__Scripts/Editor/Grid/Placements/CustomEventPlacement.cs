using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using SimpleJSON;
using UnityEngine;

public class CustomEventPlacement : BasePlacement<BaseCustomEvent, CustomEventContainer, CustomEventGridContainer>
{
    private readonly List<TextAsset> customEventDataPresets = new();
    public override bool CanClickAndDrag => false;

    public override void Start()
    {
        gameObject.SetActive(Settings.Instance.AdvancedShit);
        foreach (var asset in Resources.LoadAll<TextAsset>("Custom Event Presets")) customEventDataPresets.Add(asset);
        Debug.Log($"Loaded {customEventDataPresets.Count} presets for custom events.");
        base.Start();
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed a Custom Event.");

    protected override BaseCustomEvent GenerateOriginalData() => new();

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        var customEventTypeId = Mathf.FloorToInt(PlacementVisualContainer.transform.localPosition.x);
        if (customEventTypeId < ObjectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            QueuedData.Type = ObjectContainerCollection.CustomEventTypes[customEventTypeId];
    }

    public override void HandleApply()
    {
        QueuedData.Data = new JSONObject();

        base.HandleApply();
    }

    protected override void TransferQueuedToDraggedObject(ref BaseCustomEvent dragged, BaseCustomEvent queued) { }
}
