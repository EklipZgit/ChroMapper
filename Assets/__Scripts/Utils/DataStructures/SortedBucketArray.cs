using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SortedBucketArray<T> : ICollection<T>
{
    private int count;
    public int Count => count;
    public bool IsReadOnly => false;

    public readonly List<List<T>> Buckets = new();
    private readonly int size;
    private readonly Func<T, float> getKeyValue;

    public SortedBucketArray(Func<T, float> keyValueFn, int size, int max)
    {
        getKeyValue = keyValueFn;
        this.size = size;
        Resize(max);
    }

    public void Resize(int max)
    {
        Buckets.Clear();
        for (var i = 0; i < (max / size) + 1; i++) Buckets.Add(new List<T>());
    }

    private int GetBucketIndex(float value) =>
        Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp(value, int.MinValue, int.MaxValue) / size), 0, Buckets.Count - 1);

    public List<T> GetBucketFrom(T item) => Buckets[GetBucketIndex(getKeyValue(item))];
    public List<T> GetBucketFrom(float value) => Buckets[GetBucketIndex(value)];

    public IEnumerator<T> GetEnumerator()
    {
        var bucketIdx = 0;
        while (bucketIdx < Buckets.Count)
        {
            var chunk = Buckets[bucketIdx];
            var idx = 0;
            while (idx < chunk.Count)
            {
                yield return chunk[idx];
                idx++;
            }

            bucketIdx++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> EnumerateFrom(T item)
    {
        var bucket = GetBucketFrom(item);
        var bucketIdx = Buckets.IndexOf(bucket);
        var idx = bucket.IndexOf(item) + 1;
        while (bucketIdx < Buckets.Count)
        {
            bucket = Buckets[bucketIdx];
            while (idx < bucket.Count)
            {
                yield return bucket[idx];
                idx++;
            }

            idx = 0;
            bucketIdx++;
        }
    }

    public IEnumerator<T> EnumerateFrom(float value)
    {
        var bucket = GetBucketFrom(value);
        var idx = bucket.FindIndex(x => Mathf.Approximately(getKeyValue(x), value));
        var bucketIdx = Buckets.IndexOf(bucket);

        while (bucketIdx >= 0)
        {
            bucket = Buckets[bucketIdx];

            while (idx >= 0)
            {
                if (getKeyValue(bucket[idx]) < value) break;
                idx--;
            }

            if (idx != -1 && getKeyValue(bucket[idx]) < value)
            {
                idx++;
                break;
            }

            bucketIdx--;
            if (bucketIdx != -1) idx = Buckets[bucketIdx].Count - 1;
        }

        if (bucketIdx == -1) yield break;
        while (bucketIdx < Buckets.Count)
        {
            bucket = Buckets[bucketIdx];
            while (idx < bucket.Count)
            {
                yield return bucket[idx];
                idx++;
            }

            idx = 0;
            bucketIdx++;
        }
    }

    public void Add(T item)
    {
        var bucket = GetBucketFrom(item);
        bucket.Insert(bucket.FindLastIndex(x => getKeyValue(x) <= getKeyValue(item)) + 1, item);
        count++;
    }

    public bool Remove(T item)
    {
        var bucket = GetBucketFrom(item);
        if (!bucket.Remove(item)) return false;
        count--;
        return true;
    }

    public void CopyTo(T[] array, int arrayIdx)
    {
        foreach (var item in this) array[arrayIdx++] = item;
    }

    public void Clear()
    {
        for (var i = 0; i < Buckets.Count; i++) Buckets[i].Clear();
        count = 0;
    }

    public bool Contains(T item)
    {
        var bucket = GetBucketFrom(item);
        return bucket.Contains(item);
    }

    public int IndexOf(T item)
    {
        var bucket = GetBucketFrom(item);
        var idx = bucket.IndexOf(item);
        for (var i = 0; i < Buckets.Count && Buckets[i] != bucket; i++) idx += Buckets[i].Count;
        return idx;
    }
}
