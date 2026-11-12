using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Helper;

// Group authoritative note data once so stopped seeks only visit older notes on tracks holding their lifetime.
internal sealed class FrozenNoteTrackIndex
{
    private sealed class NoteGroup
    {
        internal string[] TrackNames;
        internal bool IsFake;
        internal readonly List<BaseNote> Notes = new();
    }

    private readonly Dictionary<(string TrackNamesKey, bool IsFake), NoteGroup> groups = new();

    internal void Clear() => groups.Clear();

    internal void Reset(IReadOnlyList<BaseNote> notes)
    {
        groups.Clear();
        foreach (var note in notes)
        {
            if (note.CustomTrack == null)
                continue;

            var groupKey = (note.CustomTrack.ToString(), note.CustomFake);
            if (!groups.TryGetValue(groupKey, out var group))
            {
                group = new NoteGroup
                {
                    TrackNames = TrackDataReader.ReadNames(note.CustomTrack),
                    IsFake = note.CustomFake
                };
                groups.Add(groupKey, group);
            }

            // The source collection is sorted by hit beat, so each group retains that ordering.
            group.Notes.Add(note);
        }
    }

    internal void GetNotesBefore(
        float hitBeat,
        float jsonTime,
        TracksManager tracksManager,
        HashSet<BaseNote> notes)
    {
        foreach (var group in groups.Values)
        {
            foreach (var trackName in group.TrackNames)
            {
                if (!tracksManager.TryGetTrackTime(trackName, jsonTime, out var normalTime))
                    continue;

                // Real notes leave at the hit. Fake notes can remain through the second half of their jump.
                var end = group.IsFake ? 1f : 0.5f;
                if (normalTime >= 0f && normalTime < end)
                {
                    var olderNotes = group.Notes.AsSpan();
                    var count = olderNotes.LowerBoundBy(hitBeat, note => note.SongBpmTime);
                    for (var index = 0; index < count; index++)
                        notes.Add(olderNotes[index]);

                }
                // Noodle uses the first track with a time property, even if it does not hold the note.
                break;
            }
        }
    }
}
