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
        private bool deferColorUntilBirth;
        private float colorBirthTime;

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

        private bool directEnvironmentTarget;
        private bool directEnvironmentTargetIsV2;
        private bool directEnvironmentTargetIsTrackLaneRing;
        private TrackLaneRing directEnvironmentTrackLaneRing;
        // Keep the light's mesh reference instead of searching for it during animation updates.
        private ParametricBoxLight directEnvironmentBoxLight;
        private bool directEnvironmentHasBoxLight;
        // Save the authored pose so seeks before the first event can restore it.
        private Vector3 directEnvironmentSpawnPosition;
        private Quaternion directEnvironmentSpawnRotation;
        private Vector3 directEnvironmentSpawnScale;
        private ObjectAnimator directEnvironmentParentAnimator;
        private bool directEnvironmentHasParentAnimator;
        private Matrix4x4 directEnvironmentParentMatrix;
        private Vector3 directEnvironmentSpawnLocalPosition;
        private Transform directEnvironmentOriginalParent;
        private bool directEnvironmentEverApplied;
        private int[] directEnvironmentTrackVersions = Array.Empty<int>();
        private bool trackParentTarget;
        private TrackAnimator controlledTrack;
        private bool trackParentTargetIsV2;
        private TrackAnimator trackParentPropertySource;
        private int trackParentLastUpdateVersion = -1;
        private ObjectAnimator trackParentAncestor;
        private bool trackParentHasAncestor;
        private Matrix4x4 trackParentRootMatrix;
        private Vector3 trackParentSpawnPosition;
        private Quaternion trackParentSpawnRotation;
        private Vector3 trackParentSpawnScale;
        private AnimateProperty<Vector3> geometryWorldPositionProperty;
        private ObjectAnimator geometryParentAnimator;
        private bool geometryHasParentAnimator;
        private bool geometryHasWorldPositionAnchor;
        private int geometryAnchorUpdateVersion = -1;
        private int[] geometryTrackVersions = Array.Empty<int>();
        private float geometryAnchorTime;
        private Vector3 geometryAnchoredLocalPosition;
        private bool geometryRotationAnchorReady;
        private float geometryRotationAnchorTime;
        private Quaternion geometryAnchoredLocalRotation;
        private Quaternion geometryAnchorWorldRotation;
        private static readonly string[] GeometryTransformKeys =
        {
            AnimationPropertyKeys.PositionKey, AnimationPropertyKeys.LegacyPositionKey,
            AnimationPropertyKeys.LocalPositionKey, AnimationPropertyKeys.LegacyLocalPositionKey,
            AnimationPropertyKeys.RotationKey, AnimationPropertyKeys.LegacyRotationKey,
            AnimationPropertyKeys.LocalRotationKey, AnimationPropertyKeys.LegacyLocalRotationKey,
            AnimationPropertyKeys.ScaleKey, AnimationPropertyKeys.LegacyScaleKey
        };
        // Materials upgraded from Standard to unlit Glowing must keep zero alpha during color animation.
        private bool materialZeroAlpha;
        private JSONNode materialTrackData;
        private AnimateProperty<Color>[] materialPathColors = Array.Empty<AnimateProperty<Color>>();
        private AnimateProperty<Color>[] chainPathColors = Array.Empty<AnimateProperty<Color>>();

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
        private static readonly int arrowDissolveId = Shader.PropertyToID("_ArrowDissolve");
        private static readonly int cutoutTexOffsetId = Shader.PropertyToID("_CutoutTexOffset");
        private static readonly int animSpawnedId = Shader.PropertyToID("_AnimationSpawned");

        public void ResetData()
        {
            AnimatedProperties.Clear();
            properties = Array.Empty<IAnimateProperty>();

            TargetType = TargetTypes.None;
            // Pooled animators must discard their previous target before attachment.
            directEnvironmentTarget = false;
            directEnvironmentTargetIsV2 = false;
            directEnvironmentTargetIsTrackLaneRing = false;
            directEnvironmentTrackLaneRing = null;
            directEnvironmentBoxLight = null;
            directEnvironmentHasBoxLight = false;
            directEnvironmentEverApplied = false;
            directEnvironmentTrackVersions = Array.Empty<int>();
            directEnvironmentSpawnPosition = Vector3.zero;
            directEnvironmentSpawnRotation = Quaternion.identity;
            directEnvironmentSpawnScale = Vector3.one;
            directEnvironmentParentAnimator = null;
            directEnvironmentOriginalParent = null;
            directEnvironmentHasParentAnimator = false;
            trackParentTarget = false;
            controlledTrack = null;
            trackParentTargetIsV2 = false;
            trackParentPropertySource = null;
            trackParentLastUpdateVersion = -1;
            trackParentAncestor = null;
            trackParentHasAncestor = false;
            geometryWorldPositionProperty = null;
            geometryParentAnimator = null;
            geometryHasParentAnimator = false;
            geometryHasWorldPositionAnchor = false;
            geometryRotationAnchorReady = false;
            geometryAnchorUpdateVersion = -1;
            geometryTrackVersions = Array.Empty<int>();
            materialZeroAlpha = false;
            materialTrackData = null;
            materialPathColors = Array.Empty<AnimateProperty<Color>>();
            chainPathColors = Array.Empty<AnimateProperty<Color>>();

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
            deferColorUntilBirth = false;

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
                    Random.onUnitSphere * 10f);
                container.MpbController.Mpb.SetFloat(animSpawnedId, 0);
                if (container is NoteContainer nc)
                {
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 0);
                    nc.ArrowMpbController.Mpb.SetFloat(arrowDissolveId, 1f);
                    nc.ArrowMpbController.Mpb.SetVector(
                        cutoutTexOffsetId,
                        Random.onUnitSphere * 10f);
                    nc.DirectionTarget.localPosition = Vector3.zero;
                }

                container.UpdateMaterials();
            }
        }

        // Disabled animators unsubscribe from seeks; restore those subscriptions when they become active.
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

            // The target may have been reset while disabled without its source track changing.
            // Force the next update to reapply the held pose
            if (trackParentTarget)
                trackParentLastUpdateVersion = -1;

            if (reattached)
                OnTimeChanged();
        }

        private void OnDisable()
        {
            // Discard pending track values on disable so the next seek cannot combine them with stale contributions.
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

            foreach (var track in tracks)
            {
                if (track != null)
                {
                    track.RemoveChild(this);
                }
            }
        }

        // Clear streamed values before a stopped seek pushes values for its new time.
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

        internal void InvalidateGeometryAnchors()
        {
            geometryAnchorUpdateVersion = -1;
            geometryRotationAnchorReady = false;
            if (trackParentTarget)
                controlledTrack.InvalidateChildGeometryAnchors();
        }

        public void RefreshTrackAnimation()
        {
            // An edit can remove values already queued for LateUpdate. Rebuild all contributions before
            // restoring defaults, including other tracks and the object's own path animation.
            FlushPendingAnimations();
            InvalidateGeometryAnchors();
            foreach (var track in tracks)
                track.PushToChild(this);

            Update();
            ApplyCurrentPose();
            LateUpdate();
        }

        public void AttachToObject(BaseGrid obj)
        {
            ResetData();

            TargetType = TargetTypes.GameplayObject;
            OffsetPosition.HoldUntilFlush = true;

            enabled = UIMode.AnimationMode && TracksManager != null;
            if (!enabled) return;

            obj.RecomputeSpawnParameters();
            deferColorUntilBirth = container is ObstacleContainer or ArcContainer;
            RefreshColorBirthTime(obj);
            var noteLookKey = TracksManager.IsV2Map ? "_disableNoteLook" : "disableNoteLook";
            disableNoteLook = obj.CustomData?.HasKey(noteLookKey) == true
                && obj.CustomData[noteLookKey].AsBool;
            float duration;
            switch (container)
            {
                case ObstacleContainer obs:
                    duration = obs.ObstacleData.DurationSongBpmTime;
                    OffsetPosition.Preload(obs.ReadPosition() - new Vector3(0, 0, 0.25f));
                    // Multiply track scale by the obstacle's authored visual scale.
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

            var preparation = TracksManager.PathAnimations.Prepare(obj, BeatSaberSongContainer.Instance.Map);
            foreach (var name in preparation.TrackDefinitions.Names) AddParent(name);
            foreach (var definition in preparation.ObjectDefinitions)
            {
                if (ObjectPathAnimationRegistry.IsColor(definition.PropertyName))
                    BindPreparedPathDefinition(definition);

            }
            // A successfully parsed local color replaces track paths. Missing named points still leave them available.
            var hasObjectColor = AnimatedProperties.ContainsKey(AnimationPropertyKeys.ColorKey)
                || AnimatedProperties.ContainsKey(AnimationPropertyKeys.LegacyColorKey);
            BindPreparedPathDefinitions(preparation.TrackDefinitions.Definitions, hasObjectColor);
            if (preparation.TrackDefinitions.Names.Length > 0)
                AnimationTrack.transform.SetParent(tracks[0].Track.ObjectParentTransform, false);
            BindPreparedPathDefinitions(preparation.ObjectDefinitions, true);
            var bug = preparation.HasDefinitePosition;

            // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            if (bug
                && (obj.CustomData["_disableNoteGravity"]?.AsBool
                    ?? obj.CustomData["disableNoteGravity"]?.AsBool ?? false))
            {
                Debug.LogError($"disableNoteGravity is bugged when combined with definitePosition, please remove it! {BeatmapLogContext.Describe(obj)}");
                var position = AnimationTrack.ObjectParentTransform.localPosition;
                position.y = (position.y * -0.1f) + 1;
                AnimationTrack.ObjectParentTransform.localPosition = position;
            }

            var boundProperties = new List<IAnimateProperty>(AnimatedProperties.Count);
            var sliceColors = new List<AnimateProperty<Color>>();
            foreach (var prop in AnimatedProperties)
            {
                prop.Value.CompletePreparedOrder();
                if (container is ChainContainer && IsColorProperty(prop.Key))
                    sliceColors.Add((AnimateProperty<Color>)prop.Value);

                else
                    boundProperties.Add(prop.Value);

            }
            properties = boundProperties.ToArray();
            chainPathColors = sliceColors.ToArray();

            Update();

            SubscribeTimeCallbacks();
        }

        internal void RefreshColorBirthTime(BaseGrid obj)
        {
            if (deferColorUntilBirth)
                colorBirthTime = TracksManager.GetColorBirthTime(obj);
        }

        // Event edits rebind affected objects without replacing their authored color with an animated color.
        public void RefreshPathAnimation()
        {
            if (TargetType == TargetTypes.Material)
            {
                var trackData = materialTrackData;
                var zeroAlpha = materialZeroAlpha;
                RestoreAuthoredColor();
                container.UpdateMaterials();
                AttachToMaterial((GeometryContainer)container, trackData, zeroAlpha);
                return;
            }
            if (TargetType != TargetTypes.GameplayObject)
                return;

            RestoreAuthoredColor();
            AttachToObject((BaseGrid)container.ObjectData);
        }

        public void AttachToGeometry(BaseEnvironmentEnhancement eh)
        {
            // Use the loaded map's version to choose legacy position units.
            var v2 = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
            ResetData();

            TargetType = TargetTypes.Transform;

            LocalTarget = AnimationThis.transform;
            WorldTarget = AnimationThis.transform;

            if (eh.Scale is Vector3 scale) Scale.Default = scale;
            if (eh.Position is Vector3 p) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * p;
            if (eh.LocalPosition is Vector3 lp) OffsetPosition.Default = (v2 ? BeatmapConstant.LaneSize : 1f) * lp;
            if (eh.Rotation is Vector3 r) LocalRotation.Default = Quaternion.Euler(r.x, r.y, r.z);
            if (eh.LocalRotation is Vector3 lr) LocalRotation.Default = Quaternion.Euler(lr.x, lr.y, lr.z);

            if (eh.TrackNames.Count > 0)
            {
                foreach (var name in eh.TrackNames)
                    AddParent(name);

                container.transform.SetParent(tracks[0].Track.ObjectParentTransform, false);

                // Paradigm's held child positions must move with later parent animations, including
                // stopped seeks across the door cut and the boss streaks (ParadigmMapParityTest).
                geometryParentAnimator = tracks[0].Animator;
                geometryHasParentAnimator = geometryParentAnimator != null;
                var positionKey = v2
                    ? AnimationPropertyKeys.LegacyPositionKey
                    : AnimationPropertyKeys.PositionKey;
                if (tracks[0].AnimatedProperties.TryGetValue(positionKey, out var positionProperty))
                {
                    geometryWorldPositionProperty = positionProperty as AnimateProperty<Vector3>;
                }

                geometryHasWorldPositionAnchor = geometryHasParentAnimator && geometryWorldPositionProperty != null;
                geometryTrackVersions = new int[tracks.Count];
                for (var i = 0; i < geometryTrackVersions.Length; i++)
                    geometryTrackVersions[i] = -1;

            }

            SubscribeTimeCallbacks();

            OnTimeChanged();
        }

        // Only supplied track properties overwrite an environment object's authored transform.
        public void AttachToEnvironmentObject(
            Transform target,
            string track,
            bool v2,
            ParametricBoxLight boxLight)
            => AttachToEnvironmentObject(target, new[] { track }, v2, boxLight);

        public void AttachToEnvironmentObject(
            Transform target,
            IReadOnlyList<string> trackNames,
            bool v2,
            ParametricBoxLight boxLight)
        {
            ResetData();

            TargetType = TargetTypes.Transform;
            LocalTarget = target;
            WorldTarget = target;
            directEnvironmentTarget = true;
            directEnvironmentTargetIsV2 = v2;
            // Reuse the enhancement's mesh reference to avoid searching during animation updates.
            directEnvironmentBoxLight = boxLight;
            directEnvironmentHasBoxLight = boxLight != null;

            // Cache the ring so position updates can preserve its wave displacement without component searches.
            directEnvironmentTrackLaneRing = target.GetComponent<TrackLaneRing>();
            directEnvironmentTargetIsTrackLaneRing = directEnvironmentTrackLaneRing != null;

            // Save the post-enhancement pose for seeks before the first animation event.
            directEnvironmentSpawnPosition = target.position;
            directEnvironmentSpawnRotation = target.rotation;
            directEnvironmentSpawnScale = target.localScale;
            directEnvironmentOriginalParent = target.parent;

            foreach (var name in trackNames)
                AddParent(name);

            directEnvironmentTrackVersions = new int[tracks.Count];
            ResetDirectEnvironmentTrackVersions();
            // Physically parent enhanced objects only when their track has an AssignTrackParent ancestor.
            // Other tracks leave the environment's original hierarchy intact.
            var trackAnimator = tracks[^1];
            foreach (var source in tracks)
            {
                if (source.Parents.Count > 0)
                    trackAnimator = source;
            }
            if (trackAnimator.Parents.Count > 0)
            {
                LocalTarget.SetParent(
                    trackAnimator.Track.ObjectParentTransform,
                    trackAnimator.ParentWorldPositionStays);
            }

            CaptureDirectEnvironmentParent(trackAnimator.Parents.Count > 0 ? trackAnimator.Animator : null);
            // Rotation snapshots can be computed while the rendered parent is collapsed or at another beat.
            var pairRotation = target.GetComponent<LightPairRotationEffect>();
            if (pairRotation != null)
                pairRotation.BindPositionSampler(GetEnvironmentPositionZAt);

            SubscribeTimeCallbacks();
        }

        // Apply track parenting to enhanced objects attached before the parenting event.
        public void ParentDirectTargetToTrack(Transform trackParent, bool worldPositionStays)
        {
            if (!directEnvironmentTarget)
                return;

            LocalTarget.SetParent(trackParent, worldPositionStays);
            CaptureDirectEnvironmentParent(trackParent.GetComponentInParent<ObjectAnimator>());
        }

        public void ClearDirectTrackParent()
        {
            if (!directEnvironmentTarget)
                return;

            LocalTarget.SetParent(directEnvironmentOriginalParent, true);
            RestoreDirectEnvironmentSpawnPose();
            CaptureDirectEnvironmentParent(null);
            ResetDirectEnvironmentTrackVersions();
        }

        private void ResetDirectEnvironmentTrackVersions()
        {
            for (var i = 0; i < directEnvironmentTrackVersions.Length; i++)
                directEnvironmentTrackVersions[i] = -1;

        }

        private bool HaveDirectEnvironmentTracksChanged()
        {
            var changed = false;
            for (var i = 0; i < tracks.Count; i++)
            {
                var version = tracks[i].UpdateVersion;
                changed |= version != directEnvironmentTrackVersions[i];
                directEnvironmentTrackVersions[i] = version;
            }
            return changed;
        }

        private void CaptureDirectEnvironmentParent(ObjectAnimator parentAnimator)
        {
            directEnvironmentParentAnimator = parentAnimator;
            directEnvironmentHasParentAnimator = parentAnimator != null;
            directEnvironmentParentMatrix = LocalTarget.parent != null
                ? LocalTarget.parent.localToWorldMatrix
                : Matrix4x4.identity;
            directEnvironmentSpawnLocalPosition = LocalTarget.localPosition;
        }

        // Sample sorted track properties without moving scene objects or changing the playhead.
        // Native laser resets read world Z at dispatch, rather than at snapshot construction.
        private float GetEnvironmentPositionZAt(float jsonTime)
        {
            var parent = directEnvironmentHasParentAnimator
                ? directEnvironmentParentAnimator.GetTrackParentMatrix(jsonTime)
                : directEnvironmentParentMatrix;
            var localKey = directEnvironmentTargetIsV2
                ? AnimationPropertyKeys.LegacyLocalPositionKey
                : AnimationPropertyKeys.LocalPositionKey;
            var worldKey = directEnvironmentTargetIsV2
                ? AnimationPropertyKeys.LegacyPositionKey
                : AnimationPropertyKeys.PositionKey;
            var units = directEnvironmentTargetIsV2 ? BeatmapConstant.LaneSize : 1f;
            var localPosition = Vector3.zero;
            var worldPosition = Vector3.zero;
            var hasLocalPosition = false;
            var hasWorldPosition = false;
            foreach (var source in tracks)
            {
                if (source.AnimatedProperties.TryGetValue(localKey, out var localProperty)
                    && jsonTime >= localProperty.StartTime)
                {
                    localPosition += ((AnimateProperty<Vector3>)localProperty).GetLerpedValue(jsonTime);
                    hasLocalPosition = true;
                }
                if (source.AnimatedProperties.TryGetValue(worldKey, out var worldProperty)
                    && jsonTime >= worldProperty.StartTime)
                {
                    worldPosition += ((AnimateProperty<Vector3>)worldProperty).GetLerpedValue(jsonTime);
                    hasWorldPosition = true;
                }
            }
            if (hasLocalPosition)
                return parent.MultiplyPoint3x4(localPosition * units).z;

            if (hasWorldPosition)
            {
                return worldPosition.z * units;
            }

            return parent.MultiplyPoint3x4(directEnvironmentSpawnLocalPosition).z;
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
            controlledTrack = track.GetComponent<TrackAnimator>();
            trackParentTargetIsV2 = isV2Map;
            LocalTarget = track.ObjectParentTransform;
            WorldTarget = track.transform;
            trackParentSpawnPosition = WorldTarget.localPosition;
            trackParentSpawnRotation = WorldTarget.localRotation;
            trackParentSpawnScale = WorldTarget.localScale;

            SubscribeTimeCallbacks();
        }

        public void SetTrackParentMapVersion(bool isV2Map)
        {
            trackParentTargetIsV2 = isV2Map;
        }

        // Reapply held world positions only when the property source changes, allowing ancestor motion to carry the child.
        public void BindPropertySource(TrackAnimator source)
        {
            trackParentPropertySource = source;
            trackParentLastUpdateVersion = -1;
            trackParentAncestor = source.Animator;
            trackParentHasAncestor = trackParentAncestor != null;
            trackParentRootMatrix = WorldTarget.parent.localToWorldMatrix;
        }

        // Sample the parent pose without depending on Unity's update order or moving the playhead.
        private Matrix4x4 GetTrackParentSelfMatrix(float jsonTime)
        {
            var ancestor = trackParentHasAncestor
                ? trackParentAncestor.GetTrackParentMatrix(jsonTime)
                : trackParentRootMatrix;
            var rotation = ReadTrackProperty(trackParentPropertySource,
                AnimationPropertyKeys.LegacyRotationKey, AnimationPropertyKeys.RotationKey, jsonTime,
                trackParentSpawnRotation);
            return ancestor * Matrix4x4.TRS(trackParentSpawnPosition, rotation, trackParentSpawnScale);
        }

        private Matrix4x4 GetTrackParentMatrix(float jsonTime)
        {
            var source = trackParentPropertySource;
            var self = GetTrackParentSelfMatrix(jsonTime);
            var localRotation = ReadTrackProperty(source,
                AnimationPropertyKeys.LegacyLocalRotationKey, AnimationPropertyKeys.LocalRotationKey,
                jsonTime, Quaternion.identity);
            var scale = ReadTrackProperty(source,
                AnimationPropertyKeys.LegacyScaleKey, AnimationPropertyKeys.ScaleKey, jsonTime, Vector3.one);
            var offset = ReadTrackProperty(source,
                AnimationPropertyKeys.LegacyPositionKey, AnimationPropertyKeys.OffsetPositionKey, jsonTime, Vector3.zero)
                * BeatmapConstant.LaneSize;
            var position = offset;
            if (!trackParentTargetIsV2)
            {
                if (source.AnimatedProperties.TryGetValue(AnimationPropertyKeys.LocalPositionKey, out var localProperty)
                    && jsonTime >= localProperty.StartTime)
                {
                    position = ((AnimateProperty<Vector3>)localProperty).GetLerpedValue(jsonTime);
                }
                else if (source.AnimatedProperties.TryGetValue(AnimationPropertyKeys.PositionKey, out var worldProperty)
                    && jsonTime >= worldProperty.StartTime)
                {
                    var property = (AnimateProperty<Vector3>)worldProperty;
                    var world = property.GetLerpedValue(jsonTime);
                    var anchorTime = property.GetTransformAnchorTime(jsonTime);
                    var anchor = anchorTime == jsonTime
                        ? self
                        : GetTrackParentSelfMatrix(anchorTime);
                    position = anchor.inverse.MultiplyVector(world);
                }
            }

            return self * Matrix4x4.TRS(position, localRotation, scale);
        }

        private static T ReadTrackProperty<T>(
            TrackAnimator source, string legacyKey, string key, float time, T defaultValue) where T : struct
        {
            if (!source.AnimatedProperties.TryGetValue(key, out var property))
            {
                source.AnimatedProperties.TryGetValue(legacyKey, out property);
            }

            if (property is AnimateProperty<T> typed && time >= typed.StartTime)
            {
                return typed.GetLerpedValue(time);
            }

            return defaultValue;
        }

        // A changed rotation or scale also makes the native controller rewrite its held world position.
        private float GetGeometryTransformAnchorTime(float jsonTime)
        {
            var anchor = float.NegativeInfinity;
            foreach (var source in tracks)
            {
                foreach (var key in GeometryTransformKeys)
                {
                    if (source.AnimatedProperties.TryGetValue(key, out var property) && jsonTime >= property.StartTime)
                        anchor = Mathf.Max(anchor, property.GetTransformAnchorTime(jsonTime));
                }
            }
            return anchor;
        }

        // Keep completed world positions relative to the parent's pose at the final write.
        private void ApplyGeometryWorldPosition(Vector3 position)
        {
            if (!geometryHasWorldPositionAnchor)
            {
                WorldTarget.position = position;
                return;
            }

            var jsonTime = Context.Atsc.CurrentJsonTime;
            var anchorTime = GetGeometryTransformAnchorTime(jsonTime);
            var changed = false;
            for (var i = 0; i < tracks.Count; i++)
            {
                var source = tracks[i];
                changed |= geometryTrackVersions[i] != source.UpdateVersion;
                geometryTrackVersions[i] = source.UpdateVersion;
            }
            if (anchorTime == jsonTime)
            {
                // Parent LateUpdate can run after this child, so project through its sampled pose even during an active write.
                LocalTarget.localPosition = geometryParentAnimator.GetTrackParentMatrix(anchorTime)
                    .inverse.MultiplyPoint3x4(position);
                geometryAnchorUpdateVersion = -1;
                return;
            }

            if (geometryAnchorUpdateVersion < 0 || changed || geometryAnchorTime != anchorTime)
            {
                geometryAnchoredLocalPosition = geometryParentAnimator.GetTrackParentMatrix(anchorTime)
                    .inverse.MultiplyPoint3x4(position);
                geometryAnchorUpdateVersion = 0;
                geometryAnchorTime = anchorTime;
            }

            LocalTarget.localPosition = geometryAnchoredLocalPosition;
        }

        public void AttachToMaterial(GeometryContainer con, string track, bool zeroAlpha = false)
            => AttachToMaterial(con, (JSONNode)new JSONString(track), zeroAlpha);

        public void AttachToMaterial(GeometryContainer con, JSONNode trackData, bool zeroAlpha = false)
        {
            ResetData();
            materialZeroAlpha = zeroAlpha;
            materialTrackData = trackData;

            TargetType = TargetTypes.Material;
            container = con;
            Colors.Default = con.MpbController.Mpb.GetColor(colorId);

            enabled = true;
            var preparation = TracksManager.PathAnimations.PrepareTracks(trackData, BeatSaberSongContainer.Instance.Map);
            foreach (var name in preparation.Names)
                AddParent(name);

            foreach (var definition in preparation.Definitions)
            {
                if (!ObjectPathAnimationRegistry.IsColor(definition.PropertyName))
                    continue;

                var parameters = definition.Parameters;
                parameters.TimeBegin = 0f;
                parameters.TimeEnd = 1f;
                AddPointDef(parameters, definition.PropertyName, definition.Source);
            }
            var colors = new List<AnimateProperty<Color>>(AnimatedProperties.Count);
            foreach (var property in AnimatedProperties.Values)
            {
                property.CompletePreparedOrder();
                colors.Add((AnimateProperty<Color>)property);
            }
            materialPathColors = colors.ToArray();
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
            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);

            if (container != null && container.ObjectData is BaseGrid obj)
            {
                var noodleAnimationLifetime = time > timeEnd ? -1 : 1;
                if (!(container is ChainContainer))
                {
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

            foreach (var path in materialPathColors)
            {
                if (path.HasValueAt(time))
                    Colors.Add(path.GetPathValue(time, 0f));

            }

            if (AnimatedTrack) AnimationTrack.UpdateTime(time);
        }

        public void LateUpdate()
        {
            // Only apply properties supplied by the track. Defaults would reset untouched transform fields.
            if (directEnvironmentTarget)
            {
                ApplyDirectEnvironmentTargets();
                return;
            }

            if (container is GeometryContainer)
                ApplyGeometryRotation(false);
            else if (LocalRotation.Count > 0)
                LocalTarget.localRotation = LocalRotation.Get();

            // Choose the version-specific position source before writing transforms.
            if (trackParentTarget || (TargetType == TargetTypes.Transform && container is GeometryContainer))
            {
                // Apply rotation first so it cannot move the world position written below.
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

            // Rotation was already consumed above; reading the drained aggregator would reset it to identity.
            if (WorldTarget is Transform && WorldRotation.Count > 0)
                if (container is not GeometryContainer && (!trackParentTarget || trackParentTargetIsV2))
                    WorldTarget.localRotation = WorldRotation.Get();

            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);
            if (!trackParentTarget && container is not GeometryContainer && WorldPosition.Count > 0)
            {
                if (timeBegin < time && time < timeEnd) AnimationTrack.HoldDefinitePosition();
                if (container is not null and not GeometryContainer)
                    container.transform.localPosition = WorldPosition.Get();
                else
                    WorldTarget.localPosition = WorldPosition.Get();
            }

            var hasChainPathColor = false;
            // Editor preloading can precede native wall and arc creation. Those hidden objects cannot retain earlier colors.
            if (deferColorUntilBirth && Context.Atsc.CurrentSongBpmTime < colorBirthTime)
            {
                Colors.Flush();
                colorRestorePending |= animatedColorApplied;
            }
            foreach (var path in chainPathColors)
                hasChainPathColor |= path.HasValueAt(time);

            if (container is ObjectContainer && (Colors.Count > 0 || hasChainPathColor
                || OpacityArrow.Count > 0 || Opacity.Count > 0 || colorRestorePending))
            {
                if (Colors.Count > 0 || hasChainPathColor)
                {
                    var color = Colors.Count > 0
                        ? Colors.Get()
                        : Color.white;
                    if (materialZeroAlpha)
                        color.a = 0f;
                    switch (container)
                    {
                        case ObstacleContainer obstacle:
                            obstacle.SetColor(color);
                            break;
                        case ChainContainer chain:
                            ApplyChainPathColors(chain, color, time);
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
                {
                    var arrowDissolve = OpacityArrow.Get();
                    nc.ArrowMpbController.Mpb.SetFloat(cutoutId, 1f - arrowDissolve);
                    nc.ArrowMpbController.Mpb.SetFloat(arrowDissolveId, arrowDissolve);
                }

                container.MpbController.Mpb.SetFloat(cutoutId, 1f - Opacity.Get());
                container.UpdateMaterials();
            }

            if (UIMode.PreviewMode && !disableNoteLook && container is NoteContainer lookNote)
                ApplyNoteLook(lookNote, time);
        }

        // Burst slices are native notes with their own cut times. Only color uses these clocks.
        private void ApplyChainPathColors(ChainContainer chain, Color globalColor, float beat)
        {
            chain.SetColor(globalColor);
            var data = chain.ChainData;
            for (var i = 0; i < data.SliceCount - 1; i++)
            {
                var cutBeat = Mathf.Lerp(
                    data.SongBpmTime,
                    data.TailSongBpmTime,
                    (i + 1f) / (data.SliceCount - 1f));
                var normalized = Mathf.Clamp01(
                    (beat - (cutBeat - data.HalfJumpDuration))
                    / (data.HalfJumpDuration * 2f));
                var color = globalColor;
                foreach (var path in chainPathColors)
                {
                    if (path.HasValueAt(beat))
                        color *= path.GetPathValue(beat, normalized);

                }
                chain.SetNodeColor(i, color);
            }
        }

        private void ApplyNoteLook(NoteContainer note, float beat)
        {
            var data = note.NoteData;
            var normalTime = Mathf.Clamp01((beat - data.SpawnSongBpmTime) / (data.HalfJumpDuration * 2f));
            var baseRotation = note.DirectionTarget.parent.rotation;
            var spawnRotation = Track.GetNoteSpawnRotation(note, normalTime);
            var authoredRotation = baseRotation * spawnRotation;
            var inverseWorldRotation = Quaternion.Inverse(WorldTarget.rotation);
            // The movable Preview camera must not turn notes away from the authored player pose.
            var headPos = TracksManager.CameraManager.PlayerCameraController.transform.position;
            var target = note.DirectionTarget.position;
            var headHeight = (inverseWorldRotation * headPos).y;
            var targetHeight = (inverseWorldRotation * target).y;
            headPos += WorldTarget.rotation * Vector3.up * (Mathf.Lerp(headHeight, targetHeight, 0.8f) - headHeight);
            var direction = target - headPos;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            // Noodle rotates the head ray through the authored base pose. Spawn progress remains independent of travel.
            var lookDirection = baseRotation * (inverseWorldRotation * direction.normalized);
            var look = Quaternion.LookRotation(lookDirection, authoredRotation * Vector3.up);
            note.DirectionTarget.rotation = Quaternion.Lerp(authoredRotation, look, normalTime * 2f);
        }

        private void RestoreAuthoredColor()
        {
            var scheme = Context != null ? Context.ColorScheme : null;
            switch (container)
            {
                case NoteContainer note:
                {
                    var data = note.NoteData;
                    if (data == null)
                        break;

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
                    if (data == null)
                        break;

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
                case ArcContainer arc:
                    arc.SetColor(arc.ArcData.CustomColor
                        ?? (arc.ArcData.Color == (int)NoteColor.Red ? scheme.LeftNoteColor : scheme.RightNoteColor));
                    break;
                case ObstacleContainer obstacle:
                    obstacle.SetColor(obstacle.ObstacleData.CustomColor ?? Colors.Default);
                    break;
                case GeometryContainer geometry:
                    geometry.MpbController.Mpb.SetColor(colorId, Colors.Default);
                    break;
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
            if (!HaveDirectEnvironmentTracksChanged())
                return hasProperty;


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

            if (hasWorldRotation && !hasLocalRotation)
            {
                WorldTarget.rotation = worldRotation;
                applied = true;
            }

            // Update only animated mesh overrides; untouched fields stay under the light controller's control.
            if (directEnvironmentHasBoxLight)
            {
                if (positionChanged)
                    directEnvironmentBoxLight.CaptureAuthoredPosition();
                if (scaleChanged)
                    directEnvironmentBoxLight.CaptureAuthoredScale();
            }

            if (applied)
                directEnvironmentEverApplied = true;
            return applied;
        }

        // Heck chooses localRotation over rotation for a transform target. Sharing the two aggregators
        // multiplied noncommuting quaternions in dictionary order, which changed after deleting and undoing events.
        private void ApplyGeometryRotation(bool restoreDefault)
        {
            var hasLocal = LocalRotation.Count > 0;
            var hasWorld = WorldRotation.Count > 0;
            var local = LocalRotation.Get();
            var world = WorldRotation.Get();
            if (hasLocal || (restoreDefault && !hasWorld))
            {
                LocalTarget.localRotation = local;
                geometryRotationAnchorReady = false;
            }
            else if (hasWorld)
            {
                if (!geometryHasParentAnimator)
                {
                    WorldTarget.rotation = world;
                    return;
                }
                var jsonTime = Context.Atsc.CurrentJsonTime;
                var anchorTime = GetGeometryTransformAnchorTime(jsonTime);
                if (anchorTime == jsonTime)
                {
                    LocalTarget.localRotation = Quaternion.Inverse(geometryParentAnimator.GetTrackParentMatrix(anchorTime).rotation) * world;
                    geometryRotationAnchorReady = false;
                    return;
                }
                // A held world rotation must inherit later parent rotation, just as its held world position does.
                if (!geometryRotationAnchorReady || geometryRotationAnchorTime != anchorTime || geometryAnchorWorldRotation != world)
                {
                    geometryAnchoredLocalRotation = Quaternion.Inverse(geometryParentAnimator.GetTrackParentMatrix(anchorTime).rotation) * world;
                    geometryRotationAnchorTime = anchorTime;
                    geometryAnchorWorldRotation = world;
                    geometryRotationAnchorReady = true;
                }
                LocalTarget.localRotation = geometryAnchoredLocalRotation;
            }
        }

        public void RestoreRemovedTrackProperty(string key)
        {
            if (key is AnimationPropertyKeys.LegacyTimeKey or AnimationPropertyKeys.TimeKey)
                SetLifeTime(-1);

            if ((key is AnimationPropertyKeys.LegacyDissolveKey or AnimationPropertyKeys.DissolveKey) && container != null)
            {
                Opacity.Flush();
                container.MpbController.Mpb.SetFloat(cutoutId, 1f - Opacity.Get());
                container.UpdateMaterials();
            }

            if (!directEnvironmentTarget)
                return;

            // Other properties can remain animated when one is deleted, so a whole-pose scrub reset is insufficient.
            switch (key)
            {
                case AnimationPropertyKeys.LegacyScaleKey:
                case AnimationPropertyKeys.ScaleKey:
                    LocalTarget.localScale = directEnvironmentSpawnScale;
                    if (directEnvironmentHasBoxLight)
                        directEnvironmentBoxLight.CaptureAuthoredScale();

                    break;
                case AnimationPropertyKeys.LegacyPositionKey:
                case AnimationPropertyKeys.PositionKey:
                case AnimationPropertyKeys.LegacyLocalPositionKey:
                case AnimationPropertyKeys.LocalPositionKey:
                    ApplyDirectEnvironmentPosition(directEnvironmentHasParentAnimator
                        ? directEnvironmentSpawnLocalPosition
                        : directEnvironmentSpawnPosition, !directEnvironmentHasParentAnimator);
                    if (directEnvironmentHasBoxLight)
                        directEnvironmentBoxLight.CaptureAuthoredPosition();

                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.RotationKey:
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    LocalTarget.rotation = directEnvironmentSpawnRotation;
                    break;
            }
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
                    if (source == null)
                        LocalTarget.position = worldPosition + WorldTarget.position;
                    else if (source.UpdateVersion != trackParentLastUpdateVersion)
                    {
                        // Completed positions keep the parent's pose at their final write. Using the current pose
                        // when preview reactivates a track shifts Censored's text after a saved-cursor restore.
                        var property = (AnimateProperty<Vector3>)source.AnimatedProperties[AnimationPropertyKeys.PositionKey];
                        var anchorTime = property.GetTransformAnchorTime(Context.Atsc.CurrentJsonTime);
                        LocalTarget.localPosition = GetTrackParentSelfMatrix(anchorTime)
                            .inverse.MultiplyVector(worldPosition);
                        trackParentLastUpdateVersion = source.UpdateVersion;
                    }
                }
                else
                {
                    ApplyGeometryWorldPosition(worldPosition);
                }
            }
            else if (hasOffsetPosition)
            {
                if (trackParentTarget)
                    LocalTarget.localPosition = offsetPosition;
                else
                {
                    ApplyGeometryWorldPosition(offsetPosition);
                }
            }
            else if (includeSpawnDefault)
                LocalTarget.localPosition = offsetPosition;
        }

        // Restore the authored pose when seeking before animation. Rings retain their wave displacement,
        // and box lights must recapture that pose before their controller refreshes.
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
                if (directEnvironmentHasBoxLight)
                    directEnvironmentBoxLight.RecaptureAuthoredTransform();
                return;
            }

            LocalTarget.SetPositionAndRotation(directEnvironmentSpawnPosition, directEnvironmentSpawnRotation);
            LocalTarget.localScale = directEnvironmentSpawnScale;
            if (directEnvironmentHasBoxLight)
                directEnvironmentBoxLight.RecaptureAuthoredTransform();
        }

        // Rebase animated ring positions while retaining each segment's wave displacement.
        // Setting position directly would collapse the segments onto the same point.
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

            ApplyCurrentPose();
        }

        private void ApplyCurrentPose()
        {

            if (directEnvironmentTarget)
            {
                if (!ApplyDirectEnvironmentTargets() && directEnvironmentEverApplied)
                {
                    RestoreDirectEnvironmentSpawnPose();
                    directEnvironmentEverApplied = false;
                }

                return;
            }

            if (container is GeometryContainer)
                ApplyGeometryRotation(true);
            else
                LocalTarget.localRotation = LocalRotation.Get();

            if (trackParentTarget || (TargetType == TargetTypes.Transform && container is GeometryContainer))
            {
                // Keep stopped-seek ordering consistent with LateUpdate: rotation before world position.
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
                // The V3 parent rotation was consumed before the position write.
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

        private static bool IsColorProperty(string key) => ObjectPathAnimationRegistry.IsColor(key)
            || key.StartsWith("track_" + AnimationPropertyKeys.ColorKey + "_", StringComparison.Ordinal)
            || key.StartsWith("track_" + AnimationPropertyKeys.LegacyColorKey + "_", StringComparison.Ordinal);

        private void BindPreparedPathDefinitions(ObjectPathAnimationRegistry.Definition[] definitions, bool skipColor = false)
        {
            foreach (var definition in definitions)
            {
                if (skipColor && ObjectPathAnimationRegistry.IsColor(definition.PropertyName))
                    continue;

                BindPreparedPathDefinition(definition);
            }
        }

        private void BindPreparedPathDefinition(ObjectPathAnimationRegistry.Definition definition)
        {
            var parameters = definition.Parameters;
            parameters.TimeBegin = timeBegin;
            parameters.TimeEnd = timeEnd;
            if (ObjectPathAnimationRegistry.IsColor(definition.PropertyName))
            {
                if (container is ArcContainer arc)
                {
                    // Native SliderUpdateColorize divides by jumpDuration + .75 * jumpDuration + span.
                    parameters.TimeEnd = timeBegin + (arc.ArcData.HalfJumpDuration * 3.5f)
                        + arc.ArcData.DurationSongBpmTime;
                }
                else if (container is ChainContainer)
                {
                    parameters.TimeBegin = 0f;
                    parameters.TimeEnd = 1f;
                }
            }
            AddPointDef(parameters, definition.PropertyName, definition.Source);
        }

        private void AddPointDef(IPointDefinition.UntypedParams p, string key, BaseCustomEvent source)
        {
            switch (key)
            {
                case AnimationPropertyKeys.LegacyDissolveKey:
                case AnimationPropertyKeys.DissolveKey:
                    AddPointDef(source, f => Opacity.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case AnimationPropertyKeys.LegacyDissolveArrowKey:
                case AnimationPropertyKeys.DissolveArrowKey:
                    AddPointDef(source, f => OpacityArrow.Add(f), PointDataParsers.ParseFloat, p, 0);
                    break;
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    AddPointDef(
                        source,
                        q => LocalRotation.Add(q),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.OffsetWorldRotationKey:
                    AddPointDef(
                        source,
                        v => WorldRotation.Add(v),
                        PointDataParsers.ParseQuaternion,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyPositionKey:
                case AnimationPropertyKeys.OffsetPositionKey:
                    AddPointDef(
                        source,
                        v => OffsetPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyDefinitePositionKey:
                case AnimationPropertyKeys.DefinitePositionKey:
                    AddPointDef(
                        source,
                        v => WorldPosition.Add(v * BeatmapConstant.LaneSize),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyScaleKey:
                case AnimationPropertyKeys.ScaleKey:
                    AddPointDef(
                        source,
                        v => Scale.Add(v),
                        PointDataParsers.ParseVector3,
                        p,
                        Vector3.one);
                    break;
                case AnimationPropertyKeys.LegacyColorKey:
                case AnimationPropertyKeys.ColorKey:
                    AddPointDef<Color>(source, (Color c) => Colors.Add(c), PointDataParsers.ParseColor, p, Color.white);
                    break;
                case AnimationPropertyKeys.LegacyInteractableKey:
                case AnimationPropertyKeys.InteractableKey:
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
            if (typeof(T) == typeof(Color) && (p.Points == null || p.Points.IsNull))
            {
                if (source != null)
                {
                    var property = GetAnimateProperty(p.Key, setter, @default);
                    property.UsePathClock = true;
                    property.AddNullDefinition(p.Time, source);
                }
                return;
            }
            if (AnimateProperty<T>.SkipsMissingPointDefinition(p, source))
                return;

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

                var property = GetAnimateProperty(p.Key, setter, @default);
                property.UsePathClock = typeof(T) == typeof(Color);
                property.AddPreparedPointDef(parser, p, source);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not load animation property. {new AnimationLogContext(p, source).Describe()}\n{e}");
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

        public class Aggregator<T> where T : struct
        {
            public int Count;
            public readonly Func<T, T, T> Func;
            public T Default;
            private readonly T instancedDefault;
            public int Keep;
            private T value;
            private T retainedValue;

            public Aggregator(T def, Func<T, T, T> func)
            {
                Default = def;
                instancedDefault = def;
                Func = func;
            }

            // Track arrays have no contributor limit. Fold their values without allocating each frame.
            public void Add(T contribution)
            {
                value = Count == 0
                    ? contribution
                    : Func(value, contribution);
                Count++;
            }

            public void Preload(T contribution)
            {
                Add(contribution);
                retainedValue = Keep == 0
                    ? contribution
                    : Func(retainedValue, contribution);
                Keep++;
            }

            public bool HoldUntilFlush;
            private bool hasHeldValue;
            private T heldValue;

            public T Get()
            {
                if (HoldUntilFlush && hasHeldValue && Count == Keep)
                    return heldValue;

                if (Count == 0)
                    return Default;

                var result = value;
                if (HoldUntilFlush && Count > Keep)
                {
                    heldValue = result;
                    hasHeldValue = true;
                }
                Count = Keep;
                value = retainedValue;
                return result;
            }

            public void Flush()
            {
                Count = Keep;
                value = retainedValue;
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
                value = default;
                retainedValue = default;
            }
        }

    }
}
