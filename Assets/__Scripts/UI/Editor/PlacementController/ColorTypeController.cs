using System;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

public class ColorTypeController : MonoBehaviour, IEditorStateProvider
{
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private LightingModeController lightingModeController;
    [SerializeField] private CustomColorsUIController customColorsUIController;

    [Header("Visual")]
    [SerializeField] private Image redTop;
    [SerializeField] private Image redBottom;
    [SerializeField] private Image redSelected;
    [SerializeField] private Image blueTop;
    [SerializeField] private Image blueBottom;
    [SerializeField] private Image blueSelected;
    [SerializeField] private Image whiteTop;
    [SerializeField] private Image whiteBottom;
    [SerializeField] private Image whiteSelected;
    
    [Header("Context Changed")]
    [SerializeField] private GameObject whiteTarget;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    private void Start()
    {
        // Color type indicators are mutually exclusive; clear every state before selecting the primary default.
        redSelected.enabled = true;
        blueSelected.enabled = false;
        whiteSelected.enabled = false;
        customColorsUIController.Context = beatmapRuntimeContext;
        customColorsUIController.RefreshColors();
        beatmapRuntimeContext.OnColorSchemeChanged += HandleColorSchemeChanged;
        editModeContext.OnEditModeChanged += HandleEditModeModeChanged;
        customColorsUIController.OnCustomColorsUpdated += HandleCustomColorUIControllerUpdated;

        HandleEditModeModeChanged(editModeContext.EditingMode);
        // Restore the selector from the owner after its color-scheme callbacks are ready.
        EditorStateService.Register(this);
    }

    private void OnDestroy()
    {
        EditorStateService.Unregister(this);
        beatmapRuntimeContext.OnColorSchemeChanged -= HandleColorSchemeChanged;
        editModeContext.OnEditModeChanged -= HandleEditModeModeChanged;
        customColorsUIController.OnCustomColorsUpdated -= HandleCustomColorUIControllerUpdated;
    }

    private void HandleColorSchemeChanged(ColorSchemeSO colorScheme)
    {
        if (editModeContext.EditingMode.HasFlag(EditingMode.Gameplay))
        {
            // Gameplay placement swatches should mirror map Chroma note overrides when present.
            redTop.color = redBottom.color = GetLeftNoteColor(colorScheme);
            blueTop.color = blueBottom.color = GetRightNoteColor(colorScheme);
        }
        else
        {
            redTop.color = colorScheme.EnvironmentLeftColor;
            redBottom.color = colorScheme.EnvironmentLeftBoostColor;
            blueTop.color = colorScheme.EnvironmentRightColor;
            blueBottom.color = colorScheme.EnvironmentRightBoostColor;
            whiteTop.color = colorScheme.EnvironmentWhiteColor;
            whiteBottom.color = colorScheme.EnvironmentWhiteBoostColor;
        }
    }

    private void HandleEditModeModeChanged(EditingMode mode)
    {
        if (mode.HasFlag(EditingMode.Gameplay))
        {
            gridLayoutGroup.cellSize = new Vector2(20, 20);
            whiteTarget.SetActive(false);
        }
        else
        {
            gridLayoutGroup.cellSize = new Vector2(14, 14);
            whiteTarget.SetActive(true);
        }

        HandleColorSchemeChanged(beatmapRuntimeContext.ColorScheme);
    }

    private void HandleCustomColorUIControllerUpdated() => HandleColorSchemeChanged(beatmapRuntimeContext.ColorScheme);

    // Map-level Chroma note colors live on difficulty info instead of the active environment color scheme.
    private Color GetLeftNoteColor(ColorSchemeSO colorScheme)
    {
        var customColor = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft;
        return customColor.HasValue
            ? customColor.Value
            : colorScheme.LeftNoteColor;
    }

    // Map-level Chroma note colors live on difficulty info instead of the active environment color scheme.
    private Color GetRightNoteColor(ColorSchemeSO colorScheme)
    {
        var customColor = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight;
        return customColor.HasValue
            ? customColor.Value
            : colorScheme.RightNoteColor;
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
        lightingModeController.UpdateValue();
        UpdateUI();
        OnColorChanged?.Invoke(NoteTypeToLightColor(type));
    }

    public void UpdateUI()
    {
        redSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Red;
        blueSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Blue;
        whiteSelected.enabled = notePlacement.QueuedData.Type == (int)NoteType.Bomb;
    }

    public bool LeftSelectedEnabled() => redSelected.enabled;
    public bool RightSelectedEnabled() => blueSelected.enabled;

    // Expose the active primary/secondary/white selection for map-scoped editor metadata persistence.
    public int SelectedColorType => notePlacement.QueuedData.Type;

    // Keep the shared primary/secondary/white selection with the selector that owns it.
    public string StateKey => "colorType";

    // Write the selector's active value directly into this component's metadata node.
    public void CaptureEditorState(JSONObject data) => data["type"] = SelectedColorType;

    // Apply this selector's cached value when metadata becomes available after Start.
    public void LoadEditorState(JSONNode data)
    {
        if (data.HasKey("type"))
        {
            UpdateValue(data["type"].AsInt);
        }
    }

    public static event Action<int> OnColorChanged;

    private static int NoteTypeToLightColor(int type) =>
        type == (int)NoteType.Bomb ? (int)LightColor.White : type;
}
