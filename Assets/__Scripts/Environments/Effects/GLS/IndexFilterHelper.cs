using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

// look, i dont know how to explain this cryptic stuff beat games pull, but i understood how it work
public static class IndexFilterHelper
{
    // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases extends filter entries with dense chunk and light coordinates while retaining three-value deconstruction for existing consumers.
    public readonly struct IndexFilterEntry
    {
        public IndexFilterEntry(
            int element,
            int durationOrder,
            int distributionOrder,
            int affectedChunkOrder,
            int affectedLightOrder)
        {
            Element = element;
            DurationOrder = durationOrder;
            DistributionOrder = distributionOrder;
            AffectedChunkOrder = affectedChunkOrder;
            AffectedLightOrder = affectedLightOrder;
        }

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases exposes immutable dense coordinates without changing OEM element, duration, or distribution ordering.
        public int Element { get; }
        public int DurationOrder { get; }
        public int DistributionOrder { get; }
        public int AffectedChunkOrder { get; }
        public int AffectedLightOrder { get; }

        // Existing GLS consumers deconstruct the OEM coordinates; color shifts read the additional dense chunk field directly.
        public void Deconstruct(out int element, out int durationOrder, out int distributionOrder)
        {
            element = Element;
            durationOrder = DurationOrder;
            distributionOrder = DistributionOrder;
        }
    }

    public class IndexFilter : IReadOnlyCollection<IndexFilterEntry>
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
        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases caches the selected-light denominator after one deterministic filter traversal instead of recounting it for every light.
        private int? affectedLightCount;
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

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases counts only yielded physical lights, including filtered and partial chunks, for the per-light endpoint denominator.
        public int AffectedLightCount
        {
            get
            {
                if (affectedLightCount.HasValue)
                {
                    return affectedLightCount.Value;
                }

                var result = 0;
                foreach (var (elementIndex, _) in GetSelectedChunkPairs())
                {
                    result += Mathf.Min(chunkSize, groupSize - (elementIndex * chunkSize));
                }

                affectedLightCount = result;
                return result;
            }
        }

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

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases increments light order for every yielded physical light while preserving one chunk order for siblings in the same chunk.
        public IEnumerator<IndexFilterEntry> GetEnumerator()
        {
            var limitedOrderIndex = 0;
            var affectedLightOrder = 0;
            foreach (var (elementIndex, index) in GetSelectedChunkPairs())
            {
                for (var localChunkIndex = 0; localChunkIndex < chunkSize; ++localChunkIndex)
                {
                    var element = (elementIndex * chunkSize) + localChunkIndex;
                    if (element < groupSize)
                    {
                        var durationOrder = LimitsDuration ? limitedOrderIndex : index;
                        var distributionOrder = LimitsDistribution ? limitedOrderIndex : index;
                        // ModeBColorShiftsUseDenseAffectedChunkOrder keeps siblings on one chunk coordinate while assigning each affected light its own dense order.
                        yield return new IndexFilterEntry(
                            element,
                            durationOrder,
                            distributionOrder,
                            limitedOrderIndex,
                            affectedLightOrder);
                        ++affectedLightOrder;
                    }
                    else
                        break;
                }

                ++limitedOrderIndex;
            }

            affectedLightCount = affectedLightOrder;
        }

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases shares deterministic random/limit selection between enumeration and denominator calculation so both coordinates describe the same affected lights.
        private IEnumerable<(int elementIndex, int index)> GetSelectedChunkPairs()
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

            return elements.ZipSkipTombstone(ids, -1);
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
        // EmptyGroupReturnsNoFilter prevents missing groups from producing NaN and billion-entry ranges; negative chunks are invalid too.
        if (groupSize <= 0 || indexFilter.Chunks < 0)
        {
            return null;
        }

        // ValidFiltersPreserveSelectedElements keeps chunk counts integral, avoiding float rounding before range construction.
        var chunkSize = indexFilter.Chunks == 0
            ? 1
            : DivideRoundUp(groupSize, indexFilter.Chunks);
        var offsetSize = DivideRoundUp(groupSize, chunkSize);
        switch (indexFilter.Type)
        {
            case (int)IndexFilterType.Division:
                var section = indexFilter.Param0;
                var sId = indexFilter.Param1;
                // InvalidDivisionParametersReturnNoFilter rejects zero divisors and invalid section IDs before arithmetic can overflow.
                if (section <= 0 || sId < 0 || sId >= section)
                {
                    return null;
                }

                // EmptyTrailingDivisionSectionReturnsNoFilter bounds the section before multiplying, so empty slices cannot reverse into valid IDs.
                var offset = DivideRoundUp(offsetSize, section);
                if (sId > (offsetSize - 1) / offset)
                {
                    return null;
                }

                // ValidFiltersPreserveSelectedElements preserves reverse order and the short final section with an explicitly bounded count.
                var sectionStart = offset * sId;
                var sectionCount = Mathf.Min(offset, offsetSize - sectionStart);
                var reverse = indexFilter.Reverse == 1;
                var firstElement = reverse
                    ? offsetSize - sectionStart - 1
                    : sectionStart;
                return new IndexFilter(
                    firstElement,
                    reverse ? -1 : 1,
                    sectionCount,
                    groupSize,
                    (RandomType)indexFilter.Random,
                    indexFilter.Seed,
                    chunkSize,
                    indexFilter.Limit,
                    (LimitAlsoAffectType)indexFilter.LimitAffectsType);
            case (int)IndexFilterType.StepAndOffset:
                var id = indexFilter.Param0;
                var step = indexFilter.Param1;
                // InvalidStepAndOffsetParametersReturnNoFilter rejects negative offsets before subtraction and negative iteration counts.
                if (id < 0 || id >= offsetSize || step < 0)
                {
                    // Preserve the invalid-filter skip, but include the serialized values needed to identify the authored GLS box.
                    Debug.LogWarning(
                        $"[GLS IndexFilter] Skipping invalid StepAndOffset filter: groupSize={groupSize}, " +
                        $"chunks={indexFilter.Chunks}, chunkSize={chunkSize}, offsetSize={offsetSize}, " +
                        $"offset={id}, step={step}, reverse={indexFilter.Reverse}, seed={indexFilter.Seed}.");
                    return null;
                }

                // ValidFiltersPreserveSelectedElements retains zero-step single selection while bounding all other ranges by the available chunks.
                var count = step == 0
                    ? 1
                    : DivideRoundUp(offsetSize - id, step);
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

    // Positive operands established by Convert avoid NaN, float rounding, and the overflow in (value + divisor - 1) / divisor.
    private static int DivideRoundUp(int value, int divisor) => ((value - 1) / divisor) + 1;
}
