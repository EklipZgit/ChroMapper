using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Base;

// Group authoritative note data once so stopped seeks only visit older notes on tracks holding their lifetime.
internal sealed class FrozenNoteTrackIndex
{
    private readonly Dictionary<(string[] TrackNames, bool IsFake), List<BaseNote>> groups = new();

    internal void Clear() => groups.Clear();

    internal void Reset(IReadOnlyList<BaseNote> notes, ObjectPathAnimationRegistry pathAnimations)
    {
        groups.Clear();
        foreach (var note in notes)
        {
            var names = pathAnimations.GetTrackNames(note);
            if (names.Length == 0)
                continue;

            var key = (names, note.CustomFake);
            if (!groups.TryGetValue(key, out var group))
            {
                group = new List<BaseNote>();
                groups.Add(key, group);
            }

            // The source collection is sorted by hit beat, so each group retains that ordering.
            group.Add(note);
        }
    }

    internal void GetNotesBefore(
        float hitBeat,
        float jsonTime,
        TracksManager tracksManager,
        HashSet<BaseNote> notes)
    {
        foreach (var (key, group) in groups)
        {
            if (!tracksManager.TryGetTrackTime(key.TrackNames, jsonTime, out var normalTime))
                continue;

            // Real notes leave at the hit. Fake notes can remain through the second half of their jump.
            var end = key.IsFake ? 1f : 0.5f;
            if (normalTime >= 0f && normalTime < end)
            {
                var olderNotes = group.AsSpan();
                var count = olderNotes.LowerBoundBy(hitBeat, static note => note.SongBpmTime);
                for (var index = 0; index < count; index++)
                    notes.Add(olderNotes[index]);
            }
        }
    }
}
