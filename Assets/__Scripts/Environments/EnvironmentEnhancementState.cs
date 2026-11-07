using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Enhancement instructions can write into existing hierarchies and register clones outside their containers.
// Keep the environment's original state once so replaying edited instructions can undo those writes first.
internal sealed class EnvironmentEnhancementState
{
    private readonly EnvironmentDescriptor descriptor;
    private readonly List<TransformState> transforms = new();
    private readonly List<Action> restores = new();
    private readonly List<GameObject> clones = new();
    private readonly ChromaIDMarker[] markers;

    private readonly struct TransformState
    {
        private readonly Transform target;
        private readonly Transform parent;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Vector3 scale;
        private readonly bool active;
        private readonly Scene scene;

        public TransformState(Transform target)
        {
            this.target = target;
            parent = target.parent;
            position = target.localPosition;
            rotation = target.localRotation;
            scale = target.localScale;
            active = target.gameObject.activeSelf;
            scene = target.gameObject.scene;
        }

        public void Restore()
        {
            target.SetParent(parent, false);
            if (parent == null && target.gameObject.scene != scene)
                SceneManager.MoveGameObjectToScene(target.gameObject, scene);

            target.SetLocalPositionAndRotation(position, rotation);
            target.localScale = scale;
            target.gameObject.SetActive(active);
        }
    }

    public EnvironmentEnhancementState(EnvironmentDescriptor descriptor)
    {
        this.descriptor = descriptor;
        markers = descriptor.ChromaIDMarkers.ToArray();
        restores.Add(descriptor.BasicEventEffectManager.CaptureRegistrations());
        foreach (var effect in descriptor.LightColorGroupEffectManager.IdToEffect.Values)
            restores.Add(effect.CaptureRegistrations());

        foreach (var root in descriptor.gameObject.scene.GetRootGameObjects())
            CaptureHierarchy(root);

    }

    private void CaptureHierarchy(GameObject root)
    {
        foreach (var target in root.GetComponentsInChildren<Transform>(true))
            transforms.Add(new TransformState(target));

        foreach (var controller in root.GetComponentsInChildren<LightController>(true))
        {
            var type = controller.Type;
            var id = controller.ID;
            restores.Add(() =>
            {
                controller.Type = type;
                controller.ID = id;
            });
        }

        foreach (var controller in root.GetComponentsInChildren<ParametricBloomFogLightController>(true))
        {
            var alpha = controller.ColorAlphaMultiplier;
            var intensity = controller.BloomFogIntensityMultiplier;
            restores.Add(() =>
            {
                controller.SetColorAlphaMultiplier(alpha);
                controller.SetBloomFogIntensityMultiplier(intensity);
            });
        }

        foreach (var box in root.GetComponentsInChildren<ParametricBoxLight>(true))
            restores.Add(box.CaptureTransformOverrides());

        foreach (var manager in root.GetComponentsInChildren<TrackLaneRingsManager>(true))
        {
            var rings = manager.Rings.ToArray();
            restores.Add(() =>
            {
                manager.Rings.Clear();
                manager.Rings.AddRange(rings);
            });
        }

        foreach (var ring in root.GetComponentsInChildren<TrackLaneRing>(true))
        {
            var offset = ring.PositionOffset;
            restores.Add(() => ring.PositionOffset = offset);
        }
    }

    public void AddClone(GameObject clone) => clones.Add(clone);

    public void Restore()
    {
        // Restore parents before destroying clones: an AssignTrackParent can move original objects beneath them.
        foreach (var state in transforms)
            state.Restore();

        for (var i = clones.Count - 1; i >= 0; i--)
        {
            if (clones[i] != null)
                UnityEngine.Object.DestroyImmediate(clones[i]);
        }

        clones.Clear();
        descriptor.ChromaIDMarkers.Clear();
        descriptor.ChromaIDMarkers.AddRange(markers);
        // Restore controller IDs before restoring registration tables, which use their physical indices.
        for (var i = restores.Count - 1; i >= 0; i--)
            restores[i]();

        descriptor.ResetCloneSiblingIndices();
        descriptor.BloomFogParams.ResetToDefaults();
    }
}
