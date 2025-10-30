using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EventPlacement : BasePlacement<BaseEvent, EventContainer, EventGridContainer>,
                              CMInput.IEventPlacementActions
{
    [SerializeField] private EventAppearanceSO eventAppearanceSo;

    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private TMP_InputField laserSpeedInputField;
    [SerializeField] private Toggle chromaToggle;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private CreateEventTypeLabels labels;

    public bool PlacePrecisionRotation;
    public int PrecisionRotationValue;

    private bool earlyRotationPlaceNow;
    private bool isHalfFloatValuePressed;
    private bool isZeroFloatValuePressed;
    private bool negativeRotations;
    internal float queuedFloatValue = 1.0f;
    internal float queuedRotation = 30f;

    internal int queuedValue = (int)LightValue.RedOn;

    public static bool CanPlaceChromaEvents => Settings.Instance.PlaceChromaColor;

    public void OnRotation15Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsLaneRotationEvent() && context.performed) UpdateRotation(negativeRotations ? -15f : 15f);
    }

    public void OnRotation30Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsLaneRotationEvent() && context.performed) UpdateRotation(negativeRotations ? -30f : 30f);
    }

    public void OnRotation45Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsLaneRotationEvent() && context.performed) UpdateRotation(negativeRotations ? -45f : 45f);
    }

    public void OnRotation60Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsLaneRotationEvent() && context.performed) UpdateRotation(negativeRotations ? -60f : 60f);
    }

    public void OnNegativeRotationModifier(InputAction.CallbackContext context) =>
        negativeRotations = context.performed;

    public void OnHalfFloatValueModifier(InputAction.CallbackContext context) =>
        isHalfFloatValuePressed = context.performed;

    public void OnZeroFloatValueModifier(InputAction.CallbackContext context) =>
        isZeroFloatValuePressed = context.performed;

    public void OnRotateInPlaceLeft(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(false, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceRight(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(true, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceModifier(InputAction.CallbackContext context) =>
        earlyRotationPlaceNow = context.performed;

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed an Event.");

    protected override BaseEvent GenerateOriginalData() => new();

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void UpdateData(PlacementInputState inputState)
    {
        if (ObjectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Off)
        {
            QueuedData.Type =
                labels.LaneIdToEventType(Mathf.FloorToInt(PlacementVisualContainer.transform.localPosition.x));
            QueuedData.CustomLightID = null;
        }
        else
        {
            var propID = Mathf.FloorToInt(PlacementVisualContainer.transform.localPosition.x - 1);
            QueuedData.Type = ObjectContainerCollection.EventTypeToPropagate;

            if (propID >= 0)
            {
                var lightIdToApply = ObjectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Prop
                    ? labels.PropIdToLightIds(ObjectContainerCollection.EventTypeToPropagate, propID)
                    : new[] { labels.EditorToLightID(ObjectContainerCollection.EventTypeToPropagate, propID) };
                QueuedData.CustomLightID = lightIdToApply;
            }
            else
                QueuedData.CustomLightID = null;
        }

        if (CanPlaceChromaEvents
            && dropdown.Visible
            && QueuedData.IsLightEvent(EnvironmentInfoHelper.GetName())
            && QueuedData.Value != (int)LightValue.Off)
            QueuedData.CustomColor = colorPicker.CurrentColor;
        else
            QueuedData.CustomColor = null;

        UpdateQueuedValue(queuedValue);
        UpdateQueuedFloatValue(queuedFloatValue);
        UpdateQueuedRotation(queuedRotation);
        UpdateAppearance();
    }

    public void UpdateQueuedValue(int value)
    {
        QueuedData.Value = value;

        if ((QueuedData.IsLaserRotationEvent() || QueuedData.IsUtilityEvent())
            && int.TryParse(laserSpeedInputField.text, out var laserSpeed))
            QueuedData.Value = laserSpeed;

        if (QueuedData.IsColorBoostEvent()) QueuedData.Value = QueuedData.Value > 0 ? 1 : 0;
    }

    public void UpdateValue(int value)
    {
        queuedValue = value;
        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedFloatValue(float value)
    {
        if (!QueuedData.IsLightEvent())
        {
            QueuedData.FloatValue = 1f;
            return;
        }

        if (isZeroFloatValuePressed)
            QueuedData.FloatValue = 0f;
        else if (isHalfFloatValuePressed)
            QueuedData.FloatValue = value * 0.5f;
        else
            QueuedData.FloatValue = value;
    }

    public void UpdateFloatValue(float value)
    {
        queuedFloatValue = value;
        UpdateQueuedFloatValue(queuedFloatValue);
        UpdateAppearance();
    }

    private void UpdateQueuedRotation(float rotation)
    {
        if (!QueuedData.IsLaneRotationEvent()) return;

        QueuedData.Rotation = rotation;
    }

    public void UpdateRotation(float rotation)
    {
        queuedRotation = rotation;
        UpdateQueuedRotation(queuedRotation);
        UpdateAppearance();
    }

    public void SwapColors(bool red)
    {
        if (!QueuedData.IsLightEvent()) return;
        if (queuedValue >= ColourManager.RgbintOffset || queuedValue == (int)LightValue.Off) return;
        if ((red && queuedValue >= (int)LightValue.RedOn)
            || (!red && queuedValue >= (int)LightValue.BlueOn && queuedValue < (int)LightValue.RedOn))
            return;

        switch (queuedValue)
        {
            case > 0 and <= 4:
            // red to white
            case > 4 and <= 8:
                queuedValue += 4; // blue to red
                break;
            case > 8 and <= 12:
                queuedValue -= 8; // white to blue
                break;
        }
    }

    private void UpdateAppearance()
    {
        if (PlacementVisualContainer is null)
        {
            CreateVisual();
            if (IsIdle) HideVisual();
        }

        PlacementVisualContainer!.EventData = QueuedData;
        eventAppearanceSo.SetEventAppearance(PlacementVisualContainer, false);
    }

    public void PlaceChroma(bool v) => Settings.Instance.PlaceChromaColor = v;

    public override void HandleApply()
    {
        var evt = QueuedData;

        if (evt.IsLaneRotationEvent())
        {
            if (!GridRotation.IsActive)
            {
                PersistentUI.Instance.ShowDialogBox("Mapper", "360warning", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }

        base.HandleApply();

        if (evt.IsLaneRotationEvent()) TracksManager.RefreshTracks();

        QueuedData = new BaseEvent(evt); // need to convert back to regular event
        QueuedData.CustomData = null;
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void TransferQueuedToDraggedObject(ref BaseEvent dragged, BaseEvent queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.Type = queued.Type;
        // Instead of copying the whole custom data, only copy prop ID
        if (dragged.CustomData != null && queued.CustomData != null)
        {
            if (queued.CustomData?[queued.CustomKeyPropID] != null)
                dragged.GetOrCreateCustom()[dragged.CustomKeyPropID] = queued.CustomData[queued.CustomKeyPropID];

            if (queued.CustomLightID != null) dragged.CustomLightID = queued.CustomLightID;
        }
    }

    internal void PlaceRotationNow(bool right, bool early)
    {
        if (!GridRotation.IsActive) return;

        var rotationType = early ? (int)EventTypeValue.EarlyLaneRotation : (int)EventTypeValue.LateLaneRotation;
        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        var evt = ObjectContainerCollection.AllRotationEvents.Find(x =>
            x.JsonTime - epsilon < Atsc.CurrentJsonTime
            && x.JsonTime + epsilon > Atsc.CurrentJsonTime
            && x.Type == rotationType);

        //todo add support for custom rotation angles

        var startingValue = right ? 4 : 3;
        if (evt != null) startingValue = evt.Value;

        if (evt != null
            && ((startingValue == 4 && !right)
                || (startingValue == 3
                    && right))) //This is for when we're going from a rotation event to no rotation event
        {
            startingValue = evt.Value;
            ObjectContainerCollection.DeleteObject(evt, false);
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(evt, "Deleted by PlaceRotationNow."));
        }
        else if ((startingValue < 7 && right) || (startingValue > 0 && !right))
        {
            if (evt != null) startingValue += right ? 1 : -1;
            var objectData = new BaseEvent
            {
                JsonTime = Atsc.CurrentJsonTime, Type = rotationType, Value = startingValue
            };

            ObjectContainerCollection.SpawnObject(objectData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        }

        QueuedData = BeatmapFactory.Clone(QueuedData);
        TracksManager.RefreshTracks();
    }

    protected override void HandleDragged() => TracksManager.RefreshTracks();
}
