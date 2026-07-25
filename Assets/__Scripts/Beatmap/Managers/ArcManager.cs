using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class ArcManager : BeatmapObjectManager<BaseArc>
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

    protected override bool AddData(IEnumerable<BaseArc> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            UpdateArcData(d);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseArc reference, BaseArc original)> data) => false;

    protected override bool RemoveData(IEnumerable<BaseArc> data) => false;

    /// <summary>
    /// Default implementation of UpdateData for arcs.
    /// Arcs don't have time-based caching like GLS groups, so this uses the
    /// RemoveData/AddData pattern which is sufficient for arc updates.
    /// </summary>
    protected override bool UpdateData(IEnumerable<(BaseArc reference, BaseArc original)> data)
    {
        var b = RemoveData(data);
        return AddData(data.Select(d => d.Item1)) || b;
    }

    private static void UpdateArcData(BaseArc arc)
    {
        var collection =
            BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);

        arc.HeadNotes.Clear();
        arc.TailNotes.Clear();

        foreach (var note in collection.GetBetween(
            arc.JsonTime - 0.1f,
            arc.JsonTime + 0.1f))
        {
            if (arc.Color != note.Color
                || !(Mathf.Abs(note.SongBpmTime - arc.SongBpmTime) < BeatmapObjectContainerCollection.Epsilon)
                || !(Vector2.Distance(arc.GetPosition(), note.GetPosition()) < 0.1f))
                continue;
            arc.HeadNotes.Add(note);
            break;
        }

        foreach (var note in collection.GetBetween(
            arc.TailJsonTime - 0.1f,
            arc.TailJsonTime + 0.1f))
        {
            if (arc.Color != note.Color
                || !(Mathf.Abs(note.SongBpmTime - arc.TailSongBpmTime)
                    < BeatmapObjectContainerCollection.Epsilon)
                || !(Vector2.Distance(arc.GetTailPosition(), note.GetPosition()) < 0.1f))
                continue;
            arc.TailNotes.Add(note);
            break;
        }
    }
}
