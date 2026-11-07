using UnityEngine;

// Smoke uses a fixed simulation cadence so a seek runs the same steps as forward playback.
[RequireComponent(typeof(ParticleSystem))]
public sealed class TimelineParticleSystem : MonoBehaviour
{
    private const float Step = 1f / 60f;
    private const int CheckpointFrames = 8 * 60;
    private const int CheckpointCount = 32;
    private readonly Checkpoint[] checkpoints = new Checkpoint[CheckpointCount];
    private ParticleSystem particles;
    private AudioTimeSyncController clock;
    private int currentFrame;
    private uint seed;
    private ParticleSystem.Particle[] fixedParticles;
    private ParticleSystem.PlaybackState fixedState;
    private int fixedParticleCount;
    private bool hasFractionalSimulation;

    private sealed class Checkpoint
    {
        public int Frame;
        public int ParticleCount;
        public ParticleSystem.Particle[] Particles;
        public ParticleSystem.PlaybackState State;
        public Matrix4x4 EmitterTransform;
    }

    public void Initialize(AudioTimeSyncController atsc)
    {
        if (clock != null)
        {
            clock.OnTimeChanged -= UpdateTime;
            clock.OnTimeChanged += UpdateTime;
            return;
        }
        clock = atsc;
        particles = GetComponent<ParticleSystem>();
        fixedParticles = new ParticleSystem.Particle[particles.main.maxParticles];
        seed = particles.randomSeed;
        if (particles.useAutoRandomSeed)
        {
            seed = 2166136261;
            for (var ancestor = transform; ancestor != null; ancestor = ancestor.parent)
            {
                foreach (var character in ancestor.name)
                {
                    seed = (seed ^ character) * 16777619;
                }
                seed = (seed ^ (uint)ancestor.GetSiblingIndex()) * 16777619;
            }
        }
        ResetSimulation();
        clock.OnTimeChanged += UpdateTime;
        UpdateTime();
    }

    private void OnDestroy()
    {
        if (clock != null)
        {
            clock.OnTimeChanged -= UpdateTime;
        }
    }

    private void ResetSimulation()
    {
        particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.useAutoRandomSeed = false;
        particles.randomSeed = seed;
        // Restarting Simulate applies the module's prewarm already. Replaying its duration here advances the emission sequence twice.
        particles.Simulate(0f, false, true, false);
        currentFrame = 0;
        hasFractionalSimulation = false;
    }

    private void UpdateTime()
    {
        var seconds = Mathf.Max(0f, clock.CurrentSeconds);
        var frame = Mathf.FloorToInt(seconds / Step);
        if (frame < currentFrame)
        {
            if (!RestoreCheckpoint(frame))
            {
                ResetSimulation();
            }
        }
        else if (hasFractionalSimulation)
        {
            particles.SetPlaybackState(fixedState);
            particles.SetParticles(fixedParticles, fixedParticleCount);
            hasFractionalSimulation = false;
        }
        while (currentFrame < frame)
        {
            particles.Simulate(Step, false, false, false);
            currentFrame++;
            if (currentFrame % CheckpointFrames == 0)
            {
                SaveCheckpoint();
            }
        }
        var remainder = seconds - frame * Step;
        if (remainder > 0f)
        {
            // Restore the fixed emission and particle state before the next update so fractional previews never accumulate into its history.
            fixedState = particles.GetPlaybackState();
            fixedParticleCount = particles.GetParticles(fixedParticles);
            particles.Simulate(remainder, false, false, false);
            hasFractionalSimulation = true;
        }
    }

    // Keep only whole-step states. Fractional samples are temporary and must never enter
    // the history used to resume a reverse seek's deterministic simulation.
    private void SaveCheckpoint()
    {
        var index = currentFrame / CheckpointFrames % CheckpointCount;
        var checkpoint = checkpoints[index];
        if (checkpoint == null)
        {
            checkpoint = new Checkpoint
            {
                Particles = new ParticleSystem.Particle[fixedParticles.Length]
            };
            checkpoints[index] = checkpoint;
        }
        checkpoint.Frame = currentFrame;
        checkpoint.ParticleCount = particles.GetParticles(checkpoint.Particles);
        checkpoint.State = particles.GetPlaybackState();
        checkpoint.EmitterTransform = transform.localToWorldMatrix;
    }

    private bool RestoreCheckpoint(int frame)
    {
        Checkpoint nearest = null;
        var emitterTransform = transform.localToWorldMatrix;
        foreach (var checkpoint in checkpoints)
        {
            // Noodle can move the emitter. A snapshot made at a different pose cannot
            // replace the existing replay at the current pose.
            if (checkpoint != null && checkpoint.Frame <= frame
                && checkpoint.EmitterTransform.Equals(emitterTransform)
                && (nearest == null || checkpoint.Frame > nearest.Frame))
            {
                nearest = checkpoint;
            }
        }
        if (nearest == null)
        {
            return false;
        }
        particles.SetPlaybackState(nearest.State);
        particles.SetParticles(nearest.Particles, nearest.ParticleCount);
        currentFrame = nearest.Frame;
        hasFractionalSimulation = false;
        return true;
    }
}
