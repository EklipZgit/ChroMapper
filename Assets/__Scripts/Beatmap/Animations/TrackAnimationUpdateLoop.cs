using UnityEngine;

namespace Beatmap.Animations
{
    // Tracks push after object paths and before ObjectAnimationUpdateLoop applies the combined pose.
    [DefaultExecutionOrder(6)]
    internal sealed class TrackAnimationUpdateLoop : MonoBehaviour
    {
        internal readonly AnimationUpdateRegistry<TrackAnimator> Targets = new(
            static target => target.AnimationUpdateIndex,
            static (target, index) => target.AnimationUpdateIndex = index);

        private void Update()
        {
            Targets.Run(static target => target.UpdateAnimation());
        }
    }
}
