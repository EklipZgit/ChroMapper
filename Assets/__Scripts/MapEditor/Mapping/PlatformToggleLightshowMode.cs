using System;
using UnityEngine;
using UnityEngine.UI;

public class PlatformToggleLightshowMode : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Toggle[] toggles;
    [SerializeField] private EnumPicker enumPicker;
    public static Action<LightshowMode> OnLightshowModeChanged;
    public static LightshowMode Mode;

    private void Start()
    {
        enumPicker.Initialize(typeof(LightshowMode));
        enumPicker.OnClick += UpdateMode;
        enumPicker.Select(Mode);
        atsc.PlayToggle += SetUninteractible;
    }

    private static void UpdateMode(Enum enumMode) => OnLightshowModeChanged.Invoke(Mode = (LightshowMode)enumMode);

    private void OnDestroy()
    {
        enumPicker.OnClick -= UpdateMode;
        atsc.PlayToggle -= SetUninteractible;
    }

    private void SetUninteractible(bool b)
    {
        enumPicker.Locked = b;
        foreach (var toggle in toggles) toggle.interactable = !b;
    }
}

public enum LightshowMode
{
    Full,
    Static,
    None,
}
