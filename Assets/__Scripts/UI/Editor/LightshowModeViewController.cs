using System;
using UnityEngine;
using UnityEngine.UI;

public class LightshowModeViewController : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private LightshowController lightshowController;
    [SerializeField] private Toggle[] toggles;
    [SerializeField] private EnumPicker enumPicker;

    private void Start()
    {
        enumPicker.Initialize(typeof(LightshowMode));
        enumPicker.OnClick += HandleClick;
        atsc.OnPlayToggled += HandlePlayToggled;
    }

    private void OnDestroy()
    {
        enumPicker.OnClick -= HandleClick;
        atsc.OnPlayToggled -= HandlePlayToggled;
    }

    private void HandleClick(Enum enumMode) => lightshowController.SetMode((LightshowMode)enumMode);

    private void HandlePlayToggled(bool play)
    {
        enumPicker.Locked = play;
        foreach (var toggle in toggles) toggle.interactable = !play;
    }
}
