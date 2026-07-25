using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class ChainManager : BeatmapObjectManager<BaseChain>
{
    protected override void Awake()
    {
        base.Awake();
        LoadInitialMap.OnLevelLoaded += Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += Refresh;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        LoadInitialMap.OnLevelLoaded -= Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= Refresh;
    }

    public override void Refresh() { }
    public override void UpdateTime() { }
    public override void UpdateTime(bool isPlaying, float beatTime) { }

    protected override bool AddData(IEnumerable<BaseChain> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            SetChainHead(d, true);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseChain reference, BaseChain original)> data)
    {
        var mark = false;
        foreach (var (d, _) in data)
        {
            SetChainHead(d, false);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseChain> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            SetChainHead(d, false);
            mark = true;
        }

        return mark;
    }

    /// <summary>
    /// Default implementation of UpdateData for chains.
    /// Chains don't have time-based caching like GLS groups, so this uses the
    /// RemoveData/AddData pattern which is sufficient for chain updates.
    /// </summary>
    protected override bool UpdateData(IEnumerable<(BaseChain reference, BaseChain original)> data)
    {
        var b = RemoveData(data);
        return AddData(data.Select(d => d.Item1)) || b;
    }

    private static void SetChainHead(BaseChain chain, bool active)
    {
        var collection =
            BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
        var notes = collection.GetBetween(
            chain.JsonTime - 0.1f,
            chain.JsonTime + 0.1f);
        foreach (var note in notes)
        {
            if (note.ObjectType != ObjectType.Note) continue;
            if (!IsHeadNote(note)) continue;
            if (active)
                note.Chains.Add(chain);
            else
                note.Chains.Remove(chain);
            if (collection.LoadedContainers.TryGetValue(note, out var container))
                (container as NoteContainer).HandleModelChanged();
        }

        return;

        bool IsHeadNote(BaseNote note)
        {
            var nPos = note.GetPosition();
            var cPos = chain.GetPosition();
            return Mathf.Abs(note.JsonTime - chain.JsonTime) < BeatmapObjectContainerCollection.Epsilon
                && Vector2.Distance(nPos, cPos) < 0.1f
                && note.Type == chain.Color;
        }
    }
}
