using System;
using UnityEngine;
using UnityEngine.UI;

public class PlatformToggleLightshowMode : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Toggle[] toggles;
    [SerializeField] private EnumPicker enumPicker;

    private void Start()
    {
        enumPicker.Initialize(typeof(LightshowMode));
        enumPicker.OnClick += UpdateMode;
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
        atsc.PlayToggle += SetUninteractible;
    }

    private void UpdateMode(Enum enumMode)
    {
        var mode = (LightshowMode)enumMode;
        switch (mode)
        {
            case LightshowMode.Full:
                descriptor?.SetLightshowMode(LightshowMode.Full);
                break;
            case LightshowMode.Static:
                descriptor?.SetLightshowMode(LightshowMode.Static);
                break;
            case LightshowMode.None:
                descriptor?.SetLightshowMode(LightshowMode.None);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        atsc.PlayToggle -= SetUninteractible;
    }

    private void PlatformLoaded(PlatformDescriptor obj)
    {
        descriptor = obj;
        descriptor.OnLightshowModeChanged += UpdateState;
    }

    private void UpdateState(LightshowMode mode) => enumPicker.Select(mode);

    private void SetUninteractible(bool b)
    {
        enumPicker.Locked = b;
        foreach (var toggle in toggles) toggle.interactable = !b;
    }
}
