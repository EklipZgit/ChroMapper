using System;
using System.Collections.Generic;
using UnityEngine;

using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    public interface IAnimateProperty
    {
        public float StartTime { get; }
        public bool IsEmpty();
        public bool UpdateProperty(float time);
        public bool ResetEvaluatedValue();
        public void Sort();
        public void RemoveEvent(BaseCustomEvent ev);
    }

    public class AnimateProperty<T> : IAnimateProperty
        where T : struct
    {
        public List<PointDefinition<T>> PointDefinitions;
        public Action<T> Setter;
        public T Default;

        public float StartTime { get; private set; } = Mathf.Infinity;
        private PointDefinition<T>[] evaluated = Array.Empty<PointDefinition<T>>();
        private int count;
        private bool hasEvaluatedValue;
        private T evaluatedValue;

        public AnimateProperty(List<PointDefinition<T>> points, Action<T> setter, T _default)
        {
            PointDefinitions = points;
            Setter = setter;
            Default = _default;
            count = 0;
        }

        public bool IsEmpty()
        {
            return PointDefinitions.Count == 0;
        }

        public static bool SkipsMissingPointDefinition(IPointDefinition.UntypedParams p)
        {
            if (p.Points is not JSONString named) return false;
            if (BeatSaberSongContainer.Instance.Map.PointDefinitions.ContainsKey(named.Value)) return false;

            Debug.LogError($"Could not find point definition [{named.Value}]");
            return true;
        }

        public void AddPointDef(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, BaseCustomEvent source)
        {
            // The Spells/Kamikazi fixtures author repeat=69420 as an "animate forever" idiom: expanding one
            // PointDefinition per repeat allocated ~70k objects per property (~30s and multiple GB per heavy
            // map load — LoadAll measured 228ms per event). A repeat is the same window shifted by
            // i*Duration, and evaluation can only query times up to the clamped song end, so repeats whose
            // window starts past the horizon are never selected; Heck's coroutine repeat
            // (CoroutineEventManager.AnimateTrackCoroutine) likewise stops at song end.
            var repeat = p.Repeat;
            if (p.Duration <= 0)
            {
                // Zero-duration repeats tile the identical window, and the game treats a zero-duration event
                // as an instant set (repeat never runs), so a single definition is enough.
                repeat = 0;
            }
            else if (TryGetSongEndJsonTime(out var songEnd))
            {
                var reachable = (songEnd - p.TimeBegin) / p.Duration;
                if (reachable < repeat)
                {
                    repeat = Mathf.Max((int)reachable, 0);
                }
            }

            for (var i = 0; i <= repeat; ++i)
            {
                var pp = p;
                pp.TimeBegin = p.TimeBegin + (i * p.Duration);
                pp.TimeEnd = p.TimeEnd + (i * p.Duration);
                if (i > 0)
                {
                    pp.Time = pp.TimeBegin;
                }

                PointDefinitions.Add(new PointDefinition<T>(parser, pp, source));
            }
        }

        // The playback clock clamps to the loaded song (AudioTimeSyncController), so the furthest reachable
        // authored beat is the song end converted through the map's BPM-event-aware timing.
        private static bool TryGetSongEndJsonTime(out float jsonTime)
        {
            jsonTime = 0f;
            var songContainer = BeatSaberSongContainer.Instance;
            if (songContainer == null
                || songContainer.LoadedSong == null
                || songContainer.Info == null
                || songContainer.Map == null)
            {
                return false;
            }

            var songBpmTime = songContainer.LoadedSong.length * (songContainer.Info.BeatsPerMinute / 60f);
            jsonTime = songContainer.Map.SongBpmTimeToJsonTime(songBpmTime) ?? songBpmTime;
            return true;
        }

        public T GetLerpedValue(float time)
        {
            GetIndexes(time, out var current, out var _);

            if (evaluated[current].StartTime > time) {
                return Default;
            }

            var cpd = evaluated[current];

            // AnimateTrack
            if (cpd.StartTime < time && time < (cpd.StartTime + cpd.Duration))
            {
                var elapsedTime = time - cpd.StartTime;
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / cpd.Duration, 1));
                float learpedTime = cpd.StartTime + (normalizedTime * cpd.Duration);
                return cpd.Interpolate(learpedTime);
            }

            // AssignPathAnimation
            // Only one active definition, no interpolate
            if (time > (cpd.StartTime + cpd.Transition)) {
                return cpd.Interpolate(time);
            }
            else
            {
                var elapsedTime = time - cpd.StartTime;
                // Tested by the WorldCavesInEnvironmentTests. Heck has some nuanced behavior here, this logic is necessary.
                // Heck's Init makes the new definition the base and blends from the previous one
                // over the event's duration, so at progress 0 the previous value wins; a zero-duration event
                // finishes instantly and returns its own value. AnimateTrack carries no Transition, so fall
                // back to Duration (elapsed/0 stays NaN->1, an instant switch, matching Heck's Finish).
                var transitionDuration = current == 0
                    ? cpd.Transition
                    : (cpd.Transition > 0 ? cpd.Transition : cpd.Duration);
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / transitionDuration, 1));
                return PointDefinitionInterpolation.Lerp<T>(current == 0 ? null : evaluated[current - 1], evaluated[current], normalizedTime, time, Default);
            }
        }

        public bool UpdateProperty(float time)
        {
            var value = GetLerpedValue(time);
            var changed = !hasEvaluatedValue || !EqualityComparer<T>.Default.Equals(evaluatedValue, value);
            Setter(value);
            evaluatedValue = value;
            hasEvaluatedValue = true;
            return changed;
        }

        public bool ResetEvaluatedValue()
        {
            var changed = hasEvaluatedValue;
            hasEvaluatedValue = false;
            return changed;
        }

        public void Sort()
        {
            // STABLE SORT :upsidedownface:
            var indexed = new (PointDefinition<T> definition, int originalIndex)[PointDefinitions.Count];
            for (var i = 0; i < indexed.Length; i++)
            {
                indexed[i] = (PointDefinitions[i], i);
            }

            Array.Sort(indexed, (a, b) =>
            {
                var byStartTime = a.definition.StartTime.CompareTo(b.definition.StartTime);
                return byStartTime != 0 ? byStartTime : a.originalIndex.CompareTo(b.originalIndex);
            });
            for (var i = 0; i < indexed.Length; i++)
            {
                PointDefinitions[i] = indexed[i].definition;
            }

            evaluated = BuildEvaluatedDefinitions();
            StartTime = evaluated[0].StartTime;
            count = evaluated.Length;
        }

        // Heck's CoroutineEventManager stops the running coroutine when the next event on the same
        // property starts (StopCoroutine(property.Coroutine)), so a repeat expansion must not outlive
        // the next distinct source event on this property — including a later-file event at the same
        // beat, which fires after the earlier one and kills its coroutine. PointDefinitions keeps every
        // expanded repeat so RemoveEvent plus a re-Sort can restore them; GetLerpedValue/GetIndexes
        // binary-search this filtered array instead.
        private PointDefinition<T>[] BuildEvaluatedDefinitions()
        {
            // The stable sort puts every source's original definition first and its repeat clones after,
            // so each source's first appearance is its original event and starts the sorted-by-start list
            // of original events in authored order.
            var sourceOriginalIndex = new Dictionary<object, int>(PointDefinitions.Count);
            var originalStarts = new List<float>(PointDefinitions.Count);
            var hasRepeats = false;
            for (var i = 0; i < PointDefinitions.Count; i++)
            {
                var key = (object)PointDefinitions[i].Source ?? PointDefinitions[i];
                if (sourceOriginalIndex.ContainsKey(key))
                {
                    hasRepeats = true;
                    continue;
                }

                sourceOriginalIndex[key] = originalStarts.Count;
                originalStarts.Add(PointDefinitions[i].StartTime);
            }

            if (!hasRepeats)
                return PointDefinitions.ToArray();

            var kept = new List<PointDefinition<T>>(PointDefinitions.Count);
            var yielded = new HashSet<object>(sourceOriginalIndex.Count);
            for (var i = 0; i < PointDefinitions.Count; i++)
            {
                var pd = PointDefinitions[i];
                var key = (object)pd.Source ?? pd;
                if (yielded.Add(key))
                {
                    kept.Add(pd);
                    continue;
                }

                // The next distinct source event is the next entry in the original-starts list;
                // this repeat is dead once its own window begins at or after that event's start.
                var next = sourceOriginalIndex[key] + 1;
                var boundary = next < originalStarts.Count ? originalStarts[next] : float.PositiveInfinity;
                if (pd.StartTime < boundary)
                    kept.Add(pd);
            }

            return kept.ToArray();
        }

        public void RemoveEvent(BaseCustomEvent ev)
        {
            PointDefinitions.RemoveAll((pd) => pd.Source == ev);
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            prev = 0;
            next = count;

            while (prev < next - 1)
            {
                int m = (prev + next) / 2;
                float pointTime = evaluated[m].StartTime;

                if (pointTime <= time)
                {
                    prev = m;
                }
                else
                {
                    next = m;
                }
            }
        }
    }
}
