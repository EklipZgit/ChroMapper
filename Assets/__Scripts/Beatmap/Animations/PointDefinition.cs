// Mostly just copied from Heck

using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using Beatmap.Base.Customs;

namespace Beatmap.Animations
{
    public interface IPointDefinition
    {
        public struct UntypedParams
        {
            public string Key;
            public bool Overwrite;
            public JSONNode Points;
            public string Easing;
            public float Time;
            public float Transition;
            public float Duration;
            public float TimeBegin;
            public float TimeEnd;
            public int Repeat;
        }

        // One evaluated number source inside a point: a literal number run or a live base-provider value
        // (Heck's IValues split between StaticValues and the BaseProvider-backed providers).
        public interface IValueSegment
        {
            int Dimension { get; }
            void Append(float[] target, ref int offset);
        }
    }

    internal readonly struct StaticValues : IPointDefinition.IValueSegment
    {
        private readonly float[] values;

        internal StaticValues(float[] values) => this.values = values;

        public int Dimension => values.Length;

        public void Append(float[] target, ref int offset)
        {
            for (var i = 0; i < values.Length; ++i)
            {
                target[offset++] = values[i];
            }
        }
    }

    public class PointDefinition<T> : IPointDefinition, IComparable<PointDefinition<T>>
        where T : struct
    {
        public BaseCustomEvent Source;
        public PointData[] Points;

        public float StartTime { get; private set; } = 0;

        // For AnimateTrack
        public float Duration = 0;

        // For AssignPathAnimation
        public float Transition = 0;
        public Func<float, float> Easing;

        public delegate T Parser(JSONArray data, ref int i);

        public delegate T InterpolationHandler(PointData[] points, int prev, int next, float time);

        // Used for searching ONLY
        public PointDefinition(float start)
        {
            StartTime = start;
        }

        public PointDefinition(Parser parser, IPointDefinition.UntypedParams p, BaseCustomEvent source)
        {
            Source = source;
            StartTime = p.Time;
            Transition = p.Transition;
            Duration = p.Duration;
            // Chroma resolves the event-level _easing through Heck's own table; HeckNamed applies its
            // Back/Bounce/Elastic/Expo variants where they diverge from the easings.net curves.
            Easing = global::Easing.HeckNamed(p.Easing ?? "easeLinear");

            // The parser delegate is retained for the existing AnimateProperty seam, but evaluation is
            // type-driven (PointType<T>) so modifiers, bases, swizzling, and smoothing work for every property
            // type exactly like Heck's PointDefinition stack instead of only colors.
            var data = ResolvePointsNode(p);

            if (data == null || data.Count == 0)
            {
                Points = Array.Empty<PointData>();
                return;
            }

            // Heck's bare-point shorthand: a node whose first child is not itself an array is one point with an
            // appended time of 0 ("dissolve": [1.5, [2, "opMul"]] is a single point).
            if (!data[0].IsArray)
            {
                var single = ParsePoint(data, p.TimeBegin, p.TimeEnd, barePoint: true);
                Points = single != null ? new[] { single } : Array.Empty<PointData>();
                return;
            }

            var parsed = new List<PointData>(data.Count);
            for (var i = 0; i < data.Count; ++i)
            {
                var point = ParsePoint(data[i].AsArray, p.TimeBegin, p.TimeEnd, barePoint: false);
                if (point != null)
                {
                    parsed.Add(point);
                }
            }

            Points = parsed.ToArray();
        }

        public T Interpolate(float time)
        {
            var count = Points.Length;

            if (count == 0)
            {
                return default;
            }

            if (Points[count - 1].Time <= time)
            {
                return Points[count - 1].Value;
            }

            if (Points[0].Time >= time)
            {
                return Points[0].Value;
            }

            GetIndexes(time, out int prev, out int next);

            float normalTime;
            float divisor = Points[next].Time - Points[prev].Time;
            if (divisor != 0)
            {
                normalTime = (time - Points[prev].Time) / divisor;
            }
            else
            {
                normalTime = 0;
            }

            normalTime = Points[next].Easing(normalTime);

            return Points[next].Lerp(Points, prev, next, normalTime);
        }

        private void GetIndexes(float time, out int prev, out int next)
        {
            prev = 0;
            next = Points.Length;

            while (prev < next - 1)
            {
                int m = (prev + next) / 2;
                float pointTime = Points[m].Time;

                if (pointTime < time)
                {
                    prev = m;
                }
                else
                {
                    next = m;
                }
            }
        }

        public int CompareTo(PointDefinition<T> other)
        {
            // TODO: might be able to cheese previous/next with this
            return StartTime.CompareTo(other.StartTime);
        }

        // Named point definitions are resolved by the caller's missing-name guard; anything that is not a JSON
        // array yields an empty definition whose Interpolate returns default.
        private static JSONArray ResolvePointsNode(IPointDefinition.UntypedParams p)
        {
            switch (p.Points)
            {
                case JSONArray arr:
                    return arr;
                case JSONString named:
                    return BeatSaberSongContainer.Instance.Map.PointDefinitions.TryGetValue(named.Value, out var definition)
                        ? definition.AsArray
                        : null;
                default:
                    return null;
            }
        }

        // Heck's PointDefinition ctor groups each point's items into values (numbers and base* strings), flags
        // (easings/splines), and modifiers (nested lists carrying an operation). The value is the first
        // Dimension numbers across the value groups in order; the time is the next number, defaulting to 0 for
        // the bare-point shorthand only (Heck appends 0 to a bare point; rows must carry the time explicitly).
        private PointData ParsePoint(JSONArray row, float tbegin, float tend, bool barePoint)
        {
            var segments = new List<IPointDefinition.IValueSegment>();
            var staticRun = new List<float>();
            var flags = new List<string>();
            var modifiers = new List<PointModifier<T>>();
            var hasLiveSegment = false;

            for (var i = 0; i < row.Count; ++i)
            {
                var item = row[i];
                if (item is JSONArray modifierNode)
                {
                    var modifier = ParseModifier(modifierNode);
                    if (modifier == null) return null;
                    modifiers.Add(modifier);
                }
                else if (item is JSONString)
                {
                    if (item.Value.StartsWith("base", StringComparison.Ordinal))
                    {
                        FlushStaticRun(segments, staticRun);
                        var segment = BaseProviderManager.Resolve(item.Value);
                        if (segment == null) return null;
                        segments.Add(segment);
                        hasLiveSegment = true;
                    }
                    else
                    {
                        flags.Add(item.Value);
                    }
                }
                else if (item is JSONNumber)
                {
                    staticRun.Add(item.AsFloat);
                }
                else
                {
                    Debug.LogError($"Point contains an unsupported entry [{item.Value}] and was skipped.");
                    return null;
                }
            }

            var dimension = PointType<T>.Dimension;

            // Heck's static fast path: one value group of exactly Dimension (+ optional time) numbers. A bare
            // point gets its time appended (Count == Dimension is valid); a row must carry the time explicitly.
            if (!hasLiveSegment)
            {
                var requiredCount = barePoint ? dimension + 1 : dimension + 1;
                var minimumCount = barePoint ? dimension : dimension + 1;
                if (staticRun.Count < minimumCount || staticRun.Count > requiredCount)
                {
                    Debug.LogError(
                        $"Point for [{typeof(T).Name}] must have {dimension} numbers plus an optional time; got {staticRun.Count} and it was skipped.");
                    return null;
                }

                var values = new float[dimension];
                for (var i = 0; i < dimension; ++i)
                {
                    values[i] = staticRun[i];
                }

                return new PointData(
                    PointType<T>.Convert(values),
                    null,
                    modifiers.ToArray(),
                    ResolveTime(staticRun.Count == dimension + 1 ? staticRun[dimension] : 0f, tbegin, tend),
                    ResolveEasing(flags),
                    ResolveLerp(flags));
            }

            FlushStaticRun(segments, staticRun);

            var total = 0;
            foreach (var segment in segments)
            {
                total += segment.Dimension;
            }

            float time;
            if (total == dimension && barePoint)
            {
                time = 0f;
            }
            else if (total == dimension + 1)
            {
                // Heck reads the time from the last value of the last group, so a live base segment can carry it.
                var scratch = new float[segments[^1].Dimension];
                var offset = 0;
                segments[^1].Append(scratch, ref offset);
                time = scratch[^1];
            }
            else
            {
                Debug.LogError(
                    $"Point for [{typeof(T).Name}] must have {dimension} numbers plus a time; got {total} and it was skipped.");
                return null;
            }

            return new PointData(
                null,
                segments.ToArray(),
                modifiers.ToArray(),
                ResolveTime(time, tbegin, tend),
                ResolveEasing(flags),
                ResolveLerp(flags));
        }

        private PointModifier<T> ParseModifier(JSONArray row)
        {
            var segments = new List<IPointDefinition.IValueSegment>();
            var staticRun = new List<float>();
            string operation = null;
            var nested = new List<PointModifier<T>>();
            var hasLiveSegment = false;

            for (var i = 0; i < row.Count; ++i)
            {
                var item = row[i];
                if (item is JSONArray innerNode)
                {
                    // Heck groups modifier operands by type, so a nested modifier does not split the static run.
                    var inner = ParseModifier(innerNode);
                    if (inner == null) return null;
                    nested.Add(inner);
                }
                else if (item is JSONString)
                {
                    if (item.Value.StartsWith("base", StringComparison.Ordinal))
                    {
                        FlushStaticRun(segments, staticRun);
                        var segment = BaseProviderManager.Resolve(item.Value);
                        if (segment == null) return null;
                        segments.Add(segment);
                        hasLiveSegment = true;
                    }
                    else if (operation == null)
                    {
                        operation = item.Value;
                    }
                    else
                    {
                        Debug.LogError("Modifier must have exactly one operation; the point was skipped.");
                        return null;
                    }
                }
                else if (item is JSONNumber)
                {
                    staticRun.Add(item.AsFloat);
                }
                else
                {
                    Debug.LogError($"Modifier contains an unsupported entry [{item.Value}] and was skipped.");
                    return null;
                }
            }

            if (operation == null)
            {
                Debug.LogError("Modifier must have one operation; the point was skipped.");
                return null;
            }

            if (!hasLiveSegment)
            {
                if (staticRun.Count != PointType<T>.Dimension)
                {
                    Debug.LogError(
                        $"Modifier for [{typeof(T).Name}] must have {PointType<T>.Dimension} numbers; got {staticRun.Count} and the point was skipped.");
                    return null;
                }

                var values = new float[PointType<T>.Dimension];
                for (var i = 0; i < PointType<T>.Dimension; ++i)
                {
                    values[i] = staticRun[i];
                }

                return new PointModifier<T>(PointType<T>.Convert(values), null, nested.ToArray(), operation);
            }

            FlushStaticRun(segments, staticRun);

            var total = 0;
            foreach (var segment in segments)
            {
                total += segment.Dimension;
            }

            if (total != PointType<T>.Dimension)
            {
                Debug.LogError(
                    $"Modifier for [{typeof(T).Name}] must have {PointType<T>.Dimension} numbers; got {total} and the point was skipped.");
                return null;
            }

            return new PointModifier<T>(null, segments.ToArray(), nested.ToArray(), operation);
        }

        private static void FlushStaticRun(
            List<IPointDefinition.IValueSegment> segments,
            List<float> staticRun)
        {
            if (staticRun.Count == 0) return;
            segments.Add(new StaticValues(staticRun.ToArray()));
            staticRun.Clear();
        }

        private static float ResolveTime(float rawTime, float tbegin, float tend) =>
            (tend == 0) ? rawTime : Mathf.LerpUnclamped(tbegin, tend, rawTime);

        // Heck takes the first ease* flag and resolves it through its own table (Heck's Back/Bounce/
        // Elastic/Expo are custom variants); unknown names fall back to linear.
        private static Func<float, float> ResolveEasing(List<string> flags)
        {
            foreach (var flag in flags)
            {
                if (flag.StartsWith("ease", StringComparison.Ordinal))
                {
                    return global::Easing.HeckNamed(flag);
                }
            }

            return global::Easing.Linear;
        }

        private static InterpolationHandler ResolveLerp(List<string> flags)
        {
            var lerp = PointDataInterpolators.LinearLerp<T>();
            foreach (var flag in flags)
            {
                if (flag == "splineCatmullRom")
                {
                    lerp = PointDataInterpolators.CatmullRomLerp<T>();
                }

                if (flag == "lerpHSV")
                {
                    lerp = PointDataInterpolators.HSVLerp<T>();
                }
            }

            return lerp;
        }

        public class PointData : IComparable<PointData>
        {
            private readonly T? staticBase;
            private readonly IPointDefinition.IValueSegment[] segments;
            private readonly PointModifier<T>[] modifiers;
            private readonly float[] buffer;

            internal PointData(
                T? staticBase,
                IPointDefinition.IValueSegment[] segments,
                PointModifier<T>[] modifiers,
                float time,
                Func<float, float> easing,
                InterpolationHandler lerp)
            {
                this.staticBase = staticBase;
                this.segments = segments;
                this.modifiers = modifiers;
                Time = time;
                Easing = easing;
                Lerp = lerp;
                buffer = segments != null ? new float[PointType<T>.Dimension] : Array.Empty<float>();
            }

            // Heck evaluates point values when grabbed, so base-driven endpoints stay live across seeks.
            public T Value
            {
                get
                {
                    if (modifiers.Length == 0 && segments == null)
                    {
                        return staticBase!.Value;
                    }

                    // Heck's BaseProviderManager.Tick advances smoothed providers before their values are read.
                    BaseProviderManager.Tick();
                    var current = segments != null ? PointType<T>.Convert(FillBuffer()) : staticBase!.Value;
                    for (var i = 0; i < modifiers.Length; ++i)
                    {
                        current = PointType<T>.ApplyOperation(current, modifiers[i].Point, modifiers[i].Operation);
                    }

                    return current;
                }
            }

            public float Time { get; }
            public Func<float, float> Easing { get; }
            public InterpolationHandler Lerp { get; }

            private float[] FillBuffer()
            {
                var offset = 0;
                foreach (var segment in segments)
                {
                    // Heck's FillValues stops at the property's dimension, so a trailing time value never
                    // reaches the evaluated point.
                    if (offset >= buffer.Length) break;
                    segment.Append(buffer, ref offset);
                }

                return buffer;
            }

            public int CompareTo(PointData other) => Time.CompareTo(other.Time);
        }
    }

