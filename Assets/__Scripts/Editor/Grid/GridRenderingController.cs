using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRenderingController : MonoBehaviour
{
    private static readonly Color colorDefault = new(0.33f, 0.33f, 0.33f, 1f);
    private static readonly Color colorHighContrast = new(0f, 0f, 0f, 1f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private VariableNJSProvider vNjsProvider;
    [SerializeField] private GridViewController gridViewController;

    [SerializeField] private Vector4 zLineSpacing = new(1f, 1f / 4f, 1f / 8f, 1f / 16f);
    [SerializeField] private Vector4 zLineThickness = new(0.1f, 0.05f, 0.025f, 0.0125f);

    private static readonly int currentHjdShaderID = Shader.PropertyToID("_CurrentHJD");
    private static readonly int displayHjdLineID = Shader.PropertyToID("_DisplayHJDLine");

    private void Awake()
    {
        atsc.OnGridMeasureSnappingChanged += HandleGridMeasureSnappingChanged;
        vNjsProvider.OnChanged += UpdateHJDLine;
        gridViewController.OnGridAdded += HandleGridAdded;
        Settings.NotifyBySettingName(nameof(Settings.HighContrastGrids), UpdateInterfaceXZ);
        Settings.NotifyBySettingName(nameof(Settings.GridTransparency), UpdateInterfaceXZ);
        Settings.NotifyBySettingName(nameof(Settings.InterfaceOpacity), UpdateInterfaceXY);
        Settings.NotifyBySettingName(nameof(Settings.TrackLength), UpdateTrackLength);
        Settings.NotifyBySettingName(nameof(Settings.OneBeatWidth), UpdateOneBeat);
        Settings.NotifyBySettingName(nameof(Settings.DisplayHJDLine), UpdateDisplayHJDLine);

        UpdateDisplayHJDLine(Settings.Instance.DisplayHJDLine);
        UpdateOneBeat(Settings.Instance.OneBeatWidth);
    }

    private void OnDestroy()
    {
        atsc.OnGridMeasureSnappingChanged -= HandleGridMeasureSnappingChanged;
        vNjsProvider.OnChanged -= UpdateHJDLine;
        gridViewController.OnGridAdded -= HandleGridAdded;
        Settings.ClearSettingNotifications(nameof(Settings.HighContrastGrids));
        Settings.ClearSettingNotifications(nameof(Settings.GridTransparency));
        Settings.ClearSettingNotifications(nameof(Settings.InterfaceOpacity));
        Settings.ClearSettingNotifications(nameof(Settings.TrackLength));
        Settings.ClearSettingNotifications(nameof(Settings.OneBeatWidth));
        Settings.ClearSettingNotifications(nameof(Settings.DisplayHJDLine));
    }

    private void HandleGridMeasureSnappingChanged(int snapping)
    {
        float gridSeparation = CMMath.GetLowestDenominator(snapping);
        if (gridSeparation < 3) gridSeparation = 4;

        zLineSpacing[0] = 1f;
        zLineSpacing[1] = 1f / gridSeparation;

        var useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        zLineSpacing[2] = useDetailedSegments ? 1f / gridSeparation : 0f;

        var usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        zLineSpacing[3] = usePreciseSegments ? 1f / gridSeparation : 0f;

        foreach (var g in gridViewController.Where(x => x is GridLane).Cast<GridLane>()) g.SetBeatSpacing(zLineSpacing);
        UpdateInterfaceXZ();
    }

    private void UpdateInterfaceXY(object _ = null)
    {
        var newColor = Color.white.WithAlpha(Settings.Instance.InterfaceOpacity);
        foreach (var g in gridViewController.Where(x => x is GridLane).Cast<GridLane>())
            g.SetXYInterfaceColor(newColor);
    }

    private void UpdateInterfaceXZ(object _ = null)
    {
        var gridAlpha = Settings.Instance.GridTransparency;
        var newColor = Settings.Instance.HighContrastGrids ? colorHighContrast : colorDefault;
        newColor.a = Mathf.Clamp01(1f - gridAlpha);
        foreach (var g in gridViewController.Where(x => x is GridLane).Cast<GridLane>())
            g.SetXZInterfaceColor(newColor);
    }

    private void UpdateTrackLength(object _ = null)
    {
        foreach (var g in gridViewController.Where(x => x is GridLane).Cast<GridLane>())
            g.Length = Settings.Instance.TrackLength * EditorScaleController.EditorScale;
    }

    private void UpdateOneBeat(object value)
    {
        zLineThickness[0] = (float)value;
        foreach (var g in gridViewController.Where(x => x is GridLane).Cast<GridLane>())
            g.SetBeatThickness(zLineThickness);
    }

    private void UpdateHJDLine() => Shader.SetGlobalFloat(currentHjdShaderID, vNjsProvider.HalfJumpDurationInBeats);
    private void UpdateDisplayHJDLine(object value) => Shader.SetGlobalInt(displayHjdLineID, (bool)value ? 1 : 0);

    // TODO: refactor the visual apply part?
    private void HandleGridAdded(GridChild obj)
    {
        if (obj is not GridLane gridLane) return;
        gridLane.SetBeatSpacing(zLineSpacing);
        var newColor = Color.white.WithAlpha(Settings.Instance.InterfaceOpacity);
        gridLane.SetXYInterfaceColor(newColor);
        var gridAlpha = Settings.Instance.GridTransparency;
        newColor = Settings.Instance.HighContrastGrids ? colorHighContrast : colorDefault;
        newColor.a = Mathf.Clamp01(1f - gridAlpha);
        gridLane.SetXZInterfaceColor(newColor);
        gridLane.Length = Settings.Instance.TrackLength * EditorScaleController.EditorScale;
        gridLane.SetBeatThickness(zLineThickness);
    }
}
