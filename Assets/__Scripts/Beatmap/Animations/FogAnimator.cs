using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    // FogAnimationTests.AnimateComponentFogEventsDriveBloomFogPreviewSeeks: AnimateComponent events target
    // environment components (BloomFogEnvironment fog parameters) rather than object transforms, so they
    // cannot ride the ObjectAnimator children a track animates for AnimateTrack. This animator is the CM
    // analog of Heck's AnimateComponent + BloomFogCustomizer pair: it resolves the event's point
    // definitions through the same pipeline and drives the descriptor's BloomFogParams (the single params
    // object the preview renderer consumes) so the fog preview matches the game.
    public class FogAnimator : MonoBehaviour
    {
        private const string ComponentName = "BloomFogEnvironment";

        public AudioTimeSyncController Atsc;
        public BeatmapRuntimeContext Context;

        private readonly Dictionary<string, AnimateProperty<float>> animatedProperties = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();
        private bool baselineCaptured;
        // Parsed points stay dormant until the V2 assignment timeline selects this track or
        // a V3 environment enhancement binds the fog component owner to this track.
        private bool hasComponentEvents;
        private bool hasFogComponentTarget;
        private LegacyFogBinding legacyBinding;
        private bool controlsLegacyBinding;

        // FogAnimationTests.AnimateComponentFogEventsDriveBloomFogPreviewSeeks: AnimateComponent data nests
        // the animated component's parameters under the component name (mirroring Heck's AnimateComponent
        // CoroutineInfos), and Heck only resolves BloomFogEnvironment's attenuation/offset/height/startY
        // properties, so both the component and the property names are filtered here.
        public void AddEvent(BaseCustomEvent ev)
        {
            if (ev.Data?[ComponentName] is not JSONObject component) return;

            // Spells' V3 component data becomes active when its environment track owns fog.
            hasComponentEvents = true;

            foreach (var jprop in component)
            {
                // AdditionalAnimationParityTest.NullPropertyErasesTheTrackProperty: a null parameter erases the
                // property (Heck's coroutine stops, so the value holds); CM drops the property so the push
                // stops writing it.
                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    if (animatedProperties.Remove(jprop.Key))
                    {
                        RefreshProperties();
                    }

                    continue;
                }

                switch (jprop.Key)
                {
                    case "attenuation":
                    case "offset":
                    case "height":
                    case "startY":
                        break;
                    default:
                        continue;
                }

                AddProperty(jprop.Key, jprop.Value, ev);
            }

            RefreshProperties();
        }

        // HeliovMapParityTest.CliffSceneAtFirstNotesUsesFogTrackAndEnvironment: V2 Chroma
        // registers the four underscored AnimateTrack float properties on a track bound by
        // AssignFogTrack. Resolve them through the same point-definition path as V3 components.
        public void AddLegacyEvent(BaseCustomEvent ev)
        {
            foreach (var jprop in ev.Data)
            {
                var key = jprop.Key switch
                {
                    "_attenuation" => "attenuation",
                    "_offset" => "offset",
                    "_height" => "height",
                    "_startY" => "startY",
                    _ => null
                };
                if (key == null) continue;

                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    if (animatedProperties.Remove(key))
                    {
                        RefreshProperties();
                    }

                    continue;
                }

                AddProperty(key, jprop.Value, ev);
            }

            RefreshProperties();
        }

        // Heliov's beat-0 AssignFogTrack may follow its beat-0 AnimateTrack in file order.
        // Keep parsed V2 values dormant until the binding timeline selects this track.
        // BloomFogChromaParityAuditTest.AnimateComponentOnNonFogTrackKeepsEnvironmentFog:
        // Chroma only animates a track that owns the fog component, established by the environment binding.
        public void BindFogComponentTarget()
        {
            hasFogComponentTarget = true;
            RefreshEnabled();
        }

        // BloomFogChromaParityAuditTest.LegacyFogBindingFollowsAssignmentCallbacksAndTrackSwitches:
        // one controller selects the latest assignment at the current beat, including backward seeks.
        public void SetLegacyBinding(LegacyFogBinding binding, bool controller)
        {
            legacyBinding = binding;
            controlsLegacyBinding = controller;
            RefreshEnabled();
        }

        private void AddProperty(string key, JSONNode points, BaseCustomEvent ev)
        {
            // Spells and Heliov use the same duration, easing, repeat, and point-definition
            // semantics after the V2 property name is mapped to the renderer parameter name.
            var p = new IPointDefinition.UntypedParams
            {
                Key = key,
                Points = points,
                Easing = ev.DataEasing,
                Time = ev.JsonTime,
                Duration = ev.DataDuration ?? 0,
                TimeBegin = ev.JsonTime,
                TimeEnd = ev.JsonTime + (ev.DataDuration ?? 0),
                Repeat = ev.DataRepeat ?? 0
            };

            // A missing named point definition is bad map content, not a reason to throw during
            // map load: an empty AnimateProperty would crash Sort and wedge the loader.
            if (AnimateProperty<float>.SkipsMissingPointDefinition(p)) return;

            GetProperty(key).AddPointDef(PointDataParsers.ParseFloat, p, ev);
        }

        // FogAnimationTests deletes nothing today, but the editor's delete flow must stay symmetric with
        // AddEvent or a removed event would keep animating the preview fog; empty properties are dropped
        // because AnimateProperty.Sort cannot sort an empty definition list.
        public void RemoveEvent(BaseCustomEvent ev)
        {
            foreach (var key in animatedProperties.Keys.ToList())
            {
                var prop = animatedProperties[key];
                prop.RemoveEvent(ev);
                if (prop.IsEmpty())
                {
                    animatedProperties.Remove(key);
                }
            }

            RefreshProperties();
        }

        private void RefreshProperties()
        {
            properties = new IAnimateProperty[animatedProperties.Count];
            var i = 0;
            foreach (var prop in animatedProperties.Values)
            {
                prop.Sort();
                properties[i++] = prop;
            }

            // Parsed points without component ownership or an active V2 controller remain dormant.
            RefreshEnabled();
        }

        private void RefreshEnabled() =>
            enabled = (properties.Length > 0 && hasComponentEvents && hasFogComponentTarget)
                || controlsLegacyBinding;

        // Playback streams fog values every frame exactly like TrackAnimator.Update reads the live time.
        private void Update() => PushAt(Atsc != null ? Atsc.CurrentJsonTime : 0);

        // TrackScrubParityTest established that stopped-time seeks must land on the as-if-played state
        // synchronously; FogAnimationTests requires the same contract for the fog preview, so seeks reach
        // this animator through OnTimeChangedEarly before the frame renders.
        public void PushOnStoppedTimeChanged()
        {
            if (!isActiveAndEnabled || Atsc == null || Atsc.IsPlaying) return;
            PushAt(Atsc.CurrentJsonTime);
        }

        private void PushAt(float time)
        {
            if (controlsLegacyBinding)
            {
                legacyBinding.PushAt(time);
            }

            if (!hasComponentEvents || !hasFogComponentTarget || properties.Length == 0) return;

            PushValuesAt(time);
        }

        // The V2 binding selects this track at the event beat; disabled tracks still retain their
        // point data so a later AssignFogTrack can activate them without rebuilding animations.
        public void PushLegacyValuesAt(float time)
        {
            if (properties.Length > 0) PushValuesAt(time);
        }

        private void PushValuesAt(float time)
        {

            // The first push happens after map load finishes (seeks and playback, never the load itself),
            // when the descriptor still holds the authored enhancement values because the environment
            // enhancement is the only other writer, so capture them once as the pre-first-event state that
            // seeking backward must restore (FogAnimationTests scrubs back to beat 0 after beat 527).
            if (!baselineCaptured)
            {
                baselineCaptured = true;
                foreach (var pair in animatedProperties)
                {
                    pair.Value.Default = ReadParam(pair.Key);
                }
            }

            for (var i = 0; i < properties.Length; ++i)
            {
                // Push unconditionally, unlike TrackAnimator's time >= StartTime guard: GetLerpedValue
                // returns the property default before the first event, so this push is what restores the
                // authored fog value when scrubbing backward instead of leaving the last animated pose.
                properties[i].UpdateProperty(time);
            }

            // BloomFogEnvironmentEnhancementUpdatesRenderingState requires descriptor mutations to reach
            // the renderer's shader globals through the runtime context, which owns that single writer.
            Context.NotifyBloomFogParamsChanged();
        }

        // WorldCavesInEnvironmentTest found named-track state persisting across HardRefresh reloads; the
        // game rebuilds all animation state per map load, so the fog animation and its captured baseline
        // must reset too or the next map's authored values would restore the previous session's fog.
        public void ResetForMapLoad()
        {
            animatedProperties.Clear();
            properties = Array.Empty<IAnimateProperty>();
            baselineCaptured = false;
            // Map swaps must not carry a prior V2 binding or V3 component ownership into the
            // next map, even though the named track GameObject survives in the editor session.
            hasComponentEvents = false;
            hasFogComponentTarget = false;
            legacyBinding = null;
            controlsLegacyBinding = false;
            enabled = false;
        }

        private AnimateProperty<float> GetProperty(string key)
        {
            if (!animatedProperties.TryGetValue(key, out var prop))
            {
                prop = new AnimateProperty<float>(
                    new List<PointDefinition<float>>(),
                    value => WriteParam(key, value),
                    0f);
                animatedProperties[key] = prop;
            }

            return prop;
        }

        // Heck's BloomFogEnvironment property names (attenuation/offset/height/startY) map onto the same
        // descriptor fields the [0]Environment enhancement writes in GeometryContainer.
        private float ReadParam(string key)
        {
            var parameters = Context.Descriptor.BloomFogParams;
            return key switch
            {
                "attenuation" => parameters.Attenuation,
                "offset" => parameters.Offset,
                "height" => parameters.Height,
                "startY" => parameters.StartY,
                _ => 0f
            };
        }

        private void WriteParam(string key, float value)
        {
            var parameters = Context.Descriptor.BloomFogParams;
            switch (key)
            {
                case "attenuation":
                    parameters.Attenuation = value;
                    break;
                case "offset":
                    parameters.Offset = value;
                    break;
                case "height":
                    parameters.Height = value;
                    break;
                case "startY":
                    parameters.StartY = value;
                    break;
            }
        }
    }

    // BloomFogChromaParityAuditTest.LegacyFogBindingFollowsAssignmentCallbacksAndTrackSwitches:
    // V2 Chroma binds one fog track at each AssignFogTrack callback. A sorted timeline reproduces
    // that selection for both playback and editor seeks without scanning map events per frame.
    public sealed class LegacyFogBinding
    {
        private readonly List<(float Time, FogAnimator Animator)> assignments = new();
        private readonly BeatmapRuntimeContext context;
        private bool baselineCaptured;
        private float attenuation;
        private float offset;
        private float height;
        private float startY;

        public LegacyFogBinding(BeatmapRuntimeContext context) => this.context = context;

        public void Add(float time, FogAnimator animator) => assignments.Add((time, animator));

        public void PushAt(float time)
        {
            if (!baselineCaptured)
            {
                var parameters = context.Descriptor.BloomFogParams;
                attenuation = parameters.Attenuation;
                offset = parameters.Offset;
                height = parameters.Height;
                startY = parameters.StartY;
                baselineCaptured = true;
            }

            var low = 0;
            var high = assignments.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (assignments[middle].Time <= time) low = middle + 1;
                else high = middle;
            }

            if (low > 0)
            {
                assignments[low - 1].Animator.PushLegacyValuesAt(time);
                return;
            }

            var fog = context.Descriptor.BloomFogParams;
            fog.Attenuation = attenuation;
            fog.Offset = offset;
            fog.Height = height;
            fog.StartY = startY;
            context.NotifyBloomFogParamsChanged();
        }
    }
}
