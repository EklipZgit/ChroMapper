using TMPro;
using UnityEngine;

public class FloatValueController : DisableActionsField, IEditorStateProvider
{
    [PickerChoice("Mapper", "bar.events.floatValue")]
    [SerializeField] private TMP_InputField floatValue;

    [SerializeField] private EventPlacement eventPlacement;

    // Keep the rendered basic-event float input with the control that owns its text.
    public string StateKey => "basicEventFloatInput";

    private void Start()
    {
        EditorStateService.Register(this);
    }

    // Save this control's backing placement value rather than relying on a global deferred refresh.
    public void CaptureEditorState(SimpleJSON.JSONObject data) => data["value"] = eventPlacement.QueuedFloatValue;

    // Preserve the normal Float Value default when older map metadata has no saved value.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        if (data.HasKey("value"))
        {
            RestoreEditorState(data["value"].AsFloat);
        }
    }

    public void UpdateManualFloatValue(string result)
    {
        if (int.TryParse(result, out var val))
        {
            eventPlacement.UpdateFloatValue(val / 100f);
        }
    }

    // Restore the input display and queued placement value together so they cannot drift after map load.
    public void RestoreEditorState(float value)
    {
        floatValue.SetTextWithoutNotify((value * 100f).ToString());
        eventPlacement.UpdateFloatValue(value);
    }
}
