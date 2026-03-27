using Beatmap.Enums;
using UnityEngine;
using UnityEngine.UI;

public class ColorTypeController : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext beatmapContext;
    [SerializeField] private EditModeContext editContext;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private LightingModeController lightMode;
    [SerializeField] private CustomColorsUIController customColors;

    [SerializeField] private Image redTop;
    [SerializeField] private Image redBottom;
    [SerializeField] private Image redSelected;
    [SerializeField] private Image blueTop;
    [SerializeField] private Image blueBottom;
    [SerializeField] private Image blueSelected;
    [SerializeField] private Image whiteTop;
    [SerializeField] private Image whiteBottom;
    [SerializeField] private Image whiteSelected;

    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    private void Start()
    {
        redSelected.enabled = true;
        blueSelected.enabled = false;
        customColors.Context = beatmapContext;
        beatmapContext.OnColorSchemeChanged += HandleColorSchemeChanged;
        editContext.OnEditModeChanged += HandleEditModeChanged;
        customColors.OnCustomColorsUpdated += HandleCustomColorUpdated;

        HandleEditModeChanged(editContext.EditingMode);
    }

    private void OnDestroy()
    {
        beatmapContext.OnColorSchemeChanged -= HandleColorSchemeChanged;
        editContext.OnEditModeChanged -= HandleEditModeChanged;
        customColors.OnCustomColorsUpdated -= HandleCustomColorUpdated;
    }

    private void HandleColorSchemeChanged(ColorSchemeSO colorScheme)
    {
        if (editContext.EditingMode.HasFlag(EditingMode.Gameplay))
        {
            redTop.color = redBottom.color = colorScheme.LeftNoteColor;
            blueTop.color = blueBottom.color = colorScheme.RightNoteColor;
        }
        else
        {
            redTop.color = colorScheme.EnvironmentRightColor;
            redBottom.color = colorScheme.EnvironmentLeftBoostColor;
            blueTop.color = colorScheme.EnvironmentRightColor;
            blueBottom.color = colorScheme.EnvironmentRightBoostColor;
            whiteTop.color = colorScheme.EnvironmentWhiteColor;
            whiteBottom.color = colorScheme.EnvironmentWhiteBoostColor;
        }
    }

    private void HandleEditModeChanged(EditingMode mode)
    {
        if (mode.HasFlag(EditingMode.Gameplay))
        {
            gridLayoutGroup.cellSize = new Vector2(16, 16);
            gridLayoutGroup.spacing = new Vector2(8, 8);
        }
        else
        {
            gridLayoutGroup.cellSize = new Vector2(12, 12);
            gridLayoutGroup.spacing = new Vector2(4, 4);
        }

        HandleColorSchemeChanged(beatmapContext.ColorScheme);
    }

    private void HandleCustomColorUpdated() => HandleColorSchemeChanged(beatmapContext.ColorScheme);

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
        redSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Red;
        blueSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Blue;
        whiteSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Bomb;
    }

    public bool LeftSelectedEnabled() => redSelected.enabled;
    public bool RightSelectedEnabled() => blueSelected.enabled;
}
