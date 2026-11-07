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
