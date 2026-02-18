using System;
using System.Collections.Generic;
using System.Linq;

public static class ShuffleExtensions
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random random)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Cannot compute shuffle on null collection.");

        var list = new List<T>();
        foreach (var item in source)
        {
            var num = random.Next(list.Count + 1);
            if (num == list.Count)
            {
                list.Add(item);
                continue;
            }

            list.Add(list[num]);
            list[num] = item;
        }

        return list;
    }

    public static IEnumerable<T> PickRandomElementsWithTombstone<T>(
        this IEnumerable<T> source,
        int limit,
        int count,
        Random random,
        T tombstone)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Cannot pick random elements on null collection");

        var num = source.Count();
        if (num != count)
        {
            throw new ArgumentException(
                $"Count property needs to equal enumerable count. Source has count: {num}, but argument is {count}",
                nameof(count));
        }

        var index = 0;
        var picked = 0;
        foreach (var item in source)
        {
            if (random.Next(count - index) < limit - picked)
            {
                picked++;
                yield return item;
            }
            else
                yield return tombstone;

            index++;
        }
    }

    public static IEnumerable<T> TakeWithTombstone<T>(this IEnumerable<T> source, int limit, T tombstone)
    {
        if (source == null) throw new ArgumentNullException(nameof(source), "Cannot take elements on null collection");

        using var enumerator = source.GetEnumerator();
        var index = 0;
        while (enumerator.MoveNext())
        {
            if (index < limit)
                yield return enumerator.Current;
            else
                yield return tombstone;

            index++;
        }
    }

    public static IEnumerable<(int, int)> ZipSkipTombstone(
        this IEnumerable<int> collection1,
        IEnumerable<int> collection2,
        int collection2Tombstone)
    {
        if (collection1 == null)
            throw new ArgumentNullException(nameof(collection1), "Cannot perform Zip with null collection 1.");

        if (collection2 == null)
            throw new ArgumentNullException(nameof(collection2), "Cannot perform Zip with null collection 2.");

        using var enum1 = collection1.GetEnumerator();
        using var enum2 = collection2.GetEnumerator();
        while (enum1.MoveNext() && enum2.MoveNext())
            if (collection2Tombstone != enum2.Current)
                yield return (enum1.Current, enum2.Current);
    }
}
