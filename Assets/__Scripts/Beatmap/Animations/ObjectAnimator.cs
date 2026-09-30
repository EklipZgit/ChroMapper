using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using Random = UnityEngine.Random;

namespace Beatmap.Animations
{
    public class ObjectAnimator : MonoBehaviour
    {
        [SerializeField] public GameObject AnimationThis;
        [SerializeField] private ObjectContainer container;

        public BeatmapRuntimeContext Context;
        public Track AnimationTrack;
        public TracksManager TracksManager;

        [SerializeField] public Transform LocalTarget;
        public Transform WorldTarget;

        public readonly Aggregator<Quaternion> LocalRotation = new(Quaternion.identity, (a, b) => a * b);
        public Aggregator<Quaternion> WorldRotation = new(Quaternion.identity, (a, b) => a * b);
        public readonly Aggregator<Vector3> OffsetPosition = new(Vector3.zero, (a, b) => a + b);
        public readonly Aggregator<Vector3> LocalPosition = new(Vector3.zero, (a, b) => a + b);
        public readonly Aggregator<Vector3> WorldPosition = new(Vector3.zero, (a, b) => a + b);
        public readonly Aggregator<Vector3> Scale = new(Vector3.one, Vector3.Scale);
        public readonly Aggregator<Color> Colors = new(Color.white, (a, b) => a * b);
        public readonly Aggregator<float> Opacity = new(1f, (a, b) => a * b);
        public readonly Aggregator<float> OpacityArrow = new(1f, (a, b) => a * b);
        public readonly Aggregator<float> Interactable = new(1f, (a, b) => a * b);

        public bool AnimatedTrack { get; private set; }
        public bool AnimatedLife { get; private set; }
        public bool ShouldRecycle;
        private bool animatedColorApplied;
        private bool colorRestorePending;
        private bool disableNoteLook;

        public enum TargetTypes
        {
            None,
            GameplayObject,
            Transform,
            Material,
        };

        public TargetTypes TargetType;

        private List<TrackAnimator> tracks = new();

        private bool timeCallbacksSubscribed;

        private void SubscribeTimeCallbacks()
        {
            if (timeCallbacksSubscribed) return;
            Context.Atsc.OnTimeChanged += OnTimeChanged;
            Context.Atsc.OnTimeFlushPending += FlushPendingAnimations;
            timeCallbacksSubscribed = true;
        }

        // Environment enhancement tracks target existing scene transforms directly so their OEM parent-child
        // hierarchy is never flattened into GeometryContainer's single animation wrapper.
        private bool directEnvironmentTarget;
        private bool directEnvironmentTargetIsV2;
        private bool directEnvironmentTargetIsTrackLaneRing;
        private TrackLaneRing directEnvironmentTrackLaneRing;
        // ParametricBoxEnhancementTransformTest: resolve a tracked box once at attachment so each
        // animation update can refresh Chroma's authored pose without component discovery in LateUpdate.
        private ParametricBoxLight directEnvironmentBoxLight;
        private bool directEnvironmentHasBoxLight;
        // TrackScrubParityTest: animated direct environment targets overwrite the matched object's transform, so
        // seeking backward before the first event must restore the transform captured at attachment (the
        // as-if-restarted state) instead of leaving the last animated pose behind.
        private Vector3 directEnvironmentSpawnPosition;
        private Quaternion directEnvironmentSpawnRotation;
        private Vector3 directEnvironmentSpawnScale;
        private bool directEnvironmentEverApplied;
        private int directEnvironmentLastTrackUpdateVersion = -1;
        private bool trackParentTarget;
        private bool trackParentTargetIsV2;
        private TrackAnimator trackParentPropertySource;
        private int trackParentLastUpdateVersion = -1;
        // DIAGNOSTIC ONLY (b221 z+2 probe): bounds [B7Dis]/[B7Diag] log volume.
        private int b7DisLogCount;
        private int b7DiagLogCount;
        // True only while animating a material whose authored Standard+shaderKeywords:[] upgraded it
        // to unlit Glowing; the animation must keep the forced zero alpha rather than the JSON color's.
        private bool materialZeroAlpha;

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
        private static readonly int cutoutTexOffsetId = Shader.PropertyToID("_CutoutTexOffset");
        private static readonly int animSpawnedId = Shader.PropertyToID("_AnimationSpawned");

        public void ResetData()
        {
            AnimatedProperties.Clear();
            properties = Array.Empty<IAnimateProperty>();

            TargetType = TargetTypes.None;
            // EnvironmentEnhancementWith*Track* regression tests require pooled animators to discard cached direct
            // target state before they are attached to a different environment object.
            directEnvironmentTarget = false;
            directEnvironmentTargetIsV2 = false;
            directEnvironmentTargetIsTrackLaneRing = false;
            directEnvironmentTrackLaneRing = null;
            // ParametricBoxEnhancementTransformTest: pooled animators must release the prior target's
            // box override before another enhanced object attaches.
            directEnvironmentBoxLight = null;
            directEnvironmentHasBoxLight = false;
            directEnvironmentEverApplied = false;
            directEnvironmentLastTrackUpdateVersion = -1;
            directEnvironmentSpawnPosition = Vector3.zero;
            directEnvironmentSpawnRotation = Quaternion.identity;
            directEnvironmentSpawnScale = Vector3.one;
            trackParentTarget = false;
            trackParentTargetIsV2 = false;
            trackParentPropertySource = null;
            trackParentLastUpdateVersion = -1;
            materialZeroAlpha = false;

            OnDisable();
            tracks.Clear();

            if (AnimatedTrack)
            {
                if (container.transform.IsChildOf(AnimationTrack.transform))
                {
                    var track = TracksManager.GetTrackAtTime(
                        container.ObjectData?.SongBpmTime ?? 0,
                        container.ObjectData is BaseGrid grid ? grid.Rotation : 0);
                    track.AttachContainer(container);
                }

                TracksManager.Remove(AnimationTrack);
                AnimationTrack = null;
                AnimatedTrack = false;
            }

            LocalRotation.Reset();
            WorldRotation.Reset();
            OffsetPosition.Reset();
            LocalPosition.Reset();
            WorldPosition.Reset();
            Scale.Reset();
            Colors.Reset();
            // Unity containers need explicit null checks before reading their current material color.
            if (container != null)
            {
                Colors.Default = container.MpbController.Mpb.GetColor(colorId);
            }
            else
            {
                Colors.Default = Color.white;
            }
            Opacity.Reset();
            OpacityArrow.Reset();
            Interactable.Reset();
            animatedColorApplied = false;
            colorRestorePending = false;
            disableNoteLook = false;

            time = null;
            AnimatedLife = false;
            ShouldRecycle = false;

            if (LocalTarget != null)
            {
                LocalTarget.localEulerAngles = Vector3.zero;
                LocalTarget.localPosition = Vector3.zero;
                LocalTarget.localScale = Vector3.one;
            }

            if (container != null && !(container is GeometryContainer))
            {
                container.UpdateGridPosition();
                container.MpbController.Mpb.SetFloat(cutoutId, 0);
                container.MpbController.Mpb.SetVector(
                    cutoutTexOffsetId,
                    Random.insideUnitCircle * 10f);
                container.MpbController.Mpb.SetFloat(animSpawnedId, 0);
                if (container is NoteContainer nc)
                {
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 0);
                    nc.ArrowMpbController.Mpb.SetVector(
                        cutoutTexOffsetId,
                        Random.insideUnitCircle * 10f);
                    nc.DirectionTarget.localPosition = Vector3.zero;
                }

                container.UpdateMaterials();
            }
        }

