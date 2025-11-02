using UnityEngine;

public class ObstacleGridController : MonoBehaviour
{
    [SerializeField] private PlacementModeController placemenModeController;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private GridLane lane;
    private bool hasExpanded;
    private bool hasOffset;

    private bool v2Mode;

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
        obstaclePlacement.OnApplied += UpdateGrid;
    }

    private void HandleLevelLoaded() => v2Mode = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
    private void HandleModeChanged(PlacementModeController.PlacementMode _) => UpdateGrid();

    private void UpdateGrid()
    {
        if (v2Mode) return;
        switch (obstaclePlacement.AllowPlacement)
        {
            case true:
                {
                    if (!hasOffset)
                    {
                        // Offset Y by whole grid or XY grid only
                        var offset = lane.XYOffset;
                        offset.y = -0.5f;
                        // lane.LocalOffset = offset;
                        lane.XYOffset = offset;
                        lane.RefreshPosition();
                        lane.RefreshVisual();

                        lane.Lane += lane.Lane + Mathf.CeilToInt(lane.Lane % 2 / 2f);

                        hasOffset = true;
                    }

                    switch (obstaclePlacement.IsPlacing)
                    {
                        case true when !hasExpanded:
                            lane.Height = 5;
                            hasExpanded = true;
                            break;
                        case false when hasExpanded:
                            lane.Height = 3;
                            hasExpanded = false;
                            break;
                    }

                    break;
                }
            case false when hasOffset || hasExpanded:
                {
                    var offset = lane.XYOffset;
                    offset.y = 0;
                    // lane.LocalOffset = offset;
                    lane.XYOffset = offset;
                    lane.RefreshPosition();
                    lane.RefreshVisual();

                    lane.Height = 3;
                    lane.Lane = NoteLanesController.LaneCount;

                    hasOffset = hasExpanded = false;
                    break;
                }
        }
    }
}
