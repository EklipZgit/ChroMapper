using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

internal static class RingRotationDiagnostics
{
    private const string Prefix = "RINGTRACE source=CM";
    private const string FileName = "RingRotationTrace-CM.log";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    // Disable the verbose per-ring capture while profiling; const false removes its guarded render-path work without discarding the investigation instrumentation.
    public const bool Enabled = false;
    private static bool initialized;
    private static bool disabled;
    private static int nextWaveId;
    private static int pendingLines;
    private static StreamWriter writer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterFlush()
    {
        Application.quitting -= Flush;
        Application.quitting += Flush;
    }

    public static int AllocateWaveId() => ++nextWaveId;

    public static void Flush()
    {
        if (!Enabled || writer == null)
            return;

        writer.Flush();
        pendingLines = 0;
    }

    public static void WaveAdd(
        Component system,
        int waveId,
        bool startup,
        float eventBeat,
        float eventSeconds,
        int snapshotFrame,
        float target,
        float step,
        float propagation,
        float speed,
        float songBeat,
        float songSeconds)
    {
        if (!Enabled)
            return;

        Write("WAVE_ADD", songBeat, songSeconds, fields => fields
            .Append(" systemName=").Append(Quote(system.name))
            .Append(" systemId=").Append(system.GetInstanceID())
            .Append(" waveId=").Append(waveId)
            .Append(" waveKind=").Append(startup ? "startup" : "authored")
            .Append(" eventBeat=").Append(Float(eventBeat))
            .Append(" eventSongSeconds=").Append(Float(eventSeconds))
            .Append(" snapshotFrame=").Append(snapshotFrame)
            .Append(" target=").Append(Float(target))
            .Append(" step=").Append(Float(step))
            .Append(" propagation=").Append(Float(propagation))
            .Append(" speed=").Append(Float(speed)));
    }

    public static void Assignment(
        Component system,
        int fixedFrame,
        int invocation,
        int waveId,
        int ring,
        float target,
        float speed,
        float songBeat,
        float songSeconds)
    {
        if (!Enabled)
            return;

        Write("ASSIGN", songBeat, songSeconds, fields => fields
            .Append(" systemName=").Append(Quote(system.name))
            .Append(" systemId=").Append(system.GetInstanceID())
            .Append(" fixedFrame=").Append(fixedFrame)
            .Append(" invocation=").Append(invocation)
            .Append(" waveId=").Append(waveId)
            .Append(" ring=").Append(ring)
            .Append(" target=").Append(Float(target))
            .Append(" speed=").Append(Float(speed)));
    }

    public static void WaveState(
        Component system,
        int waveId,
        int ring,
        RingRotationState state,
        float songBeat,
        float songSeconds)
    {
        if (!Enabled)
            return;

        Write("WAVE_STATE", songBeat, songSeconds, fields => fields
            .Append(" systemName=").Append(Quote(system.name))
            .Append(" systemId=").Append(system.GetInstanceID())
            .Append(" waveId=").Append(waveId)
            .Append(" ring=").Append(ring)
            .Append(" current=").Append(Float(state.Rotation))
            .Append(" destination=").Append(Float(state.Destination))
            .Append(" speed=").Append(Float(state.Speed)));
    }

    // Capture the exact editor render pair while the current 1/64th discontinuity investigation is active.
    public static void RenderState(
        Component system,
        int fixedFrame,
        float interpolation,
        int ring,
        float previousRotation,
        RingRotationState current,
        float renderedRotation,
        float songBeat,
        float songSeconds)
    {
        if (!Enabled)
        {
            return;
        }

        Write("RENDER", songBeat, songSeconds, fields => fields
            .Append(" systemName=").Append(Quote(system.name))
            .Append(" systemId=").Append(system.GetInstanceID())
            .Append(" fixedFrame=").Append(fixedFrame)
            .Append(" interpolation=").Append(Float(interpolation))
            .Append(" ring=").Append(ring)
            .Append(" previousRotation=").Append(Float(previousRotation))
            .Append(" currentRotation=").Append(Float(current.Rotation))
            .Append(" destination=").Append(Float(current.Destination))
            .Append(" speed=").Append(Float(current.Speed))
            .Append(" renderedRotation=").Append(Float(renderedRotation)));
    }

    private static void Write(string type, float songBeat, float songSeconds, Action<StringBuilder> appendFields)
    {
        if (disabled)
            return;

        try
        {
            EnsureInitialized(songBeat, songSeconds);
            if (disabled)
                return;

            var line = Common(type, songBeat, songSeconds);
            appendFields(line);
            writer.WriteLine(line.ToString());
            pendingLines++;
            if (pendingLines >= 256)
            {
                writer.Flush();
                pendingLines = 0;
            }
        }
        catch (Exception)
        {
            // Temporary diagnostics must never affect environment simulation.
            disabled = true;
        }
    }

    private static void EnsureInitialized(float songBeat, float songSeconds)
    {
        if (initialized)
            return;

        initialized = true;
        writer = new StreamWriter(LogPath, false, Encoding.UTF8, 1 << 20);
        var session = Common("SESSION", songBeat, songSeconds)
            .Append(" fixedDeltaTime=").Append(Float(Time.fixedDeltaTime))
            .ToString();
        writer.WriteLine(session);
        writer.Flush();
    }

    private static StringBuilder Common(string type, float songBeat, float songSeconds) => new StringBuilder(Prefix)
        .Append(" type=").Append(type)
        .Append(" unityFrame=").Append(Time.frameCount)
        .Append(" unityTime=").Append(Float(Time.time))
        .Append(" unityRealtime=").Append(Float(Time.realtimeSinceStartup))
        .Append(" songBeat=").Append(Float(songBeat))
        .Append(" songSeconds=").Append(Float(songSeconds));

    private static string LogPath => Path.Combine(Application.persistentDataPath, FileName);

    private static string Float(float value) => value.ToString("R", Invariant);

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
