using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beatmap.Animations
{
    // BaseProviderManager is the CM analog of Heck's BaseProviderManager: it resolves "base*" point values
    // (with .xyzw swizzling and .s[number] smoothing) against live editor state so point definitions can
    // reference the camera, colors, song time, and movement data the way Heck's base providers do in game.
    internal static class BaseProviderManager
    {
        private static readonly Dictionary<string, IPointDefinition.IValueSegment> BaseSegments = BuildSegments();
        private static readonly Dictionary<string, IPointDefinition.IValueSegment> ResolvedSegments = new();
        private static readonly List<SmoothedValues> Smoothed = new();
        private static int lastTickFrame = -1;
        private static CameraController headCameraController;
        private static VariableNJSProvider variableNjsProvider;

        // Heck's BaseProviderManager.Tick advances every smoothed provider once per frame; the evaluation path
        // calls this so smoothed values converge even when nothing re-parses the point definitions.
        internal static void Tick()
        {
            if (Time.frameCount == lastTickFrame) return;
            lastTickFrame = Time.frameCount;
            foreach (var segment in Smoothed)
            {
                segment.Tick();
            }
        }

        // Returns null for an unknown provider so the caller can log and skip the point like Heck's
        // GetPointData does for missing point definitions.
        internal static IPointDefinition.IValueSegment Resolve(string key)
        {
            if (BaseSegments.TryGetValue(key, out var segment)) return segment;
            if (ResolvedSegments.TryGetValue(key, out segment)) return segment;

            var dot = key.IndexOf('.');
            if (dot <= 0 || !BaseSegments.TryGetValue(key[..dot], out var source))
            {
                Debug.LogError($"Could not find base provider [{key}]; the point was skipped.");
                return null;
            }

            IPointDefinition.IValueSegment current = source;
            var remaining = key[(dot + 1)..];
            while (remaining.Length > 0)
            {
                var nextDot = remaining.IndexOf('.');
                var suffix = nextDot < 0 ? remaining : remaining[..nextDot];
                if (suffix.Length > 1 && suffix[0] == 's'
                    && float.TryParse(suffix[1..].Replace('_', '.'), out var smoothMult))
                {
                    // Heck's SmoothProvidersValues (.s[number], decimals as underscores) smooths between frames;
                    // the wrapper is cached per suffix chain so every point referencing it shares one state.
                    var smoothedSegment = new SmoothedValues(current, smoothMult);
                    Smoothed.Add(smoothedSegment);
                    current = smoothedSegment;
                }
                else
                {
                    var indices = new int[suffix.Length];
                    for (var i = 0; i < suffix.Length; ++i)
                    {
                        indices[i] = suffix[i] switch
                        {
                            'x' => 0,
                            'y' => 1,
                            'z' => 2,
                            'w' => 3,
                            _ => -1
                        };
                        if (indices[i] < 0 || indices[i] >= current.Dimension)
                        {
                            Debug.LogError($"Base provider [{key}] has an invalid swizzle [{suffix}]; the point was skipped.");
                            return null;
                        }
                    }

                    current = new SwizzledValues(current, indices);
                }

                remaining = nextDot < 0 ? string.Empty : remaining[(nextDot + 1)..];
            }

            ResolvedSegments[key] = current;
            return current;
        }

        private static Transform HeadTransform()
        {
            // Unity scene references need explicit null checks before use; the mapper scene recreates its
            // camera controller on every map load, so a dead cached reference is re-resolved here.
            if (headCameraController == null)
            {
                headCameraController = UnityEngine.Object.FindAnyObjectByType<CameraController>();
            }

            var camera = headCameraController != null ? headCameraController.Camera : null;
            return camera != null ? camera.transform : null;
        }

        private static VariableNJSProvider NjsProvider
        {
            get
            {
                if (variableNjsProvider == null)
                {
                    variableNjsProvider = UnityEngine.Object.FindAnyObjectByType<VariableNJSProvider>();
                }

                return variableNjsProvider;
            }
        }

        private static AudioTimeSyncController Atsc => AudioTimeSyncController.Instance;

        private static float SongLength => BeatSaberSongContainer.Instance.LoadedSong.length;

        private static float NoteJumpStartBeatOffset => BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteStartBeatOffset;

        private static float NoteJumpSpeed =>
            NjsProvider != null
                ? NjsProvider.NoteJumpSpeed
                : BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;

        private static float JumpDistance =>
            NjsProvider != null ? NjsProvider.JumpDistance : 0f;

        private static IPointDefinition.IValueSegment Live(int dimension, Action<float[], int> append) =>
            new LiveValues(dimension, append);

        private static Dictionary<string, IPointDefinition.IValueSegment> BuildSegments()
        {
            // Heck's PlayerTransformBaseProvider: the preview camera is the head; the editor has no VR
            // controllers, so the hand bases report their fresh-session defaults (zeroed transforms).
            var zero3 = Live(3, WriteZeros(3));
            var zero1 = Live(1, WriteZeros(1));
            return new Dictionary<string, IPointDefinition.IValueSegment>
            {
                ["baseHeadPosition"] = Live(3, WriteHeadPosition),
                ["baseHeadLocalPosition"] = Live(3, WriteHeadLocalPosition),
                ["baseHeadRotation"] = Live(3, WriteHeadRotation),
                ["baseHeadLocalRotation"] = Live(3, WriteHeadLocalRotation),
                ["baseHeadLocalScale"] = Live(3, WriteHeadLocalScale),
                ["baseLeftHandPosition"] = zero3,
                ["baseLeftHandLocalPosition"] = zero3,
                ["baseLeftHandRotation"] = zero3,
                ["baseLeftHandLocalRotation"] = zero3,
                ["baseLeftHandLocalScale"] = zero3,
                ["baseRightHandPosition"] = zero3,
                ["baseRightHandLocalPosition"] = zero3,
                ["baseRightHandRotation"] = zero3,
                ["baseRightHandLocalRotation"] = zero3,
                ["baseRightHandLocalScale"] = zero3,

                // Heck's ColorBaseProvider read from the active color scheme. Heck's saber colors read the raw
                // scheme colors without the left-handed mirror CM applies to note colors.
                ["baseNote0Color"] = Live(4, (t, o) => WriteSchemeColor(scheme => scheme.LeftNoteColor, t, o)),
                ["baseNote1Color"] = Live(4, (t, o) => WriteSchemeColor(s => s.RightNoteColor, t, o)),
                ["baseObstaclesColor"] = Live(4, (t, o) => WriteSchemeColor(s => s.ObstacleColor, t, o)),
                ["baseSaberAColor"] = Live(4, (t, o) => WriteSchemeColor(s => s.LeftNoteColor, t, o)),
                ["baseSaberBColor"] = Live(4, (t, o) => WriteSchemeColor(s => s.RightNoteColor, t, o)),
                ["baseEnvironmentColor0"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentLeftColor, t, o)),
                ["baseEnvironmentColor1"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentRightColor, t, o)),
                ["baseEnvironmentColorW"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentWhiteColor, t, o)),
                ["baseEnvironmentColor0Boost"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentLeftBoostColor, t, o)),
                ["baseEnvironmentColor1Boost"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentRightBoostColor, t, o)),
                ["baseEnvironmentColorWBoost"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentWhiteBoostColor, t, o)),

                // Heck's ScoreBaseProvider: the editor preview simulates no gameplay, so score/combo/energy
                // hold their fresh-session values while song time and length stay live.
                ["baseCombo"] = zero1,
                ["baseMultipliedScore"] = zero1,
                ["baseImmediateMaxPossibleMultipliedScore"] = zero1,
                ["baseModifiedScore"] = zero1,
                ["baseImmediateMaxPossibleModifiedScore"] = zero1,
                ["baseRelativeScore"] = zero1,
                ["baseMultiplier"] = Live(1, (t, o) => t[o] = 1f),
                ["baseEnergy"] = Live(1, (t, o) => t[o] = 1f),
                ["baseSongTime"] = Live(1, (t, o) => t[o] = Atsc != null ? Atsc.CurrentSeconds : 0f),
                ["baseSongLength"] = Live(1, (t, o) => t[o] = SongLength),

                // Heck's MovementDataBaseProvider from the map's movement data.
                ["baseNoteJumpMovementSpeed"] = Live(1, (t, o) => t[o] = NoteJumpSpeed),
                ["baseNoteJumpStartBeatOffset"] = Live(1, (t, o) => t[o] = NoteJumpStartBeatOffset),
                ["baseJumpDistance"] = Live(1, (t, o) => t[o] = JumpDistance),
                ["basePlayerHeight"] = zero1
            };
        }

        private static void WriteHeadPosition(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) Copy(head.position, target, offset);
        }

        private static void WriteHeadLocalPosition(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) Copy(head.localPosition, target, offset);
        }

        private static void WriteHeadRotation(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) CopyEuler(head.rotation, target, offset);
        }

        private static void WriteHeadLocalRotation(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) CopyEuler(head.localRotation, target, offset);
        }

        private static void WriteHeadLocalScale(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) Copy(head.localScale, target, offset);
        }

        private static void WriteSchemeColor(Func<ColorSchemeSO, Color> selector, float[] target, int offset)
        {
            var scheme = PointDataParsers.ColorScheme;
            WriteColor(scheme == null ? DefaultColors.White : selector(scheme), target, offset);
        }

        private static ColorSchemeSO Scheme() => PointDataParsers.ColorScheme;

        private static void Copy(Vector3? value, float[] target, int offset)
        {
            if (value.HasValue)
            {
                target[offset] = value.Value.x;
                target[offset + 1] = value.Value.y;
                target[offset + 2] = value.Value.z;
            }
        }

        private static void CopyEuler(Quaternion? rotation, float[] target, int offset)
        {
            if (rotation.HasValue)
            {
                var euler = rotation.Value.eulerAngles;
                target[offset] = euler.x;
                target[offset + 1] = euler.y;
                target[offset + 2] = euler.z;
            }
        }

        private static void WriteColor(Color? color, float[] target, int offset)
        {
            var value = color ?? DefaultColors.White;
            target[offset] = value.r;
            target[offset + 1] = value.g;
            target[offset + 2] = value.b;
            target[offset + 3] = value.a;
        }

        private static Action<float[], int> WriteZeros(int count)
        {
            void Write(float[] target, int offset)
            {
                for (var i = 0; i < count; ++i)
                {
                    target[offset + i] = 0f;
                }
            }

            return Write;
        }

        private sealed class LiveValues : IPointDefinition.IValueSegment
        {
            private readonly int dimension;
            private readonly Action<float[], int> append;

            internal LiveValues(int dimension, Action<float[], int> append)
            {
                this.dimension = dimension;
                this.append = append;
            }

            public int Dimension => dimension;

            public void Append(float[] target, ref int offset)
            {
                append(target, offset);
                offset += dimension;
            }
        }

        private sealed class SwizzledValues : IPointDefinition.IValueSegment
        {
            private readonly IPointDefinition.IValueSegment source;
            private readonly int[] parts;
            private readonly float[] scratch;

            internal SwizzledValues(IPointDefinition.IValueSegment source, int[] parts)
            {
                this.source = source;
                this.parts = parts;
                scratch = new float[source.Dimension];
            }

            public int Dimension => parts.Length;

            public void Append(float[] target, ref int offset)
            {
                var innerOffset = 0;
                source.Append(scratch, ref innerOffset);
                for (var i = 0; i < parts.Length; ++i)
                {
                    target[offset + i] = scratch[parts[i]];
                }

                offset += parts.Length;
            }
        }

        internal sealed class SmoothedValues : IPointDefinition.IValueSegment
        {
            private readonly IPointDefinition.IValueSegment source;
            private readonly float mult;
            private readonly float[] state;
            private readonly float[] scratch;

            internal SmoothedValues(IPointDefinition.IValueSegment source, float mult)
            {
                this.source = source;
                this.mult = mult;
                state = new float[source.Dimension];
                scratch = new float[source.Dimension];
            }

            public int Dimension => state.Length;

            public void Append(float[] target, ref int offset)
            {
                for (var i = 0; i < state.Length; ++i)
                {
                    target[offset + i] = state[i];
                }

                offset += state.Length;
            }

            // Heck's SmoothProvidersValues lerps the held state toward the live source every frame; lower
            // multipliers converge slower, so the state must advance even while nothing re-parses it.
            internal void Tick()
            {
                var delta = Time.deltaTime * mult;
                var offset = 0;
                source.Append(scratch, ref offset);
                for (var i = 0; i < state.Length; ++i)
                {
                    state[i] = Mathf.Lerp(state[i], scratch[i], delta);
                }
            }
        }
    }
}
