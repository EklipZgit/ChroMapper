using UnityEngine;

namespace Beatmap.Animations
{
    // Only directly tracked native children which update before their parent use this binding.
    internal sealed class DirectEnvironmentSharedTrackBinding
    {
        private readonly TrackAnimator source;
        private readonly bool v2;
        private readonly Matrix4x4 rootInverse;
        private readonly Quaternion rootRotation;
        private readonly Vector3 defaultPosition;
        private readonly Quaternion defaultRotation;
        private readonly Vector3 defaultScale;
        private float sampledTime = float.NaN;
        private int sampledVersion = -1;
        private bool instantWrite;
        private Vector3 parentPosition;
        private Vector3 inverseParentScale;
        private Quaternion inverseParentLocalRotation;
        private Quaternion inverseParentWorldRotation;

        internal DirectEnvironmentSharedTrackBinding(
            TrackAnimator source, bool v2, Matrix4x4 root,
            Vector3 defaultPosition, Quaternion defaultRotation, Vector3 defaultScale)
        {
            this.source = source;
            this.v2 = v2;
            rootInverse = root.inverse;
            rootRotation = root.rotation;
            this.defaultPosition = defaultPosition;
            this.defaultRotation = defaultRotation;
            this.defaultScale = defaultScale;
        }

        internal bool TryGetLocalPosition(float time, Vector3 worldPosition, out Vector3 localPosition)
        {
            localPosition = default;
            var localPositionKey = v2 ? AnimationPropertyKeys.LegacyLocalPositionKey : AnimationPropertyKeys.LocalPositionKey;
            if (TryRead<Vector3>(localPositionKey, time, out _) || !ReadPreviousParentPose(time))
            {
                return false;
            }
            var unscaled = inverseParentLocalRotation
                * (rootInverse.MultiplyPoint3x4(worldPosition) - parentPosition);
            localPosition = Vector3.Scale(unscaled, inverseParentScale);
            return true;
        }

        internal bool TryGetLocalRotation(float time, Quaternion worldRotation, out Quaternion localRotation)
        {
            localRotation = default;
            if (!ReadPreviousParentPose(time))
            {
                return false;
            }
            localRotation = inverseParentWorldRotation * worldRotation;
            return true;
        }

        private bool ReadPreviousParentPose(float time)
        {
            if (sampledTime == time && sampledVersion == source.UpdateVersion)
            {
                return instantWrite;
            }
            sampledTime = time;
            sampledVersion = source.UpdateVersion;
            var latestWrite = float.NegativeInfinity;
            var latestInstantWrite = float.NegativeInfinity;
            foreach (var property in source.AnimatedProperties.Values)
            {
                if (time < property.StartTime)
                {
                    continue;
                }
                latestWrite = Mathf.Max(latestWrite, property.GetTransformAnchorTime(time));
                if (property.TryGetInstantTransformWriteTime(time, out var writeTime))
                {
                    latestInstantWrite = Mathf.Max(latestInstantWrite, writeTime);
                }
            }
            instantWrite = latestInstantWrite != float.NegativeInfinity && latestInstantWrite == latestWrite;
            if (!instantWrite)
            {
                return false;
            }

            // Native controllers rewrite every supplied transform property when any track property changes, including color.
            var previousTime = AnimateProperty<Vector3>.GetPreviousTime(latestWrite);
            var units = v2 ? BeatmapConstant.LaneSize : 1f;
            var positionKey = v2 ? AnimationPropertyKeys.LegacyPositionKey : AnimationPropertyKeys.PositionKey;
            var localPositionKey = v2 ? AnimationPropertyKeys.LegacyLocalPositionKey : AnimationPropertyKeys.LocalPositionKey;
            parentPosition = defaultPosition;
            if (TryRead<Vector3>(localPositionKey, previousTime, out var animatedLocalPosition))
            {
                parentPosition = animatedLocalPosition * units;
            }
            else if (TryRead<Vector3>(positionKey, previousTime, out var animatedWorldPosition))
            {
                parentPosition = rootInverse.MultiplyPoint3x4(animatedWorldPosition * units);
            }

            var rotationKey = v2 ? AnimationPropertyKeys.LegacyRotationKey : AnimationPropertyKeys.RotationKey;
            var localRotationKey = v2 ? AnimationPropertyKeys.LegacyLocalRotationKey : AnimationPropertyKeys.LocalRotationKey;
            var parentRotation = Quaternion.Inverse(rootRotation) * defaultRotation;
            if (TryRead<Quaternion>(localRotationKey, previousTime, out var localRotation))
            {
                parentRotation = localRotation;
            }
            else if (TryRead<Quaternion>(rotationKey, previousTime, out var worldRotation))
            {
                parentRotation = Quaternion.Inverse(rootRotation) * worldRotation;
            }
            inverseParentLocalRotation = Quaternion.Inverse(parentRotation);
            inverseParentWorldRotation = Quaternion.Inverse(rootRotation * parentRotation);
            var scaleKey = v2 ? AnimationPropertyKeys.LegacyScaleKey : AnimationPropertyKeys.ScaleKey;
            var parentScale = TryRead<Vector3>(scaleKey, previousTime, out var scale) ? scale : defaultScale;
            // Unity preserves other axes when assigning world position under a parent with a collapsed axis.
            // Matrix4x4.inverse instead returns a zero matrix for the entire singular transform.
            inverseParentScale = new Vector3(
                parentScale.x == 0f ? 0f : 1f / parentScale.x,
                parentScale.y == 0f ? 0f : 1f / parentScale.y,
                parentScale.z == 0f ? 0f : 1f / parentScale.z);
            return true;
        }

        private bool TryRead<T>(string key, float time, out T value) where T : struct
        {
            value = default;
            if (source.AnimatedProperties.TryGetValue(key, out var request)
                && request is AnimateProperty<T> property && property.HasValueAt(time))
            {
                value = property.GetLerpedValue(time);
                return true;
            }
            return false;
        }
    }
}
