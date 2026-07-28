using System;
using Beatmap.Enums;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LightingModeController : MonoBehaviour, IEditorStateProvider
{
    public enum LightingMode
    {
        [PickerChoice("Mapper", "bar.events.on")]
        On,

        [PickerChoice("Mapper", "bar.events.off")]
        Off,

        [PickerChoice("Mapper", "bar.events.flash")]
        Flash,

        [PickerChoice("Mapper", "bar.events.fade")]
        Fade,

        [PickerChoice("Mapper", "bar.events.transition")]
        Transition
    }

    [SerializeField] private EnumPicker lightingPicker;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private MaskableGraphic modeLock;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;
    private LightingMode currentMode;

    private bool hasInitialized;

    // Expose the selected basic-event mode for map-scoped editor metadata persistence.
    public LightingMode CurrentMode => currentMode;

    // Keep the basic-event mode with the picker that presents and changes it.
    public string StateKey => "basicEventLightingMode";

    private void Start()
    {
        InitIfNeeded();
        EditorStateService.Register(this);
    }

    public void SetMode(Enum lightingMode)
    {
        InitIfNeeded();
        lightingPicker.Select(lightingMode);
        UpdateMode(lightingMode);
    }

    // Restore the mode through its regular picker path so the selected UI and queued event value agree.
    public void RestoreEditorState(LightingMode mode) => SetMode(mode);

    // Release the picker and state provider together when this control is destroyed.
    private void OnDestroy()
    {
        EditorStateService.Unregister(this);
        lightingPicker.OnClick -= UpdateMode;
    }

    // Write the currently selected lighting mode without querying another UI component.
    public void CaptureEditorState(JSONObject data) => data["mode"] = (int)currentMode;

    // Apply this picker's cached mode when metadata becomes available after Start.
    public void LoadEditorState(JSONNode data)
    {
        if (data.HasKey("mode"))
        {
            RestoreEditorState((LightingMode)data["mode"].AsInt);
        }
    }

    private void InitIfNeeded()
    {
        if (hasInitialized) return;
        lightingPicker.Initialize(typeof(LightingMode));
        lightingPicker.OnClick += UpdateMode;
        hasInitialized = true;
    }

    public void UpdateValue()
    {
        var red = notePlacement.QueuedData.Type == (int)NoteType.Red;
        var white = notePlacement.QueuedData.Type == (int)NoteType.Bomb;
        switch (currentMode)
        {
            case LightingMode.Off:
                eventPlacement.UpdateValue((int)LightValue.Off);
                break;
            case LightingMode.On:
                eventPlacement.UpdateValue(
                    red ? (int)LightValue.RedOn : white ? (int)LightValue.WhiteOn : (int)LightValue.BlueOn);
                break;
            case LightingMode.Flash:
                eventPlacement.UpdateValue(
                    red ? (int)LightValue.RedFlash : white ? (int)LightValue.WhiteFlash : (int)LightValue.BlueFlash);
                break;
            case LightingMode.Fade:
                eventPlacement.UpdateValue(
                    red ? (int)LightValue.RedFade : white ? (int)LightValue.WhiteFade : (int)LightValue.BlueFade);
                break;
            case LightingMode.Transition:
                eventPlacement.UpdateValue(
                    red   ? (int)LightValue.RedTransition :
                    white ? (int)LightValue.WhiteTransition : (int)LightValue.BlueTransition);
                break;
        }
    }

    private void UpdateMode(Enum lightingMode)
    {
        currentMode = (LightingMode)lightingMode;
        UpdateValue();
    }
}
