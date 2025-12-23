using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

// look, i dont know how to explain this cryptic stuff beat games pull, but i understood how it work
public class IndexFilterHelper
{
    public class IndexFilter : IReadOnlyCollection<(int element, int durationOrder, int distributionOrder)>
    {
        private readonly RandomType _random;
        private readonly int _seed;
        private readonly int _groupSize;
        private readonly int _chunkSize;
        private readonly int _visibleCount;
        private readonly LimitAlsoAffectType _limitAlsoAffectType;
        private readonly int _start;
        private readonly int _step;
        private readonly int _count;
        public int Count => _count;

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
            _start = start;
            _step = step;
            _count = count;
            _random = random;
            _seed = seed;
            _groupSize = groupSize;
            _chunkSize = chunkSize;
            _visibleCount = limit is 0f or 1f
                ? _count
                : Mathf.CeilToInt(_count * limit);
            _limitAlsoAffectType = limitAlsoAffectType;
        }

        private bool LimitsDuration => _limitAlsoAffectType.HasFlag(LimitAlsoAffectType.Duration);
        private bool LimitsDistribution => _limitAlsoAffectType.HasFlag(LimitAlsoAffectType.Distribution);

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
            var ints1 = GetValues();
            if (_random != RandomType.NoRandom
                && !_random.HasFlag(RandomType.KeepOrder))
                ints1 = ints1.Shuffle(new System.Random(_seed));
            var ints2 = Enumerable.Range(0, _count);
            if (_visibleCount > 0)
            {
                ints2 = _random.HasFlag(RandomType.RandomElements)
                    ? ints2.PickRandomElementsWithTombstone(
                        _visibleCount,
                        _count,
                        new System.Random(_seed),
                        -1)
                    : ints2.TakeWithTombstone(_visibleCount, -1);
            }

            var valueTuples = ints1.ZipSkipTombstone(ints2, -1);
            var limitedOrderIndex = 0;
            foreach (var (elementIndex, index) in valueTuples)
            {
                for (var localChunkIndex = 0; localChunkIndex < _chunkSize; ++localChunkIndex)
                {
                    var element = (elementIndex * _chunkSize) + localChunkIndex;
                    if (element < _groupSize)
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
            var value = _start;
            for (var i = 0; i < _count; ++i)
            {
                yield return value;
                value += _step;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static IndexFilter Convert(BaseIndexFilter indexFilter, int groupSize)
    {
        var chunkSize = indexFilter.Chunks == 0 ? 1 : Mathf.CeilToInt(groupSize / (float)indexFilter.Chunks);
        var num1 = Mathf.CeilToInt(groupSize / (float)chunkSize);
        switch (indexFilter.Type)
        {
            case (int)IndexFilterType.Division:
                var p1 = indexFilter.Param0;
                var t1 = indexFilter.Param1;
                var num2 = Mathf.CeilToInt(num1 / (float)p1);
                if (indexFilter.Reverse == 1)
                {
                    var start = num1 - (num2 * t1) - 1;
                    return new IndexFilter(
                        start,
                        Mathf.Max(0, start - num2 + 1),
                        groupSize,
                        (RandomType)indexFilter.Random,
                        indexFilter.Seed,
                        chunkSize,
                        indexFilter.Limit,
                        (LimitAlsoAffectType)indexFilter.LimitAffectsType);
                }

                var start1 = num2 * t1;
                return new IndexFilter(
                    start1,
                    Mathf.Min(num1 - 1, start1 + num2 - 1),
                    groupSize,
                    (RandomType)indexFilter.Random,
                    indexFilter.Seed,
                    chunkSize,
                    indexFilter.Limit,
                    (LimitAlsoAffectType)indexFilter.LimitAffectsType);
            case (int)IndexFilterType.StepAndOffset:
                var p2 = indexFilter.Param0;
                var t2 = indexFilter.Param1;
                var num3 = num1 - p2;
                if (num3 <= 0)
                {
                    Debug.LogWarning("Step and Offset has negative size.");
                    return null;
                }

                var count = t2 == 0 ? 1 : Mathf.CeilToInt(num3 / (float)t2);
                return indexFilter.Reverse == 1
                    ? new IndexFilter(
                        num1 - 1 - p2,
                        -t2,
                        count,
                        groupSize,
                        (RandomType)indexFilter.Random,
                        indexFilter.Seed,
                        chunkSize,
                        indexFilter.Limit,
                        (LimitAlsoAffectType)indexFilter.LimitAffectsType)
                    : new IndexFilter(
                        p2,
                        t2,
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
