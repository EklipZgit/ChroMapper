using System;
using UnityEngine;

public class PlacementLaneController : MonoBehaviour
{
    [SerializeField] private PlacementModeController placemenModeController;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private GridLane lane;
    private bool hasExpanded;
    private bool hasOffset;

    public int LaneCount = 4;
    private bool v2Mode;

    private void OnValidate() => UpdateLane();

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

    private void HandleLevelLoaded() => v2Mode = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
    private void HandleModeChanged(PlacementModeController.PlacementMode _) => UpdateGrid();

    private void HandleNoteLanesChanged(object value)
    {
        var noteLanesText = value.ToString();
        if (!int.TryParse(noteLanesText, out var noteLanes)) return;
        if (noteLanes < 1) return;
        LaneCount = noteLanes;
        UpdateLane();
    }

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

                        UpdateLane();

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
                    UpdateLane();

                    hasOffset = hasExpanded = false;
                    break;
                }
        }
    }

    private void UpdateLane()
    {
        if (obstaclePlacement.AllowPlacement && !v2Mode)
            lane.Lane = (LaneCount * 2) + Mathf.CeilToInt(LaneCount % 2 / 2f);
        else
            lane.Lane = LaneCount;
    }
}