        // TrackScrubParityTest: track object animators are disabled while their track has no children and
        // re-enabled once an object attaches; OnDisable removes the seek subscriptions below, so re-enabling
        // must restore them or a stopped seek stops reaching the track's object parent until a later frame.
        private void OnEnable()
        {
            if (Context == null || Context.Atsc == null) return;
            SubscribeTimeCallbacks();

            var reattached = false;
            foreach (var track in tracks)
            {
                if (track != null && !track.Children.Contains(this))
                {
                    track.AddChild(this);
                    track.PushToChild(this);
                    reattached = true;
                }
            }

            // SaltyOpeningSphereParityTest.BasicEventToPlayingAppliesDieBanishFromVisibleStart: while
            // this animator was disabled, workspace code reset the track-parent target's pose
            // (hehe/hehe2 proxy back to position 0) while the property-source track's UpdateVersion
            // stayed frozen, so the unchanged-version gate would skip the held WorldPosition write
            // forever. Track-parent animators are driven through the source track's Children list,
            // not `tracks`, so invalidate on re-enable itself rather than only on reattachment;
            // ordinary frames keep honoring the gate.
            if (trackParentTarget)
                trackParentLastUpdateVersion = -1;

            if (reattached)
                OnTimeChanged();
        }

        private void OnDisable()
        {
            // DIAGNOSTIC ONLY (b221 z+2 probe): whether beat7's animator holds an unflushed
            // WorldPosition entry across disable/unsubscribe. Gated to pending entries around b222.
            var diagB7 = b7DisLogCount < 12 && gameObject != null && gameObject.name == "beat7"
                && WorldPosition.Count > WorldPosition.Keep && Context != null && Context.Atsc != null
                && Context.Atsc.CurrentJsonTime >= 220f && Context.Atsc.CurrentJsonTime <= 223f;
            if (diagB7)
            {
                ++b7DisLogCount;
                Debug.Log($"[B7Dis] f={Time.frameCount} disable begin wpN={WorldPosition.Count}/" +
                    $"{WorldPosition.Keep} subscribed={timeCallbacksSubscribed}");
            }
            // SaltyBeat219PlacementParityTest.Beat221WhiteAndRedWallsSpanAtGameDepth: a track-parent
            // animator disabled while a streamed WorldPosition entry is still pending would carry it
            // across the unsubscribe, so the next stopped seek's push stacks on the stale entry.
            if (trackParentTarget)
            {
                FlushPendingAnimations();
            }
            if (Context != null && timeCallbacksSubscribed)
            {
                Context.Atsc.OnTimeChanged -= OnTimeChanged;
                Context.Atsc.OnTimeFlushPending -= FlushPendingAnimations;
                timeCallbacksSubscribed = false;
            }
            if (diagB7)
            {
                Debug.Log($"[B7Dis] f={Time.frameCount} disable end wpN={WorldPosition.Count}/" +
                    $"{WorldPosition.Keep}");
            }

            foreach (var track in tracks)
            {
                if (track != null)
                {
                    track.RemoveChild(this);
                }
            }
        }

        // TrackScrubParityTest: a stopped-time seek drops every transiently streamed value before the seek's
        // OnTimeChangedEarly push, so the pending state applied on OnTimeChanged is exactly the as-if-played
        // value for the sought time instead of folding in values pushed for the previous time.
        public void FlushPendingAnimations()
        {
            LocalRotation.Flush();
            WorldRotation.Flush();
            OffsetPosition.Flush();
            LocalPosition.Flush();
            WorldPosition.Flush();
            Scale.Flush();
            Colors.Flush();
            Opacity.Flush();
            OpacityArrow.Flush();
            Interactable.Flush();
            colorRestorePending |= animatedColorApplied;
        }

