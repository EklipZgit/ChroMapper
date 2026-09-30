using System;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class ObstacleGridContainer : BeatmapObjectContainerCollection<BaseObstacle>
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    [SerializeField] private VariableNJSProvider vNjsProvider;

    public override ObjectType ContainerType => ObjectType.Obstacle;

    // KamikaziLightArrayTest: loading a map while a preview/playing mode is active fires OnTimeChanged during
    // the 03_Mapper scene Start (AudioTimeSyncController.Start -> ResetTime -> UpdateMovables) before any map
    // data reaches this collection, so OnTimeChanged -> RefreshWalls dereferenced null sorted arrays and the
    // uncaught copy from EditorScaleController.Start crashed the whole scene-load sequence. Initialize both to
    // empty arrays so the hot path can rely on them existing before the first map load.
    public BaseObstacle[] SpawnSortedObjects = Array.Empty<BaseObstacle>();
    private int spawnIndex;

    public BaseObstacle[] DespawnSortedObjects = Array.Empty<BaseObstacle>();
    private int despawnIndex;

    private static readonly int mainAlpha = Shader.PropertyToID("_MainAlpha");

    internal override void SubscribeToCallbacks()
    {
        // Salty wall regressions (SaltyBeat219PlacementParityTest
        // .Beat221WallsStayAtAuthoredDepthOnImmediateReverseSeek /
        // SaltyFullMapPlacementParityTest.Beat221WallsSeekSynchronouslyToStableGameDepth): stopped
        // seeks rebuild the wall pool on OnTimeFlushPending — the FIRST event in the stopped-seek
        // sequence, before OnTimeChangedEarly pushes held track values — so spawned wall animators
        // already exist when the push lands and one OnTimeChanged applies it; the gameplay-object
        // OffsetPosition hold keeps that pose on later frames.
        BeatmapContext.Atsc.OnTimeFlushPending += PrepareStoppedSeek;
        BeatmapContext.Atsc.OnTimeChanged += OnTimeChanged;
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggled;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;

        Settings.NotifyBySettingName(nameof(Settings.ObstacleOpacity), ObstacleOpacityChanged);
        ObstacleOpacityChanged(Settings.Instance.ObstacleOpacity);
    }

    private void HandlePlayToggled(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        foreach (ObstacleContainer obj in LoadedContainers.Values) obj.SetIndicators(!this.isPlaying);
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnTimeFlushPending -= PrepareStoppedSeek;
        BeatmapContext.Atsc.OnTimeChanged -= OnTimeChanged;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;

        Settings.ClearSettingNotifications(nameof(Settings.ObstacleOpacity));
    }

    private void ObstacleOpacityChanged(object obj) => Shader.SetGlobalFloat(mainAlpha, (float)obj);

    public override void RefreshPool(bool force)
    {
        if (UIMode.AnimationMode)
        {
            SpawnSortedObjects = MapObjects
                .OrderBy(o => o.SongBpmTime - Mathf.Max(o.HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats))
                .ToArray();
            DespawnSortedObjects = MapObjects
                .OrderBy(o =>
                    o.SongBpmTime
                    + o.DurationSongBpmTime
                    + Mathf.Max(o.HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats))
                .ToArray();
            RefreshWalls();
        }
        else
        {
            base.RefreshPool(force);
        }
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public void UpdateColor(Color color) => obstacleAppearance.NormalColor = color;

    private bool updateFrame = false;
    private bool isPlaying;

    internal override void LateUpdate()
    {
        if (!UIMode.AnimationMode) base.LateUpdate();
    }

    // Same wall regressions: OnTimeFlushPending is only emitted while the audio is stopped, so this
    // refreshes the pool before any track's held-value push on the same seek; OnTimeChanged keeps
    // only the playing-time spawn/despawn loops.
    private void PrepareStoppedSeek()
    {
        if (UIMode.AnimationMode) RefreshWalls();
    }

    private void OnTimeChanged()
    {
        if (!UIMode.AnimationMode || !BeatmapContext.Atsc.IsPlaying) return;

        var time = BeatmapContext.Atsc.CurrentSongBpmTime;
        while (spawnIndex < SpawnSortedObjects.Length
            && time + Track.JUMP_TIME
            >= SpawnSortedObjects[spawnIndex].SongBpmTime
            - Mathf.Max(SpawnSortedObjects[spawnIndex].HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats))
        {
            if (SpawnSortedObjects[spawnIndex].HasMatchingTrack(TrackFilterID))
                CreateContainerFromPool(SpawnSortedObjects[spawnIndex]);
            ++spawnIndex;
        }

        while (despawnIndex < DespawnSortedObjects.Length
            && time
            >= DespawnSortedObjects[despawnIndex].SongBpmTime
            + DespawnSortedObjects[despawnIndex].DurationSongBpmTime
            + Mathf.Max(
                DespawnSortedObjects[despawnIndex].HalfJumpDuration,
                vNjsProvider.MaxHalfJumpDurationInBeats))
        {
            var objectData = DespawnSortedObjects[despawnIndex];
            if (LoadedContainers.ContainsKey(objectData))
            {
                if (!LoadedContainers[objectData].Animator.AnimatedLife)
                    RecycleContainer(objectData);
                else
                    LoadedContainers[objectData].Animator.ShouldRecycle = true;
            }

            ++despawnIndex;
        }
    }

    private void RefreshWalls()
    {
        var time = BeatmapContext.Atsc.CurrentSongBpmTime;
        foreach (var obj in LoadedContainers.Values.ToList())
        {
            RecycleContainer(obj.ObjectData);
        }

        GetIndexes(
            time,
            (i) => SpawnSortedObjects[i].SongBpmTime
                - Mathf.Max(SpawnSortedObjects[spawnIndex].HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats),
            SpawnSortedObjects.Length,
            out spawnIndex,
            out _
        );
        GetIndexes(
            time,
            (i) => DespawnSortedObjects[i].SongBpmTime
                + DespawnSortedObjects[despawnIndex].DurationSongBpmTime
                + Mathf.Max(
                    DespawnSortedObjects[despawnIndex].HalfJumpDuration,
                    vNjsProvider.MaxHalfJumpDurationInBeats),
            DespawnSortedObjects.Length,
            out despawnIndex,
            out _
        );
        // SaltyFullMapPlacementParityTest.Beat529TimingWindowAndBeat609WallsMatchPlaybackAfterScrubs /
        // SaltyBeat531TimingWindowParityTest.TimeFrozenWallsSurviveStoppedSeekPastAuthoredDuration:
        // an AnimateTrack "time"/_time property freezes the wall's animated lifetime inside its
        // authored window (Heck GetTimeProperty picks the first such property across the object's
        // authored track list). A stopped seek past the raw spawn+duration window must keep such a
        // wall spawned when the evaluated normalized time stays in [0,1]; ordinary expired walls
        // still follow the raw window.
        var jsonTime = BeatmapContext.Atsc.CurrentJsonTime;
        var toSpawn = SpawnSortedObjects.Where(o =>
            (o.SongBpmTime - Mathf.Max(o.HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats) <= time
                && (time
                    < o.SongBpmTime
                    + o.DurationSongBpmTime
                    + Mathf.Max(o.HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats)
                    || HasFrozenLifetimeAtSeek(o, time, jsonTime))));
        foreach (var obj in toSpawn)
        {
            if (!obj.HasMatchingTrack(TrackFilterID)) continue;
            var expired = time
                >= obj.SongBpmTime
                + obj.DurationSongBpmTime
                + Mathf.Max(obj.HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats);
            CreateContainerFromPool(obj);
            // The frozen wall outlived its raw window; flag it so a later unfrozen animated time
            // can recycle it through ObjectAnimator's normal lifetime check.
            if (expired
                && LoadedContainers.TryGetValue(obj, out var spawned)
                && spawned is ObstacleContainer wall
                && wall.Animator != null)
            {
                wall.Animator.ShouldRecycle = true;
            }
        }
    }

    // Heck GetTimeProperty picks the FIRST authored track that carries a time property — even
    // when that property's value would not freeze the lifetime — so an earlier non-freezing
    // time property must win over a later track's frozen one. Only tracks with no time property
    // at all fall through to the next authored name.
    private bool HasFrozenLifetimeAtSeek(BaseObstacle obj, float songBpmTime, float jsonTime)
    {
        if (obj.SongBpmTime > songBpmTime) return false;
        switch (obj.CustomTrack)
        {
            case SimpleJSON.JSONString name:
                return TryGetTimeProperty(name.Value, jsonTime, out var single)
                    && single >= 0f && single <= 1f;
            case SimpleJSON.JSONArray tracks:
                foreach (var node in tracks.Children)
                {
                    if (node is SimpleJSON.JSONString trackName
                        && TryGetTimeProperty(trackName.Value, jsonTime, out var normalTime))
                    {
                        return normalTime >= 0f && normalTime <= 1f;
                    }
                }
                return false;
            default:
                return false;
        }
    }

    private bool TryGetTimeProperty(string trackName, float jsonTime, out float normalized)
    {
        normalized = float.NaN;
        if (!tracksManager.TryGetAnimationTrack(trackName, out var animator) || animator == null)
            return false;
        foreach (var key in timePropertyKeys)
        {
            if (animator.AnimatedProperties.TryGetValue(key, out var property)
                && property is Beatmap.Animations.AnimateProperty<float> timeProperty
                && !timeProperty.IsEmpty())
            {
                normalized = timeProperty.GetLerpedValue(jsonTime);
                return true;
            }
        }
        return false;
    }

    private static readonly string[] timePropertyKeys = { "_time", "time" };

    protected override void HandleObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    protected override void HandleObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    public override ObjectContainer CreateContainer()
    {
        var con = ObstacleContainer.SpawnObstacle(null, tracksManager, ref obstaclePrefab);
        con.Animator.Context = BeatmapContext;
        con.Animator.TracksManager = tracksManager;
        return con;
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var obstacle = con as ObstacleContainer;
        obstacle.SwitchMaterial();
        if (!obstacle.IsRotatedByNoodleExtensions && !obstacle.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(obj.SongBpmTime, obstacle.ObstacleData.Rotation);
            track.AttachContainer(con);
        }

        obstacle.SetIndicators(!this.isPlaying);

        obstacleAppearance.SetObstacleAppearance(obstacle);
    }

    // Where is a good global place to dump this? It's much faster than List.BinarySearch
    private void GetIndexes(float time, Func<int, float> getter, int count, out int prev, out int next)
    {
        prev = 0;
        next = count;

        while (prev < next - 1)
        {
            int m = (prev + next) / 2;
            float itemTime = getter(m);

            if (itemTime < time)
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
