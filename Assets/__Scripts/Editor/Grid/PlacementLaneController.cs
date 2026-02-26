using UnityEngine;

public class PlacementLaneController : MonoBehaviour
{
    [SerializeField] private PlacementModeController placemenModeController;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private GridLane lane;
    private bool hasOffset;
    private bool hasExpanded;

    public int HeightCount = 3;
    public int LaneCount = 4;
    public int ObstacleLaneExtend;
    private bool canExpand;
    private bool expandFullyOnBothState;

    private void OnValidate() => UpdateObstacleLane();

    public void Awake()
    {
        Settings.NotifyBySettingName("NoteLanes", HandleNoteLanesChanged);
        Settings.NotifyBySettingName("ObstacleLanesExtend", HandleObstacleLanesExtendChanged);
        LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
        placemenModeController.OnModeChanged += HandleModeChanged;
        obstaclePlacement.OnApplied += UpdateGrid;
        if (Settings.NonPersistentSettings.ContainsKey("NoteLanes")) Settings.NonPersistentSettings["NoteLanes"] = 4;
        if (Settings.NonPersistentSettings.ContainsKey("ObstacleLanesExtend"))
            Settings.NonPersistentSettings["ObstacleLanesExtend"] = 0;
    }

    public void OnDestroy()
    {
        Settings.ClearSettingNotifications("NoteLanes");
        Settings.ClearSettingNotifications("ObstacleLanesExtend");
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
        placemenModeController.OnModeChanged -= HandleModeChanged;
        obstaclePlacement.OnApplied -= UpdateGrid;
    }

    private void HandleLevelLoaded()
    {
        canExpand = BeatSaberSongContainer.Instance.Map.MajorVersion != 2;
        expandFullyOnBothState = BeatSaberSongContainer.Instance.Map.MajorVersion == 4;
    }

    private void HandleModeChanged(PlacementModeController.PlacementMode _) => UpdateGrid();

    private void HandleNoteLanesChanged(object value)
    {
        var text = value.ToString();
        if (!int.TryParse(text, out var lane)) return;
        if (lane < 1) return;
        LaneCount = lane;
        UpdateObstacleLane();
    }

    private void HandleObstacleLanesExtendChanged(object value)
    {
        var text = value.ToString();
        if (!int.TryParse(text, out var lane)) return;
        if (lane < 1) return;
        ObstacleLaneExtend = lane;
        UpdateObstacleLane();
    }

    private void UpdateGrid()
    {
        if (!canExpand) return;
        switch (obstaclePlacement.AllowPlacement)
        {
            case true:
            case false when hasOffset || hasExpanded:
                {
                    UpdateObstacleLane();
                    break;
                }
        }
    }

    private void UpdateObstacleLane()
    {
        if (obstaclePlacement.AllowPlacement && canExpand)
        {
            if (!hasOffset)
            {
                // Offset Y by whole grid or XY grid only
                var offset = lane.XYOffset;
                offset.y = BeatmapConstant.ObstacleYOffset - (BeatmapConstant.PlayerYOffset / 2f);
                // lane.LocalOffset = offset;
                lane.XYOffset = offset;
                lane.RefreshPosition();
                lane.RefreshVisual();
            }

            lane.Lane = LaneCount + (ObstacleLaneExtend * 2);
            switch (obstaclePlacement.IsPlacing)
            {
                case false when expandFullyOnBothState:
                case true:
                    lane.Height = 5;
                    hasExpanded = true;
                    break;
                case false:
                    lane.Height = HeightCount;
                    hasExpanded = false;
                    break;
            }

            hasOffset = true;
        }
        else
        {
            var offset = lane.XYOffset;
            offset.y = 0;
            // lane.LocalOffset = offset;
            lane.XYOffset = offset;
            lane.RefreshPosition();
            lane.RefreshVisual();

            lane.Lane = LaneCount;
            lane.Height = HeightCount;
            hasOffset = hasExpanded = false;
        }
    }
}
