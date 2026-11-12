using System;
using System.Collections.Generic;
using Beatmap.Base;

namespace Beatmap.Shared
{
    internal sealed class ObstacleEditWindowIndex
    {
        private IReadOnlyList<BaseObstacle> walls;
        // Leaves follow MapObjects order. Each parent stores the latest wall end below it,
        // so a query can skip groups of walls that finish before the visible grid.
        private float[] maximumEnds = Array.Empty<float>();
        private int leafCount;

        // Pool refreshes after map edits reuse the array so rebuilding wall end times does not allocate.
        public void Rebuild(IReadOnlyList<BaseObstacle> mapWalls)
        {
            walls = mapWalls;
            var requiredLeaves = 1;
            while (requiredLeaves < walls.Count)
            {
                requiredLeaves *= 2;
            }
            if (requiredLeaves > leafCount)
            {
                leafCount = requiredLeaves;
                maximumEnds = new float[leafCount * 2];
            }
            Array.Fill(maximumEnds, float.NegativeInfinity);
            for (var index = 0; index < walls.Count; index++)
            {
                var wall = walls[index];
                maximumEnds[leafCount + index] = wall.SongBpmTime + wall.Duration;
            }
            for (var index = leafCount - 1; index > 0; index--)
            {
                maximumEnds[index] = Math.Max(maximumEnds[index * 2], maximumEnds[index * 2 + 1]);
            }
        }

        public void GetSourcesBefore(float lowerBound, int endIndex, ICollection<BaseObstacle> results)
        {
            AddOverlappingWalls(1, 0, leafCount, lowerBound, endIndex, results);
        }

        private void AddOverlappingWalls(
            int node, int first, int count, float lowerBound, int endIndex, ICollection<BaseObstacle> results)
        {
            if (first >= endIndex || maximumEnds[node] < lowerBound)
            {
                return;
            }
            if (count == 1)
            {
                results.Add(walls[first]);
                return;
            }
            var half = count / 2;
            AddOverlappingWalls(node * 2, first, half, lowerBound, endIndex, results);
            AddOverlappingWalls(node * 2 + 1, first + half, half, lowerBound, endIndex, results);
        }
    }
}
