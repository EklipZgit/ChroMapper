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
    /// IMPORTANT: This method is used when removing or updating states.
    /// When an object's time changes (e.g., when an event group is moved), the state may be in a different
    /// bucket than expected based on the original time. The fallback linear search ensures we find the state
    /// even when it's in the wrong bucket due to time changes.
    /// 
    /// CRITICAL: Any code that modifies an object's time (JsonTime) must ensure the corresponding state
    /// is properly removed and re-inserted via the StateManager's RemoveData/InsertData mechanism,
    /// otherwise the state will remain in the wrong bucket and cause rendering issues.
    /// 
    /// NOTE: The SortedBucketArray uses bucket indices based on StartTime, so when an object's time changes,
    /// the state must be removed from the old bucket and re-inserted into the new bucket.
    /// </summary>
    public TState GetStateFrom(TData reference, TData original)
    {
        // First try to find in the bucket based on the original time
        var chunk = Collection.GetBucketFrom(original.SongBpmTime);
        var idx = chunk.FindIndex(x => x.Base == reference);
        if (idx >= 0)
            return chunk[idx];

        // Fallback: linear search through all buckets
        // This handles cases where the object's time has changed and the state is in a different bucket
        foreach (var state in Collection)
        {
            if (state.Base == reference)
            {
                Debug.LogWarning($"Found state in wrong bucket: {state}");
                return state;
            }
        }

        Debug.LogError($"Failed to find state at all for {reference}, original {original}");
        return null;
    }

    /// <summary>
    /// Removes a state from the container.
    /// IMPORTANT: When removing a state whose time has changed, the state may be in a different bucket
    /// than expected. The Remove method uses the item's current StartTime to find the bucket.
    /// 
    /// CRITICAL: States must be removed using GetStateFrom (which handles wrong buckets) before
    /// calling this method directly.
    /// </summary>
    public bool RemoveState(TState state) => Collection.Remove(state);

    /// <summary>
    /// Updates a state's StartTime when the underlying object's time changes.
    /// IMPORTANT: This method must be called when an object's JsonTime changes to ensure the state
    /// is in the correct bucket for time-based lookups.
    /// 
    /// CRITICAL: This method removes the state from its current bucket and re-inserts it into the
    /// correct bucket based on the new time. This is necessary because SortedBucketArray uses
    /// bucket indices based on StartTime for performance.
    /// 
    /// NOTE: This is a performance-critical operation. Only call this when the object's time actually changes.
    /// </summary>
    public void UpdateStateTime(TState state, float newStartTime)
    {
        if (state.StartTime == newStartTime)
            return; // No change needed

        Collection.Remove(state);
        state.StartTime = newStartTime;
        Collection.Add(state);
    }
}
