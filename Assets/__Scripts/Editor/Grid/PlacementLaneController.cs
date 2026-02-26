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
    private bool canExpand;
    private bool expandFullyOnBothState;

    private void OnValidate() => UpdateObstacleLane();

    public void Awake()
    {
        Settings.NotifyBySettingName("NoteLanes", HandleNoteLanesChanged);
        LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
        placemenModeController.OnModeChanged += HandleModeChanged;
        obstaclePlacement.OnApplied += UpdateGrid;
        if (Settings.NonPersistentSettings.ContainsKey("NoteLanes")) Settings.NonPersistentSettings["NoteLanes"] = 4;
    }

    public void OnDestroy()
    {
        Settings.ClearSettingNotifications("NoteLanes");
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
        placemenModeController.OnModeChanged -= HandleModeChanged;
        obstaclePlacement.OnApplied -= UpdateGrid;
    }

    private void HandleLevelLoaded()
    {
        canExpand = BeatSaberSongContainer.Instance.Map.MajorVersion != 2;
        expandFullyOnBothState = BeatSaberSongContainer.Instance.Map.MajorVersion == 4;
    }

    private void HandleModeChanged(PlacementModeController.PlacementMode _)
    {
        UpdateGrid();
    }

    private void HandleNoteLanesChanged(object value)
    {
        var noteLanesText = value.ToString();
        if (!int.TryParse(noteLanesText, out var noteLanes)) return;
        if (noteLanes < 1) return;
        LaneCount = noteLanes;
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
                offset.y = BeatmapConstant.ObstacleYOffset;
                // lane.LocalOffset = offset;
                lane.XYOffset = offset;
                lane.RefreshPosition();
                lane.RefreshVisual();
            }
            
            lane.Lane = (LaneCount * 2) + Mathf.CeilToInt(LaneCount % 2 / 2f);
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
