using System;
using UnityEngine;

public class GLSShiftPicker : MonoBehaviour
{
    [SerializeField] private GLSColorShiftArrayViewController colorShifts;
    [SerializeField] private GLSColorShiftArrayViewController strobeColorShifts;
    [SerializeField] private ButtonComponent clearButton;
    [SerializeField] private ButtonComponent closeButton;
    [SerializeField] private FlyoutPanelController flyout;

    public event Action<string[]> OnShiftsChanged;
    public event Action<string[]> OnStrobeShiftsChanged;

    public string[] ColorShifts => colorShifts.GetShifts();
    public string[] StrobeColorShifts => strobeColorShifts.GetShifts();

    public void Open()
    {
        flyout.Open();
    }

    private void Awake()
    {
        clearButton.OnClick(HandleClearClicked);
        colorShifts.Initialize(s => OnShiftsChanged?.Invoke(s));
        strobeColorShifts.Initialize(s => OnStrobeShiftsChanged?.Invoke(s));
        closeButton.OnClick(() => flyout.Close());
    }

    public void SetShifts(string[] color, string[] strobeColor)
    {
        if (color != null) colorShifts.SetShifts(color);
        if (strobeColor != null) strobeColorShifts.SetShifts(strobeColor);
    }

    private void HandleClearClicked()
    {
        colorShifts.SetShifts(Array.Empty<string>());
        strobeColorShifts.SetShifts(Array.Empty<string>());
        OnShiftsChanged?.Invoke(Array.Empty<string>());
        OnStrobeShiftsChanged?.Invoke(Array.Empty<string>());
    }
}