        public void AttachToObject(BaseGrid obj)
        {
            ResetData();

            TargetType = TargetTypes.GameplayObject;
            OffsetPosition.HoldUntilFlush = true;

            enabled = UIMode.AnimationMode && TracksManager != null;
            if (!enabled) return;

            obj.RecomputeSpawnParameters();
            disableNoteLook = obj.CustomData?.HasKey("disableNoteLook") == true
                && obj.CustomData["disableNoteLook"].AsBool;
            float duration;
            switch (container)
            {
                case ObstacleContainer obs:
                    duration = obs.ObstacleData.DurationSongBpmTime;
                    OffsetPosition.Preload(obs.ReadPosition() - new Vector3(0, 0, 0.25f));
                    // Track scale must multiply the authored visual scale, not identity, and the
                    // authored value is the correct reset for pooled reuse.
                    // Regression: SaltyBeat531TimingWindowParityTest.Beat531TimingWindowKeepsThinHollowSquare
                    Scale.Preload(obs.ObstacleData.CustomVisualScale);
                    break;
                case ArcContainer arc:
                    duration = arc.ArcData.DurationSongBpmTime;
                    break;
                case ChainContainer chain:
                    duration = chain.ChainData.DurationSongBpmTime;
                    break;
                default:
                    duration = 0f;
                    break;
            }

            if (obj.CustomLocalRotation is JSONNode rot) LocalRotation.Preload(Quaternion.Euler(rot.ReadVector3()));
            switch (obj.CustomWorldRotation)
            {
                case JSONArray wrot:
                    WorldRotation.Preload(Quaternion.Euler(wrot.ReadVector3()));
                    break;
                case JSONNumber yrot:
                    WorldRotation.Preload(Quaternion.Euler(0, yrot, 0));
                    break;
                default:
                    if (BeatSaberSongContainer.Instance.Map.MajorVersion == 4)
                        WorldRotation.Preload(Quaternion.Euler(0, obj.Rotation, 0));
                    break;
            }

            timeBegin = obj.SpawnSongBpmTime;
            // Can't use DespawnSongBpmTime because obstacles jump out early
            timeEnd = obj.SongBpmTime + duration + obj.HalfJumpDuration;

            RequireAnimationTrack();
            WorldTarget = AnimationTrack.transform;

            var bug = false;

            if (obj.CustomTrack != null)
            {
                var tracks = obj.CustomTrack switch
                {
                    JSONString s => new List<string> { s },
                    JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
                    _ => new List<string>()
                };
                foreach (var tr in tracks)
                {
                    AddParent(tr);

                    List<BaseCustomEvent> events = null;

                    BeatmapObjectContainerCollection
                        .GetCollectionForType<CustomEventGridContainer>(ObjectType.CustomEvent)
                        .EventsByTrack
                        ?.TryGetValue(tr, out events);
                    if (events == null) continue;

                    var map = BeatSaberSongContainer.Instance.Map;
                    foreach (var ce in events.Where(ev => ev.Type == "AssignPathAnimation"))
                    {
                        foreach (var jprop in ce.Data)
                        {
                            // AdditionalAnimationParityTest.NullPropertyErasesTheTrackProperty: a null path
                            // property erases it (Heck's GetPointData returns null and the property is not
                            // assigned), so the object keeps its un-animated path state.
                            if (jprop.Value == null || jprop.Value.IsNull) continue;

                            if (jprop.Key == "_definitePosition" || jprop.Key == "definitePosition") bug = true;
                            var p = new IPointDefinition.UntypedParams
                            {
                                Key = $"track_{jprop.Key}",
                                Overwrite = false,
                                Points = jprop.Value,
                                Easing = ce.DataEasing,
                                Time = ce.SongBpmTime,
                                Transition = ce.DataDuration ?? 0,
                                TimeBegin = timeBegin,
                                TimeEnd = timeEnd,
                            };
                            if (p.Transition != 0)
                            {
                                p.Transition = (float)map.JsonTimeToSongBpmTime(ce.JsonTime + p.Transition)
                                    - ce.SongBpmTime;
                            }

                            AddPointDef(p, jprop.Key, ce);
                        }
                    }
                }

                if (tracks.Count > 0)
                    AnimationTrack.transform.SetParent(this.tracks[0].Track.ObjectParentTransform, false);
            }

            // Individual Path Animation
            if (obj.CustomAnimation != null)
            {
                foreach (var jprop in obj.CustomAnimation.AsObject)
                {
                    if (jprop.Key == "_definitePosition" || jprop.Key == "definitePosition") bug = true;
                    var p = new IPointDefinition.UntypedParams
                    {
                        Key = jprop.Key,
                        Overwrite = true,
                        Points = jprop.Value,
                        Easing = null,
                        TimeBegin = timeBegin,
                        TimeEnd = timeEnd,
                    };
                    AddPointDef(p, jprop.Key, null);
                }
            }

            // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            if (bug
                && (obj.CustomData["_disableNoteGravity"]?.AsBool
                    ?? obj.CustomData["disableNoteGravity"]?.AsBool ?? false))
            {
                Debug.LogError("disableNoteGravity is bugged when combined with definitePosition, please remove it!");
                var position = AnimationTrack.ObjectParentTransform.localPosition;
                position.y = (position.y * -0.1f) + 1;
                AnimationTrack.ObjectParentTransform.localPosition = position;
            }

