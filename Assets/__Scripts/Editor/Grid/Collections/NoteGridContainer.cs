using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class NoteGridContainer : BeatmapObjectContainerCollection<BaseNote>
{
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;

    [FormerlySerializedAs("noteAppearanceSO")] [SerializeField]
    private NoteAppearanceSO noteAppearanceSo;

    [SerializeField] private TracksManager tracksManager;

    [SerializeField] private CountersPlusController countersPlus;

    private readonly List<ObjectContainer> objectsAtSameTime = new();
    private readonly NoteJumpDurationIndex jumpDurations = new();
    private bool isPlaying;
    private bool useNoodleJump;
    private string disableNoteGravityKey;
    private const string LegacyDisableNoteGravityKey = "_disableNoteGravity";
    private const string DisableNoteGravityKey = "disableNoteGravity";
    private NoteSpawnEffect spawnEffectPrefab;

    public static bool ShowArcVisualizer { get; private set; }

    public override ObjectType ContainerType => ObjectType.Note;

    protected override void OnMapObjectsAssigned()
    {
        jumpDurations.Reset(MapObjects);
        useNoodleJump = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements.Contains("Noodle Extensions");
        disableNoteGravityKey = BeatSaberSongContainer.Instance.Map.MajorVersion == 2
            ? LegacyDisableNoteGravityKey
            : DisableNoteGravityKey;
    }

    public override void SilentRemoveObject(BaseObject obj)
    {
        base.SilentRemoveObject(obj);
        if (obj is BaseNote note && !ContainsObject(note))
        {
            jumpDurations.Remove(note);
            RefreshJumpStatesAtTime(note.JsonTime);
        }
    }

    internal override void SubscribeToCallbacks()
    {
        spawnEffectPrefab = Resources.Load<NoteSpawnEffect>("NoteSpawnEffect");
        SpawnCallbackController.OnNotePassedThreshold += SpawnCallback;
        SpawnCallbackController.OnRecursiveNoteCheckFinished += OnRecursiveCheckFinished;
        DespawnCallbackController.OnNotePassedThreshold += DespawnCallback;
        // Refresh the paused spawn window before track values are pushed for a stopped seek. New note
        // animators must exist before OnTimeChangedEarly applies the held values, even when the seek stays
        // inside one chunk.
        BeatmapContext.Atsc.OnTimeFlushPending += PrepareStoppedSeek;
        BeatmapContext.Atsc.OnPlayToggled += OnPlayToggle;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;

        Settings.NotifyBySettingName(nameof(Settings.NoteColorMultiplier), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.ArrowColorMultiplier), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.ArrowColorWhiteBlend), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.AccurateNoteSize), AppearanceChanged);
        Settings.NotifyBySettingName(nameof(Settings.HideNoteSpawnEffect), AppearanceChanged);
    }

    internal override void UnsubscribeToCallbacks()
    {
        jumpDurations.Clear();
        SpawnCallbackController.OnNotePassedThreshold -= SpawnCallback;
        SpawnCallbackController.OnRecursiveNoteCheckFinished -= OnRecursiveCheckFinished;
        DespawnCallbackController.OnNotePassedThreshold -= DespawnCallback;
        BeatmapContext.Atsc.OnTimeFlushPending -= PrepareStoppedSeek;
        BeatmapContext.Atsc.OnPlayToggled -= OnPlayToggle;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;

        Settings.ClearSettingNotifications(nameof(Settings.NoteColorMultiplier));
        Settings.ClearSettingNotifications(nameof(Settings.ArrowColorMultiplier));
        Settings.ClearSettingNotifications(nameof(Settings.ArrowColorWhiteBlend));
        Settings.ClearSettingNotifications(nameof(Settings.AccurateNoteSize));
        Settings.StopNotifyingBySettingName(nameof(Settings.HideNoteSpawnEffect), AppearanceChanged);
    }

    private void PrepareStoppedSeek()
    {
        if (UIMode.PreviewMode)
            RefreshPool();
    }

    private void OnPlayToggle(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        if (!this.isPlaying) RefreshPool();

        foreach (NoteContainer obj in LoadedContainers.Values) obj.SetIndicators(!this.isPlaying);
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    private void AppearanceChanged(object _) => RefreshPool(true);

    //We don't need to check index as that's already done further up the chain
    private void SpawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            if (!LoadedContainers[objectData].Animator.AnimatedLife)
                RecycleContainer(objectData);
            else
                LoadedContainers[objectData].Animator.ShouldRecycle = true;
        }
    }

    private void OnRecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();

    public override void RefreshPool(bool forceRefresh = false)
    {
        if (UIMode.PreviewMode && !BeatmapContext.Atsc.IsPlaying)
        {
            var time = BeatmapContext.Atsc.CurrentSongBpmTime;
            var spawnOffset = SpawnCallbackController.Offset;
            var span = MapObjects.AsSpan();
            var walkStart = span.LowerBoundBy(time + spawnOffset, obj => obj.SongBpmTime);
            var spawnFloor = walkStart < span.Length ? span[walkStart].JsonTime : float.PositiveInfinity;

            // Hit-time order differs from jump-in order. Bound the query by the longest current jump,
            // then check each candidate so a short-jump note cannot hide a later long-jump note.
            var upperBound = UIMode.AnimationMode
                ? Mathf.Max(time + spawnOffset, time + Mathf.Max(jumpDurations.Maximum, spawnOffset) + Track.JUMP_TIME)
                : time + spawnOffset;
            RefreshPool(
                Mathf.Min(time + DespawnCallbackController.Offset, time + spawnOffset),
                upperBound,
                forceRefresh,
                new PausedGameplayNoteFilter(
                    time + DespawnCallbackController.Offset,
                    time + spawnOffset,
                    spawnFloor,
                    time,
                    spawnOffset,
                    UIMode.AnimationMode));
            return;
        }

        base.RefreshPool(forceRefresh);
    }

    // TODO(EklipZ) we should do the same thing for obstacles. Their jump-in isn't being rendered in the Playing / Preview pause view still. 
    // TODO(EklipZ) what about chains / arcs / bombs as well?
    private readonly struct PausedGameplayNoteFilter : IContainerPoolFilter
    {
        private readonly float despawnFloor;
        private readonly float spawnCap;
        private readonly float spawnFloor;
        private readonly float time;
        private readonly float spawnOffset;
        private readonly bool animationLookahead;

        public PausedGameplayNoteFilter(
            float despawnFloor,
            float spawnCap,
            float spawnFloor,
            float time,
            float spawnOffset,
            bool animationLookahead)
        {
            this.despawnFloor = despawnFloor;
            this.spawnCap = spawnCap;
            this.spawnFloor = spawnFloor;
            this.time = time;
            this.spawnOffset = spawnOffset;
            this.animationLookahead = animationLookahead;
        }

        // Use each note's HalfJumpDuration in animation preview so paused preview matches playback. Ordinary
        // editing uses the shared spawn offset.
        public bool Includes(BaseNote note)
        {
            if (note.SongBpmTime >= despawnFloor && note.SongBpmTime <= spawnCap)
                return true;
            if (note.JsonTime < spawnFloor)
                return false;
            var lookahead = animationLookahead
                ? Mathf.Max(note.HalfJumpDuration, spawnOffset) + Track.JUMP_TIME
                : spawnOffset;
            return note.SongBpmTime <= time + lookahead;
        }
    }

    public void UpdateColor(Color red, Color blue) => noteAppearanceSo.UpdateColor(red, blue);

    public override ObjectContainer CreateContainer()
    {
        ObjectContainer con = NoteContainer.SpawnBeatmapNote(null, ref notePrefab);
        con.Animator.Context = BeatmapContext;
        con.Animator.TracksManager = tracksManager;
        var note = (NoteContainer)con;
        note.SpawnEffect = Instantiate(spawnEffectPrefab, transform);
        return con;
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var note = con as NoteContainer;
        var noteData = obj as BaseNote;
        UpdateJumpState(note, noteData);
        noteAppearanceSo.SetNoteAppearance(note);
        note.Setup();
        note.SetIndicators(!isPlaying);
        note.DirectionTargetEuler = NoteContainer.Directionalize(noteData);
        note.DirectionTarget.localEulerAngles = note.DirectionTargetEuler;

        if (!note.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(obj.SongBpmTime, note.NoteData.Rotation);
            track.AttachContainer(con);
        }

        if (UIMode.AnimationMode && obj.CustomTrack != null && note.Animator.enabled)
        {
            tracksManager.PushHeldValuesToChild(obj.CustomTrack, note.Animator);
            note.Animator.ApplyAnimation();
        }
        note.SpawnEffect.Bind(note, BeatmapContext);
    }

    internal override void LateUpdate()
    {
        base.LateUpdate();
        if (!UIMode.PreviewMode)
            return;

        foreach (NoteContainer note in LoadedContainers.Values)
            note.SpawnEffect.Evaluate();
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        jumpDurations.Add((BaseNote)obj);
        RefreshJumpStatesAtTime(obj.JsonTime);
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        jumpDurations.Remove((BaseNote)obj);
        RefreshJumpStatesAtTime(obj.JsonTime);
        countersPlus.UpdateStatistic(CountersPlusStatistic.Notes);
    }

    private void UpdateJumpState(NoteContainer container, BaseNote note)
    {
        var floorOffset = CalculateFloorOffset(note);
        var seconds = BeatmapContext.Atsc.GetSecondsFromBeat(note.SongBpmTime);
        container.JumpRotationOffset = NoteJumpRotation.GetMiddleRotationOffset(seconds, floorOffset);
        var heights = CalculateJumpHeights(note, floorOffset.y);
        container.JumpStartHeight = heights.x;
        container.JumpApexHeight = heights.y;
    }

    internal Vector2 CalculateJumpHeights(BaseNote note) => CalculateJumpHeights(note, CalculateFloorOffset(note).y);

    private Vector2 CalculateJumpHeights(BaseNote note, float floorHeight)
    {
        var position = note.GetPosition();
        var startHeight = note.CustomData?.GetValueOrDefault(disableNoteGravityKey, null)?.AsBool == true
            ? position.y + BeatmapConstant.YOffset
            : floorHeight;
        // Native notes use the shipped apex table. Noodle replaces it with a continuous layer formula.
        var apexHeight = useNoodleJump
            ? 0.525f * (position.y / BeatmapConstant.LaneSize) + 0.858333f
            : note.PosY switch { 0 => 0.85f, 1 => 1.4f, _ => 1.9f };
        return new Vector2(startHeight, apexHeight);
    }

    private Vector2 CalculateFloorOffset(BaseNote note)
    {
        var position = note.GetPosition();
        var floorLayer = 0;
        var reachedNote = false;
        // Movement and the random turn use the stacked floor layer rather than the authored layer.
        // Query authoritative data when binding the container so loading order cannot change the jump.
        foreach (var other in GetBetween(note.JsonTime - Epsilon, note.JsonTime + Epsilon))
        {
            if (ReferenceEquals(other, note))
            {
                reachedNote = true;
                continue;
            }
            var otherPosition = other.GetPosition();
            if (otherPosition.x == position.x
                && (otherPosition.y < position.y || (otherPosition.y == position.y && !reachedNote)))
            {
                floorLayer++;
            }
        }
        return new Vector2(position.x, BeatmapConstant.YOffset + floorLayer * BeatmapConstant.LaneSize);
    }

    private void RefreshJumpStatesAtTime(float jsonTime)
    {
        foreach (var note in GetBetween(jsonTime - Epsilon, jsonTime + Epsilon))
        {
            if (LoadedContainers.TryGetValue(note, out var container))
            {
                UpdateJumpState((NoteContainer)container, note);
            }
        }
        GetCollectionForType<ArcGridContainer>(ObjectType.Arc).RefreshJumpHeightsAtTime(jsonTime);
    }

    // Here we check to see if any special angled notes are required.
    protected override void HandleContainerSpawn(ObjectContainer container, BaseObject obj) =>
        RefreshSpecialAngles(obj, true, BeatmapContext.Atsc.IsPlaying);

    protected override void HandleContainerDespawn(ObjectContainer container, BaseObject obj) =>
        RefreshSpecialAngles(obj, false, BeatmapContext.Atsc.IsPlaying);

    public void RefreshSpecialAngles(BaseObject obj, bool objectWasSpawned, bool isNatural)
    {
        // Do not bother refreshing if objects are despawning naturally (while playing back the song)
        if (!objectWasSpawned && isNatural) return;

        // Do not do special angles for bombs and fakes
        var note = obj as BaseNote;
        if (note.Type == (int)NoteType.Bomb || note.CustomFake) return;

        // Grab all objects with the same type, and time (within epsilon)
        PopulateObjectsAtSameTime(note);

        // Early return if we don't have exactly 2 notes to snap
        if (objectsAtSameTime.Count != 2)
        {
            ClearSpecialAnglesFromObjectsAtSameTime();
            return;
        }

        // Due to the potential for "obj" not having a container, we cannot reuse it as "a".
        var a = objectsAtSameTime[0].ObjectData as BaseNote;
        var b = objectsAtSameTime[^1].ObjectData as BaseNote;

        // Grab the containers we will be flipping
        var containerA = objectsAtSameTime[0] as NoteContainer;
        var containerB = objectsAtSameTime[^1] as NoteContainer;

        // Clear angles if directions are not the same (and both are not dot notes) or is precision placed
        var hasNEcoordinates = a.CustomCoordinate != null || b.CustomCoordinate != null;
        var hasMEprecision = a.CutDirection >= 1000
            || a.CutDirection <= -1000
            || b.CutDirection >= 1000
            || b.CutDirection <= -1000;

        if (a.CutDirection != b.CutDirection
            && a.CutDirection != (int)NoteCutDirection.Any
            && b.CutDirection != (int)NoteCutDirection.Any
            && !hasMEprecision
            && !hasNEcoordinates)
        {
            var directionA = NoteContainer.Directionalize(a);
            var directionB = NoteContainer.Directionalize(b);

            containerA.DirectionTarget.localEulerAngles = containerA.DirectionTargetEuler = directionA;
            containerB.DirectionTarget.localEulerAngles = containerB.DirectionTargetEuler = directionB;
            return;
        }

        // Swap references if our first note is a dot note
        if (a.CutDirection == (int)NoteCutDirection.Any)
        {
            (a, b) = (b, a);
            (containerA, containerB) = (containerB, containerA);
        }

        // Note jump animation broke when we used container local position. Use position from note data instead
        var posA = a.GetPosition();
        var posB = b.GetPosition();

        var cutVector = a.CutDirection == (int)NoteCutDirection.Any ? Vector2.up : Direction(a);
        var line = posA - posB;
        var angle = SignedAngleToLine(cutVector, line);

        // if both notes are dots, line them up with each other by adding the signed angle.
        if (a.CutDirection == (int)NoteCutDirection.Any && b.CutDirection == (int)NoteCutDirection.Any)
        {
            containerA.DirectionTargetEuler = Vector3.forward * angle;
            containerB.DirectionTargetEuler = Vector3.forward * angle;
        }
        // We restrict angles below 40 otherwise display their normal direction
        else if (Mathf.Abs(angle) <= 40)
        {
            var originalA = NoteContainer.Directionalize(a) + new Vector3(0, 0, -a.AngleOffset);
            var originalB = NoteContainer.Directionalize(b) + new Vector3(0, 0, -b.AngleOffset);

            containerA.DirectionTargetEuler = originalA + (Vector3.forward * angle);
            if (b.CutDirection == (int)NoteCutDirection.Any && !a.IsMainDirection)
                containerB.DirectionTargetEuler = originalB + (Vector3.forward * (angle + 45));
            else
                containerB.DirectionTargetEuler = originalB + (Vector3.forward * angle);
        }
        // These notes do not snap so lets reset their angle
        else
        {
            var directionA = NoteContainer.Directionalize(a);
            var directionB = NoteContainer.Directionalize(b);

            containerA.DirectionTargetEuler = directionA;
            containerB.DirectionTargetEuler = directionB;
        }

        // Immediately update direction target
        containerA.DirectionTarget.localEulerAngles = containerA.DirectionTargetEuler;
        containerB.DirectionTarget.localEulerAngles = containerB.DirectionTargetEuler;
    }

    public void ClearSpecialAngles(BaseObject obj)
    {
        var note = obj as BaseNote;

        PopulateObjectsAtSameTime(note);
        ClearSpecialAnglesFromObjectsAtSameTime();
    }

    // Grab all objects with the same type, and time (within epsilon)
    private void PopulateObjectsAtSameTime(BaseNote note)
    {
        objectsAtSameTime.Clear();
        foreach (var x in LoadedContainers)
        {
            if (note.CustomFake
                || !(x.Key.JsonTime - Epsilon <= note.JsonTime
                    && x.Key.JsonTime + Epsilon >= note.JsonTime
                    && (x.Key as BaseNote).Type == note.Type))
            {
                continue;
            }

            objectsAtSameTime.Add(x.Value);
        }
    }

    private void ClearSpecialAnglesFromObjectsAtSameTime()
    {
        foreach (var toReset in objectsAtSameTime)
        {
            var direction = NoteContainer.Directionalize(toReset.ObjectData as BaseNote);
            (toReset as NoteContainer).DirectionTarget.localEulerAngles = direction;
            (toReset as NoteContainer).DirectionTargetEuler = direction;
        }
    }

    // Grab a Vector2 plane based on the cut direction
    public static Vector2 Direction(BaseNote obj)
    {
        return obj.CutDirection switch
        {
            (int)NoteCutDirection.Up => new Vector2(0f, 1f),
            (int)NoteCutDirection.Down => new Vector2(0f, -1f),
            (int)NoteCutDirection.Left => new Vector2(-1f, 0f),
            (int)NoteCutDirection.Right => new Vector2(1f, 0f),
            (int)NoteCutDirection.UpLeft => new Vector2(-0.7071f, 0.7071f),
            (int)NoteCutDirection.UpRight => new Vector2(0.7071f, 0.7071f),
            (int)NoteCutDirection.DownLeft => new Vector2(-0.7071f, -0.7071f),
            (int)NoteCutDirection.DownRight => new Vector2(0.7071f, -0.7071f),
            _ => new Vector2(0f, 0f),
        };
    }

    // Totally not ripped from Beat Saber (jaroslav beck plz dont hurt me)
    private float SignedAngleToLine(Vector2 vec, Vector2 line)
    {
        var positive = Vector2.SignedAngle(vec, line);
        var negative = Vector2.SignedAngle(vec, -line);
        if (Mathf.Abs(positive) >= Mathf.Abs(negative)) return negative;
        return positive;
    }
}
