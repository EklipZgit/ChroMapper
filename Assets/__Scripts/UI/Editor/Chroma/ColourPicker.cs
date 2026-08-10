using Beatmap.Base;
using Assets.HSVPicker;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

public class ColourPicker : MonoBehaviour, IEditorStateProvider
{
    // Placement components need the same picker instance that the Chroma menu displays.
    public static ColorPicker ActivePicker { get; private set; }

    // Keep controls outside the flyout synchronized with the authoritative Chroma placement setting.
    public static event System.Action<bool> OnPlaceChromaEventsChanged;

    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Toggle placeChromaToggle;

    // The main Chroma picker is editor-wired with Chroma toggles, while the strobe flyout intentionally leaves them unset.
    private bool IsPrimaryPicker => toggle != null || placeChromaToggle != null;

    // Keep the palette and primary picker selection with the component that owns both controls.
    public string StateKey => "chromaPicker";

    // Start is called before the first frame update
    private void Start()
    {
        // Keep the strobe flyout from replacing Picker 2.0 as the shared Chroma placement picker.
        if (IsPrimaryPicker)
        {
            ActivePicker = picker;
            SelectionController.OnObjectWasSelected += SelectedOnObject;
            EditorStateService.Register(this);
        }
        // Strobe's flyout host intentionally has no Chroma toggles of its own.
        if (toggle != null)
        {
            toggle.isOn = Settings.Instance.PickColorFromChromaEvents;
        }

        if (placeChromaToggle != null)
        {
            // Replace the scene callback so tile and flyout changes share one setting update path.
            placeChromaToggle.onValueChanged = new Toggle.ToggleEvent();
            placeChromaToggle.onValueChanged.AddListener(SetPlaceChromaEvents);
            SetPlaceChromaEvents(Settings.Instance.PlaceChromaColor);
        }

    }

    private void OnDestroy()
    {
        // The main picker alone owns selection synchronization, so teardown only unregisters that editor-wired instance.
        if (IsPrimaryPicker)
        {
            SelectionController.OnObjectWasSelected -= SelectedOnObject;
            EditorStateService.Unregister(this);
            // Do not leave a destroyed menu picker available to placement components.
            if (ReferenceEquals(ActivePicker, picker))
                ActivePicker = null;
        }
    }

    public void UpdateColourPicker(bool enabled) => Settings.Instance.PickColorFromChromaEvents = enabled;

    // Let the color-type tile open the existing picker while explicitly controlling Chroma event placement.
    public void OpenForChromaEvents()
    {
        SetPlaceChromaEvents(true);
        dropdown.ToggleDropdown(true);
    }

    // Keep the tile's deselect action from leaving Chroma placement enabled behind a closed picker.
    public void CloseForChromaEvents()
    {
        SetPlaceChromaEvents(false);
        dropdown.ToggleDropdown(false);
    }

    // Close the picker without touching the tile-controlled Chroma event placement setting.
    public void ClosePicker() => dropdown.ToggleDropdown(false);

    // Use one public setter so the flyout checkbox and color-type tile cannot diverge.
    public void SetPlaceChromaEvents(bool enabled)
    {
        Settings.Instance.PlaceChromaColor = enabled;
        if (placeChromaToggle != null)
        {
            placeChromaToggle.SetIsOnWithoutNotify(enabled);
        }

        OnPlaceChromaEventsChanged?.Invoke(enabled);
    }

    // Serialize palette data here so the service never has to discover UI objects.
    public void CaptureEditorState(JSONObject data)
    {
        var presets = new JSONObject();
        foreach (var preset in ColorPresetManager.Presets)
        {
            var colors = new JSONArray();
            foreach (var color in preset.Value.Colors)
            {
                var colorData = new JSONObject();
                colorData.WriteColor(color);
                colors.Add(colorData);
            }

            presets[preset.Key] = colors;
        }

        data["presets"] = presets;
        var selectedColor = new JSONObject();
        selectedColor.WriteColor(picker.CurrentColor);
        data["selectedColor"] = selectedColor;
    }

    // Restore this control after its picker and palette collections have initialized.
    public void LoadEditorState(JSONNode data)
    {
        var presets = data["presets"].AsObject;
        if (presets != null)
        {
            foreach (var preset in presets)
            {
                var colors = new System.Collections.Generic.List<Color>();
                foreach (JSONNode colorData in preset.Value.AsArray)
                {
                    colors.Add(colorData.ReadColor(Color.black));
                }

                ColorPresetManager.Get(preset.Key).UpdateList(colors);
            }
        }

        if (data["selectedColor"].IsObject)
        {
            picker.CurrentColor = data["selectedColor"].ReadColor(Color.black);
        }
    }

    private void SelectedOnObject(BaseObject obj)
    {
        if (!Settings.Instance.PickColorFromChromaEvents || !dropdown.Visible)
            return;
        if (obj.CustomColor != null)
            picker.CurrentColor = (Color)obj.CustomColor;
        if (obj is BaseGLSEvent gls
            && gls.IsChroma()
            && gls.CustomData != null
            && gls.CustomData.HasKey(gls.CustomKeyColor))
        {
            picker.CurrentColor = gls.CustomData[gls.CustomKeyColor].ReadColor();
        }
        if (obj is not BaseEvent e)
            return;
        if (e.Value >= ColourManager.RgbintOffset)
            picker.CurrentColor = ColourManager.ColourFromInt(e.Value);
        else if (e.CustomLightGradient != null)
            picker.CurrentColor = e.CustomLightGradient.StartColor;
    }
}
