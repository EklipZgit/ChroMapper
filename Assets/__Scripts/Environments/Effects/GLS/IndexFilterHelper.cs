using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

// look, i dont know how to explain this cryptic stuff beat games pull, but i understood how it work
public static class IndexFilterHelper
{
    public class IndexFilter : IReadOnlyCollection<(int element, int durationOrder, int distributionOrder)>
    {
        private readonly RandomType random;
        private readonly int seed;
        private readonly int groupSize;
        private readonly int chunkSize;
        private readonly int visibleCount;
        private readonly LimitAlsoAffectType limitAlsoAffectType;
        private readonly int start;
        private readonly int step;
        private readonly int count;
        public int Count => count;

        public IndexFilter(
            int start,
            int step,
            int count,
            int groupSize,
            RandomType random,
            int seed,
            int chunkSize,
            float limit,
            LimitAlsoAffectType limitAlsoAffectType)
        {
            this.start = start;
            this.step = step;
            this.count = count;
            this.random = random;
            this.seed = seed;
            this.groupSize = groupSize;
            this.chunkSize = chunkSize;
            visibleCount = limit is 0f or 1f
                ? this.count
                : Mathf.CeilToInt(this.count * limit);
            this.limitAlsoAffectType = limitAlsoAffectType;
        }

        public bool LimitsDuration => limitAlsoAffectType.HasFlag(LimitAlsoAffectType.Duration);
        public bool LimitsDistribution => limitAlsoAffectType.HasFlag(LimitAlsoAffectType.Distribution);
        public int VisibleCount => visibleCount;

        public IndexFilter(
            int start,
            int end,
            int groupSize,
            RandomType random,
            int seed,
            int chunkSize,
            float limit,
            LimitAlsoAffectType limitAlsoAffectType)
            : this(
                start,
                end - start < 0 ? -1 : 1,
                Mathf.Abs(end - start) + 1,
                groupSize,
                random,
                seed,
                chunkSize,
                limit,
                limitAlsoAffectType)
        {
        }

        public IEnumerator<(int element, int durationOrder, int distributionOrder)> GetEnumerator()
        {
            var elements = GetValues();
            if (random != RandomType.NoRandom
                && !random.HasFlag(RandomType.KeepOrder))
                elements = elements.Shuffle(new System.Random(seed));
            var ids = Enumerable.Range(0, count);
            if (visibleCount > 0)
            {
                ids = random.HasFlag(RandomType.RandomElements)
                    ? ids.PickRandomElementsWithTombstone(
                        visibleCount,
                        count,
                        new System.Random(seed),
                        -1)
                    : ids.TakeWithTombstone(visibleCount, -1);
            }

            var elementIdPairs = elements.ZipSkipTombstone(ids, -1);
            var limitedOrderIndex = 0;
            foreach (var (elementIndex, index) in elementIdPairs)
            {
                for (var localChunkIndex = 0; localChunkIndex < chunkSize; ++localChunkIndex)
                {
                    var element = (elementIndex * chunkSize) + localChunkIndex;
                    if (element < groupSize)
                    {
                        var durationOrder = LimitsDuration ? limitedOrderIndex : index;
                        var distributionOrder = LimitsDistribution ? limitedOrderIndex : index;
                        yield return (element, durationOrder, distributionOrder);
                    }
                    else
                        break;
                }

                ++limitedOrderIndex;
            }
        }

        private IEnumerable<int> GetValues()
        {
            var value = start;
            for (var i = 0; i < count; ++i)
            {
                yield return value;
                value += step;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static IndexFilter Convert(BaseIndexFilter indexFilter, int groupSize)
    {
        var chunkSize = indexFilter.Chunks == 0 ? 1 : Mathf.CeilToInt(groupSize / (float)indexFilter.Chunks);
        var offsetSize = Mathf.CeilToInt(groupSize / (float)chunkSize);
        switch (indexFilter.Type)
        {
            case (int)IndexFilterType.Division:
                var section = indexFilter.Param0;
                var sId = indexFilter.Param1;
                var offset = Mathf.CeilToInt(offsetSize / (float)section);
                if (indexFilter.Reverse == 1)
                {
                    var start = offsetSize - (offset * sId) - 1;
                    return new IndexFilter(
                        start,
                        Mathf.Max(0, start - offset + 1),
                        groupSize,
                        (RandomType)indexFilter.Random,
                        indexFilter.Seed,
                        chunkSize,
                        indexFilter.Limit,
                        (LimitAlsoAffectType)indexFilter.LimitAffectsType);
                }

                var start1 = offset * sId;
                return new IndexFilter(
                    start1,
                    Mathf.Min(offsetSize - 1, start1 + offset - 1),
                    groupSize,
                    (RandomType)indexFilter.Random,
                    indexFilter.Seed,
                    chunkSize,
                    indexFilter.Limit,
                    (LimitAlsoAffectType)indexFilter.LimitAffectsType);
            case (int)IndexFilterType.StepAndOffset:
                var id = indexFilter.Param0;
                var step = indexFilter.Param1;
                var offsetStep = offsetSize - id;
                if (offsetStep <= 0)
                {
                    Debug.LogWarning("Step and Offset has negative size.");
                    return null;
                }

                var count = step == 0 ? 1 : Mathf.CeilToInt(offsetStep / (float)step);
                return indexFilter.Reverse == 1
                    ? new IndexFilter(
                        offsetSize - 1 - id,
                        -step,
                        count,
                        groupSize,
                        (RandomType)indexFilter.Random,
                        indexFilter.Seed,
                        chunkSize,
                        indexFilter.Limit,
                        (LimitAlsoAffectType)indexFilter.LimitAffectsType)
                    : new IndexFilter(
                        id,
                        step,
                        count,
                        groupSize,
                        (RandomType)indexFilter.Random,
                        indexFilter.Seed,
                        chunkSize,
                        indexFilter.Limit,
                        (LimitAlsoAffectType)indexFilter.LimitAffectsType);
            default:
                return null;
        }
    }
}
