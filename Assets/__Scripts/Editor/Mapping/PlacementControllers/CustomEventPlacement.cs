using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using SimpleJSON;
using UnityEngine;

public class
    CustomEventPlacement : PlacementController<BaseCustomEvent, CustomEventContainer, CustomEventGridContainer>
{
    private readonly List<TextAsset> customEventDataPresets = new();

    public override int PlacementXMax => objectContainerCollection.CustomEventTypes.Count;

    protected override bool CanClickAndDrag { get; set; } = false;

    protected override Vector2 VanillaOffset { get; } = new(-0.5f, -0.5f);

    internal override void Start()
    {
        gameObject.SetActive(Settings.Instance.AdvancedShit);
        foreach (var asset in Resources.LoadAll<TextAsset>("Custom Event Presets")) customEventDataPresets.Add(asset);
        Debug.Log($"Loaded {customEventDataPresets.Count} presets for custom events.");
        base.Start();
    }

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a Custom Event.");

    public override BaseCustomEvent GenerateOriginalData() => new();

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        var localPosition = instantiatedContainer.transform.localPosition;
        instantiatedContainer.transform.localPosition = new Vector3(localPosition.x + 0.5f, 0.5f, localPosition.z);
        var customEventTypeId = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x);
        if (customEventTypeId < objectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            queuedData.Type = objectContainerCollection.CustomEventTypes[customEventTypeId];
    }

    internal override void ApplyToMap()
    {
        queuedData.Data = new JSONObject();

        base.ApplyToMap();
    }

    public override void TransferQueuedToDraggedObject(ref BaseCustomEvent dragged, BaseCustomEvent queued) { }
}
