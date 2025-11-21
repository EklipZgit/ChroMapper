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

    [FormerlySerializedAs("obstacleAppearanceSO")] [SerializeField]
    private ObstacleAppearanceSO obstacleAppearanceSo;

    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    [SerializeField] private VariableNJSProvider vNjsProvider;

    public override ObjectType ContainerType => ObjectType.Obstacle;

    public BaseObstacle[] SpawnSortedObjects;
    private int spawnIndex;

    public BaseObstacle[] DespawnSortedObjects;
    private int despawnIndex;

    private static readonly int mainAlpha = Shader.PropertyToID("_MainAlpha");

    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.OnTimeChanged += OnTimeChanged;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;

        Settings.NotifyBySettingName(nameof(Settings.ObstacleOpacity), ObstacleOpacityChanged);
        ObstacleOpacityChanged(Settings.Instance.ObstacleOpacity);
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.OnTimeChanged -= OnTimeChanged;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;

        Settings.ClearSettingNotifications(nameof(Settings.ObstacleOpacity));
    }

    private void ObstacleOpacityChanged(object obj) => Shader.SetGlobalFloat(mainAlpha, (float)obj);

    public override void RefreshPool(bool force)
    {
        if (UIMode.AnimationMode)
        {
            SpawnSortedObjects = MapObjects
                .OrderBy(o => o.SongBpmTime - Mathf.Max(o.Hjd, vNjsProvider.MaxHalfJumpDurationInBeats))
                .ToArray();
            DespawnSortedObjects = MapObjects
                .OrderBy(o =>
                    o.SongBpmTime + o.DurationSongBpmTime + Mathf.Max(o.Hjd, vNjsProvider.MaxHalfJumpDurationInBeats))
                .ToArray();
            RefreshWalls();
        }
        else
        {
            base.RefreshPool(force);
        }
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public void UpdateColor(Color obstacle) => obstacleAppearanceSo.DefaultObstacleColor = obstacle;

    private bool updateFrame = false;

    internal override void LateUpdate()
    {
        if (!UIMode.AnimationMode) base.LateUpdate();
    }

    private void OnTimeChanged()
    {
        if (!UIMode.AnimationMode) return;

        var time = AudioTimeSyncController.CurrentSongBpmTime;
        if (AudioTimeSyncController.IsPlaying)
        {
            while (spawnIndex < SpawnSortedObjects.Length
                && time + Track.JUMP_TIME
                >= SpawnSortedObjects[spawnIndex].SongBpmTime
                - Mathf.Max(SpawnSortedObjects[spawnIndex].Hjd, vNjsProvider.MaxHalfJumpDurationInBeats))
            {
                if (SpawnSortedObjects[spawnIndex].HasMatchingTrack(TrackFilterID))
                    CreateContainerFromPool(SpawnSortedObjects[spawnIndex]);
                ++spawnIndex;
            }

            while (despawnIndex < DespawnSortedObjects.Length
                && time
                >= DespawnSortedObjects[despawnIndex].SongBpmTime
                + DespawnSortedObjects[despawnIndex].DurationSongBpmTime
                + Mathf.Max(DespawnSortedObjects[despawnIndex].Hjd, vNjsProvider.MaxHalfJumpDurationInBeats))
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
        else
        {
            RefreshWalls();
        }
    }

    private void RefreshWalls()
    {
        var time = AudioTimeSyncController.CurrentSongBpmTime;
        foreach (var obj in LoadedContainers.Values.ToList())
        {
            RecycleContainer(obj.ObjectData);
        }

        GetIndexes(
            time,
            (i) => SpawnSortedObjects[i].SongBpmTime
                - Mathf.Max(SpawnSortedObjects[spawnIndex].Hjd, vNjsProvider.MaxHalfJumpDurationInBeats),
            SpawnSortedObjects.Length,
            out spawnIndex,
            out _
        );
        GetIndexes(
            time,
            (i) => DespawnSortedObjects[i].SongBpmTime
                + DespawnSortedObjects[despawnIndex].DurationSongBpmTime
                + Mathf.Max(DespawnSortedObjects[despawnIndex].Hjd, vNjsProvider.MaxHalfJumpDurationInBeats),
            DespawnSortedObjects.Length,
            out despawnIndex,
            out _
        );
        var toSpawn = SpawnSortedObjects.Where(o =>
            (o.SongBpmTime - Mathf.Max(o.Hjd, vNjsProvider.MaxHalfJumpDurationInBeats) <= time
                && time
                < o.SongBpmTime + o.DurationSongBpmTime + Mathf.Max(o.Hjd, vNjsProvider.MaxHalfJumpDurationInBeats)));
        foreach (var obj in toSpawn)
        {
            if (obj.HasMatchingTrack(TrackFilterID)) CreateContainerFromPool(obj);
        }
    }

    protected override void HandleObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    protected override void HandleObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    public override ObjectContainer CreateContainer()
    {
        var con = ObstacleContainer.SpawnObstacle(null, tracksManager, ref obstaclePrefab);
        con.Animator.Atsc = AudioTimeSyncController;
        con.Animator.TracksManager = tracksManager;
        return con;
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var obstacle = con as ObstacleContainer;
        obstacle.SwitchMaterial();
        if (!obstacle.IsRotatedByNoodleExtensions && !obstacle.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(obj.SongBpmTime);
            track.AttachContainer(con);
        }

        obstacleAppearanceSo.SetObstacleAppearance(obstacle);
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
