using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HydraulicCarTimeline : StateManager<BaseEvent>
{
    [Serializable]
    public class Car
    {
        public int Body;
        public int Joint;
        public HydraulicCarJump Jump;
        public HydraulicCarSuspension Suspension;
        public Vector3 ForcePosition;
    }

    private struct CarEvent
    {
        public BaseEvent Source;
        public float Seconds;
        public int Type;
        public int Value;
    }

    private struct BodyPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private const int FramesPerBlock = 512;
    public HydraulicCarPhysicsRig Template;
    public Rigidbody[] VisualBodies;
    public Car[] Cars;

    private readonly List<CarEvent> events = new();
    private readonly List<BodyPose[]> poseBlocks = new();
    private HydraulicCarPhysicsRig simulation;
    private Scene simulationScene;
    private PhysicsScene physicsScene;
    private bool[] useGravity;
    private float[] lastBounceSeconds;
    private int frameCount;
    private int nextEvent;
    private bool dirty;

    public override void Initialize()
    {
        ReleaseSimulation();
        events.Clear();
        dirty = true;
    }

    public override void InsertData(BaseEvent data)
    {
        var state = new CarEvent
        {
            Source = data,
            Seconds = Atsc.GetSecondsFromBeat(data.SongBpmTime),
            Type = data.Type,
            Value = data.Value
        };
        var index = FindAfter(state.Seconds);
        events.Insert(index, state);
        dirty = true;
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var seconds = Atsc.GetSecondsFromBeat(original.SongBpmTime);
        for (var index = FindAfter(seconds) - 1; index >= 0 && events[index].Seconds == seconds; index--)
        {
            if (!ReferenceEquals(events[index].Source, reference))
                continue;

            events.RemoveAt(index);
            dirty = true;
            break;
        }

    }

    private int FindAfter(float seconds)
    {
        return events.AsSpan().UpperBoundBy(seconds, static entry => entry.Seconds);
    }

    public override void Refresh() => UpdateTime(false, Atsc.CurrentSongBpmTime);

    public override void UpdateTime(bool isPlaying, float time)
    {
        if (dirty)
            RebuildSimulation();

        var seconds = Mathf.Max(0f, Atsc.GetSecondsFromBeat(time));
        var fixedTime = seconds / Template.FixedDeltaTime;
        var frame = Mathf.FloorToInt(fixedTime);
        while (frameCount <= frame + 1)
            SimulateNextFrame();

        // Rigidbody.Interpolate renders between the previous and current completed physics steps.
        var previous = Mathf.Max(0, frame - 1);
        var amount = fixedTime - frame;
        for (var body = 0; body < VisualBodies.Length; body++)
        {
            var from = GetPose(previous, body);
            var to = GetPose(frame, body);
            VisualBodies[body].transform.SetLocalPositionAndRotation(
                Vector3.LerpUnclamped(from.Position, to.Position, amount),
                Quaternion.SlerpUnclamped(from.Rotation, to.Rotation, amount));
        }

    }

    private void RebuildSimulation()
    {
        ReleaseSimulation();
        simulationScene = SceneManager.CreateScene(
            // Scene unloading completes asynchronously, so an edit can rebuild while the previous scene still exists.
            $"Interscope car physics {Guid.NewGuid():N}",
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        physicsScene = simulationScene.GetPhysicsScene();
        simulation = Instantiate(Template);
        // Captured body positions and force points use the native environment's coordinates.
        simulation.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        simulation.transform.localScale = Vector3.one;
        SceneManager.MoveGameObjectToScene(simulation.gameObject, simulationScene);
        useGravity = new bool[simulation.Bodies.Length];
        for (var index = 0; index < simulation.Bodies.Length; index++)
        {
            var body = simulation.Bodies[index];
            useGravity[index] = body.useGravity;
            // Local physics scenes share global gravity. Apply the captured acceleration only to this rig.
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
        }

        simulation.gameObject.SetActive(true);
        for (var index = 0; index < simulation.Bodies.Length; index++)
        {
            var body = simulation.Bodies[index];
            body.position = simulation.StartupPositions[index];
            body.rotation = simulation.StartupRotations[index];
            body.linearVelocity = simulation.StartupVelocities[index];
            body.angularVelocity = simulation.StartupAngularVelocities[index];
        }

        lastBounceSeconds = new float[Cars.Length];
        Array.Fill(lastBounceSeconds, float.NegativeInfinity);
        nextEvent = 0;
        StoreFrame();
        dirty = false;
    }

    private void SimulateNextFrame()
    {
        var seconds = (frameCount - 1) * Template.FixedDeltaTime;
        while (nextEvent < events.Count
            && TimeHelper.GetPreviewCallbackSeconds(events[nextEvent].Seconds) <= seconds + 0.000001f)
        {
            ApplyEvent(events[nextEvent], nextEvent);
            nextEvent++;
        }

        for (var index = 0; index < simulation.Bodies.Length; index++)
        {
            var body = simulation.Bodies[index];
            if (useGravity[index] && !body.IsSleeping())
                body.AddForce(Template.Gravity, ForceMode.Acceleration);
        }

        physicsScene.Simulate(Template.FixedDeltaTime);
        StoreFrame();
    }

    private void ApplyEvent(CarEvent state, int eventIndex)
    {
        for (var index = 0; index < Cars.Length; index++)
        {
            var car = Cars[index];
            var suspension = car.Suspension;
            var body = simulation.Bodies[car.Body];
            var joint = simulation.Joints[car.Joint];
            if (state.Type == suspension.ContractEventType && Array.IndexOf(suspension.ContractEventValues, state.Value) >= 0)
            {
                joint.minDistance = suspension.ContractDistance;
                joint.maxDistance = suspension.ContractDistance;
                body.WakeUp();
            }

            if (state.Type == suspension.ExpandEventType && Array.IndexOf(suspension.ExpandEventValues, state.Value) >= 0)
            {
                joint.minDistance = suspension.ExpandDistance;
                joint.maxDistance = suspension.ExpandDistance;
                body.WakeUp();
            }

            var jump = car.Jump;
            if (state.Type != jump.EventType || Array.IndexOf(jump.EventValues, state.Value) < 0
                || state.Seconds - lastBounceSeconds[index] < jump.MinDelayBetweenEvents)
            {
                continue;
            }

            lastBounceSeconds[index] = state.Seconds;
            // Keep each event's variation stable across seeks without consuming Unity's shared random stream.
            var random = new System.Random(unchecked((eventIndex * 397) ^ index));
            var multiplier = 1f + (((float)random.NextDouble() - 0.5f) * jump.Randomness);
            // The native method uses ForceMode.Force, despite naming the field "impulse".
            body.AddForceAtPosition(jump.Impulse * multiplier, car.ForcePosition);
        }

    }

    private void StoreFrame()
    {
        if (frameCount % FramesPerBlock == 0)
            poseBlocks.Add(new BodyPose[FramesPerBlock * simulation.Bodies.Length]);

        var block = poseBlocks[frameCount / FramesPerBlock];
        var offset = (frameCount % FramesPerBlock) * simulation.Bodies.Length;
        for (var index = 0; index < simulation.Bodies.Length; index++)
        {
            var body = simulation.Bodies[index];
            block[offset + index] = new BodyPose
            {
                Position = body.transform.parent.InverseTransformPoint(body.position),
                Rotation = Quaternion.Inverse(body.transform.parent.rotation) * body.rotation
            };
        }

        frameCount++;
    }

    private BodyPose GetPose(int frame, int body) =>
        poseBlocks[frame / FramesPerBlock][((frame % FramesPerBlock) * VisualBodies.Length) + body];

    private void OnDestroy() => ReleaseSimulation();

    private void ReleaseSimulation()
    {
        if (simulation != null)
            simulation.gameObject.SetActive(false);

        if (simulationScene.IsValid() && simulationScene.isLoaded)
            SceneManager.UnloadSceneAsync(simulationScene);

        simulationScene = default;
        simulation = null;
        poseBlocks.Clear();
        frameCount = 0;
    }
}
