using Beatmap.Base;
using Beatmap.V2;
using Beatmap.Containers;
using UnityEngine;

public class Track : MonoBehaviour
{
    public Transform ObjectParentTransform;
    public VariableNJSProvider vNjsProvider;

    public Vector3 RotationValue = Vector3.zero;

    private readonly Vector3 rotationPoint = LoadInitialMap.PlatformOffset;

    private BaseGrid gridObject;
    private ObjectContainer gridContainer;
    private float spawnTime;
    private float spawnPosition;
    private float despawnTime;
    private float despawnPosition;

    // this number pulled from my ass, but it looks fine
    // oh, it's actually correct
    const float JUMP_FAR = 500f;

    // this number also pulled from my ass, song bpm time
    public const float JUMP_TIME = 2f;

    // gonna need to find better way to attach this
    public void Awake() => vNjsProvider = FindAnyObjectByType<VariableNJSProvider>();

    public void OnEnable() => vNjsProvider.OnChanged += UpdateState;
    public void OnDisable() => vNjsProvider.OnChanged -= UpdateState;

    public void AssignRotationValue(Vector3 rotation)
    {
        RotationValue = rotation;
        transform.RotateAround(rotationPoint, Vector3.right, RotationValue.x);
        transform.RotateAround(rotationPoint, Vector3.up, RotationValue.y);
        transform.RotateAround(rotationPoint, Vector3.forward, RotationValue.z);
    }

    public void UpdatePosition(float position)
    {
        ObjectParentTransform.localPosition = new Vector3(
            ObjectParentTransform.localPosition.x,
            ObjectParentTransform.localPosition.y,
            position);
    }

    public void UpdateTime(float time)
    {
        var z = 0f;
        var v2 = gridObject is V2Object;
        var position = ObjectParentTransform.localPosition;

        // Jump in
        if (time < spawnTime)
        {
            z = (gridObject.CustomSpawnEffect ?? !v2) ^ v2
                ? Mathf.Lerp(spawnPosition, JUMP_FAR, (spawnTime - time) / JUMP_TIME)
                : JUMP_FAR;
        }
        else if (time < despawnTime)
            z = Mathf.Lerp(spawnPosition, despawnPosition, (time - spawnTime) / (despawnTime - spawnTime));
        // Jump out
        else
            z = Mathf.Lerp(despawnPosition, -JUMP_FAR, (time - despawnTime) / JUMP_TIME);

        position.z = z;

        // oh yeah you know its good when things start with a check like this
        if (gridObject is BaseNote note)
        {
            // Normalized [0-1] between despawn time and spawn time
            var normalizedLifetime = Mathf.Clamp01(Mathf.InverseLerp(gridObject.DespawnSongBpmTime, spawnTime, time));

            // [0-1] between spawn time and note time
            // 0.3 magic number taken from ArcViewer (thanks polandball)
            var spawnLifetime = Mathf.Clamp01(1 - ((normalizedLifetime - 0.5f) * 2));
            var rotationLifetime = Mathf.Clamp01(spawnLifetime / 0.3f);

            // Beat Saber uses a parabolic arc so we use Quadratic Out easing because im lazy
            var jumpT = Easing.Quadratic.Out(spawnLifetime);
            var rotationT = Easing.Quadratic.Out(rotationLifetime);

            // TODO: Pre-compute starting position so notes can stack and flip can be supported
            //   (Notes need to be aware of other notes)
            position.y = Mathf.Lerp(0.5f, note.GetPosition().y + 0.5f, jumpT);

            // Multiply euler rotation by spawn lifetime if we are in the first half (spawning) portion of our object lifetime
            if (normalizedLifetime >= 0.5f && gridContainer is NoteContainer noteContainer)
            {
                var quaternion = Quaternion.Euler(noteContainer.DirectionTargetEuler);

                noteContainer.DirectionTarget.localRotation = Quaternion.Lerp(
                    Quaternion.identity,
                    quaternion,
                    rotationT);
            }
        }

        ObjectParentTransform.localPosition = position;
    }

    public void UpdateState()
    {
        if (!UIMode.PreviewMode) return;
        if (gridObject == null) return;
        spawnTime = gridObject.SongBpmTime - vNjsProvider.HalfJumpDurationInBeats;
        spawnPosition = vNjsProvider.JumpDistance;
        if (gridObject is BaseObstacle obs)
        {
            despawnPosition = -vNjsProvider.HalfJumpDistance - (obs.DurationSongBpm * vNjsProvider.EditorScale);
            despawnTime = obs.SongBpmTime + obs.DurationSongBpm + (vNjsProvider.HalfJumpDurationInBeats * 0.5f);
        }
        else
        {
            despawnPosition = -vNjsProvider.JumpDistance;
            despawnTime = gridObject.SongBpmTime + vNjsProvider.HalfJumpDurationInBeats;
        }

        // ok but why do i have to check twice
        if (gridContainer.ObjectData != null) gridContainer.UpdateScalable(vNjsProvider.EditorScale);
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
        UpdateState();
    }

    public void UpdateMaterialRotation(ObjectContainer obj)
    {
        if (obj is ObstacleContainer || obj is NoteContainer) obj.SetRotation(RotationValue.y);
    }
}
