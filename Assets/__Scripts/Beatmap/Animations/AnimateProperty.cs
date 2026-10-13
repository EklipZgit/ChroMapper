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
        public void CompletePreparedOrder();
        public void RemoveEvent(BaseCustomEvent ev);
        public void RefreshBpmTiming();
        public float GetTransformAnchorTime(float time);
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
        private float[] constantTransformWriteTimes = Array.Empty<float>();
        private bool hasEvaluatedValue;
        private T evaluatedValue;
        private readonly List<(PointDefinition<T>.Parser Parser, IPointDefinition.UntypedParams Parameters,
            BaseCustomEvent Source, int Order)> sourceDefinitions = new();
        // Null clears the original request's contribution, including repeats that would otherwise occur later.
        private readonly List<(float Time, BaseCustomEvent Source, int Order)> nullDefinitions = new();
        private (float Time, int FileOrder, int Order, bool HasValue, BaseCustomEvent Source)[] presenceRequests =
            Array.Empty<(float, int, int, bool, BaseCustomEvent)>();
        private readonly HashSet<BaseCustomEvent> pathsAfterNull = new();
        private int nextSourceOrder;
        internal bool UsePathClock;

        internal void AddNullDefinition(float time, BaseCustomEvent source)
        {
            nullDefinitions.Add((time, source, nextSourceOrder++));
        }

        internal bool HasValueAt(float time)
        {
            if (nullDefinitions.Count == 0)
                return time >= StartTime;

            var index = presenceRequests.AsSpan().UpperBoundBy(time, static request => request.Time) - 1;
            return index >= 0 && presenceRequests[index].HasValue;
        }

        private void SortPresenceRequests()
        {
            pathsAfterNull.Clear();
            if (nullDefinitions.Count == 0)
            {
                presenceRequests = Array.Empty<(float, int, int, bool, BaseCustomEvent)>();
                return;
            }
            var requests = new List<(float Time, int FileOrder, int Order, bool HasValue, BaseCustomEvent Source)>(
                sourceDefinitions.Count + nullDefinitions.Count);
            foreach (var definition in sourceDefinitions)
                requests.Add(
                    (definition.Parameters.Time, definition.Source.FileOrder,
                    definition.Order, true, definition.Source));

            foreach (var definition in nullDefinitions)
                requests.Add((definition.Time, definition.Source.FileOrder, definition.Order, false, definition.Source));

            requests.Sort(
                static (left, right) =>
                {
                    var chronology = Beatmap.Comparers.EventOrderComparer.CompareKeys(
                        left.Time,
                        left.FileOrder,
                        right.Time,
                        right.FileOrder);
                    return chronology != 0
                        ? chronology
                        : left.Order.CompareTo(right.Order);
                });
            presenceRequests = requests.ToArray();
            StartTime = Mathf.Min(StartTime, presenceRequests[0].Time);
            for (var i = 1; i < presenceRequests.Length; i++)
            {
                if (presenceRequests[i].HasValue && !presenceRequests[i - 1].HasValue)
                    pathsAfterNull.Add(presenceRequests[i].Source);

            }
        }

        public AnimateProperty(List<PointDefinition<T>> points, Action<T> setter, T _default)
        {
            PointDefinitions = points;
            Setter = setter;
            Default = _default;
            count = 0;
        }

        public bool IsEmpty()
        {
            return PointDefinitions.Count == 0 && nullDefinitions.Count == 0;
        }

        public static bool SkipsMissingPointDefinition(
            IPointDefinition.UntypedParams p,
            BaseCustomEvent source = null)
        {
            if (p.Points is not JSONString named)
                return false;

            if (BeatSaberSongContainer.Instance.Map.PointDefinitions.ContainsKey(named.Value))
                return false;

            Debug.LogError($"Could not find point definition [{named.Value}]. {new AnimationLogContext(p, source).Describe()}");
            return true;
        }

        public void AddPointDef(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, BaseCustomEvent source)
        {
            sourceDefinitions.Add((parser, p, source, nextSourceOrder++));
            AddPointDefinitions(parser, p, source);
        }

        // Prepared indices belong to a shared source timeline. Each animator still owns its parsed points.
        public void AddPreparedPointDef(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p,
            BaseCustomEvent source)
        {
            sourceDefinitions.Add((parser, p, source, nextSourceOrder++));
            var definition = new PointDefinition<T>(parser, p, source);
            PointDefinitions.Add(definition);
            if (p.PreparedIndex < 0)
                return;
            if (evaluated.Length == 0)
                evaluated = new PointDefinition<T>[p.PreparedLength];
            evaluated[p.PreparedIndex] = definition;
        }

        public void CompletePreparedOrder()
        {
            count = 0;
            // Missing point references are skipped while binding. Compact their unused slots without sorting.
            for (var i = 0; i < evaluated.Length; i++)
            {
                if (evaluated[i] != null)
                    evaluated[count++] = evaluated[i];
            }

            StartTime = count > 0
                ? evaluated[0].StartTime
                : float.PositiveInfinity;
            PrepareTransformWriteTimes();
            SortPresenceRequests();
        }

        private void PrepareTransformWriteTimes()
        {
            constantTransformWriteTimes = new float[count];
            for (var i = 0; i < count; i++)
            {
                var definition = evaluated[i];
                constantTransformWriteTimes[i] = definition.StartTime;
                if (i > 0 && definition.IsConstant && evaluated[i - 1].IsConstant
                    && EqualityComparer<T>.Default.Equals(definition.Points[0].Value, evaluated[i - 1].Points[0].Value))
                {
                    constantTransformWriteTimes[i] = constantTransformWriteTimes[i - 1];
                }
            }
        }

        private void AddPointDefinitions(PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p,
            BaseCustomEvent source)
        {
            // Repeats past song end cannot be played. Cap them here to avoid allocating unused definitions.
            var repeat = p.Repeat;
            if (p.Duration <= 0)
            {
                // Events without a duration apply once.
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

        public T GetLerpedValue(float time) => Evaluate(time, time, !UsePathClock);

        // Material paths select the current event but always interpolate its points at normalized time zero.
        public T GetPathValue(float time, float pathTime) => Evaluate(time, pathTime, false);

        private T Evaluate(float time, float pointTime, bool durationClock)
        {
            GetIndexes(time, out var current, out var _);

            if (evaluated[current].StartTime > time)
                return Default;


            var cpd = evaluated[current];

            // A first path, or one assigned after null, has no previous native interpolation base.
            if (!durationClock && (current == 0 || pathsAfterNull.Contains(cpd.Source)))
                return cpd.Interpolate(pointTime);


            // AnimateTrack replaces the previous definition at its start, including constant points with a nonzero duration.
            if (durationClock && cpd.StartTime <= time && time < (cpd.StartTime + cpd.Duration))
            {
                var elapsedTime = time - cpd.StartTime;
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / cpd.Duration, 1));
                float learpedTime = cpd.StartTime + (normalizedTime * cpd.Duration);
                return cpd.Interpolate(learpedTime);
            }

            if (time > (cpd.StartTime + cpd.Transition) || (!durationClock && cpd.Transition == 0f))
                return cpd.Interpolate(pointTime);

            else
            {
                var elapsedTime = time - cpd.StartTime;
                // Blend from the previous definition, or Default for the first event.
                // AnimateTrack has no Transition, so use Duration when a previous definition exists.
                // Zero-duration events switch immediately.
                var transitionDuration = current == 0
                    ? cpd.Transition
                    : (cpd.Transition > 0 ? cpd.Transition : cpd.Duration);
                float normalizedTime = cpd.Easing(Mathf.Min(elapsedTime / transitionDuration, 1));
                return PointDefinitionInterpolation.Lerp<T>(
                    current == 0
                    ? null
                    : evaluated[current - 1],
                    evaluated[current],
                    normalizedTime,
                    pointTime,
                    Default);
            }
        }

        // A completed world-position animation stays in the parent's frame at its final write.
        // Sampling the previous representable beat keeps a parent cut at that endpoint after the write.
        public float GetTransformAnchorTime(float time)
        {
            GetIndexes(time, out var current, out _);
            // Native single-point requests write once, regardless of duration. Identical repeats do not write again.
            if (evaluated[current].IsConstant)
                return constantTransformWriteTimes[current];

            var end = evaluated[current].StartTime + evaluated[current].Duration;
            if (time < end)
            {
                return time;
            }

            if (evaluated[current].Duration == 0f)
            {
                return end;
            }

            if (end == 0f)
            {
                return -float.Epsilon;
            }

            var bits = BitConverter.SingleToInt32Bits(end);
            return BitConverter.Int32BitsToSingle(end > 0f ? bits - 1 : bits + 1);
        }

        public bool UpdateProperty(float time)
        {
            if (nullDefinitions.Count > 0 && !HasValueAt(time))
                return ResetEvaluatedValue();

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
            // Stable sort :upsidedownface: turns out that's important lmoa
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

            evaluated = AnimationDefinitionOrdering.FilterRepeatedSources(
                PointDefinitions,
                static definition => definition.StartTime,
                static definition => definition.Source);
            count = evaluated.Length;
            StartTime = count > 0
                ? evaluated[0].StartTime
                : float.PositiveInfinity;
            PrepareTransformWriteTimes();
            SortPresenceRequests();
        }

        public void RemoveEvent(BaseCustomEvent ev)
        {
            sourceDefinitions.RemoveAll(definition => definition.Source == ev);
            nullDefinitions.RemoveAll(definition => definition.Source == ev);
            PointDefinitions.RemoveAll((pd) => pd.Source == ev);
        }

        public void RefreshBpmTiming()
        {
            var hasRepeats = false;
            foreach (var definition in sourceDefinitions)
            {
                if (definition.Parameters.Repeat > 0 && definition.Parameters.Duration > 0)
                {
                    hasRepeats = true;
                    break;
                }
            }

            if (!hasRepeats)
            {
                return;
            }

            // A BPM edit changes the song-end beat, so repeats clipped during loading may become reachable.
            PointDefinitions.Clear();
            foreach (var definition in sourceDefinitions)
            {
                AddPointDefinitions(definition.Parser, definition.Parameters, definition.Source);
            }

            Sort();
            ResetEvaluatedValue();
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            next = Mathf.Clamp(evaluated.AsSpan(0, count).UpperBoundBy(time, static point => point.StartTime),
                Mathf.Min(1, count), count);
            prev = Mathf.Max(0, next - 1);
        }
    }
}
