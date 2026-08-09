using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class TracksManager : MonoBehaviour
{
    [SerializeField] private Track trackPrefab;
    [SerializeField] private Transform tracksParent;
    [SerializeField] private RotationEventGridContainer rotationEventGridContainer;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private VariableNJSProvider vNjsProvider;

    private readonly Stack<Track> trackPool = new();
    private readonly Dictionary<Vector3, Track> loadedTracks = new();
    private readonly Dictionary<string, TrackAnimator> animationTracks = new();

    private readonly List<BeatmapObjectContainerCollection> objectContainerCollections = new();

    private float position;

    private float lowestRotation;
    private float highestRotation;

    private void Start()
    {
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note));
        objectContainerCollections.Add(
            BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle));
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc));
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain));
    }

    private Track GetOrCreateTrack()
    {
        var track = trackPool.Count > 0 ? trackPool.Pop() : Instantiate(trackPrefab);
        track.gameObject.SetActive(true);
        track.SelfTransform.SetParent(tracksParent, false);
        return track;
    }

    public void Remove(Track track)
    {
        track.gameObject.SetActive(false);
        track.SelfTransform.SetParent(tracksParent, false);
        track.ResetData();
        trackPool.Push(track);
    }

    /// <summary>
    ///     Create a new <see cref="Track" /> with the specified global rotation. If a track already exists with that rotation,
    ///     it will simply return that track.
    /// </summary>
    /// <param name="rotation">Global euler rotation</param>
    /// <returns></returns>
    public Track CreateTrack(Vector3 rotation)
    {
        if (loadedTracks.TryGetValue(rotation, out var track)) return track;

        track = GetOrCreateTrack();
        track.gameObject.name = $"Track [{rotation.x}, {rotation.y}, {rotation.z}]";

        track.vNjsProvider = vNjsProvider;
        track.enabled = true;
        track.AssignRotationValue(rotation);
        track.UpdatePosition(position);

        loadedTracks.Add(rotation, track);
        return track;
    }

    /// <summary>
    ///     Create a new <see cref="Track" /> with the specified rotation around the Y axis.
    ///     It simply calls <see cref="CreateTrack(Vector3)" /> with a Vector3 of (0, <paramref name="rotation" />, 0)/>
    /// </summary>
    /// <param name="rotation">Y-axis rotation.</param>
    public Track CreateTrack(float rotation)
    {
        var roundedRotation = FloatModulo(rotation, 360);
        var vectorRotation = new Vector3(0, roundedRotation, 0);
        return CreateTrack(vectorRotation);
    }

    public TrackAnimator GetAnimationTrack(string name)
    {
        if (animationTracks.TryGetValue(name, out var animator)) return animator;

        var track = GetOrCreateTrack();
        track.gameObject.name = name;

        animator = track.gameObject.GetOrAddComponent<TrackAnimator>();
        animator.enabled = false;
        animator.Atsc = atsc;
        animator.Track = track;
        animator.Track.vNjsProvider = vNjsProvider;
        animator.Track.enabled = true;

        animationTracks.Add(name, animator);
        return animator;
    }

    // Used for world rotation
    public Track CreateIndividualTrack(BaseGrid obj)
    {
        // TODO: This is the same math used for 90/360 tacks, but does it actually handle BPM changes?
        var pos = -1 * obj.JsonTime * EditorScaleController.EditorScale;
        var track = GetOrCreateTrack();
        track.gameObject.name = $"Track Object {obj.JsonTime}";

        track.vNjsProvider = vNjsProvider;
        track.enabled = true;
        track.UpdatePosition(pos);

        var rotation = BeatSaberSongContainer.Instance.Map.MajorVersion == 4
            ? obj.Rotation
            : GetRotationAtTime(obj.SongBpmTime);
        track.AssignRotationValue(obj.CustomWorldRotation ?? new Vector3(0, rotation, 0));
        return track;
    }

    public Track GetTrackAtTime(float beatInSongBpm, int rotation)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        var rot = BeatSaberSongContainer.Instance.Map.MajorVersion == 4
            ? rotation
            : GetRotationAtTime(beatInSongBpm);

        return CreateTrack(rot);
    }

    public Track GetTrackAtTime(float beatInSongBpm)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        var rot = GetRotationAtTime(beatInSongBpm);

        return CreateTrack(rot);
    }

    public float GetRotationAtTime(float beatInSongBpm)
    {
        float rotation = 0;
        foreach (var rotationEvent in rotationEventGridContainer.MapObjects)
        {
            if (rotationEvent.SongBpmTime > beatInSongBpm + 0.001f) continue;
            if (Mathf.Approximately(rotationEvent.SongBpmTime, beatInSongBpm)
                && rotationEvent.Type == (int)EventTypeValue.LateRotationEventType)
                continue;

            rotation += rotationEvent.Rotation;
            if (rotation < lowestRotation) lowestRotation = rotation;
            if (rotation > highestRotation) highestRotation = rotation;
        }

        return rotation;
    }

    public void RefreshTracks()
    {
        foreach (var collection in objectContainerCollections)
        {
            foreach (var container in collection.LoadedContainers.Values)
            {
                if (container is ObstacleContainer obstacle && obstacle.IsRotatedByNoodleExtensions) continue;
                if (container.Animator != null && container.Animator.AnimatedTrack) continue;
                var track = GetTrackAtTime(
                    container.ObjectData.SongBpmTime,
                    container.ObjectData is BaseGrid grid ? grid.Rotation : 0);
                track.AttachContainer(container);
                container.UpdateGridPosition();
            }
        }
    }

    private float FloatModulo(float x, float m) =>
        //float largestFactor = Mathf.Floor(x / m); //Same functionality as x % m but with floats cuz fuck you
        //float regularModulo = x - largestFactor * m;

        //float moduloAddBase = regularModulo + m;
        //float betterLargestFactor = Mathf.Floor(moduloAddBase / m);
        //float betterModulo = moduloAddBase - betterLargestFactor * m;
        x - (Mathf.Floor(x / m) * m) + m - (Mathf.Floor((x - (Mathf.Floor(x / m) * m) + m) / m) * m);

    //Take our position from AudioTimeSyncController and broadcast that to every track.
    public void UpdatePosition(float position)
    {
        this.position = position;
        foreach (var track in loadedTracks.Values) track.UpdatePosition(position);
    }
}
