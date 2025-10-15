using Beatmap.Base;
using Beatmap.V2;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ColourPicker : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private EventGridContainer eventGridContainer;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Toggle placeChromaToggle;

    // Start is called before the first frame update
    private void Start()
    {
        SelectionController.OnObjectWasSelected += SelectedOnObject;
        toggle.isOn = Settings.Instance.PickColorFromChromaEvents;
        placeChromaToggle.isOn = Settings.Instance.PlaceChromaColor;
    }

    private void OnDestroy() => SelectionController.OnObjectWasSelected -= SelectedOnObject;

    public void UpdateColourPicker(bool enabled) => Settings.Instance.PickColorFromChromaEvents = enabled;

    private void SelectedOnObject(BaseObject obj)
    {
        if (!Settings.Instance.PickColorFromChromaEvents || !dropdown.Visible) return;
        if (obj.CustomColor != null) picker.CurrentColor = (Color)obj.CustomColor;
        if (!(obj is BaseEvent e)) return;
        if (e.Value >= ColourManager.RgbintOffset)
            picker.CurrentColor = ColourManager.ColourFromInt(e.Value);
        else if (e.CustomLightGradient != null) picker.CurrentColor = e.CustomLightGradient.StartColor;
    }
}
