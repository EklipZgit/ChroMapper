using System.Collections.Generic;
using UnityEngine;

public class GridRenderingController : MonoBehaviour
{
    private static readonly Color colorDefault = new(0.33f, 0.33f, 0.33f, 1f);
    private static readonly Color colorHighContrast = new(0f, 0f, 0f, 1f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private GameObject gridParent;

    [SerializeField] private Vector4 zLineSpacing = new(1f, 1f / 4f, 1f / 8f, 1f / 16f);
    [SerializeField] private Vector4 zLineThickness = new(0.1f, 0.05f, 0.025f, 0.0125f);

    private readonly List<GridLane> gridLanes = new();
    private static readonly int offsetID = Shader.PropertyToID("_Offset");

    private void Awake()
    {
        foreach (var gridLane in gridParent.GetComponentsInChildren<GridLane>()) gridLanes.Add(gridLane);

        atsc.OnGridMeasureSnappingChanged += HandleGridMeasureSnappingChanged;
        Settings.NotifyBySettingName(nameof(Settings.HighContrastGrids), UpdateInterfaceXZ);
        Settings.NotifyBySettingName(nameof(Settings.GridTransparency), UpdateInterfaceXZ);
        Settings.NotifyBySettingName(nameof(Settings.InterfaceOpacity), UpdateInterfaceXY);
        Settings.NotifyBySettingName(nameof(Settings.TrackLength), UpdateTrackLength);
        Settings.NotifyBySettingName(nameof(Settings.OneBeatWidth), UpdateOneBeat);

        UpdateOneBeat(Settings.Instance.OneBeatWidth);
    }

    private void OnDestroy()
    {
        atsc.OnGridMeasureSnappingChanged -= HandleGridMeasureSnappingChanged;
        Settings.ClearSettingNotifications(nameof(Settings.HighContrastGrids));
        Settings.ClearSettingNotifications(nameof(Settings.GridTransparency));
        Settings.ClearSettingNotifications(nameof(Settings.InterfaceOpacity));
        Settings.ClearSettingNotifications(nameof(Settings.TrackLength));
        Settings.ClearSettingNotifications(nameof(Settings.OneBeatWidth));
    }

    public void UpdateOffset(float offset)
    {
        Shader.SetGlobalFloat(offsetID, offset);
        if (!atsc.IsPlaying) HandleGridMeasureSnappingChanged(atsc.GridMeasureSnapping);
    }

    private void HandleGridMeasureSnappingChanged(int snapping)
    {
        float gridSeparation = CMMath.GetLowestDenominator(snapping);
        if (gridSeparation < 3) gridSeparation = 4;

        zLineSpacing[0] = EditorScaleController.EditorScale / 4f;
        zLineSpacing[1] = EditorScaleController.EditorScale / 4f / gridSeparation;

        var useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        zLineSpacing[2] = useDetailedSegments ? EditorScaleController.EditorScale / 4f / gridSeparation : 0f;

        var usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        zLineSpacing[3] = usePreciseSegments ? EditorScaleController.EditorScale / 4f / gridSeparation : 0f;

        foreach (var g in gridLanes) g.SetBeatSpacing(zLineSpacing);
        UpdateInterfaceXZ();
    }

    private void UpdateInterfaceXY(object _ = null)
    {
        var newColor = Color.white.WithAlpha(Settings.Instance.InterfaceOpacity);
        foreach (var g in gridLanes) g.SetXYInterfaceColor(newColor);
    }

    private void UpdateInterfaceXZ(object _ = null)
    {
        var gridAlpha = Settings.Instance.GridTransparency;
        var newColor = Settings.Instance.HighContrastGrids ? colorHighContrast : colorDefault;
        newColor.a = Mathf.Clamp01(1f - gridAlpha);
        foreach (var g in gridLanes) g.SetXZInterfaceColor(newColor);
    }

    private void UpdateTrackLength(object _ = null)
    {
        foreach (var gridLane in gridLanes)
            gridLane.Length = Settings.Instance.TrackLength * EditorScaleController.EditorScale;
    }

    private void UpdateOneBeat(object value)
    {
        zLineThickness[0] = (float)value;
        foreach (var g in gridLanes) g.SetBeatThickness(zLineThickness);
    }
}
