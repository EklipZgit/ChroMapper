using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class NoteManager : BeatmapObjectManager<BaseNote>
{
    private NoteGridContainer collection;

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

    public override void Refresh()
    {
        collection = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
        foreach (var note in collection.MapObjects)
        {
            note.Chains.Clear();
            ConnectToChain(note);
            ConnectToArc(note, true);
            if (collection.LoadedContainers.TryGetValue(note, out var container))
                (container as NoteContainer).HandleModelChanged();
        }
    }

    public override void UpdateTime() { }
    public override void UpdateTime(bool isPlaying, float beatTime) { }

    protected override bool AddData(IEnumerable<BaseNote> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            ConnectToChain(d);
            ConnectToArc(d, true);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseNote reference, BaseNote original)> data)
    {
        var mark = false;
        foreach (var (d, _) in data)
        {
            ConnectToArc(d, false);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseNote> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            ConnectToArc(d, false);
            mark = true;
        }

        return mark;
    }

    /// <summary>
    /// Default implementation of UpdateData for notes.
    /// Notes don't have time-based caching like GLS groups, so this uses the
    /// RemoveData/AddData pattern which is sufficient for note updates.
    /// </summary>
    protected override bool UpdateData(IEnumerable<(BaseNote reference, BaseNote original)> data)
    {
        var b = RemoveData(data);
        return AddData(data.Select(d => d.Item1)) || b;
    }

    public void ConnectToChain(BaseNote note)
    {
        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        var chains = chainCollection.GetBetween(
            note.JsonTime - 0.1f,
            note.JsonTime + 0.1f);
        foreach (var chain in chains)
        {
            if (!IsHeadNote(chain)) continue;
            note.Chains.Add(chain);
        }

        if (collection.LoadedContainers.TryGetValue(note, out var container))
            (container as NoteContainer).HandleModelChanged();

        return;

        bool IsHeadNote(BaseChain chain)
        {
            var nPos = note.GetPosition();
            var cPos = chain.GetPosition();
            return Mathf.Abs(note.JsonTime - chain.JsonTime) < BeatmapObjectContainerCollection.Epsilon
                && Vector2.Distance(nPos, cPos) < 0.1f
                && note.Type == chain.Color;
        }
    }

    public static void ConnectToArc(BaseNote note, bool active)
    {
        var arcCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);

        foreach (var arc in arcCollection.GetBetween(
            note.JsonTime - 0.1f,
            note.JsonTime + 0.1f))
        {
            if (!IsHeadNote(arc)) continue;
            if (active)
                arc.HeadNotes.Add(note);
            else
                arc.HeadNotes.Remove(note);
        }

        foreach (var arc in arcCollection.GetBetweenTail(
            note.JsonTime - 0.1f,
            note.JsonTime + 0.1f))
        {
            if (!IsTailNote(arc)) continue;
            if (active)
                arc.TailNotes.Add(note);
            else
                arc.TailNotes.Remove(note);
        }

        return;

        bool IsHeadNote(BaseArc arc)
        {
            var nPos = note.GetPosition();
            var cPos = arc.GetPosition();
            return Mathf.Abs(note.JsonTime - arc.JsonTime) < BeatmapObjectContainerCollection.Epsilon
                && Vector2.Distance(nPos, cPos) < 0.1f
                && note.Type == arc.Color;
        }

        bool IsTailNote(BaseArc arc)
        {
            var nPos = note.GetPosition();
            var cPos = arc.GetTailPosition();
            return Mathf.Abs(note.JsonTime - arc.TailJsonTime) < BeatmapObjectContainerCollection.Epsilon
                && Vector2.Distance(nPos, cPos) < 0.1f
                && note.Type == arc.Color;
        }
    }
}
