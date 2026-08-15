using UnityEngine;

public class PlacementLaneController : MonoBehaviour
{
    [SerializeField] private PlacementModeController placemenModeController;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private GridLane lane;
    private bool hasOffset;
    private bool hasExpanded;

    private int laneCount = 4;
    private int obstacleLaneExtend;
    private bool canExpand;
    private bool expandFullyOnBothState;

    private const int heightCount = 3;
    private const int obstacleHeightCount = 5;

    public int LaneCount
    {
        get => laneCount;
        set
        {
            if (laneCount == value) return;
            laneCount = value;
            HandleNoteLanesChanged();
        }
    }

    public int ObstacleLaneExtend
    {
        get => obstacleLaneExtend;
        set
        {
            if (obstacleLaneExtend == value) return;
            obstacleLaneExtend = value;
            HandleObstacleLanesExtendChanged();
        }
    }

    private void OnValidate() => UpdateObstacleLane();

    public void Awake()
    {
        LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
        placemenModeController.OnModeChanged += HandleModeChanged;
        obstaclePlacement.OnApplied += UpdateGrid;
    }

    public void OnDestroy()
    {
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
        placemenModeController.OnModeChanged -= HandleModeChanged;
        obstaclePlacement.OnApplied -= UpdateGrid;
    }

    private void HandleLevelLoaded()
    {
        canExpand = BeatSaberSongContainer.Instance.Map.MajorVersion != 2;
        expandFullyOnBothState = BeatSaberSongContainer.Instance.Map.MajorVersion == 4;

        // Apply the restored wall mode and lane-extension value after map-version capabilities become available.
        UpdateGrid();
    }

    private void HandleModeChanged(PlacementModeController.PlacementMode _) => UpdateGrid();

    private void HandleNoteLanesChanged()
    {
        if (LaneCount < 1) return;
        UpdateObstacleLane();
    }

    private void HandleObstacleLanesExtendChanged()
    {
        if (ObstacleLaneExtend < 0) return;
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
                    lane.Height = obstacleHeightCount;
                    hasExpanded = true;
                    break;
                case false:
                    lane.Height = heightCount;
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
            lane.Height = heightCount;
            hasOffset = hasExpanded = false;
        }
    }
}
