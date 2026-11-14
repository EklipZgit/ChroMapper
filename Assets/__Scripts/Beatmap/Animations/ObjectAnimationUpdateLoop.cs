using UnityEngine;

namespace Beatmap.Animations
{
    // Sample object paths before track values arrive, then apply their combined pose in LateUpdate.
    [DefaultExecutionOrder(5)]
    internal sealed class ObjectAnimationUpdateLoop : MonoBehaviour
    {
        private readonly AnimationUpdateRegistry<ObjectAnimator> targets = new(
            static target => target.AnimationUpdateIndex,
            static (target, index) => target.AnimationUpdateIndex = index);

        private readonly AnimationUpdateRegistry<ObjectAnimator> seekTargets = new(
            static target => target.AnimationSeekIndex,
            static (target, index) => target.AnimationSeekIndex = index);
        private readonly AnimationUpdateRegistry<ObjectAnimator> pathTargets = new(
            static target => target.AnimationPathUpdateIndex,
            static (target, index) => target.AnimationPathUpdateIndex = index);
        private AudioTimeSyncController clock;
        private bool callbacksSubscribed;

        // Clock steps that produced each recent frame, recorded before the LateUpdate apply pass
        // so note facing replays on the real per-frame cadence. One extra slot covers the
        // previous-position sample of the oldest replayed frame.
        private readonly float[] playingDeltaSeconds = new float[ObjectAnimator.NoteLookHistoryFrames + 1];
        private int playingDeltaCount;

        internal float GetPlayingDeltaSeconds(int framesAgo) =>
            framesAgo >= 0 && framesAgo < playingDeltaCount ? playingDeltaSeconds[framesAgo] : 0f;

        private void RecordPlayingDelta()
        {
            if (clock == null || !clock.IsPlaying)
            {
                playingDeltaCount = 0;
                return;
            }

            var delta = clock.CurrentSeconds - clock.PreviousFrameSeconds;
            if (delta <= 0f || delta > 0.5f)
            {
                // Seeks and long stalls are clock jumps, not simulated frames; nothing before
                // them is contiguous in song time so the replay history restarts empty.
                playingDeltaCount = 0;
                return;
            }

            for (var i = Mathf.Min(playingDeltaCount, playingDeltaSeconds.Length - 1); i > 0; i--)
                playingDeltaSeconds[i] = playingDeltaSeconds[i - 1];
            playingDeltaSeconds[0] = delta;
            if (playingDeltaCount < playingDeltaSeconds.Length)
                playingDeltaCount++;
        }

        internal void Initialize(AudioTimeSyncController clock)
        {
            this.clock = clock;
        }

        internal void Register(ObjectAnimator target)
        {
            targets.Register(target);
            if (target.NeedsPathUpdate)
            {
                pathTargets.Register(target);
            }
            if (!target.ParticipatesInSharedSeeks)
            {
                return;
            }
            seekTargets.Register(target);
            if (!callbacksSubscribed)
            {
                // Environment animators stay loaded. A shared subscription prevents note pooling
                // from copying their large invocation lists on every spawn and recycle.
                clock.OnTimeChanged += ApplyStoppedSeek;
                clock.OnTimeFlushPending += FlushPendingAnimations;
                callbacksSubscribed = true;
            }
        }

        internal void Unregister(ObjectAnimator target)
        {
            targets.Unregister(target);
            pathTargets.Unregister(target);
            seekTargets.Unregister(target);
        }

        private void ApplyStoppedSeek()
        {
            if (clock.IsPlaying)
            {
                return;
            }
            seekTargets.Run(static target => target.ApplyStoppedSeek());
        }

        private void FlushPendingAnimations()
        {
            seekTargets.Run(static target => target.FlushPendingAnimations());
        }

        private void Update()
        {
            pathTargets.Run(static target => target.UpdateAnimation());
        }

        private void LateUpdate()
        {
            RecordPlayingDelta();
            targets.Run(static target => target.ApplyAnimation());
            seekTargets.Compact();
        }

        private void OnDestroy()
        {
            if (callbacksSubscribed && clock != null)
            {
                clock.OnTimeChanged -= ApplyStoppedSeek;
                clock.OnTimeFlushPending -= FlushPendingAnimations;
            }
        }
    }
}
