using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Animations
{
    public class TrackAnimator : MonoBehaviour
    {
        public AudioTimeSyncController Atsc;
        public Track Track;
        public ObjectAnimator Animator;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new Dictionary<string, IAnimateProperty>();
        private IAnimateProperty[] properties = new IAnimateProperty[0];

        public int UpdateVersion { get; private set; }

        public List<TrackAnimator> Parents = new List<TrackAnimator>();
        public List<ObjectAnimator> Children = new List<ObjectAnimator>();
        public ObjectAnimator[] CachedChildren = new ObjectAnimator[] {};

        private readonly Dictionary<string, Action<ObjectAnimator>> childPushers = new();

        // WorldCavesInEnvironmentTest's enhanced constructs must ride their AssignTrackParent parent with the
        // event's _worldPositionStays. Environment enhancements attach after custom events load, so the flag has
        // to live on the track for late-attaching objects, mirroring Noodle's ParentObject which parents every
        // object added to a child track with the same flag.
        public bool ParentWorldPositionStays;

        public void AddEvent(BaseCustomEvent ev)
        {
            foreach (var jprop in ev.Data)
            {
                // AdditionalAnimationParityTest.NullPropertyErasesTheTrackProperty: Heck allows a property set
                // to null to "erase" it — the property returns to as-if-never-set. CM drops the property from
                // the animator so the setter stops being called; already-active objects keep their last
                // pushed value, matching the game's "cannot update active objects" caveat.
                if (jprop.Value == null || jprop.Value.IsNull)
                {
                    if (AnimatedProperties.Remove(jprop.Key))
                    {
                        childPushers.Remove(jprop.Key);
                        RefreshProperties();
                    }

                    continue;
                }

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
                AddPointDef(p, jprop.Key, ev);
            }

            RefreshProperties();
        }

        public void RemoveEvent(BaseCustomEvent ev)
        {
            foreach (var prop in AnimatedProperties.Keys.ToList())
            {
                AnimatedProperties[prop].RemoveEvent(ev);
                if (AnimatedProperties[prop].IsEmpty())
                {
                    AnimatedProperties.Remove(prop);
                    childPushers.Remove(prop);
                }
            }
            RefreshProperties();
        }

        private void RefreshProperties()
        {
            properties = new IAnimateProperty[AnimatedProperties.Count];
            var i = 0;
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties[i++] = prop.Value;
            }

            DoUpdate("RefreshProperties");
        }

        private bool preload = false;

        // DIAGNOSTIC ONLY (b221 z+2 probe): identifies which entry point delivered each
        // `position` Add to the beat7 child; remove once confirmed.
        private string pushOrigin = "?";
        private int b7PushLogCount;

        public void Update() => DoUpdate("UnityUpdate");

        private void DoUpdate(string origin)
        {
            pushOrigin = origin;
            var time = Atsc.CurrentJsonTime;
            if (CachedChildren.Length == 0)
            {
                enabled = false;
                if (Animator != null) Animator.enabled = false;
                return;
            }
            var changed = false;
            for (var i = 0; i < properties.Length; ++i)
            {
                var prop = properties[i];
                if (time >= prop.StartTime)
                {
                    changed |= prop.UpdateProperty(time);
                }
                else
                {
                    changed |= prop.ResetEvaluatedValue();
                }
            }
            if (changed)
                UpdateVersion++;
        }

        public void AddChild(ObjectAnimator oa)
        {
            Children.Add(oa);
            OnChildrenChanged();
        }

        public void RemoveChild(ObjectAnimator oa)
        {
            Children.Remove(oa);
            OnChildrenChanged();
        }

        public void PushToChild(ObjectAnimator child)
        {
            pushOrigin = "PushToChild";
            foreach (var push in childPushers.Values)
                push(child);
        }

        public void OnChildrenChanged()
        {
            CachedChildren = Children.Where(o => o.enabled).ToArray();
            enabled = CachedChildren.Length > 0;
            if (Animator != null) Animator.enabled = enabled;
            Parents.ForEach((t) => t.OnChildrenChanged());
        }

        // TrackScrubParityTest: seeks land on the as-if-played state immediately, so re-push every property at
        // the sought time during OnTimeChangedEarly (before ObjectAnimator.OnTimeChanged applies). Playback keeps
        // the per-frame streaming push, so it must not double-push here.
        public void PushOnStoppedTimeChanged()
        {
            if (!isActiveAndEnabled || Atsc.IsPlaying) return;
            DoUpdate("Early");
        }

        // WorldCavesInEnvironmentTest reloads the same map several times in one session and found constructs
        // anchored to their parent track's previous-load end transform: named animation tracks persist for the
        // whole editor session, so a world-position-stays attach snapshots the stale animated offset and the
        // construct only rides later deltas, while a still-live chain even applies the new map's beat-0 values
        // before enhancements spawn. The game rebuilds all track state for every map load (no ParentObject or
        // track property survives a transition), so HardRefresh resets each named track to that fresh state.
        public void ResetForMapLoad()
        {
            // Local (not world) resets keep chained AssignTrackParent tracks order-independent: a child track
            // root lives under its parent track's ObjectParentTransform, so a world-space reset would bake the
            // not-yet-reset parent offset into the child's local transform.
            Track.SelfTransform.localPosition = Vector3.zero;
            Track.SelfTransform.localRotation = Quaternion.identity;
            Track.SelfTransform.localScale = Vector3.one;
            Track.ObjectParentTransform.localPosition = Vector3.zero;
            Track.ObjectParentTransform.localRotation = Quaternion.identity;
            Track.ObjectParentTransform.localScale = Vector3.one;

            AnimatedProperties.Clear();
            properties = Array.Empty<IAnimateProperty>();
            childPushers.Clear();
            UpdateVersion = 0;
            Parents.Clear();
            Children.Clear();
            CachedChildren = Array.Empty<ObjectAnimator>();
            ParentWorldPositionStays = false;
            enabled = false;
            if (Animator != null) Animator.enabled = false;
        }

        public void DestroyTrackBoundEnvironmentObjects()
        {
            foreach (var child in Children.ToArray())
            {
                child.DestroyTrackBoundEnvironmentTarget();
            }
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
            case "_dissolve":
            case "dissolve":
                AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.Opacity.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_dissolveArrow":
            case "dissolveArrow":
                AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            case "_localRotation":
            case "localRotation":
                AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => animator.LocalRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "rotation":
                AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => { if (animator.TargetType == ObjectAnimator.TargetTypes.Transform) animator.WorldRotation.Add(v); }, PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_rotation":
            case "offsetWorldRotation":
                AddPointDef<Quaternion>(source, (ObjectAnimator animator, Quaternion v) => animator.WorldRotation.Add(v), PointDataParsers.ParseQuaternion, p, Quaternion.identity);
                break;
            case "_position":
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.OffsetPosition.Add(v * BeatmapConstant.LaneSize), PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "offsetPosition":
                // Heck Noodle multiplies track position offsets by the 0.6 lane distance for
                // gameplay objects.
                // The lanesize mult matches that
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => { if (animator.TargetType == ObjectAnimator.TargetTypes.GameplayObject) animator.OffsetPosition.Add(v * BeatmapConstant.LaneSize); }, PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_localPosition":
                AddPointDef<Vector3>(
                    source,
                    (ObjectAnimator animator, Vector3 v) =>
                    {
                        if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                            animator.LocalPosition.Add(v * BeatmapConstant.LaneSize);
                    },
                    PointDataParsers.ParseVector3,
                    p,
                    Vector3.zero);
                break;
            case "localPosition":
                AddPointDef<Vector3>(
                    source,
                    (ObjectAnimator animator, Vector3 v) =>
                    {
                        if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                            animator.LocalPosition.Add(v);
                    },
                    PointDataParsers.ParseVector3,
                    p,
                    Vector3.zero);
                break;
            case "position":
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) =>
                {
                    if (animator.TargetType == ObjectAnimator.TargetTypes.Transform)
                    {
                        // DIAGNOSTIC ONLY: b221 z+2 probe — log bruh->beat7 position Adds around b222.
                        var diag = b7PushLogCount < 20 && gameObject.name == "bruh"
                            && animator.gameObject.name == "beat7"
                            && Atsc.CurrentJsonTime >= 220f && Atsc.CurrentJsonTime <= 223f;
                        if (diag)
                        {
                            ++b7PushLogCount;
                            Debug.Log($"[B7Push] f={Time.frameCount} t={Atsc.CurrentJsonTime:F3} " +
                                $"playing={Atsc.IsPlaying} origin={pushOrigin} val={v} " +
                                $"tgt={animator.gameObject.name} tgtEnabled={animator.enabled} " +
                                $"tgtActive={animator.isActiveAndEnabled} " +
                                $"wpBefore={animator.WorldPosition.Count}");
                        }
                        animator.WorldPosition.Add(v);
                        if (diag)
                        {
                            Debug.Log($"[B7Push] wpAfter={animator.WorldPosition.Count} " +
                                $"tgt={animator.gameObject.name}");
                        }
                    }
                }, PointDataParsers.ParseVector3, p, Vector3.zero);
                break;
            case "_scale":
            case "scale":
                AddPointDef<Vector3>(source, (ObjectAnimator animator, Vector3 v) => animator.Scale.Add(v), PointDataParsers.ParseVector3, p, Vector3.one);
                break;
            case "_color":
            case "color":
                AddPointDef<Color>(source, (ObjectAnimator animator, Color v) => { if (animator.TargetType != ObjectAnimator.TargetTypes.Transform) animator.Colors.Add(v); }, PointDataParsers.ParseColor, p, Color.white);
                break;
            case "_time":
            case "time":
                AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.SetLifeTime(f), PointDataParsers.ParseFloat, p, -1);
                break;
            case "interactable":
                // AdditionalAnimationParityTest.AnimatedInteractableParsesAndEvaluates: Heck registers
                // interactable as a track property; the preview evaluates it on the object's animator.
                AddPointDef<float>(source, (ObjectAnimator animator, float f) => animator.Interactable.Add(f), PointDataParsers.ParseFloat, p, 1);
                break;
            }
        }

        private void AddPointDef<T>(BaseCustomEvent source, Action<ObjectAnimator, T> _setter, PointDefinition<T>.Parser parser, IPointDefinition.UntypedParams p, T _default) where T : struct
        {
            if (AnimateProperty<T>.SkipsMissingPointDefinition(p)) return;

            Action<T> setter = (v) => { for (var i = 0; i < CachedChildren.Length; ++i) { _setter(CachedChildren[i], v); } };

            var animateProperty = GetAnimateProperty<T>(p.Key, setter, _default);
            animateProperty.AddPointDef(parser, p, source);
            childPushers[p.Key] = child =>
            {
                var time = Atsc.CurrentJsonTime;
                if (time >= animateProperty.StartTime)
                {
                    _setter(child, animateProperty.GetLerpedValue(time));
                }
            };
        }

        private AnimateProperty<T> GetAnimateProperty<T>(string key, Action<T> setter, T _default) where T : struct
        {
            if (!AnimatedProperties.ContainsKey(key)) {
                AnimatedProperties[key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>(),
                    setter,
                    _default
                );
            }
            return AnimatedProperties[key] as AnimateProperty<T>;
        }
    }
}