    internal sealed class PointModifier<T>
        where T : struct
    {
        private readonly T? raw;
        private readonly IPointDefinition.IValueSegment[] segments;
        private readonly PointModifier<T>[] nested;
        private readonly float[] buffer;

        internal PointModifier(
            T? raw,
            IPointDefinition.IValueSegment[] segments,
            PointModifier<T>[] nested,
            string operation)
        {
            this.raw = raw;
            this.segments = segments;
            this.nested = nested;
            Operation = operation;
            buffer = segments != null ? new float[PointType<T>.Dimension] : Array.Empty<float>();
        }

        public string Operation { get; }

        // Heck's Modifier.Point aggregates the original value with each nested modifier's live point using that
        // nested modifier's own operation, so nested chains resolve inside-out.
        public T Point
        {
            get
            {
                var current = raw ?? PointType<T>.Convert(FillBuffer());
                for (var i = 0; i < nested.Length; ++i)
                {
                    current = PointType<T>.ApplyOperation(current, nested[i].Point, nested[i].Operation);
                }

                return current;
            }
        }

        private float[] FillBuffer()
        {
            var offset = 0;
            foreach (var segment in segments)
            {
                if (offset >= buffer.Length) break;
                segment.Append(buffer, ref offset);
            }

            return buffer;
        }
    }

