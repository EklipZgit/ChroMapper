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
    [SerializeField] private Toggle chromaToggle;
    [SerializeField] private Image chromaSelected;
    // The Chroma tile has one fill image, so this swatch follows the shared picker color directly.
    [SerializeField] private Image chromaTop;
    // Scene-wired references keep Chroma tile behavior independent of runtime object discovery.
    [SerializeField] private ColourPicker chromaColorPicker;
    [SerializeField] private ColorPicker chromaColorValuePicker;
    [SerializeField] private CanvasGroup[] oemColorCanvasGroups;
    
    [Header("Context Changed")]
    [SerializeField] private GameObject whiteTarget;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    private void Start()
    {
        // Chroma is an independent placement mode, so retain the OEM type indicator beneath its separate selection.
        redSelected.enabled = true;
        blueSelected.enabled = false;
        whiteSelected.enabled = false;
        SetChromaUi(Settings.Instance.PlaceChromaColor);
        customColorsUIController.Context = beatmapRuntimeContext;
        customColorsUIController.RefreshColors();
        beatmapRuntimeContext.OnColorSchemeChanged += HandleColorSchemeChanged;
        editModeContext.OnEditModeChanged += HandleEditModeModeChanged;
        customColorsUIController.OnCustomColorsUpdated += HandleCustomColorUIControllerUpdated;
        ColourPicker.OnPlaceChromaEventsChanged += SetChromaUi;
        chromaColorValuePicker.ONValueChanged.AddListener(SetChromaColor);
        SetChromaColor(chromaColorValuePicker.CurrentColor);

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
        ColourPicker.OnPlaceChromaEventsChanged -= SetChromaUi;
        chromaColorValuePicker.ONValueChanged.RemoveListener(SetChromaColor);
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
            // Keep all four lighting color tiles compact at runtime to match the authored picker layout.
            gridLayoutGroup.cellSize = new Vector2(11, 11);
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
        if (active)
        {
            SelectOemColor((int)NoteType.Red);
        }
    }

    public void BlueNote(bool active)
    {
        if (active)
        {
            SelectOemColor((int)NoteType.Blue);
        }
    }

    public void BombNote(bool active)
    {
        if (active)
        {
            SelectOemColor((int)NoteType.Bomb);
        }
    }

    // The Chroma color tile augments the current OEM color rather than replacing its fallback event type.
    public void ChromaColor(bool active)
    {
        if (chromaColorPicker == null)
        {
            Debug.LogError("[ColorTypeController] Chroma Color Picker is not assigned in 03_Mapper.");
            Settings.Instance.PlaceChromaColor = false;
            SetChromaUi(false);
            return;
        }

        if (active)
        {
            // White light events intentionally render as white, so switch their non-Chroma fallback to Primary before applying an RGB override.
            if (SelectedColorType == (int)NoteType.Bomb)
            {
                UpdateValue((int)NoteType.Red);
            }

            chromaColorPicker.OpenForChromaEvents();
        }
        else
        {
            chromaColorPicker.CloseForChromaEvents();
        }
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

    // Selecting an OEM tile returns to normal events and closes the Chroma flyout if it owns placement.
    private void SelectOemColor(int type)
    {
        if (chromaToggle.isOn)
        {
            // OEM selection must disable the same Chroma color event mode that the tile enables.
            if (chromaColorPicker != null)
            {
                chromaColorPicker.CloseForChromaEvents();
            }
        }

        UpdateValue(type);
    }

    // Dim every OEM tile while preserving its selected ring so the fallback type remains visible.
    private void SetChromaUi(bool enabled)
    {
        chromaToggle.SetIsOnWithoutNotify(enabled);
        chromaSelected.enabled = enabled;
        foreach (var canvasGroup in oemColorCanvasGroups)
        {
            if (canvasGroup == null)
            {
                Debug.LogError("[ColorTypeController] An OEM Color Canvas Group is not assigned in 03_Mapper.");
                continue;
            }

            // Dim OEM fallback tiles to 40% opacity so the active Chroma tile is visually unambiguous.
            canvasGroup.alpha = enabled ? 0.4f : 1f;
        }
    }

    // Match the Chroma tile's single fill image to the shared picker color.
    private void SetChromaColor(Color color)
    {
        chromaTop.color = color;
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
