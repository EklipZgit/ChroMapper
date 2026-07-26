using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class StateChunksContainer<TState, TData> where TState : StateData<TData> where TData : BaseObject
{
    public readonly SortedBucketArray<TState> Collection = new(value => value?.StartTime ?? 0f, 10, 100);
    public TState CurrentState;

    private List<TState> currBucket;
    private int currBucketIdx;
    private int currLocalIdx;

    public void Resize(float max) => Collection.Resize((int)max);

    public void AddState(TState state) => Collection.Add(state);

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
        var idx = Collection.BinarySearchRight(bucket, time);

        if (idx == -1)
        {
            while (bucketIdx > 0)
            {
                bucket = Collection.Buckets[--bucketIdx];
                idx = Collection.BinarySearchRight(bucket, time);
                if (idx != -1) break;
            }
        }

        return (bucketIdx, idx, bucket[idx]);
    }

    public TState GetPreviousStateFrom(TState state)
    {
        var bucket = Collection.GetBucketFrom(state.StartTime);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = Collection.BinarySearchRight(bucket, state.StartTime) - 1;

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
        var idx = Collection.BinarySearchRight(bucket, state.StartTime);

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
        var idx = Collection.BinarySearchRight(bucket, state.StartTime) + 1;

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

    /// <summary>
    /// Gets a state from the container by reference.
    /// The reference must be the exact live instance originally inserted into the state bucket.
    /// </summary>
    public TState GetStateFrom(TData reference, TData original)
    {
        // Use the outgoing object's original time so replacements never scan the entire beatmap state cache.
        var chunk = Collection.GetBucketFrom(original.SongBpmTime);
        var idx = chunk.FindIndex(x => x.Base == reference);
        if (idx >= 0)
            return chunk[idx];

        return null;
    }

    // StateManager resolves the exact cached state before requesting its bucket removal.
    public bool RemoveState(TState state) => Collection.Remove(state);
}
