using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRenderingController : MonoBehaviour
{
    private static readonly int offset = Shader.PropertyToID("_Offset");
    private static readonly int gridSpacing = Shader.PropertyToID("_GridSpacing");
    private static readonly int mainColor = Shader.PropertyToID("_Color");
    private static readonly int gridThickness = Shader.PropertyToID("_GridThickness");
    private static readonly Color mainColorDefault = new(0.33f, 0.33f, 0.33f, 1f);
    private static readonly Color mainColorHighContrast = new(0f, 0f, 0f, 1f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private GameObject gridParent;

    private readonly List<GridLane> gridLanes = new();
    private readonly List<Renderer> opaqueGrids = new();
    private readonly List<Renderer> transparentGrids = new();

    private readonly List<Renderer> oneBeat = new();
    private readonly List<Renderer> smallBeatSegment = new();
    private readonly List<Renderer> detailedBeatSegment = new();
    private readonly List<Renderer> preciseBeatSegment = new();

    private readonly List<Renderer> allRenderers = new();

    private MaterialPropertyBlock oneBeatPropertyBlock;
    private MaterialPropertyBlock smallBeatPropertyBlock;
    private MaterialPropertyBlock detailedBeatPropertyBlock;
    private MaterialPropertyBlock preciseBeatPropertyBlock;
    private MaterialPropertyBlock beatColorPropertyBlock;

    private void Awake()
    {
        atsc.OnGridMeasureSnappingChanged += HandleGridMeasureSnappingChanged;

        oneBeatPropertyBlock = new();
        smallBeatPropertyBlock = new();
        detailedBeatPropertyBlock = new();
        preciseBeatPropertyBlock = new();
        beatColorPropertyBlock = new();

        foreach (var gridLane in gridParent.GetComponentsInChildren<GridLane>())
        {
            gridLanes.Add(gridLane);
            opaqueGrids.Add(gridLane.XZ.InterfaceOpaque);
            transparentGrids.Add(gridLane.XZ.InterfaceTransparent);

            oneBeat.Add(gridLane.XZ.OneBeat);
            smallBeatSegment.Add(gridLane.XZ.SmallBeatSegment);
            detailedBeatSegment.Add(gridLane.XZ.DetailedBeatSegment);
            preciseBeatSegment.Add(gridLane.XZ.PreciseBeatSegment);
        }

        allRenderers.AddRange(oneBeat);
        allRenderers.AddRange(smallBeatSegment);
        allRenderers.AddRange(detailedBeatSegment);
        allRenderers.AddRange(preciseBeatSegment);

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
        Shader.SetGlobalFloat(GridRenderingController.offset, offset);
        if (!atsc.IsPlaying) HandleGridMeasureSnappingChanged(atsc.GridMeasureSnapping);
    }

    private void HandleGridMeasureSnappingChanged(int snapping)
    {
        float gridSeparation = GetLowestDenominator(snapping);
        if (gridSeparation < 3) gridSeparation = 4;

        oneBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f);
        foreach (var g in oneBeat) g.SetPropertyBlock(oneBeatPropertyBlock);

        smallBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (var g in smallBeatSegment) g.SetPropertyBlock(smallBeatPropertyBlock);

        var useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        detailedBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (var g in detailedBeatSegment)
        {
            g.enabled = useDetailedSegments;
            g.SetPropertyBlock(detailedBeatPropertyBlock);
        }

        var usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        preciseBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (var g in preciseBeatSegment)
        {
            g.enabled = usePreciseSegments;
            g.SetPropertyBlock(preciseBeatPropertyBlock);
        }

        UpdateGridColors();
    }

    private void UpdateGridColors(object _ = null)
    {
        var gridAlpha = Settings.Instance.GridTransparency;
        var newColor = Settings.Instance.HighContrastGrids ? mainColorHighContrast : mainColorDefault;
        newColor.a = 1f - gridAlpha;
        beatColorPropertyBlock.SetColor(mainColor, newColor);
        foreach (var g in transparentGrids)
        {
            g.SetPropertyBlock(beatColorPropertyBlock);
            g.enabled = !Mathf.Approximately(newColor.a, 1f);
        }

        foreach (var g in opaqueGrids)
        {
            g.SetPropertyBlock(beatColorPropertyBlock);
            g.enabled = Mathf.Approximately(newColor.a, 1f);
        }
    }

    private void UpdateTrackLength(object _)
    {
        foreach (var gridLane in gridLanes)
            gridLane.SetLength(Settings.Instance.TrackLength * EditorScaleController.EditorScale);
    }

    private void UpdateOneBeat(object value)
    {
        oneBeatPropertyBlock.SetFloat(gridThickness, (float)value);
        foreach (var g in oneBeat) g.SetPropertyBlock(oneBeatPropertyBlock);
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
