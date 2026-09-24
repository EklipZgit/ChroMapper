using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Animations
{
    // TubeBloomAnimationTests.AnimateComponentTubeBloomEventsDriveLightMultipliers: AnimateComponent events
    // targeting TubeBloomPrePassLight animate the light multipliers of every track-bound object, exactly like
    // Heck's AnimateComponent + TubeBloomLightCustomizer pair. CM's analog of the component lives on the
    // ParametricBloomFogLightController each bound object carries, so this animator resolves those controllers
    // once (after the environment enhancements spawn) and drives them through the same point-definition
    // pipeline the fog animator uses.
    public class TubeBloomAnimator : MonoBehaviour
    {
        private const string ComponentName = "TubeBloomPrePassLight";

        public AudioTimeSyncController Atsc;

        private readonly Dictionary<string, AnimateProperty<float>> animatedProperties = new();
        private readonly Dictionary<string, float[]> baselines = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();
        private ParametricBloomFogLightController[] controllers = Array.Empty<ParametricBloomFogLightController>();
        private bool resolved;

        // TubeBloomAnimationTests: AnimateComponent data nests the animated component's parameters under the
        // component name, and Heck only resolves TubeBloomPrePassLight's colorAlphaMultiplier and
        // bloomFogIntensityMultiplier properties, so both levels are filtered here.
        public void AddEvent(BaseCustomEvent ev)
        {
            if (ev.Data?[ComponentName] is not JSONObject component) return;

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
                    case "colorAlphaMultiplier":
                    case "bloomFogIntensityMultiplier":
                        break;
                    default:
                        continue;
                }

                // The same UntypedParams shape TrackAnimator feeds its AnimateTrack point definitions, so
                // event duration/easing/repeat and per-point [value, time, easing] rows behave identically.
                var p = new IPointDefinition.UntypedParams
                {
                    Key = jprop.Key,
                    Points = jprop.Value,
                    Easing = ev.DataEasing,
                    Time = ev.JsonTime,
                    Duration = ev.DataDuration ?? 0,
                    TimeBegin = ev.JsonTime,
                    TimeEnd = ev.JsonTime + (ev.DataDuration ?? 0),
                    Repeat = ev.DataRepeat ?? 0
                };

                GetProperty(jprop.Key).AddPointDef(PointDataParsers.ParseFloat, p, ev);
            }

            RefreshProperties();
        }

        // Symmetric with AddEvent so a deleted tube-bloom event stops contributing points; empty properties
        // are dropped because AnimateProperty.Sort cannot sort an empty definition list.
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

            enabled = properties.Length > 0;
        }

        // Playback streams light values every frame exactly like TrackAnimator.Update reads the live time.
        private void Update() => PushAt(Atsc != null ? Atsc.CurrentJsonTime : 0);

        // TrackScrubParityTest established that stopped-time seeks must land on the as-if-played state
        // synchronously; TubeBloomAnimationTests requires the same contract for the light preview.
        public void PushOnStoppedTimeChanged()
        {
            if (!isActiveAndEnabled || Atsc == null || Atsc.IsPlaying) return;
            PushAt(Atsc.CurrentJsonTime);
        }

        private void PushAt(float time)
        {
            if (properties.Length == 0) return;

            // The first push happens after map load finishes (seeks and playback, never the load itself),
            // when the environment enhancements have spawned the track's lights and authored their component
            // values, so resolve the targets and capture their authored baselines once.
            if (!resolved)
            {
                ResolveControllers();
            }

            // Heck's AnimateComponent skips a track whose objects carry no TubeBloomPrePassLight components.
            if (controllers.Length == 0) return;

            foreach (var pair in animatedProperties)
            {
                if (time >= pair.Value.StartTime)
                {
                    // Push unconditionally past the first event: the latest definition at or before the time
                    // wins, matching Heck's latest-coroutine-wins behavior for scrubbing and playback alike.
                    pair.Value.UpdateProperty(time);
                }
                else
                {
                    // Before the property's first event, restore each controller's own authored value so
                    // scrubbing backward matches the freshly-loaded state even when the track's lights
                    // authored different multipliers.
                    RestoreBaseline(pair.Key);
                }
            }
        }

        // The first push happens after map load finishes, when the environment enhancements have spawned the
        // track's bound objects, so resolve Heck's GetComponents equivalent: every light controller under the
        // track's bound objects, with each controller's authored values captured as the pre-first-event state.
        // Custom events load before the enhancements spawn, so this retries until the track has bound objects
        // instead of caching an empty target set.
        private void ResolveControllers()
        {
            var trackAnimator = GetComponent<TrackAnimator>();
            if (trackAnimator == null || trackAnimator.Children.Count == 0)
            {
                return;
            }

            resolved = true;

            controllers = trackAnimator.Children
                .Select(animator => animator.LocalTarget)
                .Where(target => target != null)
                .SelectMany(target => target.GetComponentsInChildren<ParametricBloomFogLightController>(true))
                .Distinct()
                .ToArray();

            foreach (var key in animatedProperties.Keys)
            {
                baselines[key] = controllers.Select(controller => ReadParam(controller, key)).ToArray();
                if (animatedProperties.TryGetValue(key, out var prop) && controllers.Length > 0)
                {
                    prop.Default = baselines[key][0];
                }
            }
        }

        // WorldCavesInEnvironmentTest found named-track state persisting across HardRefresh reloads; the game
        // rebuilds all animation state per map load, so the light animation, its resolved targets, and its
        // captured baselines must reset too.
        public void ResetForMapLoad()
        {
            animatedProperties.Clear();
            baselines.Clear();
            properties = Array.Empty<IAnimateProperty>();
            controllers = Array.Empty<ParametricBloomFogLightController>();
            resolved = false;
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

        // Heck's TubeBloomPrePassLight property names map onto the same controller fields the environment
        // enhancement writes in GeometryContainer.
        private float ReadParam(ParametricBloomFogLightController controller, string key) => key switch
        {
            "colorAlphaMultiplier" => controller.ColorAlphaMultiplier,
            "bloomFogIntensityMultiplier" => controller.BloomFogIntensityMultiplier,
            _ => 0f
        };

        // Restores each controller's own captured baseline so scrubbing backward before the property's first
        // event matches the freshly-loaded state (TubeBloomAnimationTests scrubs back to beat 0 after beat 17).
        private void RestoreBaseline(string key)
        {
            if (!baselines.TryGetValue(key, out var values)) return;
            for (var i = 0; i < controllers.Length && i < baselines[key].Length; ++i)
            {
                WriteParam(controllers[i], key, baselines[key][i]);
            }
        }

        private void WriteParam(string key, float value)
        {
            for (var i = 0; i < controllers.Length; ++i)
            {
                WriteParam(controllers[i], key, value);
            }
        }

        private static void WriteParam(ParametricBloomFogLightController controller, string key, float value)
        {
            switch (key)
            {
                case "colorAlphaMultiplier":
                    controller.SetColorAlphaMultiplier(value);
                    break;
                case "bloomFogIntensityMultiplier":
                    controller.SetBloomFogIntensityMultiplier(value);
                    break;
            }
        }
    }
}
