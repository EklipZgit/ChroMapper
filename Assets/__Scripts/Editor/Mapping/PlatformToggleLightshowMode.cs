using System;
using UnityEngine;
using UnityEngine.UI;

public class PlatformToggleLightshowMode : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private LightshowController lightshowController;
    [SerializeField] private Toggle[] toggles;
    [SerializeField] private EnumPicker enumPicker;

    private void Start()
    {
        enumPicker.Initialize(typeof(LightshowMode));
        enumPicker.OnClick += UpdateMode;
        atsc.OnPlayToggled += SetUninteractible;
    }

    private void UpdateMode(Enum enumMode) => lightshowController.SetMode((LightshowMode)enumMode);

    private void OnDestroy()
    {
        enumPicker.OnClick -= UpdateMode;
        atsc.OnPlayToggled -= SetUninteractible;
    }

    private void SetUninteractible(bool b)
    {
        enumPicker.Locked = b;
        foreach (var toggle in toggles) toggle.interactable = !b;
    }
}