    internal static class PointType<T>
        where T : struct
    {
        internal static readonly int Dimension = ResolveDimension();
        internal static readonly Func<float[], T> Convert = CreateConverter();
        internal static readonly Func<T, T, string, T> ApplyOperation = CreateOperations();

        private static int ResolveDimension() =>
            typeof(T) == typeof(float) ? 1
            : typeof(T) == typeof(Vector3) || typeof(T) == typeof(Quaternion) ? 3
            : typeof(T) == typeof(Color) ? 4
            : throw new Exception($"Unhandled point definition type {typeof(T).Name}");

        private static Func<float[], T> CreateConverter() =>
            typeof(T) == typeof(float) ? values => (T)(object)values[0]
            : typeof(T) == typeof(Vector3) ? values => (T)(object)new Vector3(values[0], values[1], values[2])
            : typeof(T) == typeof(Quaternion) ? values => (T)(object)Quaternion.Euler(values[0], values[1], values[2])
            : values => (T)(object)new Color(values[0], values[1], values[2], values[3]);

        private static Func<T, T, string, T> CreateOperations() =>
            typeof(T) == typeof(float)
                ? (current, modifier, operation) =>
                {
                    var a = (float)(object)current;
                    var b = (float)(object)modifier;
                    return (T)(object)(operation switch
                    {
                        "opAdd" => a + b,
                        "opSub" => a - b,
                        "opMul" => a * b,
                        "opDiv" => a / b,
                        "opNone" => a,
                        _ => throw new Exception($"[{operation}] cannot be performed on type float.")
                    });
                }
            : typeof(T) == typeof(Vector3)
                ? (current, modifier, operation) =>
                {
                    var a = (Vector3)(object)current;
                    var b = (Vector3)(object)modifier;
                    return (T)(object)(operation switch
                    {
                        "opAdd" => a + b,
                        "opSub" => a - b,
                        "opMul" => Vector3.Scale(a, b),
                        "opDiv" => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z),
                        "opNone" => a,
                        _ => throw new Exception($"[{operation}] cannot be performed on type Vector3.")
                    });
                }
            : typeof(T) == typeof(Quaternion)
                ? (current, modifier, operation) =>
                {
                    // Heck's QuaternionPointDefinition applies operations on the euler representation.
                    var a = ((Quaternion)(object)current).eulerAngles;
                    var b = ((Quaternion)(object)modifier).eulerAngles;
                    var result = operation switch
                    {
                        "opAdd" => a + b,
                        "opSub" => a - b,
                        "opMul" => Vector3.Scale(a, b),
                        "opDiv" => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z),
                        "opNone" => a,
                        _ => throw new Exception($"[{operation}] cannot be performed on type Quaternion.")
                    };
                    return (T)(object)Quaternion.Euler(result);
                }
            : (current, modifier, operation) =>
            {
                var a = (Color)(object)current;
                var b = (Color)(object)modifier;
                return (T)(object)(operation switch
                {
                    "opAdd" => a + b,
                    "opSub" => a - b,
                    "opMul" => a * b,
                    "opDiv" => new Color(a.r / b.r, a.g / b.g, a.b / b.b, a.a / b.a),
                    "opNone" => a,
                    _ => throw new Exception($"[{operation}] cannot be performed on type Color.")
                });
            };
    }

    // The legacy per-property positional readers. Evaluation is type-driven (PointType<T>) so modifiers, bases,
    // swizzling, and smoothing work for every property type, but the parsers remain the public seam that
    // AnimateProperty.AddPointDef and existing tests use to select the property's type.
    public class PointDataParsers
    {
        public static ColorSchemeSO ColorScheme;

        public static float ParseFloat(JSONArray data, ref int i)
        {
            i += 1;
            return data[0];
        }

        public static Color ParseColor(JSONArray data, ref int i)
        {
            Color result;

            if (data[i].IsString)
            {
                i += 1;

                result = data[0].Value switch
                {
                    // Intentionally not supporting baseSaber_Color since those don't mirror with left handed
                    // mode while baseNote_Color does. Should almost always use baseNote over baseSaber.
                    "baseNote0Color" => ColorScheme.LeftNoteColor,
                    "baseNote1Color" => ColorScheme.RightNoteColor,
                    "baseEnvironmentColor0" => ColorScheme.EnvironmentLeftColor,
                    "baseEnvironmentColor1" => ColorScheme.EnvironmentRightColor,
                    "baseEnvironmentColorW" => ColorScheme.EnvironmentWhiteColor,
                    "baseEnvironmentColor0Boost" => ColorScheme.EnvironmentLeftBoostColor,
                    "baseEnvironmentColor1Boost" => ColorScheme.EnvironmentRightBoostColor,
                    "baseEnvironmentColorWBoost" => ColorScheme.EnvironmentWhiteBoostColor,
                    "baseObstaclesColor" => ColorScheme.ObstacleColor,
                    _ => DefaultColors.White
                };
            }
            else
            {
                i += 4;
                result = new Color(data[0], data[1], data[2], data[3]);
            }

            if (data[i] is JSONArray array)
            {
                i += 1;

                var innerIdx = 0;
                var subColor = ParseColor(array, ref innerIdx);

                var colorOp = array[innerIdx].Value;

                result = colorOp switch
                {
                    "opAdd" => result + subColor,
                    "opSub" => result - subColor,
                    "opMul" => result * subColor,
                    "opDiv" => new Color(
                        result.r / subColor.r,
                        result.g / subColor.g,
                        result.b / subColor.b,
                        result.a / subColor.a),
                    _ => result
                };
            }

            return result;
        }

        public static Vector3 ParseVector3(JSONArray data, ref int i)
        {
            i += 3;
            return new Vector3(data[0], data[1], data[2]);
        }

        public static Quaternion ParseQuaternion(JSONArray data, ref int i)
        {
            i += 3;
            return Quaternion.Euler(data[0], data[1], data[2]);
        }
    }
}
