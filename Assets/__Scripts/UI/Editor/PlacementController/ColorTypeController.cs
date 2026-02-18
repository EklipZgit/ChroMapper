using Beatmap.Enums;
using UnityEngine;
using UnityEngine.UI;

public class ColorTypeController : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private LightingModeController lightMode;
    [SerializeField] private CustomColorsUIController customColors;
    [SerializeField] private Image leftSelected;
    [SerializeField] private Image rightSelected;
    [SerializeField] private Image leftNote;
    [SerializeField] private Image leftLight;
    [SerializeField] private Image rightNote;
    [SerializeField] private Image rightLight;

    private void Start()
    {
        leftSelected.enabled = true;
        rightSelected.enabled = false;
        customColors.Context = context;
        context.OnColorSchemeChanged += UpdateColors;
        customColors.OnCustomColorsUpdated += SetupColors;
    }

    private void OnDestroy()
    {
        customColors.OnCustomColorsUpdated -= SetupColors;
        context.OnColorSchemeChanged -= UpdateColors;
    }

    private void SetupColors() => UpdateColors(context.ColorScheme);

    private void UpdateColors(ColorSchemeSO colorScheme)
    {
        leftNote.color = context.ColorScheme.LeftNoteColor;
        leftLight.color = context.ColorScheme.EnvironmentLeftColor;
        rightNote.color = context.ColorScheme.RightNoteColor;
        rightLight.color = context.ColorScheme.EnvironmentRightColor;
    }

    public void RedNote(bool active)
    {
        if (active) UpdateValue((int)NoteType.Red);
    }

    public void BlueNote(bool active)
    {
        if (active) UpdateValue((int)NoteType.Blue);
    }

    public void BombNote(bool active)
    {
        if (active) UpdateValue((int)NoteType.Bomb);
    }

    public void UpdateValue(int type)
    {
        notePlacement.UpdateType(type);
        lightMode.UpdateValue();
        UpdateUI();
    }

    public void UpdateUI()
    {
        leftSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Red;
        rightSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Blue;
    }

    public bool LeftSelectedEnabled() => leftSelected.enabled;
    public bool RightSelectEnabled() => rightSelected.enabled;
}
