using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class StateChunksContainer<TState, TData> where TState : StateData<TData> where TData : BaseObject
{
    public readonly SortedBucketArray<TState> Collection = new(value => value.StartTime, 10, 100);
    public TState CurrentState;

    private List<TState> currBucket;
    private int currBucketIdx;
    private int currLocalIdx;

    public void Resize(float max) => Collection.Resize((int)max);

    public void AddState(TState state) => Collection.Add(state);
    public bool RemoveState(TState state) => Collection.Remove(state);

    public bool IsCurrentOrFindState(float time, bool playing) =>
        playing ? UseCurrentOrNextState(time) : UseCurrentOrFindState(time);

    private bool UseCurrentOrNextState(float time)
    {
        if (time < CurrentState.EndTime) return true;
        SetNextState(time);
        return false;
    }

    private void SetNextState(float time)
    {
        while (currBucketIdx < Collection.Buckets.Count)
        {
            currBucket = Collection.Buckets[currBucketIdx];
            while (currLocalIdx < currBucket.Count)
            {
                CurrentState = currBucket[currLocalIdx];
                if (CurrentState.IsWithinRange(time)) return;
                currLocalIdx++;
            }

            currLocalIdx = 0;
            currBucketIdx++;
        }
    }

    private bool UseCurrentOrFindState(float time)
    {
        if (CurrentState.IsWithinRange(time)) return true;
        SetStateAt(time);
        return false;
    }

    public void SetStateAt(float time)
    {
        var (bucketIdx, localIdx, state) = GetStateAt(time);
        currBucket = Collection.Buckets[bucketIdx];
        currBucketIdx = bucketIdx;
        currLocalIdx = localIdx;
        CurrentState = state;
    }

    public (int chunkIdx, int localIdx, TState state) GetStateAt(float time)
    {
        var bucket = Collection.GetBucketFrom(time);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = BinarySearch(bucket, time);

        if (idx == -1)
        {
            while (bucketIdx > 0)
            {
                bucket = Collection.Buckets[--bucketIdx];
                idx = BinarySearch(bucket, time);
                if (idx != -1) break;
            }
        }

        return (bucketIdx, idx, bucket[idx]);
    }

    public TState GetPreviousStateFrom(TState state)
    {
        var bucket = Collection.GetBucketFrom(state.StartTime);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = BinarySearch(bucket, state.StartTime) - 1;

        if (idx < 0)
        {
            while (bucketIdx > 0)
            {
                bucket = Collection.Buckets[--bucketIdx];
                if (bucket.Count != 0) break;
            }

            idx = bucket.Count - 1;
        }

        return bucket[idx];
    }

    public TState GetOverlappingStateFrom(TState state)
    {
        var bucket = Collection.GetBucketFrom(state.StartTime);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = BinarySearch(bucket, state.StartTime);

        if (idx < 0)
        {
            while (bucketIdx > 0)
            {
                bucket = Collection.Buckets[--bucketIdx];
                if (bucket.Count != 0) break;
            }

            idx = bucket.Count - 1;
        }

        return bucket[idx];
    }

    public TState GetNextStateFrom(TState state)
    {
        var bucket = Collection.GetBucketFrom(state.StartTime);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = BinarySearch(bucket, state.StartTime) + 1;

        if (idx == -1 || idx == bucket.Count)
        {
            while (++bucketIdx < Collection.Buckets.Count)
            {
                bucket = Collection.Buckets[bucketIdx];
                if (bucket.Count != 0) break;
            }

            idx = 0;
        }

        return bucket[idx];
    }

    public TState GetStateFrom(TData reference, TData original)
    {
        var chunk = Collection.GetBucketFrom(original.SongBpmTime);
        var idx = chunk.FindIndex(x => x.Base == reference);

        return chunk[idx];
    }

    private static int BinarySearch(List<TState> bucket, float time)
    {
        var right = bucket.Count - 1;
        var left = 0;

        while (left <= right)
        {
            var mid = (left + right) / 2;
            if (bucket[mid].IsWithinRange(time)) return mid; // we want the rightmost
            if (bucket[mid].StartTime <= time)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return -1;
    }
}
