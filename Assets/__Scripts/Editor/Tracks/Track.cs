using System;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform SelfTransform;
    public Transform ObjectParentTransform;
    public VariableNJSProvider vNjsProvider;
    public bool IgnoreZScale;

    public Vector3 RotationValue = Vector3.zero;

    private readonly Vector3 rotationPoint = Vector3.zero;

    private BaseGrid gridObject;
    private ObjectContainer gridContainer;
    private (Transform, Vector3, Quaternion)[] nodesTarget;
    private readonly Vector3[] newSplinePosition = new Vector3[ArcContainer.NumSamples + 1];
    private bool useCustom;
    private bool movementUpdatesRegistered;
    private float spawnTime;
    private float spawnPosition;
    private float despawnTime;
    private float despawnPosition;
    private float zScale = 1f;
    private float floorDurationSongBpm;
    private float movementPerBeat;

    internal const float FloorDurationSeconds = 0.5f;
    private const float FloorTravelDistance = 100f;

    // this number pulled from my ass, but it looks fine
    // oh, it's actually correct
    const float JUMP_FAR = 500f;

    // this number also pulled from my ass, song bpm time
    public const float JUMP_TIME = 2f;

    public void Awake() => zScale = IgnoreZScale ? 1f : BeatmapConstant.LaneSize;

    public void OnEnable()
    {
        if (gridObject != null && !useCustom)
            RegisterMovementUpdates();
    }

    public void OnDisable()
    {
        if (!movementUpdatesRegistered)
            return;
        vNjsProvider.UnregisterMovementTrack(this);
        movementUpdatesRegistered = false;
    }

    private void RegisterMovementUpdates()
    {
        if (movementUpdatesRegistered)
            return;
        vNjsProvider.RegisterMovementTrack(this);
        movementUpdatesRegistered = true;
    }

    public void AssignRotationValue(Vector3 rotation)
    {
        RotationValue = rotation;
        SelfTransform.RotateAround(rotationPoint, Vector3.right, RotationValue.x);
        SelfTransform.RotateAround(rotationPoint, Vector3.up, RotationValue.y);
        SelfTransform.RotateAround(rotationPoint, Vector3.forward, RotationValue.z);
    }

    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition = new Vector3(
            ObjectParentTransform.localPosition.x,
            ObjectParentTransform.localPosition.y,
            position * zScale);
    }

    // Noodle replaces the floor movement's end position with the definite position, retaining only its approach distance.
    public void HoldDefinitePosition(float time)
    {
        var position = ObjectParentTransform.localPosition;
        if (gridObject is BaseNote note)
        {
            var lane = note.GetPosition();
            position.x = lane.x;
            position.y = lane.y + BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset;
        }
        position.z = time < spawnTime ? GetFloorPosition(time, spawnTime) - spawnPosition : 0f;
        // Ordinary travel compensates this child offset. Definite-position travel must preserve that compensation too.
        if (gridObject is BaseObstacle)
            position.z += ObstacleContainer.AnimationZOffset;
        ObjectParentTransform.localPosition = position;
    }

    public void UpdateTime(float time)
    {
        float z;
        var position = ObjectParentTransform.localPosition;

        // Arcs use continuous jump travel rather than the half-second approach used by notes and walls.
        if (gridObject is BaseArc)
        {
            z = spawnPosition + (spawnTime - time) * movementPerBeat;
        }
        else if (time < spawnTime)
        {
            z = time < spawnTime - floorDurationSongBpm
                ? JUMP_FAR
                : GetFloorPosition(time, spawnTime);
        }
        else if (time < despawnTime)
            z = Mathf.LerpUnclamped(spawnPosition, despawnPosition, (time - spawnTime) / (despawnTime - spawnTime));
        // Jump out
        else
            z = Mathf.LerpUnclamped(despawnPosition, -JUMP_FAR, (time - despawnTime) / JUMP_TIME);

        position.z = z;

        // oh yeah you know its good when things start with a check like this
        switch (gridObject)
        {
            case BaseNote note:
                {
                    // Normalized [0-1] between despawn time and spawn time
                    var normalizedLifetime = Mathf.Clamp01(Mathf.InverseLerp(despawnTime, spawnTime, time));

                    var noteContainer = (NoteContainer)gridContainer;
                    var jump = GetNoteJumpPosition(noteContainer, 1f - normalizedLifetime);
                    position.x = GetNoteJumpHorizontalPosition(noteContainer, 1f - normalizedLifetime);
                    position.y = jump.x;
                    if (time >= spawnTime && time < despawnTime)
                        position.z = jump.y;

                    noteContainer.DirectionTarget.localRotation = GetNoteSpawnRotation(
                        noteContainer,
                        1f - normalizedLifetime);

                    break;
                }
            case BaseChain chain:
                {
                    var len = Math.Min(nodesTarget.Length, chain.SliceCount - 1);
                    for (var i = 0; i < len; i++)
                    {
                        var (nodeTransform, targetPosition, targetRotation) = nodesTarget[i];
                        var localPosition = nodeTransform.localPosition;
                        var offset = Mathf.LerpUnclamped(
                            0f,
                            chain.TailSongBpmTime - chain.SongBpmTime,
                            (i + 1f) / (chain.SliceCount - 1f));

                        // it's just copypasted above
                        var normalizedLifetime = Mathf.Clamp01(
                            Mathf.InverseLerp(
                                despawnTime + offset,
                                spawnTime + offset,
                                time));
                        var spawnLifetime = Mathf.Clamp01(1 - ((normalizedLifetime - 0.5f) * 2));
                        var rotationLifetime = Mathf.Clamp01(spawnLifetime / 0.3f);
                        var jumpT = Easing.Quadratic.Out(spawnLifetime);
                        var rotationT = Easing.Quadratic.Out(rotationLifetime);
                        localPosition.y = Mathf.LerpUnclamped(
                            -position.y
                            + BeatmapConstant.YOffset,
                            targetPosition.y,
                            jumpT);
                        // BurstSliderSpawner gives each link its own time, so links can still be approaching
                        // after the head starts jumping. A fixed offset from the head gives the wrong spacing.
                        var sliceJumpStart = spawnTime + offset;
                        var sliceZ = time < sliceJumpStart
                            ? GetFloorPosition(time, sliceJumpStart)
                            : spawnPosition + (sliceJumpStart - time) * movementPerBeat;
                        localPosition.z = time < spawnTime - floorDurationSongBpm ? 0f : sliceZ - position.z;
                        nodeTransform.localPosition = localPosition;
                        nodeTransform.localRotation = Quaternion.LerpUnclamped(
                            Quaternion.identity,
                            targetRotation,
                            rotationT);
                    }

                    break;
                }
            case BaseArc arc:
                {
                    var arcContainer = gridContainer as ArcContainer;

                    var normalizedLifetime = Mathf.Clamp01(
                        Mathf.InverseLerp(
                            arc.SongBpmTime + arc.HalfJumpDuration,
                            arc.SpawnSongBpmTime,
                            time));
                    var spawnLifetime = Mathf.Clamp01(1f - ((normalizedLifetime - 0.5f) * 2f));
                    var jumpT = arcContainer.ArcData.HeadNotes.Count > 0 ? Easing.Quadratic.Out(spawnLifetime) : 1f;
                    var headPosY = arc.GetPosition().y;
                    var headY = arcContainer.ArcData.HeadNotes.Count > 0
                        ? Mathf.LerpUnclamped(arcContainer.HeadJumpHeights.x, arcContainer.HeadJumpHeights.y, jumpT)
                            - (headPosY + BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset)
                        : 0f;

                    var tailOffset = arc.DurationSongBpmTime;
                    var tailNormalizedLifetime = Mathf.Clamp01(
                        Mathf.InverseLerp(
                            arc.SongBpmTime + arc.HalfJumpDuration + tailOffset,
                            arc.SpawnSongBpmTime + tailOffset,
                            time));
                    var tailSpawnLifetime = Mathf.Clamp01(1f - ((tailNormalizedLifetime - 0.5f) * 2f));
                    var tailJumpT = arcContainer.ArcData.TailNotes.Count > 0
                        ? Easing.Quadratic.Out(tailSpawnLifetime)
                        : 1f;
                    var tailPosY = arc.GetTailPosition().y;
                    var tailY = arcContainer.ArcData.TailNotes.Count > 0
                        ? Mathf.LerpUnclamped(arcContainer.TailJumpHeights.x, arcContainer.TailJumpHeights.y, tailJumpT)
                            - (tailPosY + BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset)
                        : 0f;

                    // yoink from polandball
                    // https://github.com/AllPoland/ArcViewer/blob/main/Assets/__Scripts/Previewer/MapControl/Objects/ArcManager.cs#L362
                    var basePositions = arcContainer.BaseSplinePoints;
                    var arcLength = basePositions[^1].z;
                    for (var i = 0; i < basePositions.Length; i++)
                    {
                        var point = basePositions[i];

                        //Get the preferred offset based on distance from the head
                        var headDist = point.z / arc.HalfJumpDistance;
                        var headT = 1 - Easing.Quadratic.Out(Mathf.Clamp01(headDist));
                        var headPreferredOffset = headY * headT;

                        //Get the preferred offset based on distance from the tail
                        var tailDist = (arcLength - point.z) / arc.HalfJumpDistance;
                        var tailT = 1 - Easing.Quadratic.Out(Mathf.Clamp01(tailDist));
                        var tailPreferredOffset = tailY * tailT;

                        //Weight the adjustment based on which end of the arc the point is closer to
                        var relativePosition = point.z / arcLength;
                        point.y += Mathf.LerpUnclamped(headPreferredOffset, tailPreferredOffset, relativePosition);

                        //Squish the arc if needed
                        point.z *= arc.DurationSongBpmTime * arc.HalfJumpDistance / arc.HalfJumpDuration;

                        newSplinePosition[i] = point;
                    }

                    var splineRenderer = arcContainer.SplineRenderer;
                    splineRenderer.SetPositions(newSplinePosition);
                    break;
                }
        }

        ObjectParentTransform.localPosition = position;
    }

    private float GetFloorPosition(float time, float jumpStartTime) =>
        spawnPosition + FloorTravelDistance * (jumpStartTime - time) / floorDurationSongBpm;

    internal static Vector2 GetNoteJumpPosition(NoteContainer note, float normalTime)
    {
        var elapsed = normalTime * 2f;
        var height = note.JumpStartHeight
            + (note.JumpApexHeight - note.JumpStartHeight) * (2f * elapsed - elapsed * elapsed);
        if (note.JumpYAvoidance != 0f && normalTime < 0.25f)
            height += (0.5f - Mathf.Cos(normalTime * 8f * Mathf.PI) * 0.5f) * note.JumpYAvoidance;

        var distance = note.NoteData.HalfJumpDistance * (1f - 2f * normalTime) + BeatmapConstant.NoteZOffset;
        // NoteJump pulls missed and fake notes away during the last quarter of their full jump.
        var quarter = Mathf.Max(0f, (normalTime - 0.75f) / 0.25f);
        distance -= JUMP_FAR * quarter * quarter * quarter;
        return new Vector2(height, distance);
    }

    internal static float GetNoteJumpHorizontalPosition(NoteContainer note, float normalTime)
    {
        if (note.JumpStartX == note.JumpEndX || normalTime >= 0.25f)
            return note.JumpEndX;

        return Mathf.LerpUnclamped(note.JumpStartX, note.JumpEndX, Easing.Quadratic.InOut(normalTime * 4f));
    }

    internal static Quaternion GetNoteSpawnRotation(NoteContainer note, float normalTime)
    {
        var endRotation = Quaternion.Euler(note.DirectionTargetEuler);
        // Noodle writes the cut-direction rotation only before the hit, even with note look disabled.
        if (note.Animator.DisableNoteLook)
        {
            note.HasJumpRotation |= normalTime < 0.5f;
            return note.HasJumpRotation ? endRotation : Quaternion.identity;
        }
        return NoteJumpRotation.Evaluate(
            endRotation,
            note.NoteData.Chains.Count == 0 ? note.JumpRotationOffset : Vector3.zero,
            normalTime);
    }

    public void InitState()
    {
        useCustom = (gridObject.CustomNoteJumpMovementSpeed?.IsNumber ?? false)
            || (gridObject.CustomNoteJumpStartBeatOffset?.IsNumber ?? false);
        if (!useCustom)
        {
            gridObject.SetSpawnParameters(
                vNjsProvider.HalfJumpDurationInBeats,
                vNjsProvider.HalfJumpDistance);
        }

        UpdateSpawning();
    }

    public void UpdateState()
    {
        if (!UIMode.PreviewMode || useCustom || gridObject == null || gridContainer.ObjectData == null) return;

        gridObject.SetSpawnParameters(
            vNjsProvider.HalfJumpDurationInBeats,
            vNjsProvider.HalfJumpDistance);
        UpdateSpawning();
    }

    public void UpdateSpawning()
    {
        floorDurationSongBpm = FloorDurationSeconds * BeatSaberSongContainer.Instance.Info.BeatsPerMinute / 60f;
        movementPerBeat = gridObject.HalfJumpDistance / gridObject.HalfJumpDuration;
        spawnTime = gridObject.SongBpmTime - gridObject.HalfJumpDuration;
        spawnPosition = gridObject.HalfJumpDistance;
        switch (gridObject)
        {
            case BaseObstacle obs:
                despawnPosition = -(obs.HalfJumpDistance * 0.5f)
                    - (obs.DurationSongBpmTime * obs.HalfJumpDistance / obs.HalfJumpDuration);
                despawnTime = obs.SongBpmTime + obs.DurationSongBpmTime + (obs.HalfJumpDuration * 0.5f);
                break;
            case BaseArc arc:
                despawnPosition = -arc.HalfJumpDistance
                    - (arc.DurationSongBpmTime * arc.HalfJumpDistance / arc.HalfJumpDuration);
                despawnTime = arc.DespawnSongBpmTime;
                break;
            case BaseChain chain:
                despawnPosition = -chain.HalfJumpDistance
                    - (chain.DurationSongBpmTime * chain.HalfJumpDistance / chain.HalfJumpDuration);
                despawnTime = chain.DespawnSongBpmTime;
                break;
            default:
                despawnPosition = -gridObject.HalfJumpDistance;
                despawnTime = gridObject.DespawnSongBpmTime;
                break;
        }

        var zOffset = gridObject is BaseNote ? BeatmapConstant.NoteZOffset : BeatmapConstant.ZOffset;
        spawnPosition += zOffset;
        despawnPosition += zOffset;

        gridContainer.UpdateScalable(movementPerBeat);
    }

    public void AttachContainer(ObjectContainer obj)
    {
        UpdateMaterialRotation(obj);
        if (obj.transform.parent == ObjectParentTransform) return;
        obj.transform.SetParent(ObjectParentTransform, false);
        obj.AssignTrack(this);

        if (obj.ObjectData is not BaseGrid g) return;
        gridContainer = obj;
        gridObject = g;

        if (gridContainer is ChainContainer chainContainer)
        {
            nodesTarget = chainContainer
                .Nodes.Select(n => (n.transform, n.transform.localPosition, n.transform.localRotation))
                .ToArray();
        }

        InitState();
        // Geometry tracks and objects with authored jump parameters never consume shared NJS updates.
        // Register after attachment determines whether the object uses the shared NJS.
        if (isActiveAndEnabled && !useCustom)
            RegisterMovementUpdates();
    }

    public void UpdateMaterialRotation(ObjectContainer obj)
    {
        if (obj is ObstacleContainer || obj is NoteContainer) obj.SetRotation(RotationValue.y);
    }

    public void ResetData()
    {
        enabled = false;
        gridObject = null;
        gridContainer = null;
        SelfTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        SelfTransform.localScale = Vector3.one;
        ObjectParentTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ObjectParentTransform.localScale = Vector3.one;

        if (gameObject.TryGetComponent<TrackAnimator>(out var animator)) animator.enabled = false;
    }
}
