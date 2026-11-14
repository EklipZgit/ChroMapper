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
        internal int AnimationUpdateIndex = -1;
        internal int AnimationSeekIndex = -1;
        internal int AnimationPathUpdateIndex = -1;
        internal bool ParticipatesInSharedSeeks => container is not ObstacleContainer;

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
        internal bool DisableNoteLook => disableNoteLook;
        public bool ShouldRecycle;
        private bool animatedColorApplied;
        private bool colorRestorePending;
        private bool disableNoteLook;
        private bool hasDefinitePosition;
        internal const int NoteLookHistoryFrames = 32;
        private const float NoteLookHistoryStart = 0.4f;
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

        private void RegisterAnimationUpdates()
        {
            if (isActiveAndEnabled)
            {
                TracksManager.ObjectAnimationUpdates.Register(this);
            }
        }

        private bool directEnvironmentTarget;
        private bool directEnvironmentTargetIsV2;
        private bool directEnvironmentTargetIsTrackLaneRing;
        private TrackLaneRing directEnvironmentTrackLaneRing;
        private Vector3 directEnvironmentRingPositionOffset;
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
        private Quaternion directEnvironmentSpawnLocalRotation;
        private Vector3 directEnvironmentOriginalLocalPosition;
        private Quaternion directEnvironmentOriginalLocalRotation;
        private Transform directEnvironmentOriginalParent;
        private int[] directEnvironmentTrackVersions = Array.Empty<int>();
        private DirectEnvironmentSharedTrackBinding directEnvironmentSharedTrackBinding;
        private bool trackParentTarget;
        private TrackAnimator controlledTrack;
        private bool trackParentTargetIsV2;
        private TrackAnimator trackParentPropertySource;
        private int trackParentLastUpdateVersion = -1;
        private ObjectAnimator trackParentAncestor;
        private bool trackParentHasAncestor;
        private Matrix4x4 trackParentRootMatrix;
        private Quaternion trackParentRootRotation;
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
        // Materials upgraded from Standard to unlit Glowing must keep zero alpha during color animation.
        private bool materialZeroAlpha;
        private bool canRetainMaterialColor;
        private int materialTrackVersion = -1;
        private int materialBlockVersion = -1;
        private bool canRetainGeometryPose;
        private bool hasAppliedGeometryPose;
        private int geometryPoseTrackVersion;
        private int geometryBlockVersion = -1;
        private bool geometryRequiresWorldWrite;
        private float materialCutout;
        internal bool PullsTrackValues { get; private set; }
        internal bool IsNote => container is NoteContainer;
        internal bool NeedsPathUpdate => TargetType == TargetTypes.GameplayObject || materialPathColors.Length > 0;
        private JSONNode materialTrackData;
        private AnimateProperty<Color>[] materialPathColors = Array.Empty<AnimateProperty<Color>>();
        private AnimateProperty<Color>[] chainPathColors = Array.Empty<AnimateProperty<Color>>();

        public Dictionary<string, IAnimateProperty> AnimatedProperties = new();
        private IAnimateProperty[] properties = Array.Empty<IAnimateProperty>();

        private static readonly PointDefinition<float>.Parser floatParser = PointDataParsers.ParseFloat;
        private static readonly PointDefinition<Color>.Parser colorParser = PointDataParsers.ParseColor;
        private static readonly PointDefinition<Vector3>.Parser vector3Parser = PointDataParsers.ParseVector3;
        private static readonly PointDefinition<Quaternion>.Parser quaternionParser = PointDataParsers.ParseQuaternion;

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
            directEnvironmentRingPositionOffset = Vector3.zero;
            directEnvironmentBoxLight = null;
            directEnvironmentHasBoxLight = false;
            directEnvironmentTrackVersions = Array.Empty<int>();
            directEnvironmentSharedTrackBinding = null;
            directEnvironmentSpawnPosition = Vector3.zero;
            directEnvironmentSpawnRotation = Quaternion.identity;
            directEnvironmentSpawnLocalRotation = Quaternion.identity;
            directEnvironmentSpawnScale = Vector3.one;
            directEnvironmentParentAnimator = null;
            directEnvironmentOriginalParent = null;
            directEnvironmentOriginalLocalPosition = Vector3.zero;
            directEnvironmentOriginalLocalRotation = Quaternion.identity;
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
            canRetainMaterialColor = false;
            canRetainGeometryPose = false;
            PullsTrackValues = false;
            materialCutout = 0f;
            materialTrackVersion = -1;
            materialBlockVersion = -1;
            hasAppliedGeometryPose = false;
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
            hasDefinitePosition = false;
            deferColorUntilBirth = false;

            time = null;
            AnimatedLife = false;
            ShouldRecycle = false;
            if (container is NoteContainer lookNote)
                lookNote.HasHitLookRotation = false;

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

        // Disabled animators leave the update lists. Reattach them before the next frame or seek.
        private void OnEnable()
        {
            materialTrackVersion = -1;
            materialBlockVersion = -1;
            hasAppliedGeometryPose = false;
            if (Context == null || Context.Atsc == null) return;
            RegisterAnimationUpdates();

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
            if (TracksManager != null)
            {
                TracksManager.ObjectAnimationUpdates.Unregister(this);
            }
            // Discard pending track values on disable so the next seek cannot combine them with stale contributions.
            if (trackParentTarget)
            {
                FlushPendingAnimations();
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
            // A stopped seek can move before the first time override. Clear it before active tracks resample
            // so a future freeze does not keep the object's lifetime pinned while rewinding.
            time = null;
            FlushTransformAnimations();
            Colors.Flush();
            Opacity.Flush();
            OpacityArrow.Flush();
            Interactable.Flush();
            colorRestorePending |= animatedColorApplied;
        }

        internal void InvalidateGeometryAnchors()
        {
            materialTrackVersion = -1;
            materialBlockVersion = -1;
            hasAppliedGeometryPose = false;
            geometryAnchorUpdateVersion = -1;
            geometryRotationAnchorReady = false;
            if (trackParentTarget)
                controlledTrack.InvalidateChildGeometryAnchors();
        }

        public void RefreshTrackAnimation()
        {
            if (container is NoteContainer lookNote)
                lookNote.HasHitLookRotation = false;

            ResetNoteJumpRotation();

            // An edit can remove values already queued for LateUpdate. Rebuild all contributions before
            // restoring defaults, including other tracks and the object's own path animation.
            FlushPendingAnimations();
            if (TargetType == TargetTypes.GameplayObject)
                ApplyCurrentPose();
            InvalidateGeometryAnchors();
            foreach (var track in tracks)
                track.PushToChild(this);

            // Recycling clears the container, so there is no remaining pose to apply.
            if (!UpdateAnimationState())
                return;

            // Gameplay defaults were restored before sampling. Reading the new contributions here
            // and again in LateUpdate would consume animated scale and rotation twice.
            if (TargetType != TargetTypes.GameplayObject)
                ApplyCurrentPose();
            ApplyAnimation();
        }

        public void AttachToObject(BaseGrid obj)
        {
            ResetData();

            TargetType = TargetTypes.GameplayObject;
            OffsetPosition.HoldUntilFlush = true;
            // A seek can run after Update, so Unity's following LateUpdate must retain its sampled pose.
            LocalRotation.HoldUntilFlush = true;
            WorldRotation.HoldUntilFlush = true;
            Scale.HoldUntilFlush = true;

            enabled = UIMode.AnimationMode && TracksManager != null;
            if (!enabled)
            {
                ApplyEditorObstacleColor(obj);
                return;
            }

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
                    OffsetPosition.Preload(obs.ReadPosition() - new Vector3(0, 0, ObstacleContainer.AnimationZOffset));
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

            RequireAnimationTrack();
            WorldTarget = AnimationTrack.transform;

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
                    // Keep the cached spawn turn when Noodle composes path and track rotation offsets.
                    WorldRotation.Preload(Quaternion.Euler(AnimationTrack.RotationValue));
                    break;
            }

            timeBegin = obj.SpawnSongBpmTime;
            // Can't use DespawnSongBpmTime because obstacles jump out early
            timeEnd = obj.SongBpmTime + duration + obj.HalfJumpDuration;

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
            hasDefinitePosition = preparation.HasDefinitePosition;

            // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            if (hasDefinitePosition
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

            ResetNoteJumpRotation();
            UpdateAnimation();

            RegisterAnimationUpdates();
        }

        private void ResetNoteJumpRotation()
        {
            if (container is not NoteContainer note)
                return;

            note.HasJumpRotation = true;
            // SetToStart leaves identity. A time track starting at the hit skips NoteJump's rotation window.
            var map = BeatSaberSongContainer.Instance.Map;
            var spawnTime = (float)map.SongBpmTimeToJsonTime(note.NoteData.SpawnSongBpmTime)
                .GetValueOrDefault(note.NoteData.JsonTime);
            foreach (var source in tracks)
            {
                var lifetime = ReadTrackProperty(
                    source,
                    AnimationPropertyKeys.LegacyTimeKey,
                    AnimationPropertyKeys.TimeKey,
                    spawnTime,
                    -1f);
                if (lifetime < 0f)
                    continue;

                note.HasJumpRotation = lifetime < 0.5f;
                break;
            }
        }

        private void ApplyEditorObstacleColor(BaseGrid obj)
        {
            if (container is not ObstacleContainer obstacle || obj.CustomAnimation == null)
                return;

            var key = TracksManager.IsV2Map
                ? AnimationPropertyKeys.LegacyColorKey
                : AnimationPropertyKeys.ColorKey;
            if (!obj.CustomAnimation.HasKey(key))
                return;

            // Editing keeps walls on the grid, but their initial authored color must survive disabling motion.
            var parameters = new IPointDefinition.UntypedParams
            {
                Key = key,
                Points = obj.CustomAnimation[key],
                LogSource = obj,
                Overwrite = true,
                PreparedIndex = 0,
                PreparedLength = 1
            };
            AddPointDef<Color>(null, Colors.Add, colorParser, parameters, Color.white);
            if (!AnimatedProperties.TryGetValue(key, out var property))
                return;

            var colorProperty = (AnimateProperty<Color>)property;
            if (colorProperty.IsEmpty() || colorProperty.PointDefinitions[0].Points.Length == 0)
                return;

            obstacle.SetColor(colorProperty.PointDefinitions[0].Interpolate(0f));
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
            // Keep multi-track geometry on the existing push order because quaternion composition depends on it.
            canRetainGeometryPose = eh.TrackNames.Count <= 1 && HasStandardMaterialWithoutLight((GeometryContainer)container);
            PullsTrackValues = canRetainGeometryPose;

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
                var positionKey = v2
                    ? AnimationPropertyKeys.LegacyPositionKey
                    : AnimationPropertyKeys.PositionKey;
                if (tracks[0].AnimatedProperties.TryGetValue(positionKey, out var positionProperty))
                {
                    geometryWorldPositionProperty = positionProperty as AnimateProperty<Vector3>;
                }

                CaptureGeometryParent();
                geometryTrackVersions = new int[tracks.Count];
                for (var i = 0; i < geometryTrackVersions.Length; i++)
                    geometryTrackVersions[i] = -1;

            }

            RegisterAnimationUpdates();

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
            directEnvironmentRingPositionOffset = directEnvironmentTargetIsTrackLaneRing
                ? directEnvironmentTrackLaneRing.PositionOffset : Vector3.zero;

            // Save the post-enhancement pose for seeks before the first animation event.
            directEnvironmentSpawnPosition = target.position;
            directEnvironmentSpawnRotation = target.rotation;
            directEnvironmentSpawnScale = target.localScale;
            directEnvironmentOriginalParent = target.parent;
            directEnvironmentOriginalLocalPosition = target.localPosition;
            directEnvironmentOriginalLocalRotation = target.localRotation;

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

            RegisterAnimationUpdates();
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

            LocalTarget.SetParent(directEnvironmentOriginalParent, false);
            RestoreDirectEnvironmentSpawnPose();
            CaptureDirectEnvironmentParent(null);
            ResetDirectEnvironmentTrackVersions();
        }

        private void CaptureGeometryParent()
        {
            geometryParentAnimator = tracks[0].Animator;
            geometryHasParentAnimator = geometryParentAnimator != null;
            geometryHasWorldPositionAnchor = geometryHasParentAnimator && geometryWorldPositionProperty != null;
        }

        internal void RefreshTrackParentBinding()
        {
            if (trackParentTarget)
                BindPropertySource(trackParentPropertySource);
            else if (TargetType == TargetTypes.Transform && container is GeometryContainer && tracks.Count == 1)
                CaptureGeometryParent();

            InvalidateGeometryAnchors();
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

        internal void BindNativeEnvironmentParent(ObjectAnimator parent)
        {
            directEnvironmentSharedTrackBinding = null;
            if (parent == null || tracks.Count != 1 || parent.tracks.Count != 1
                || tracks[0] != parent.tracks[0] || directEnvironmentHasParentAnimator)
            {
                return;
            }
            // The sampled root must be static. Deeper tracked ancestors need a separate transform history.
            if (TracksManager.HasAnimatedEnvironmentAncestor(parent.LocalTarget))
            {
                return;
            }
            var marker = LocalTarget.GetComponent<ChromaIDMarker>();
            var parentMarker = parent.LocalTarget.GetComponent<ChromaIDMarker>();
            if (marker.NativeTransformInstanceId == 0 || parentMarker.NativeTransformInstanceId == 0
                || marker.NativeTransformInstanceId >= parentMarker.NativeTransformInstanceId)
            {
                return;
            }
            directEnvironmentSharedTrackBinding = new DirectEnvironmentSharedTrackBinding(
                tracks[0], directEnvironmentTargetIsV2, parent.directEnvironmentParentMatrix,
                parent.directEnvironmentSpawnLocalPosition, parent.directEnvironmentSpawnRotation,
                parent.directEnvironmentSpawnScale);
            ResetDirectEnvironmentTrackVersions();
        }

        internal bool HasEnvironmentTransformAnimation()
        {
            foreach (var source in tracks)
            {
                foreach (var key in TrackAnimator.TransformPropertyKeys)
                {
                    if (source.AnimatedProperties.ContainsKey(key))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void CaptureDirectEnvironmentParent(ObjectAnimator parentAnimator)
        {
            directEnvironmentParentAnimator = parentAnimator;
            directEnvironmentHasParentAnimator = parentAnimator != null;
            directEnvironmentParentMatrix = LocalTarget.parent != null
                ? LocalTarget.parent.localToWorldMatrix
                : Matrix4x4.identity;
            directEnvironmentSpawnLocalPosition = LocalTarget.localPosition;
            directEnvironmentSpawnLocalRotation = LocalTarget.localRotation;
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
                    localPosition += ((AnimateProperty<Vector3>)localProperty)
                        .GetLerpedValue(jsonTime);
                    hasLocalPosition = true;
                }
                if (source.AnimatedProperties.TryGetValue(worldKey, out var worldProperty)
                    && jsonTime >= worldProperty.StartTime)
                {
                    worldPosition += ((AnimateProperty<Vector3>)worldProperty)
                        .GetLerpedValue(jsonTime);
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

            RegisterAnimationUpdates();
        }

        // Reapply held world positions only when the property source changes, allowing ancestor motion to carry the child.
        public void BindPropertySource(TrackAnimator source)
        {
            trackParentPropertySource = source;
            trackParentLastUpdateVersion = -1;
            trackParentAncestor = source.Animator;
            trackParentHasAncestor = trackParentAncestor != null;
            trackParentRootMatrix = WorldTarget.parent.localToWorldMatrix;
            trackParentRootRotation = WorldTarget.parent.rotation;
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

        internal Matrix4x4 GetTrackParentMatrix(float jsonTime)
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
                    position = ((AnimateProperty<Vector3>)localProperty)
                        .GetLerpedValue(jsonTime);
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

        // A zero or nonuniform scale cannot be decomposed back into the track's rotation reliably.
        private Quaternion GetTrackParentRotation(float jsonTime)
        {
            var ancestor = trackParentHasAncestor
                ? trackParentAncestor.GetTrackParentRotation(jsonTime)
                : trackParentRootRotation;
            var rotation = ReadTrackProperty(
                trackParentPropertySource,
                AnimationPropertyKeys.LegacyRotationKey,
                AnimationPropertyKeys.RotationKey,
                jsonTime,
                trackParentSpawnRotation);
            var localRotation = ReadTrackProperty(
                trackParentPropertySource,
                AnimationPropertyKeys.LegacyLocalRotationKey,
                AnimationPropertyKeys.LocalRotationKey,
                jsonTime,
                Quaternion.identity);
            return ancestor * rotation * localRotation;
        }

        internal static T ReadTrackProperty<T>(
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
                anchor = Mathf.Max(anchor, source.GetTransformAnchorTime(jsonTime));
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
            canRetainMaterialColor = materialPathColors.Length == 0 && HasStandardMaterialWithoutLight(con);
            PullsTrackValues = canRetainMaterialColor && preparation.Names.Length <= 1;
            foreach (var name in preparation.Names)
            {
                AddParent(name);
            }
            RegisterAnimationUpdates();
        }

        private static bool HasStandardMaterialWithoutLight(GeometryContainer geometry)
        {
            return (geometry.MaterialDefinition.Shader is null or "Standard")
                && geometry.EnvironmentEnhancement.Components?.HasKey("ILightWithId") != true;
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

        public void UpdateAnimation() => UpdateAnimationState();

        private bool UpdateAnimationState()
        {
            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);

            if (container != null && container.ObjectData is BaseGrid obj)
            {
                // Track time can hold exactly one. Native fake notes finish there instead of waiting for a later frame.
                var fakeNoteFinished = container is NoteContainer && obj.CustomFake && time >= timeEnd;
                var noodleAnimationLifetime = time > timeEnd || fakeNoteFinished ? -1 : 1;
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
                    if (time > despawnTime || fakeNoteFinished)
                    {
                        BeatmapObjectContainerCollection
                            .GetCollectionForType(container.ObjectData.ObjectType)
                            .RecycleContainer(container.ObjectData);
                        AnimatedLife = false;
                        return false;
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

            if (AnimatedTrack)
            {
                AnimationTrack.UpdateTime(time);
                // Do not expose normal travel between Update and LateUpdate on definite-position objects.
                if (WorldPosition.Count > 0
                    && (container is NoteContainer or ObstacleContainer || timeBegin < time) && time < timeEnd)
                {
                    AnimationTrack.HoldDefinitePosition(time);
                }
            }

            return true;
        }

        // A refreshed enhancement binds after the track's last update, so publish held values before rendering it.
        internal void RefreshEnvironmentAnimation()
        {
            foreach (var source in tracks)
                source.PushToChild(this);

            if (UpdateAnimationState())
                ApplyAnimation();
        }

        public void ApplyAnimation()
        {
            if (RetainUnchangedMaterialColor())
            {
                return;
            }
            if (RetainUnchangedGeometryPose())
            {
                return;
            }
            if (PullsTrackValues)
            {
                PullCurrentTrackValues();
                geometryRequiresWorldWrite = (LocalPosition.Count == 0 && !geometryHasWorldPositionAnchor
                    && (WorldPosition.Count > 0 || OffsetPosition.Count > 0))
                    || (LocalRotation.Count == 0 && WorldRotation.Count > 0 && !geometryHasParentAnimator);
            }
            // Only apply properties supplied by the track. Defaults would reset untouched transform fields.
            if (directEnvironmentTarget)
            {
                ApplyDirectEnvironmentTargets();
                return;
            }

            var time = this.time ?? (Context.Atsc != null ? Context.Atsc.CurrentSongBpmTime : 0);
            if (TargetType == TargetTypes.Material)
            {
                materialCutout = 1f - Opacity.Default;
                FlushTransformAnimations();
            }
            else
            {
                ApplyTransformAnimation(time);
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

                var cutout = 1f - Opacity.Get();
                materialCutout = cutout;
                container.MpbController.Mpb.SetFloat(cutoutId, cutout);
                container.UpdateMaterials();
            }

            if (canRetainGeometryPose)
            {
                geometryBlockVersion = ((GeometryContainer)container).MaterialUpdateVersion;
            }
            if (canRetainMaterialColor)
            {
                materialBlockVersion = ((GeometryContainer)container).MaterialUpdateVersion;
            }

            if (UIMode.PreviewMode && !disableNoteLook && container is NoteContainer lookNote
                && lookNote.NoteData.Chains.Count == 0)
            {
                ApplyNoteLook(lookNote, time);
            }
        }

        // Standard geometry keeps its pose until a track changes. Rebuild its contributions together
        // so a changed property still combines with the track's other held values.
        private void PullCurrentTrackValues()
        {
            FlushPendingAnimations();
            foreach (var source in tracks)
            {
                source.PushEvaluatedValuesToChild(this);
            }
        }

        private void FlushTransformAnimations()
        {
            LocalRotation.Flush();
            WorldRotation.Flush();
            OffsetPosition.Flush();
            LocalPosition.Flush();
            WorldPosition.Flush();
            Scale.Flush();
        }

        private void ApplyTransformAnimation(float time)
        {
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

            if (Scale.Count > 0)
                LocalTarget.localScale = Scale.Get();

            // Rotation was already consumed above; reading the drained aggregator would reset it to identity.
            if (WorldTarget is Transform && WorldRotation.Count > 0)
                if (container is not GeometryContainer && (!trackParentTarget || trackParentTargetIsV2))
                    WorldTarget.localRotation = WorldRotation.Get();

            if (!trackParentTarget && container is not GeometryContainer && WorldPosition.Count > 0)
            {
                if ((container is NoteContainer or ObstacleContainer || timeBegin < time) && time < timeEnd)
                    AnimationTrack.HoldDefinitePosition(time);
                if (container is not null and not GeometryContainer)
                    container.transform.localPosition = WorldPosition.Get();
                else
                    WorldTarget.localPosition = WorldPosition.Get();
            }
        }

        private int GetTrackUpdateVersion()
        {
            var version = 0;
            for (var i = 0; i < tracks.Count; i++)
            {
                version += tracks[i].UpdateVersion;
            }
            return version;
        }

        private bool RetainUnchangedGeometryPose()
        {
            // Unanchored world positions and rotations must still counter motion of CM's track transform.
            if (canRetainGeometryPose && !geometryRequiresWorldWrite)
            {
                var version = GetTrackUpdateVersion();
                var geometry = (GeometryContainer)container;
                var unchanged = hasAppliedGeometryPose && geometryPoseTrackVersion == version
                    && geometryBlockVersion == geometry.MaterialUpdateVersion;
                geometryPoseTrackVersion = version;
                hasAppliedGeometryPose = true;
                if (unchanged)
                {
                    return true;
                }
            }
            return false;
        }

        private bool RetainUnchangedMaterialColor()
        {
            if (!canRetainMaterialColor)
            {
                return false;
            }
            var version = GetTrackUpdateVersion();
            var unchanged = materialTrackVersion == version;
            materialTrackVersion = version;
            if (!unchanged)
            {
                return false;
            }

            // These Standard materials have no light controller or material path replacing their color.
            var geometry = (GeometryContainer)container;
            if (materialBlockVersion != geometry.MaterialUpdateVersion)
            {
                var cutout = materialCutout;
                var mpb = container.MpbController.Mpb;
                // The geometry's transform track can also write dissolve into this block.
                if (mpb.GetFloat(cutoutId) != cutout)
                {
                    mpb.SetFloat(cutoutId, cutout);
                    container.UpdateMaterials();
                }
            }
            materialBlockVersion = geometry.MaterialUpdateVersion;
            if (!PullsTrackValues)
            {
                FlushPendingAnimations();
            }
            colorRestorePending = false;
            return true;
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
            if (normalTime >= 0.5f)
            {
                if (!note.HasHitLookRotation)
                {
                    // Native never writes a facing rotation when track time starts beyond its pre-hit rotation window.
                    if (!note.HasJumpRotation)
                        note.HitLookLocalRotation = Quaternion.identity;
                    // These paths can hold notes for hundreds of beats, outside the short jump history reconstructed below.
                    else if (time.HasValue || hasDefinitePosition)
                    {
                        SampleNotePose(note, 0.5f, false, out var position, out var rotation, out var worldRotation, out _);
                        var jsonHit = (float)BeatSaberSongContainer.Instance.Map
                            .SongBpmTimeToJsonTime(data.SongBpmTime).GetValueOrDefault(data.JsonTime);
                        var head = TracksManager.CameraManager.PlayerCameraController.GetPlayerHeadPosition(jsonHit);
                        var look = CalculateNoteLook(rotation, worldRotation,
                            Track.GetNoteSpawnRotation(note, 0.5f - 0.000001f), head, position, 0.5f);
                        note.HitLookLocalRotation = Quaternion.Inverse(rotation) * look;
                    }
                    else
                    {
                        // NoteJump holds the final pre-hit local rotation while its parent keeps animating.
                        var seconds = TimeHelper.GetPreviousPreviewSeconds(Context.Atsc.GetSecondsFromBeat(data.SongBpmTime));
                        var lastTime = (Context.Atsc.GetBeatFromSeconds(seconds) - data.SpawnSongBpmTime)
                            / (data.HalfJumpDuration * 2f);
                        note.HitLookLocalRotation = SampleNoteLookWindow(note, lastTime);
                    }
                    note.HasHitLookRotation = true;
                }
                note.DirectionTarget.localRotation = note.HitLookLocalRotation;
                return;
            }
            note.HasJumpRotation = true;
            note.HasHitLookRotation = false;
            if (!time.HasValue && normalTime > 0f && (hasDefinitePosition || normalTime >= NoteLookHistoryStart))
            {
                var look = SampleNoteLookWindow(note, normalTime);
                note.DirectionTarget.localRotation = look;
                return;
            }

            var baseRotation = note.DirectionTarget.parent.rotation;
            var target = note.DirectionTarget.position;
            // Native DoNoteLook reads the previous position before NoteJump writes this frame's position.
            if (!time.HasValue && normalTime > 0f)
                SampleNotePose(note, Mathf.Max(0f, normalTime - GetNoteLookFrameDuration(note)), false,
                    out target, out _, out _, out _);

            note.DirectionTarget.rotation = CalculateNoteLook(baseRotation, WorldTarget.rotation,
                Track.GetNoteSpawnRotation(note, normalTime),
                TracksManager.CameraManager.PlayerCameraController.GetPlayerHeadPosition(),
                target, normalTime);
        }

        private float GetNoteLookFrameDuration(NoteContainer note) => GetNoteLookDelta(note, 0);

        // Step between a replayed sample and the frame before it. Playing mode uses the recorded
        // per-frame clock steps so the window keeps the real history spacing; spacing every sample
        // by the current frame's delta rescales the whole window and visibly stutters rotation.
        private float GetNoteLookDelta(NoteContainer note, int framesAgo)
        {
            var seconds = 1f / TimeHelper.PreviewCallbackRate;
            if (Context.Atsc.IsPlaying && TracksManager.ObjectAnimationUpdates != null)
            {
                var recorded = TracksManager.ObjectAnimationUpdates.GetPlayingDeltaSeconds(framesAgo);
                if (recorded > 0f)
                    seconds = recorded;
            }
            return Context.Atsc.GetBeatFromSeconds(seconds) / (note.NoteData.HalfJumpDuration * 2f);
        }

        private Quaternion SampleNoteLookWindow(NoteContainer note, float end)
        {
            var local = Quaternion.identity;
            var map = BeatSaberSongContainer.Instance.Map;
            // Native facing retains the preceding visual up direction. Reconstruct only frames within
            // the jump, bounded to recent history so a cold seek never replays a long note lifetime.
            var back = 0f;
            var frameCount = 0;
            while (frameCount < NoteLookHistoryFrames && back < end)
            {
                back += GetNoteLookDelta(note, frameCount);
                frameCount++;
            }
            for (var i = frameCount; i >= 0; i--)
            {
                var sampleTime = Mathf.Max(0f, end - back);
                SampleNotePose(note, sampleTime, false, out _, out var rotation, out var world, out _);
                SampleNotePose(note, Mathf.Max(0f, sampleTime - GetNoteLookDelta(note, i)), false,
                    out var position, out _, out _, out _);
                var beat = Mathf.LerpUnclamped(note.NoteData.SpawnSongBpmTime, note.NoteData.DespawnSongBpmTime, sampleTime);
                var jsonTime = (float)map.SongBpmTimeToJsonTime(beat).GetValueOrDefault(beat);
                var head = TracksManager.CameraManager.PlayerCameraController.GetPlayerHeadPosition(jsonTime);
                var look = CalculateNoteLook(rotation, world, Track.GetNoteSpawnRotation(note, sampleTime),
                    head, position, sampleTime, rotation * local * Vector3.up);
                local = Quaternion.Inverse(rotation) * look;
                if (i > 0)
                    back -= GetNoteLookDelta(note, i - 1);
            }
            return local;
        }

        private static Quaternion CalculateNoteLook(
            Quaternion baseRotation,
            Quaternion worldRotation,
            Quaternion spawnRotation,
            Vector3 headPos,
            Vector3 target,
            float normalTime) => CalculateNoteLook(baseRotation, worldRotation, spawnRotation, headPos,
                target, normalTime, baseRotation * spawnRotation * Vector3.up);

        private static Quaternion CalculateNoteLook(
            Quaternion baseRotation,
            Quaternion worldRotation,
            Quaternion spawnRotation,
            Vector3 headPos,
            Vector3 target,
            float normalTime,
            Vector3 up)
        {
            var authoredRotation = baseRotation * spawnRotation;
            var inverseWorldRotation = Quaternion.Inverse(worldRotation);
            var headHeight = (inverseWorldRotation * headPos).y;
            var targetHeight = (inverseWorldRotation * target).y;
            headPos += worldRotation * Vector3.up * (Mathf.Lerp(headHeight, targetHeight, 0.8f) - headHeight);
            var direction = target - headPos;
            if (direction.sqrMagnitude < 0.0001f)
                return authoredRotation;

            // Noodle rotates the head ray through the authored base pose. Spawn progress remains independent of travel.
            var lookDirection = baseRotation * (inverseWorldRotation * direction.normalized);
            var look = Quaternion.LookRotation(lookDirection, up);
            return Quaternion.Lerp(authoredRotation, look, normalTime * 2f);
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

        private void ApplyDirectEnvironmentTargets()
        {
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

            if (!HaveDirectEnvironmentTracksChanged())
                return;

            if (hasLocalRotation)
                LocalTarget.localRotation = localRotation;

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
            }

            if (hasScale)
            {
                LocalTarget.localScale = scale;
                scaleChanged = true;
            }

            if (hasWorldRotation && !hasLocalRotation)
            {
                if (directEnvironmentSharedTrackBinding != null
                    && directEnvironmentSharedTrackBinding.TryGetLocalRotation(
                        Context.Atsc.CurrentJsonTime, worldRotation, out var anchoredLocalRotation))
                {
                    LocalTarget.localRotation = anchoredLocalRotation;
                }
                else
                {
                    WorldTarget.rotation = worldRotation;
                }
            }

            // Update only animated mesh overrides; untouched fields stay under the light controller's control.
            if (directEnvironmentHasBoxLight)
            {
                if (positionChanged)
                    directEnvironmentBoxLight.CaptureAuthoredPosition();

                if (scaleChanged)
                    directEnvironmentBoxLight.CaptureAuthoredScale();
            }
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
                    LocalTarget.localRotation = Quaternion.Inverse(geometryParentAnimator.GetTrackParentRotation(anchorTime)) * world;
                    geometryRotationAnchorReady = false;
                    return;
                }
                // A held world rotation must inherit later parent rotation, just as its held world position does.
                if (!geometryRotationAnchorReady || geometryRotationAnchorTime != anchorTime || geometryAnchorWorldRotation != world)
                {
                    geometryAnchoredLocalRotation = Quaternion.Inverse(geometryParentAnimator.GetTrackParentRotation(anchorTime)) * world;
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
                    ApplyDirectEnvironmentPosition(directEnvironmentSpawnLocalPosition, false);
                    if (directEnvironmentHasBoxLight)
                        directEnvironmentBoxLight.CaptureAuthoredPosition();

                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.RotationKey:
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    // The enhancement's local pose follows its current parent, including a rotating square frame.
                    LocalTarget.localRotation = directEnvironmentSpawnLocalRotation;
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

        // Restore the local pose because the original parent can animate while this target is reparented.
        // Rings retain their wave displacement, and box lights recapture the pose before refreshing.
        private void RestoreDirectEnvironmentSpawnPose()
        {
            if (directEnvironmentTargetIsTrackLaneRing && directEnvironmentTrackLaneRing != null)
            {
                // The transform already includes the ring's wave displacement, which is separate from its base offset.
                directEnvironmentTrackLaneRing.RebasePositionOffset(directEnvironmentRingPositionOffset);
                LocalTarget.localRotation = directEnvironmentOriginalLocalRotation;
                LocalTarget.localScale = directEnvironmentSpawnScale;
                if (directEnvironmentHasBoxLight)
                    directEnvironmentBoxLight.RecaptureAuthoredTransform();
                return;
            }

            LocalTarget.SetLocalPositionAndRotation(
                directEnvironmentOriginalLocalPosition,
                directEnvironmentOriginalLocalRotation);
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
                // A native child updated before its parent keeps the local position written at an instant cut.
                if (directEnvironmentSharedTrackBinding != null
                    && directEnvironmentSharedTrackBinding.TryGetLocalPosition(
                        Context.Atsc.CurrentJsonTime, position, out var localPosition))
                {
                    LocalTarget.localPosition = localPosition;
                }
                else
                {
                    LocalTarget.position = position;
                }
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

        // Birth sampling must not move the playhead or rewrite live tracks during a seek.
        internal void GetNoteSpawnPose(
            NoteContainer note,
            out Vector3 position,
            out Quaternion rotation,
            out Color color)
        {
            SampleNotePose(note, 0f, true, out position, out rotation, out _, out color);
        }

        private void SampleNotePose(
            NoteContainer note,
            float lifetime,
            bool applyTrackTime,
            out Vector3 position,
            out Quaternion rotation,
            out Quaternion worldRotation,
            out Color color)
        {
            var data = note.NoteData;
            var lanePosition = data.GetPosition();
            var sampleBeat = Mathf.LerpUnclamped(data.SpawnSongBpmTime, data.DespawnSongBpmTime, lifetime);
            var jsonSample = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(sampleBeat)
                .GetValueOrDefault(sampleBeat);
            var values = new SpawnPoseValues
            {
                Rotation = AnimatedTrack ? WorldRotation.BaseValue : Quaternion.Euler(note.AssignedTrack.RotationValue),
                LocalRotation = AnimatedTrack ? LocalRotation.BaseValue : Quaternion.identity,
                Color = data.Type == (int)NoteType.Bomb
                    ? data.CustomColor ?? new Color(0.1f, 0.1f, 0.1f)
                    : Colors.Default
            };
            var parent = Matrix4x4.identity;
            var parentRotation = Quaternion.identity;
            if (AnimatedTrack)
            {
                var pathTime = sampleBeat;
                var hasTrackTime = false;
                foreach (var source in tracks)
                {
                    foreach (var pair in source.AnimatedProperties)
                        values.Add(pair.Key, pair.Value, jsonSample);

                    var trackLifetime = ReadTrackProperty(
                        source,
                        AnimationPropertyKeys.LegacyTimeKey,
                        AnimationPropertyKeys.TimeKey,
                        jsonSample,
                        -1f);
                    if (applyTrackTime && !hasTrackTime && trackLifetime >= 0f)
                    {
                        pathTime = Mathf.LerpUnclamped(timeBegin, timeEnd, trackLifetime);
                        hasTrackTime = true;
                    }
                }
                foreach (var pair in AnimatedProperties)
                    values.Add(pair.Key, pair.Value, pathTime);

                if (tracks.Count > 0)
                {
                    var source = tracks[0];
                    parent = source.Animator != null
                        ? source.Animator.GetTrackParentMatrix(jsonSample)
                        : source.Track.ObjectParentTransform.localToWorldMatrix;
                    parentRotation = source.Animator != null
                        ? source.Animator.GetTrackParentRotation(jsonSample)
                        : source.Track.ObjectParentTransform.rotation;
                }
            }
            var jump = Track.GetNoteJumpPosition(note, lifetime);
            var start = new Vector3(Track.GetNoteJumpHorizontalPosition(note, lifetime), jump.x, jump.y);
            if (values.HasDefinitePosition)
            {
                start = new Vector3(
                    lanePosition.x,
                    lanePosition.y + BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset,
                    0f);
                start += values.DefinitePosition;
            }
            position = parent.MultiplyPoint3x4(values.Rotation * (start + values.Offset));
            worldRotation = parentRotation * values.Rotation;
            rotation = worldRotation * values.LocalRotation;
            color = values.Color;
        }

        private struct SpawnPoseValues
        {
            public Vector3 Offset;
            public Vector3 DefinitePosition;
            public bool HasDefinitePosition;
            public Quaternion Rotation;
            public Quaternion LocalRotation;
            public Color Color;

            public void Add(string key, IAnimateProperty property, float time)
            {
                if (time < property.StartTime
                    || (property is AnimateProperty<Vector3> vector && !vector.HasValueAt(time))
                    || (property is AnimateProperty<Quaternion> quaternion && !quaternion.HasValueAt(time))
                    || (property is AnimateProperty<Color> color && !color.HasValueAt(time)))
                    return;

                // AssignPathAnimation keeps inherited paths under track_ keys alongside per-object paths.
                switch (key)
                {
                    case AnimationPropertyKeys.LegacyPositionKey:
                    case AnimationPropertyKeys.OffsetPositionKey:
                    case "track_" + AnimationPropertyKeys.LegacyPositionKey:
                    case "track_" + AnimationPropertyKeys.OffsetPositionKey:
                        Offset += ((AnimateProperty<Vector3>)property).GetLerpedValue(time) * BeatmapConstant.LaneSize;
                        break;
                    case AnimationPropertyKeys.LegacyDefinitePositionKey:
                    case AnimationPropertyKeys.DefinitePositionKey:
                    case "track_" + AnimationPropertyKeys.LegacyDefinitePositionKey:
                    case "track_" + AnimationPropertyKeys.DefinitePositionKey:
                        DefinitePosition += ((AnimateProperty<Vector3>)property).GetLerpedValue(time) * BeatmapConstant.LaneSize;
                        HasDefinitePosition = true;
                        break;
                    case AnimationPropertyKeys.LegacyRotationKey:
                    case AnimationPropertyKeys.OffsetWorldRotationKey:
                    case "track_" + AnimationPropertyKeys.LegacyRotationKey:
                    case "track_" + AnimationPropertyKeys.OffsetWorldRotationKey:
                        Rotation *= ((AnimateProperty<Quaternion>)property).GetLerpedValue(time);
                        break;
                    case AnimationPropertyKeys.LegacyLocalRotationKey:
                    case AnimationPropertyKeys.LocalRotationKey:
                    case "track_" + AnimationPropertyKeys.LegacyLocalRotationKey:
                    case "track_" + AnimationPropertyKeys.LocalRotationKey:
                        LocalRotation *= ((AnimateProperty<Quaternion>)property).GetLerpedValue(time);
                        break;
                    default:
                        if (IsColorProperty(key))
                            Color *= ((AnimateProperty<Color>)property).GetLerpedValue(time);

                        break;
                }
            }
        }

        private void OnTimeChanged()
        {
            if (Context.Atsc.IsPlaying) return;

            ApplyStoppedSeek();
        }

        internal void ApplyStoppedSeek()
        {
            if (TargetType == TargetTypes.GameplayObject)
            {
                RefreshTrackAnimation();
                return;
            }

            ApplyCurrentPose();
        }

        private void ApplyCurrentPose()
        {
            hasAppliedGeometryPose = false;
            if (PullsTrackValues && TargetType != TargetTypes.Material)
            {
                PullCurrentTrackValues();
            }
            // Material color and dissolve reach the renderer in ApplyAnimation.
            // The empty material target needs no transform reset during a seek.
            if (TargetType == TargetTypes.Material)
            {
                return;
            }

            if (directEnvironmentTarget)
            {
                ApplyDirectEnvironmentTargets();
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
                    AddPointDef(source, f => Opacity.Add(f), floatParser, p, 0);
                    break;
                case AnimationPropertyKeys.LegacyDissolveArrowKey:
                case AnimationPropertyKeys.DissolveArrowKey:
                    AddPointDef(source, f => OpacityArrow.Add(f), floatParser, p, 0);
                    break;
                case AnimationPropertyKeys.LegacyLocalRotationKey:
                case AnimationPropertyKeys.LocalRotationKey:
                    AddPointDef(
                        source,
                        q => LocalRotation.Add(q),
                        quaternionParser,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyRotationKey:
                case AnimationPropertyKeys.OffsetWorldRotationKey:
                    AddPointDef(
                        source,
                        v => WorldRotation.Add(v),
                        quaternionParser,
                        p,
                        Quaternion.identity);
                    break;
                case AnimationPropertyKeys.LegacyPositionKey:
                case AnimationPropertyKeys.OffsetPositionKey:
                    AddPointDef(
                        source,
                        v => OffsetPosition.Add(v * BeatmapConstant.LaneSize),
                        vector3Parser,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyDefinitePositionKey:
                case AnimationPropertyKeys.DefinitePositionKey:
                    AddPointDef(
                        source,
                        v => WorldPosition.Add(v * BeatmapConstant.LaneSize),
                        vector3Parser,
                        p,
                        Vector3.zero);
                    break;
                case AnimationPropertyKeys.LegacyScaleKey:
                case AnimationPropertyKeys.ScaleKey:
                    AddPointDef(
                        source,
                        v => Scale.Add(v),
                        vector3Parser,
                        p,
                        Vector3.one);
                    break;
                case AnimationPropertyKeys.LegacyColorKey:
                case AnimationPropertyKeys.ColorKey:
                    AddPointDef<Color>(source, (Color c) => Colors.Add(c), colorParser, p, Color.white);
                    break;
                case AnimationPropertyKeys.LegacyInteractableKey:
                case AnimationPropertyKeys.InteractableKey:
                    AddPointDef(source, f => Interactable.Add(f), floatParser, p, 1);
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
                // A first AssignPathAnimation has no previous path to blend from, even with a duration.
                property.UsePathClock = true;
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

            // Pose sampling needs the fixed contributions without consuming the current frame's animated values.
            public T BaseValue => Keep > 0 ? retainedValue : Default;

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
