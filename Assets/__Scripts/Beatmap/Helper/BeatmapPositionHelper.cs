using UnityEngine;

public static class BeatmapPositionHelper
{
    public const float HysteresisThreshold = 0.01f;

    public static float SnapWithHysteresis(float rawValue, float prevSnapped) =>
        SnapAxisWithHysteresis(rawValue, prevSnapped);

    public static Vector2 SnapWithHysteresis(Vector2 rawPosition, Vector2 previousPosition) =>
        new(
            SnapAxisWithHysteresis(rawPosition.x, previousPosition.x),
            SnapAxisWithHysteresis(rawPosition.y, previousPosition.y));

    private static float SnapAxisWithHysteresis(float rawValue, float prevSnapped)
    {
        var snappedValue = Mathf.Floor(rawValue);

        if (snappedValue == prevSnapped) return prevSnapped;

        if (Mathf.Abs(snappedValue - prevSnapped) > 1f) return snappedValue;

        var boundary = Mathf.Max(snappedValue, prevSnapped);
        var crossedThreshold = snappedValue > prevSnapped
            ? rawValue > boundary + HysteresisThreshold
            : rawValue < boundary - HysteresisThreshold;

        return crossedThreshold ? snappedValue : prevSnapped;
    }

    public static Vector3 LocalPositionToLanePosition(in Vector3 worldPosition, float precision, float yOffset)
    {
        return new Vector3(
            Mathf.FloorToInt(worldPosition.x / BeatmapConstant.LaneSize * precision) / precision,
            Mathf.FloorToInt(
                (worldPosition.y - BeatmapConstant.YOffset - yOffset) / BeatmapConstant.LaneSize * precision)
            / precision,
            worldPosition.z);
    }

    public static Vector3 LocalPositionToLanePositionRound(in Vector3 worldPosition, float precision, float yOffset)
    {
        return new Vector3(
            Mathf.Round(worldPosition.x / BeatmapConstant.LaneSize * precision) / precision,
            Mathf.Round((worldPosition.y - BeatmapConstant.YOffset - yOffset) / BeatmapConstant.LaneSize * precision)
            / precision,
            worldPosition.z);
    }

    public static float SongTimeToLanePositionZ(float songTime) =>
        (songTime * EditorScaleController.EditorScale) + (BeatmapConstant.ZOffset / BeatmapConstant.LaneSize);

    public static Vector3 LocalPositionToLanePosition(in Vector3 worldPosition, float yOffset)
    {
        return new Vector3(
            Mathf.FloorToInt(worldPosition.x / BeatmapConstant.LaneSize),
            Mathf.FloorToInt((worldPosition.y - BeatmapConstant.YOffset - yOffset) / BeatmapConstant.LaneSize),
            worldPosition.z);
    }

    public static Vector3 LanePositionToLocalPosition(in Vector3 lanePosition, float yOffset) =>
        (lanePosition * BeatmapConstant.LaneSize) + new Vector3(0, BeatmapConstant.YOffset + yOffset, 0);

    public static Vector3 LanePositionToLocalPosition(in Vector3 lanePosition, in Bounds bounds, float yOffset)
    {
        var minX = bounds.min.x;
        var maxX = bounds.max.x;
        var minY = bounds.min.y;
        var maxY = bounds.max.y;
        return ((new Vector3(
                        Mathf.Clamp(lanePosition.x, minX, maxX - 1),
                        Mathf.Clamp(lanePosition.y, minY, maxY - 1),
                        lanePosition.z)
                    + (Vector3)(Vector2.one / 2f))
                * BeatmapConstant.LaneSize)
            + new Vector3(0, BeatmapConstant.YOffset + yOffset, 0);
    }
}
