using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class StateChunksContainer<TData, TBase> : IEnumerable<TData>
    where TData : StateData<TBase> where TBase : BaseObject
{
    public TData CurrentState;
    public readonly List<List<TData>> Chunks = new();

    private int currentChunkIndex;
    private int currentLocalIndex;
    private const float chunkByBeatTime = 10f;

    public void GenerateChunk(AudioTimeSyncController atsc)
    {
        Chunks.Clear();
        for (var secondTime = 0f;
            secondTime < atsc.SongAudioSource.clip.length;
            secondTime += atsc.GetSecondsFromBeat(chunkByBeatTime))
            Chunks.Add(new List<TData>());
    }

    public (int index, List<TData> chunk) GetChunk(float beatTime)
    {
        var index = Mathf.Clamp(Mathf.FloorToInt(beatTime / chunkByBeatTime), 0, Chunks.Count - 1);
        return (index, Chunks[index]);
    }

    public bool IsCurrentOrFindState(float time, bool playing) =>
        playing ? UseCurrentOrNextState(time) : UseCurrentOrFindState(time);

    public (int chunkIndex, int localIndex, TData state) GetStateAt(float time)
    {
        var (chunkIdx, chunk) = GetChunk(time);
        var idx = BinarySearch(chunk, time);

        if (idx == -1)
        {
            chunkIdx--;
            while (chunkIdx >= 0)
            {
                chunk = Chunks[chunkIdx];
                idx = BinarySearch(chunk, time);

                if (idx != -1) break;
                chunkIdx--;
            }
        }

        return (chunkIdx, idx, chunk[idx]);
    }

    public (List<TData> chunk, int index, TData state) GetPreviousStateFrom(TData state)
    {
        var (chunkIdx, chunk) = GetChunk(state.StartTime);
        var idx = BinarySearch(chunk, state.StartTime) - 1;

        if (idx < 0)
        {
            while (--chunkIdx >= 0)
            {
                chunk = Chunks[chunkIdx];
                if (Chunks[chunkIdx].Count != 0) break;
            }

            idx = chunk.Count - 1;
        }

        return (chunk, idx, chunk[idx]);
    }

    public (List<TData> chunk, int index, TData state) GetOverlappingStateFrom(TData state)
    {
        var (chunkIdx, chunk) = GetChunk(state.StartTime);
        var idx = BinarySearch(chunk, state.StartTime);

        if (idx < 0)
        {
            while (--chunkIdx >= 0)
            {
                chunk = Chunks[chunkIdx];
                if (Chunks[chunkIdx].Count != 0) break;
            }

            idx = chunk.Count - 1;
        }

        return (chunk, idx, chunk[idx]);
    }

    public (List<TData> chunk, int index, TData state) GetNextStateFrom(TData state)
    {
        var (chunkIdx, chunk) = GetChunk(state.StartTime);
        var idx = BinarySearch(chunk, state.StartTime) + 1;

        if (idx == -1 || idx == chunk.Count)
        {
            while (++chunkIdx < Chunks.Count)
            {
                chunk = Chunks[chunkIdx];
                if (Chunks[chunkIdx].Count != 0) break;
            }

            idx = 0;
        }

        return (chunk, idx, chunk[idx]);
    }

    private bool UseCurrentOrNextState(float time)
    {
        if (time < CurrentState.EndTime) return true;
        SetNextState(time);
        return false;
    }

    private void SetNextState(float time)
    {
        while (currentChunkIndex < Chunks.Count)
        {
            var chunk = Chunks[currentChunkIndex];
            while (currentLocalIndex < chunk.Count)
            {
                CurrentState = chunk[currentLocalIndex];
                if (CurrentState.IsWithinRange(time)) return;
                currentLocalIndex++;
            }

            currentLocalIndex = 0;
            currentChunkIndex++;
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
        var (chunkIndex, localIndex, state) = GetStateAt(time);
        currentChunkIndex = chunkIndex;
        currentLocalIndex = localIndex;
        CurrentState = state;
    }

    public (int chunkIndex, int localIndex, TData state) GetStateFrom(BaseObject evt) => GetStateAt(evt.SongBpmTime);

    public int GetStateIndex(TData state)
    {
        var (chunkIdx, chunk) = GetChunk(state.StartTime);
        var idx = chunk.IndexOf(state);
        for (var i = 0; i < chunkIdx; i++) idx += Chunks[i].Count;
        return idx;
    }

    private static int BinarySearch(List<TData> chunk, float time)
    {
        var right = chunk.Count - 1;
        var left = 0;

        while (left <= right)
        {
            var mid = (left + right) / 2;
            if (chunk[mid].IsWithinRange(time)) return mid;
            if (chunk[mid].StartTime <= time)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return -1;
    }

    public IEnumerator<TData> GetEnumerator()
    {
        var chunkIdx = 0;
        while (chunkIdx < Chunks.Count)
        {
            var chunk = Chunks[chunkIdx];
            var index = 0;
            while (index < chunk.Count)
            {
                yield return chunk[index];
                index++;
            }

            chunkIdx++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<TData> EnumerateFrom(TData state)
    {
        var (chunkIdx, chunk) = GetChunk(state.StartTime);
        var localIdx = chunk.IndexOf(state) + 1;
        while (chunkIdx < Chunks.Count)
        {
            chunk = Chunks[chunkIdx];
            while (localIdx < chunk.Count)
            {
                yield return chunk[localIdx];
                localIdx++;
            }

            localIdx = 0;
            chunkIdx++;
        }
    }

    public IEnumerator<TData> EnumerateFrom(float time)
    {
        var (chunkIdx, localIdx, _) = GetStateAt(time);

        // we want very first of the state of same time
        while (localIdx > 0 && Mathf.Approximately(Chunks[chunkIdx][localIdx - 1].StartTime, time)) localIdx--;

        while (chunkIdx < Chunks.Count)
        {
            var chunk = Chunks[chunkIdx];
            while (localIdx < chunk.Count)
            {
                yield return chunk[localIdx];
                localIdx++;
            }

            localIdx = 0;
            chunkIdx++;
        }
    }
}