            properties = new IAnimateProperty[AnimatedProperties.Count];
            var i = 0;
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.Sort();
                properties[i++] = prop.Value;
            }

            Update();

            SubscribeTimeCallbacks();
        }

        public void AttachToGeometry(BaseEnvironmentEnhancement eh)
        {
            // Map version, rather than the static V2 serializer helper, determines legacy position unit conversion.
            var v2 = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
            ResetData();

            TargetType = TargetTypes.Transform;

            LocalTarget = AnimationThis.transform;
            //WorldTarget = container.transform;
            WorldTarget = AnimationThis.transform;

            WorldRotation = LocalRotation;

            if (eh.Scale is Vector3 scale) Scale.Default = scale;
            if (eh.Position is Vector3 p) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * p;
            if (eh.LocalPosition is Vector3 lp) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * lp;
            if (eh.Rotation is Vector3 r) LocalRotation.Default = Quaternion.Euler(r.x, r.y, r.z);
            if (eh.LocalRotation is Vector3 lr) LocalRotation.Default = Quaternion.Euler(lr.x, lr.y, lr.z);

            if (eh.Track != null)
            {
                AddParent(eh.Track);
                container.transform.SetParent(tracks[0].Track.ObjectParentTransform, false);
            }

            SubscribeTimeCallbacks();

            OnTimeChanged();
        }

        // EnvironmentEnhancementWith*Track* regression tests require missing track properties to preserve the spawn
        // transform, while authored properties directly overwrite each matched scene object as they do in Chroma.
        public void AttachToEnvironmentObject(
            Transform target,
            string track,
            bool v2,
            ParametricBoxLight boxLight)
        {
            ResetData();

            TargetType = TargetTypes.Transform;
            LocalTarget = target;
            WorldTarget = target;
            directEnvironmentTarget = true;
            directEnvironmentTargetIsV2 = v2;
            // ParametricBoxEnhancementTransformTest: GeometryContainer has already found the child
            // mesh, so animated transforms can recapture it without a per-frame hierarchy search.
            directEnvironmentBoxLight = boxLight;
            directEnvironmentHasBoxLight = boxLight != null;

            // Cache the optional native ring dependency during attachment so animated position updates can preserve
            // DefaultEnvironment's per-segment wave without performing component discovery in LateUpdate.
            directEnvironmentTrackLaneRing = target.GetComponent<TrackLaneRing>();
            directEnvironmentTargetIsTrackLaneRing = directEnvironmentTrackLaneRing != null;

            // TrackScrubParityTest: remember the authored transform (after the enhancement applied its own
            // position/scale/rotation) so seeking backward before the first event can restore it, exactly like a
            // freshly restarted map whose animation has not begun yet.
            directEnvironmentSpawnPosition = target.position;
            directEnvironmentSpawnRotation = target.rotation;
            directEnvironmentSpawnScale = target.localScale;

            AddParent(track);
            // WorldCavesInEnvironmentTest's constructs never rode their parent tracks: an enhanced object only
            // registered this data-level animator while AssignTrackParent physically moved the child track, so
            // notes and the camera rode but enhanced objects stayed put. In game, Noodle's ParentObject parents
            // every child-track object under the animated parent (ParentObject.Init -> ParentToObject), so once
            // this track has been assigned a parent the matched scene object must physically ride this track's
            // ObjectParentTransform just like note containers do. Tracks without an AssignTrackParent ancestor
            // stay purely data-level so EnvironmentEnhancementWith*Track* OEM-hierarchy parity is preserved.
            var trackAnimator = tracks[^1];
            if (trackAnimator.Parents.Count > 0)
            {
                LocalTarget.SetParent(
                    trackAnimator.Track.ObjectParentTransform,
                    trackAnimator.ParentWorldPositionStays);
            }

            SubscribeTimeCallbacks();
        }

        // WorldCavesInEnvironmentTest: AssignTrackParent must also parent enhanced objects that were already
        // attached to the child track when the event is processed (the editing flow), matching ParentObject's
        // parenting of every current child-track object.
        public void ParentDirectTargetToTrack(Transform trackParent, bool worldPositionStays)
        {
            if (!directEnvironmentTarget) return;
            LocalTarget.SetParent(trackParent, worldPositionStays);
        }

        internal void DestroyTrackBoundEnvironmentTarget()
        {
            if (!directEnvironmentTarget) return;
            enabled = false;
            if (LocalTarget == null) return;
            if (LocalTarget.parent == null || LocalTarget.parent.GetComponentInParent<Track>() == null) return;
            DestroyImmediate(LocalTarget.gameObject);
        }

        public void AttachToTrack(Track track, string name, bool isV2Map)
        {
            ResetData();

            TargetType = TargetTypes.Transform;

            trackParentTarget = true;
            trackParentTargetIsV2 = isV2Map;
            LocalTarget = track.ObjectParentTransform;
            WorldTarget = track.transform;

            SubscribeTimeCallbacks();
        }

        public void SetTrackParentMapVersion(bool isV2Map)
        {
            trackParentTargetIsV2 = isV2Map;
        }

        // CensoredFullMapVisibilityTest.Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround /
        // EnvironmentPositionPrecedenceTest.ParentWorldPositionIsAppliedAfterTrackRotation: a V3
        // track-parent target must write its held world position only when the track that sources
        // its properties actually updated; a change in an ancestor alone must carry the child
        // transform rather than re-anchor it (Heck TransformController.Update applies transforms
        // only when the source track UpdatedThisFrame).
        public void BindPropertySource(TrackAnimator source)
        {
            trackParentPropertySource = source;
            trackParentLastUpdateVersion = -1;
        }

        public void AttachToMaterial(GeometryContainer con, string track, bool zeroAlpha = false)
        {
            ResetData();
            materialZeroAlpha = zeroAlpha;

            TargetType = TargetTypes.Material;
            container = con;

            enabled = true;
            AddParent(track);
        }

        public void AddParent(string name)
        {
            var track = TracksManager.GetAnimationTrack(name);
            track.AddChild(this);
            tracks.Add(track);
        }

        private float? time;
        private float timeBegin;
        private float timeEnd;

        public void Update()
        {
            // Unity time controllers need explicit null checks before reading their playback time.
            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);

            if (container != null && container.ObjectData is BaseGrid obj)
            {
                var noodleAnimationLifetime = time > timeEnd ? -1 : 1;
                if (!(container is ChainContainer))
                {
                    // Unity containers need explicit null checks before updating spawned-state shader values.
                    container.MpbController.Mpb.SetFloat(
                        animSpawnedId,
                        noodleAnimationLifetime);
                    if (container is NoteContainer nc)
                    {
                        nc.ArrowMpbController.Mpb.SetFloat(
                            animSpawnedId,
                            noodleAnimationLifetime);
                    }
                }

                AnimatedLife =
                    (this.time != null && this.time < obj.SongBpmTime)
                    || WorldPosition.Count > 0
                    || (obj.CustomFake && time < timeEnd);
                if (ShouldRecycle)
                {
                    var despawnTime = WorldPosition.Count == 0 && !obj.CustomFake
                        ? obj.SongBpmTime
                        : timeEnd;
                    if (time > despawnTime)
                    {
                        BeatmapObjectContainerCollection
                            .GetCollectionForType(container.ObjectData.ObjectType)
                            .RecycleContainer(container.ObjectData);
                        AnimatedLife = false;
                        return;
                    }
                }
            }

            var l = properties.Length;
            for (var i = 0; i < l; ++i)
            {
                var prop = properties[i];
                if (time >= prop.StartTime) prop.UpdateProperty(time);
            }

            if (AnimatedTrack) AnimationTrack.UpdateTime(time);
        }

        // TODO(KamikaziIn): temporary diagnostic sampler for the deployed-build "light array renders nothing"
        // investigation. Logs one geometry animator's state every ~4 seconds of frames; remove once resolved.
        // TODO DIAG REMOVE
        private static int geometryDiagnosticFrame;
        private static float nextGeometryDiagnosticLog;
        private static ObjectAnimator geometryDiagnosticSample;
        private static float geometryDiagnosticFirstEvent = float.PositiveInfinity;

        public void LateUpdate()
        {
            // Direct environment targets apply only properties supplied by AnimateTrack. Reading aggregator defaults
            // here would incorrectly reset absent position, rotation, or scale fields on empty and scale-only events.
            if (directEnvironmentTarget)
            {
                ApplyDirectEnvironmentTargets();
                return;
            }

            // TODO(KamikaziIn): temporary diagnostic sampling one geometry light every ~240 frames so the
            // deployed build's log shows whether the array's animation and renderer state are alive. The
            // sample prefers a track whose first event starts by beat 20 (a wave-1 light) because later-wave
            // lights are legitimately parked until their own event beat.
            if (container is GeometryContainer && TargetType == TargetTypes.Transform)
            {
                if (geometryDiagnosticSample == null
                    || (geometryDiagnosticFirstEvent > 20f && tracks.Count > 0
                        && tracks[0].AnimatedProperties.Count > 0))
                {
                    geometryDiagnosticSample = this;
                    // MaterialTrackAnimationTest found trackless geometry animators throwing
                    // ArgumentOutOfRangeException here every frame (tracks[0] on an empty list); the sampler
                    // still needs to log trackless containers, so only index tracks when one is attached.
                    geometryDiagnosticFirstEvent = tracks.Count > 0
                        ? tracks[0].AnimatedProperties.Values
                            .DefaultIfEmpty().Min(property => property?.StartTime ?? float.PositiveInfinity)
                        : float.PositiveInfinity;
                }

                if (ReferenceEquals(this, geometryDiagnosticSample) && Time.frameCount >= nextGeometryDiagnosticLog)
                {
                    geometryDiagnosticFrame += 1;
                    nextGeometryDiagnosticLog = Time.frameCount + 240;
                    var shapeRenderer = container.MpbController != null && container.MpbController.Renderers.Count > 0
                        ? container.MpbController.Renderers[0]
                        : null;
                    // SaltyFullMapParityTest.FreshMapperLoadOpeningSphereAndRingState: the live t6 log
                    // showed a trackless LightTransparent animator and cannot distinguish a hidden
                    // sphere from a stray material animator; log enhancement track, transform and
                    // renderer world state (all inside the existing ~240-frame throttle only).
                    var enhancement = container as GeometryContainer;
                    var parent = container.transform.parent;
                    Debug.Log(
                        $"[GeoDiag] t={Context.Atsc.CurrentJsonTime:F2} pos={LocalTarget.position} " +
                        $"worldPosCount={WorldPosition.Count} offsetCount={OffsetPosition.Count} " +
                        $"enhTrack={(enhancement != null && enhancement.EnvironmentEnhancement != null ? enhancement.EnvironmentEnhancement.Track ?? "<null>" : "<n/a>")} " +
                        $"targetType={TargetType} " +
                        $"containerPos={container.transform.position} containerLossyScale={container.transform.lossyScale} " +
                        $"parent={(parent != null ? $"{parent.name} localPos={parent.localPosition} localScale={parent.localScale} lossyScale={parent.lossyScale}" : "<none>")} " +
                        $"rendererEnabled={(shapeRenderer != null ? shapeRenderer.enabled.ToString() : "<no shape>")} " +
                        $"rendererState={(shapeRenderer != null ? $"pos={shapeRenderer.transform.position} lossyScale={shapeRenderer.transform.lossyScale} bounds={shapeRenderer.bounds} active={shapeRenderer.gameObject.activeInHierarchy}" : "<none>")} " +
                        $"mat={(shapeRenderer != null && shapeRenderer.sharedMaterial != null ? shapeRenderer.sharedMaterial.name : "<null>")} " +
                        $"animationMode={UIMode.AnimationMode} " +
                        (tracks.Count > 0
                            ? $"track={tracks[0].name} firstEventBeat={geometryDiagnosticFirstEvent:F1} tickEnabled={tracks[0].enabled} cachedChildren={tracks[0].CachedChildren.Length} props={tracks[0].AnimatedProperties.Count}"
                            : "track=<none>"));
                }
            }

            if (LocalRotation.Count > 0) LocalTarget.localRotation = LocalRotation.Get();

            // EnvironmentPositionPrecedenceTest: geometry and parent proxies choose
            // their version-specific position source before writing either transform.
            // TODO THIS LOOKS GHETTO, NO BETTER WAY THAN THE BEFORE AND AFTER GeometryContainer CHECK?
            if (trackParentTarget || (TargetType == TargetTypes.Transform && container is GeometryContainer))
            {
                // EnvironmentPositionPrecedenceTest.ParentWorldPositionIsAppliedAfterTrackRotation:
                // a V3 track-parent target's own world rotation must land before its world-position
                // write, otherwise the position just written gets rotated back around the track
                // node when the rotation applies a few lines below.
                if (trackParentTarget && !trackParentTargetIsV2 && WorldTarget is Transform && WorldRotation.Count > 0)
                    WorldTarget.localRotation = WorldRotation.Get();
                ApplyTransformPosition(false);
            }
            else
            {
                var hasLocalPosition = LocalPosition.Count > 0;
                var localPosition = hasLocalPosition ? LocalPosition.Get() : Vector3.zero;
                var hasOffsetPosition = OffsetPosition.Count > 0;
                var offsetPosition = hasOffsetPosition ? OffsetPosition.Get() : Vector3.zero;
                if (hasLocalPosition)
                    LocalTarget.localPosition = localPosition;
                else if (hasOffsetPosition)
                    LocalTarget.localPosition = offsetPosition;
            }

            if (Scale.Count > 0) LocalTarget.localScale = Scale.Get();

            // EnvironmentPositionPrecedenceTest.ParentWorldPositionIsAppliedAfterTrackRotation: the
            // V3 track-parent rotation was already applied before its position write above; a second
            // Get() here reads the drained aggregator and resets the rotation to identity.
            if (WorldTarget is Transform && WorldRotation.Count > 0)
                if (container is not GeometryContainer && (!trackParentTarget || trackParentTargetIsV2))
                    WorldTarget.localRotation = WorldRotation.Get();

            // Unity time controllers need explicit null checks before reading their playback time.
            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);
            if (!trackParentTarget && container is not GeometryContainer && WorldPosition.Count > 0)
            {
                if (timeBegin < time && time < timeEnd) AnimationTrack.HoldDefinitePosition();
                if (container is not null and not GeometryContainer)
                    container.transform.localPosition = WorldPosition.Get();
                else
                    WorldTarget.localPosition = WorldPosition.Get();
            }

            if (container is ObjectContainer && (Colors.Count > 0 || OpacityArrow.Count > 0 || Opacity.Count > 0 || colorRestorePending))
            {
                if (Colors.Count > 0)
                {
                    var color = Colors.Get();
                    if (materialZeroAlpha) color.a = 0f;
                    switch (container)
                    {
                        case ObstacleContainer obstacle:
                            obstacle.SetColor(color);
                            break;
                        case ChainContainer chain:
                            chain.SetColor(color);
                            break;
                        case NoteContainer note:
                            note.SetColor(color);
                            break;
                        default:
                            container.MpbController.Mpb.SetColor(colorId, color);
                            break;
                    }

                    animatedColorApplied = true;
                }
                else if (colorRestorePending)
                {
                    RestoreAuthoredColor();
                    animatedColorApplied = false;
                }

                colorRestorePending = false;

                if (container is NoteContainer nc)
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 1f - OpacityArrow.Get());

                container.MpbController.Mpb.SetFloat(cutoutId, 1f - Opacity.Get());
                container.UpdateMaterials();
            }

            if (UIMode.PreviewMode && !disableNoteLook && container is NoteContainer lookNote)
                ApplyNoteLook(lookNote, time);
        }

        private void ApplyNoteLook(NoteContainer note, float beat)
        {
            var data = note.NoteData;
            var window = data.SongBpmTime - data.SpawnSongBpmTime;
            var blend = window > Mathf.Epsilon
                ? Mathf.Clamp01((beat - data.SpawnSongBpmTime) / window)
                : 1f;
            if (blend <= 0f) return;

            var headPos = TracksManager.CameraManager.SelectedCameraController.transform.position;
            var target = note.DirectionTarget.position;
            headPos.y = Mathf.Lerp(headPos.y, target.y, 0.8f);
            var direction = target - headPos;
            if (direction.sqrMagnitude < 0.0001f) return;
            var baseRotation = note.DirectionTarget.parent.rotation
                * Quaternion.Euler(note.DirectionTargetEuler);
            var look = Quaternion.LookRotation(direction, baseRotation * Vector3.up);
            note.DirectionTarget.rotation = Quaternion.Slerp(baseRotation, look, blend);
        }

        private void RestoreAuthoredColor()
        {
            var scheme = Context != null ? Context.ColorScheme : null;
            switch (container)
            {
                case NoteContainer note:
                {
                    var data = note.NoteData;
                    if (data == null) break;
                    if (data.CustomColor is Color customColor)
                    {
                        note.SetColor(customColor);
                    }
                    else if (scheme != null
                        && (data.Type == (int)NoteType.Red || data.Type == (int)NoteType.Blue))
                    {
                        note.SetColor(data.Type == (int)NoteType.Red
                            ? scheme.LeftNoteColor
                            : scheme.RightNoteColor);
                    }
                    else
                    {
                        note.SetColor(null);
                    }
                    break;
                }
                case ChainContainer chain:
                {
                    var data = chain.ChainData;
                    if (data == null) break;
                    if (data.CustomColor is Color customColor)
                    {
                        chain.SetColor(customColor);
                    }
                    else if (scheme != null
                        && (data.Color == (int)NoteColor.Red || data.Color == (int)NoteColor.Blue))
                    {
                        chain.SetColor(data.Color == (int)NoteColor.Red
                            ? scheme.LeftNoteColor
                            : scheme.RightNoteColor);
                    }
                    break;
                }
            }
        }

        private bool ApplyDirectEnvironmentTargets()
        {
            var applied = false;
            var positionChanged = false;
            var scaleChanged = false;

            var hasLocalRotation = LocalRotation.Count > 0;
            var localRotation = hasLocalRotation ? LocalRotation.Get() : Quaternion.identity;

            var hasLocalPosition = LocalPosition.Count > 0;
            var localPosition = hasLocalPosition ? LocalPosition.Get() : Vector3.zero;
            var hasWorldPosition = WorldPosition.Count > 0;
            var worldPosition = hasWorldPosition ? WorldPosition.Get() : Vector3.zero;
            var hasOffsetPosition = OffsetPosition.Count > 0;
            var offsetPosition = hasOffsetPosition ? OffsetPosition.Get() : Vector3.zero;
            var hasScale = Scale.Count > 0;
            var scale = hasScale ? Scale.Get() : Vector3.one;
            var hasWorldRotation = WorldRotation.Count > 0;
            var worldRotation = hasWorldRotation ? WorldRotation.Get() : Quaternion.identity;

            var hasProperty = hasLocalRotation || hasLocalPosition || hasWorldPosition
                || hasOffsetPosition || hasScale || hasWorldRotation;
            var updateVersion = tracks[0].UpdateVersion;
            if (directEnvironmentLastTrackUpdateVersion == updateVersion)
            {
                return hasProperty;
            }
            directEnvironmentLastTrackUpdateVersion = updateVersion;

            if (hasLocalRotation)
            {
                LocalTarget.localRotation = localRotation;
                applied = true;
            }
            if (hasLocalPosition || hasWorldPosition || hasOffsetPosition)
            {
                var selectedPosition = hasLocalPosition
                    ? localPosition
                    : hasWorldPosition
                        ? worldPosition
                        : offsetPosition;
                ApplyDirectEnvironmentPosition(
                    selectedPosition,
                    !hasLocalPosition && (hasWorldPosition || directEnvironmentTargetIsV2));
                positionChanged = true;
                applied = true;
            }

            if (hasScale)
            {
                LocalTarget.localScale = scale;
                scaleChanged = true;
                applied = true;
            }

            if (hasWorldRotation)
            {
                WorldTarget.rotation = worldRotation;
                applied = true;
            }

            // ParametricBoxEnhancementTransformTest: only animated properties replace the matching
            // saved override; untouched dimensions continue to follow their native light controller.
            if (directEnvironmentHasBoxLight)
            {
                if (positionChanged)
                    directEnvironmentBoxLight.CaptureAuthoredPosition();
                if (scaleChanged)
                    directEnvironmentBoxLight.CaptureAuthoredScale();
            }

            if (applied) directEnvironmentEverApplied = true;
            return applied;
        }

        private void ApplyTransformPosition(bool includeSpawnDefault)
        {
            var hasLocalPosition = LocalPosition.Count > 0;
            var localPosition = hasLocalPosition ? LocalPosition.Get() : Vector3.zero;
            var hasWorldPosition = WorldPosition.Count > 0;
            var worldPosition = hasWorldPosition ? WorldPosition.Get() : Vector3.zero;
            var hasOffsetPosition = OffsetPosition.Count > 0;
            var offsetPosition = hasOffsetPosition || includeSpawnDefault
                ? OffsetPosition.Get()
                : Vector3.zero;

            if (trackParentTargetIsV2)
            {
                if (hasOffsetPosition || includeSpawnDefault)
                    LocalTarget.localPosition = offsetPosition;
                return;
            }

            if (hasLocalPosition)
                LocalTarget.localPosition = localPosition;
            else if (hasWorldPosition)
            {
                if (trackParentTarget)
                {
                    var source = trackParentPropertySource;
                    // CensoredFullMapVisibilityTest.Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround /
                    // EnvironmentPositionPrecedenceTest.ParentWorldPositionIsAppliedAfterTrackRotation:
                    // hold the world-space write, but only re-apply it when the property-source track
                    // updated — an ancestor moving on its own must carry the child (censson's b124
                    // 180° rotation must centre redcen), not re-anchor the stale authored position.
                    if (source == null || source.UpdateVersion != trackParentLastUpdateVersion)
                    {
                        // DIAGNOSTIC ONLY (b221 z+2 order-dependent probe): log the track-parent
                        // world-position compose for the "beat7" track around b222. Remove once
                        // the ordering root cause is confirmed.
                        var diagB7 = b7DiagLogCount < 12 && gameObject.name == "beat7" && Context.Atsc != null
                            && Context.Atsc.CurrentSongBpmTime >= 220f && Context.Atsc.CurrentSongBpmTime <= 223f;
                        if (diagB7)
                        {
                            ++b7DiagLogCount;
                            Debug.Log($"[B7Diag] write before wp={worldPosition} " +
                                $"wt={WorldTarget.position} lt={LocalTarget.position} " +
                                $"ltl={LocalTarget.localPosition} srcVer={(source == null ? -1 : source.UpdateVersion)} " +
                                $"lastVer={trackParentLastUpdateVersion} incDef={includeSpawnDefault} " +
                                $"t={Context.Atsc.CurrentSongBpmTime:F3}");
                        }
                        LocalTarget.position = worldPosition + WorldTarget.position;
                        if (diagB7) Debug.Log($"[B7Diag] write after lt={LocalTarget.position}");
                        if (source != null)
                            trackParentLastUpdateVersion = source.UpdateVersion;
                    }
                }
                else
                    WorldTarget.position = worldPosition;
            }
            else if (hasOffsetPosition)
            {
                if (trackParentTarget)
                    LocalTarget.localPosition = offsetPosition;
                else
                    WorldTarget.position = offsetPosition;
            }
            else if (includeSpawnDefault)
                LocalTarget.localPosition = offsetPosition;
        }

        // TrackScrubParityTest: a stopped seek that lands before the first event of every property must restore
        // the transform captured at attachment (the map-restart state). Native rings keep their wave state, so
        // their base is rebased instead of assigning the transform directly.
        private void RestoreDirectEnvironmentSpawnPose()
        {
            if (directEnvironmentTargetIsTrackLaneRing && directEnvironmentTrackLaneRing != null)
            {
                var localPosition = LocalTarget.parent != null
                    ? LocalTarget.parent.InverseTransformPoint(directEnvironmentSpawnPosition)
                    : directEnvironmentSpawnPosition;
                directEnvironmentTrackLaneRing.RebasePositionOffset(localPosition);
                LocalTarget.rotation = directEnvironmentSpawnRotation;
                LocalTarget.localScale = directEnvironmentSpawnScale;
                // ParametricBoxEnhancementTransformTest: seeking before animation restores the
                // saved mesh overrides to the spawn pose before another light refresh occurs.
                if (directEnvironmentHasBoxLight)
                    directEnvironmentBoxLight.RecaptureAuthoredTransform();
                return;
            }

            LocalTarget.SetPositionAndRotation(directEnvironmentSpawnPosition, directEnvironmentSpawnRotation);
            LocalTarget.localScale = directEnvironmentSpawnScale;
            // ParametricBoxEnhancementTransformTest: a seek must not keep the last animated box
            // override after the scene transform itself has returned to its authored spawn pose.
            if (directEnvironmentHasBoxLight)
                directEnvironmentBoxLight.RecaptureAuthoredTransform();
        }

        // Chroma treats an animated environment position as the TrackLaneRing's new base and retains its current wave
        // displacement; assigning Transform.position directly would instead stack every segment at the animated point.
        private void ApplyDirectEnvironmentPosition(Vector3 position, bool worldSpace)
        {
            if (directEnvironmentTargetIsTrackLaneRing)
            {
                var localPosition = worldSpace && LocalTarget.parent != null
                    ? LocalTarget.parent.InverseTransformPoint(position)
                    : position;
                directEnvironmentTrackLaneRing.RebasePositionOffset(localPosition);
                return;
            }

            if (worldSpace)
            {
                LocalTarget.position = position;
            }
            else
            {
                LocalTarget.localPosition = position;
            }
        }

        public void SetLifeTime(float normalTime)
        {
            time = normalTime < 0
                ? null
                : Mathf.LerpUnclamped(timeBegin, timeEnd, normalTime);
        }

        private void OnTimeChanged()
        {
            if (Context.Atsc.IsPlaying) return;

            if (directEnvironmentTarget)
            {
                if (!ApplyDirectEnvironmentTargets() && directEnvironmentEverApplied)
                {
                    RestoreDirectEnvironmentSpawnPose();
                    directEnvironmentEverApplied = false;
                }

                return;
            }

            LocalTarget.localRotation = LocalRotation.Get();

            if (trackParentTarget || (TargetType == TargetTypes.Transform && container is GeometryContainer))
            {
                // EnvironmentPositionPrecedenceTest.ParentWorldPositionIsAppliedAfterTrackRotation:
                // same stopped-seek ordering as LateUpdate — own world rotation before the V3
                // track-parent world-position write.
                if (trackParentTarget && !trackParentTargetIsV2 && WorldTarget is Transform)
                    WorldTarget.localRotation = WorldRotation.Get();
                ApplyTransformPosition(true);
            }
            else
            {
                var offsetPosition = OffsetPosition.Get();
                LocalTarget.localPosition = LocalPosition.Count > 0
                    ? LocalPosition.Get()
                    : offsetPosition;
            }

            LocalTarget.localScale = Scale.Get();

            if (TargetType == TargetTypes.Transform && container == null && !trackParentTarget
                && WorldPosition.Count > 0)
            {
                WorldTarget.localPosition = WorldPosition.Get();
            }

            if (WorldTarget is Transform)
            {
                // EnvironmentPositionPrecedenceTest.ParentWorldPositionIsAppliedAfterTrackRotation:
                // skip the drained second Get() for V3 track-parent targets — their rotation was
                // applied before the world-position write above.
                if (!(container is GeometryContainer) && (!trackParentTarget || trackParentTargetIsV2))
                    WorldTarget.localRotation = WorldRotation.Get();
            }
        }

        private void RequireAnimationTrack()
        {
            if (AnimationTrack == null)
            {
                AnimationTrack = TracksManager.CreateIndividualTrack(container.ObjectData as BaseGrid);
                AnimationTrack.AttachContainer(container);
                AnimationTrack.ObjectParentTransform.localPosition = new Vector3(
                    container.transform.localPosition.x,
                    container.transform.localPosition.y,
                    0);
                AnimationTrack.transform.localPosition = Vector3.zero;
                container.transform.localPosition = Vector3.zero;
                AnimatedTrack = true;
            }
        }

        // Only used for gameplay objects?
        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
                case "_dissolve":
                case "dissolve":
                    AddPointDef(source, f => Opacity.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case "_dissolveArrow":
                case "dissolveArrow":
                    AddPointDef(source, f => OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case "_localRotation":
                case "localRotation":
                    AddPointDef(
                        source,
                        q => LocalRotation.Add(q),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case "_rotation":
                case "offsetWorldRotation":
                    AddPointDef(
                        source,
                        v => WorldRotation.Add(v),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case "_position":
                case "offsetPosition":
                    AddPointDef(
                        source,
                        v => OffsetPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case "_definitePosition":
                case "definitePosition":
                    AddPointDef(
                        source,
                        v => WorldPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case "_scale":
                case "scale":
                    AddPointDef(
                        source,
                        v => Scale.Add(v),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.one);
                    break;
                case "_color":
                case "color":
                    AddPointDef<Color>(source, (Color c) => Colors.Add(c), PointDataParsers.ParseColor, p, Color.white);
                    break;
                case "_interactable":
                case "interactable":
                    AddPointDef(source, f => Interactable.Add(f), PointDataParsers.ParseFloat, p, 1);
                    break;
            }
        }

        private void AddPointDef<T>(
            BaseCustomEvent source,
            Action<T> setter,
            PointDefinition<T>.Parser parser,
            IPointDefinition.UntypedParams p,
            T @default) where T : struct
        {
            if (AnimateProperty<T>.SkipsMissingPointDefinition(p)) return;

            try
            {
                if (p.Overwrite)
                {
                    AnimatedProperties[p.Key] = new AnimateProperty<T>(
                        new List<PointDefinition<T>>(),
                        setter,
                        @default
                    );
                }

                GetAnimateProperty(p.Key, setter, @default).AddPointDef(parser, p, source);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private AnimateProperty<T> GetAnimateProperty<T>(string key, Action<T> setter, T @default) where T : struct
        {
            if (!AnimatedProperties.ContainsKey(key))
            {
                AnimatedProperties[key] = new AnimateProperty<T>(
                    new List<PointDefinition<T>>(),
                    setter,
                    @default
                );
            }

            return AnimatedProperties[key] as AnimateProperty<T>;
        }

        private static float minWall = 0.06f;

        private static float WallClamp(float a)
        {
            if (-minWall < a && a < minWall) return minWall;

            return a;
        }

        // I should never be allowed to use a profiler
        public class Aggregator<T> where T : struct
        {
            public int Count;
            public readonly Func<T, T, T> Func;
            public T Default;
            private readonly T instancedDefault;
            public int Keep;

            public Aggregator(T def, Func<T, T, T> func)
            {
                Default = def;
                instancedDefault = Default;
                Func = func;
            }

            public void Add(T v)
            {
                // This shouldn't ever go above 3, but check anyway
                if (Count >= 4)
                    return;
                else
                    items[Count] = v;
                ++Count;
            }

            public void Preload(T v)
            {
                Add(v);
                ++Keep;
            }

            public bool HoldUntilFlush;
            private bool hasHeldValue;
            private T heldValue;

            public T Get()
            {
                if (HoldUntilFlush && hasHeldValue && Count == Keep) return heldValue;
                if (Count == 0) return Default;
                var value = items[0];
                for (var i = 1; i < Count; ++i) value = Func(value, items[i]);

                if (HoldUntilFlush && Count > Keep)
                {
                    heldValue = value;
                    hasHeldValue = true;
                }
                Count = Keep;
                return value;
            }

            public void Flush()
            {
                Count = Keep;
                hasHeldValue = false;
            }

            public void Reset()
            {
                Default = instancedDefault;
                Count = 0;
                Keep = 0;
                HoldUntilFlush = false;
                hasHeldValue = false;
                heldValue = default;
                for (var i = 0; i < items.Length; i++) items[i] = default;
            }

            private readonly T[] items = new T[4];
        }
    }
}
