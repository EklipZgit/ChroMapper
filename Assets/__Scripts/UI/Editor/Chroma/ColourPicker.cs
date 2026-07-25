using Beatmap.Base;
using UnityEngine;
using UnityEngine.UI;

public class ColourPicker : MonoBehaviour
{
    // Placement components need the same picker instance that the Chroma menu displays.
    public static ColorPicker ActivePicker { get; private set; }

    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Toggle placeChromaToggle;

    // Start is called before the first frame update
    private void Start()
    {
        // Keep the strobe flyout from replacing Picker 2.0 as the shared Chroma placement picker.
        if (GetComponentInChildren<StrobeColorPickerController>(true) == null)
        {
            ActivePicker = picker;
            // Apply this map's saved Chroma selection after this picker has initialized.
            ColourHistory.ApplyLoadedChromaColor(picker);
            SelectionController.OnObjectWasSelected += SelectedOnObject;
        }
        // Strobe's flyout host intentionally has no Chroma toggles of its own.
        if (toggle != null)
            toggle.isOn = Settings.Instance.PickColorFromChromaEvents;
        if (placeChromaToggle != null)
            placeChromaToggle.isOn = Settings.Instance.PlaceChromaColor;
    }

    private void OnDestroy()
    {
        if (GetComponentInChildren<StrobeColorPickerController>(true) == null)
        {
            SelectionController.OnObjectWasSelected -= SelectedOnObject;
            // Do not leave a destroyed menu picker available to placement components.
            if (ReferenceEquals(ActivePicker, picker))
                ActivePicker = null;
        }
    }

    public void UpdateColourPicker(bool enabled) => Settings.Instance.PickColorFromChromaEvents = enabled;

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
