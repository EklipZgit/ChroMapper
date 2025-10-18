using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class GridRenderingController : MonoBehaviour
{
    private static readonly Color colorDefault = new(0.33f, 0.33f, 0.33f, 1f);
    private static readonly Color colorHighContrast = new(0f, 0f, 0f, 1f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private GameObject gridParent;

    private readonly List<GridLane> gridLanes = new();

    private static readonly int colorID = Shader.PropertyToID("_Color");
    private static readonly int offsetID = Shader.PropertyToID("_Offset");
    private static readonly int gridSpacingID = Shader.PropertyToID("_GridSpacing");
    private static readonly int gridThicknessID = Shader.PropertyToID("_GridThickness");

    [SerializeField] private Vector4 subBeat = new(1f, 1f / 4f, 1f / 8f, 1f / 16f);
    [SerializeField] private Vector4 subBeatThickness = new(0.1f, 0.05f, 0.025f, 0.0125f);

    private MaterialPropertyBlock gridMaterialPropertyBlock;
    private MaterialPropertyBlock interfaceMaterialPropertyBlock;

    private void Awake()
    {
        atsc.OnGridMeasureSnappingChanged += HandleGridMeasureSnappingChanged;

        foreach (var gridLane in gridParent.GetComponentsInChildren<GridLane>()) gridLanes.Add(gridLane);

        gridMaterialPropertyBlock = new MaterialPropertyBlock();
        interfaceMaterialPropertyBlock = new MaterialPropertyBlock();

        Settings.NotifyBySettingName(nameof(Settings.HighContrastGrids), UpdateGridColors);
        Settings.NotifyBySettingName(nameof(Settings.GridTransparency), UpdateGridColors);
        Settings.NotifyBySettingName(nameof(Settings.TrackLength), UpdateTrackLength);
        Settings.NotifyBySettingName(nameof(Settings.OneBeatWidth), UpdateOneBeat);

        UpdateOneBeat(Settings.Instance.OneBeatWidth);
    }

    private void OnDestroy()
    {
        atsc.OnGridMeasureSnappingChanged -= HandleGridMeasureSnappingChanged;
        Settings.ClearSettingNotifications(nameof(Settings.HighContrastGrids));
        Settings.ClearSettingNotifications(nameof(Settings.GridTransparency));
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
        float gridSeparation = GetLowestDenominator(snapping);
        if (gridSeparation < 3) gridSeparation = 4;

        subBeat[0] = EditorScaleController.EditorScale / 4f;
        subBeat[1] = EditorScaleController.EditorScale / 4f / gridSeparation;

        var useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        subBeat[2] = useDetailedSegments ? EditorScaleController.EditorScale / 4f / gridSeparation : 0f;

        var usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        subBeat[3] = usePreciseSegments ? EditorScaleController.EditorScale / 4f / gridSeparation : 0f;

        gridMaterialPropertyBlock.SetVector(gridSpacingID, subBeat);
        foreach (var g in gridLanes.Select(g => g.XZ.Grid)) g.SetPropertyBlock(gridMaterialPropertyBlock);
        UpdateGridColors();
    }

    private void UpdateGridColors(object _ = null)
    {
        var gridAlpha = Settings.Instance.GridTransparency;
        var newColor = Settings.Instance.HighContrastGrids ? colorHighContrast : colorDefault;
        newColor.a = Mathf.Clamp01(1f - gridAlpha);
        interfaceMaterialPropertyBlock.SetColor(colorID, newColor);
        foreach (var g in gridLanes.Select(g => g.XZ.Interface)) g.SetPropertyBlock(interfaceMaterialPropertyBlock);
    }

    private void UpdateTrackLength(object _)
    {
        foreach (var gridLane in gridLanes)
            gridLane.SetLength(Settings.Instance.TrackLength * EditorScaleController.EditorScale);
    }

    private void UpdateOneBeat(object value)
    {
        subBeatThickness[0] = (float)value;
        gridMaterialPropertyBlock.SetVector(gridThicknessID, subBeatThickness);
        foreach (var g in gridLanes.Select(g => g.XZ.Grid)) g.SetPropertyBlock(gridMaterialPropertyBlock);
    }

    private int GetLowestDenominator(int a)
    {
        if (a <= 1) return 2;

        IEnumerable<int> factors = PrimeFactors(a);

        if (factors.Any()) return factors.Max();
        return a;
    }

    public static List<int> PrimeFactors(int a)
    {
        var retval = new List<int>();
        for (var b = 2; a > 1; b++)
        {
            while (a % b == 0)
            {
                a /= b;
                retval.Add(b);
            }
        }

        return retval;
    }
}
