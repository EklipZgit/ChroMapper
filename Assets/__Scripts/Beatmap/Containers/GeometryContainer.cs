using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beatmap.Containers
{
    public class GeometryContainer : ObjectContainer
    {
        private static Mesh triangleMesh = null;

        public GameObject Shape;

        public override BaseObject ObjectData
        {
            get => EnvironmentEnhancement;
            set => EnvironmentEnhancement = (BaseEnvironmentEnhancement)value;
        }

        public BeatmapRuntimeContext Context;

        public BaseEnvironmentEnhancement EnvironmentEnhancement;

        public ObjectAnimator MaterialAnimator;

        public override void UpdateGridPosition()
        {
        }

        public static GeometryContainer SpawnGeometry(
            BaseEnvironmentEnhancement eh,
            ref GameObject prefab,
            BeatmapRuntimeContext context,
            TracksManager tracksManager)
        {
            var container = Instantiate(prefab).GetComponent<GeometryContainer>();
            if (context.Descriptor != null)
                SceneManager.MoveGameObjectToScene(container.gameObject, context.Descriptor.gameObject.scene);
            container.Context = context;
            container.Animator.Context = context;
            container.Animator.TracksManager = tracksManager;
            container.EnvironmentEnhancement = eh;

            if (eh.Geometry != null)
            {
                // Continue with geometry generation if the Geometry object is defined
                GeneratePrimitiveGeometry(container, eh, context);
            }
            else
            {
                // Otherwise, fallback to environment enhancement
                GenerateEnvironmentEnhancement(container, eh, context);
            }

            container.Animator.AttachToGeometry(eh);
            container.gameObject.SetActive(true);
            container.UpdateCollisionGroups();

            return container;
        }

        private static void GeneratePrimitiveGeometry(
            GeometryContainer container,
            BaseEnvironmentEnhancement eh,
            BeatmapRuntimeContext ctx)
        {
            PrimitiveType type;
            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
                type = PrimitiveType.Quad;
            else
            {
                if (!Enum.TryParse(eh.Geometry[eh.GeometryKeyType], out type))
                    Debug.LogError($"Invalid geometry type '{(string)eh.Geometry[eh.GeometryKeyType]}'!");
            }

            container.Shape = GameObject.CreatePrimitive(type);
            container.Shape.layer = 9;

            var collider = container.Shape.GetComponentInChildren<Collider>();
            if (collider != null) DestroyImmediate(collider);

            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                if (triangleMesh == null) triangleMesh = CreateTriangleMesh();

                container.Shape.GetComponent<MeshFilter>().sharedMesh = triangleMesh;
                container.SelectionRenderers[0].transform.localPosition = new Vector3(0, 0, 0.01f);
            }
            else if (type == PrimitiveType.Quad)
                container.SelectionRenderers[0].transform.localPosition = new Vector3(0, 0, -0.01f);

            var mesh = container.Shape.GetComponent<MeshFilter>().sharedMesh;
            container.SelectionRenderers[0].GetComponent<MeshFilter>().sharedMesh = mesh;
            var intersection = container.Shape.AddComponent<IntersectionCollider>();
            var renderer = container.Shape.GetComponent<MeshRenderer>();
            intersection.Mesh = mesh;

            if (container.MaterialPropertyBlock == null)
            {
                container.MaterialPropertyBlock = new MaterialPropertyBlock();
                container.ModelRenderers.Add(renderer);
                container.RendererCount = container.ModelRenderers.Count;
            }

            container.Colliders.Add(intersection);
            container.Shape.transform.parent = container.Animator.AnimationThis.transform;
            container.Shape.transform.localScale = 5f / 3f * Vector3.one;

            // Handle components if needed
            var descriptor = ctx.Descriptor;
            // if (descriptor == null) return;

            if (eh.Components?.HasKey("ILightWithId") ?? false)
            {
                var controller = container.Shape.AddComponent<ParametricBloomFogLightController>();

                var light = container.Shape.AddComponent<ParametricBoxLight>();
                light.UpdateTransform = false;
                light.Renderer = container.Shape.GetComponent<Renderer>();
                controller.BoxLight = light;

                var bf = container.Shape.AddComponent<BloomFogObject>();
                controller.BloomFog = bf;

                controller.Type = eh.LightType ?? 0;
                controller.ID = eh.LightID ?? -1;
                descriptor.BasicEventEffectManager.Register(controller, false);
            }

            if (eh.Components?.HasKey("TubeBloomPrePassLight") ?? false)
            {
                var ppLight = eh.Components["TubeBloomPrePassLight"];
                var controller = container.Shape.GetComponent<ParametricBloomFogLightController>();
                if (controller == null) return;
                if (ppLight["colorAlphaMultiplier"] != null)
                {
                    if (controller.BoxLight != null)
                        controller.BoxLight.AlphaMultiplier = ppLight["colorAlphaMultiplier"];
                    if (controller.SpriteLight != null)
                        controller.SpriteLight.AlphaMultiplier = ppLight["colorAlphaMultiplier"];
                }

                if (ppLight["bloomFogIntensityMultiplier"] != null)
                    controller.BloomFog.IntensityMultiplier = ppLight["bloomFogIntensityMultiplier"];
            }
        }

        private static void GenerateEnvironmentEnhancement(
            GeometryContainer container,
            BaseEnvironmentEnhancement eh,
            BeatmapRuntimeContext ctx)
        {
            // Get descriptor of currently loaded environment
            var descriptor = ctx.Descriptor;

            // No environment? No enhancement.
            if (descriptor == null) return;

            // Use the ID / Lookup method to find our target marker
            var chromaIDMarkers = descriptor.ChromaIDMarkers;
            // Yes, all the matching IDs, don't ask me why
            var targetObjects = chromaIDMarkers.FindAll(marker => FindMarker(marker, eh));

            container.MaterialPropertyBlock ??= new MaterialPropertyBlock();
            container.RendererCount = 0;

            // We need to handle duplicates if defined!
            if (eh.Duplicate != null)
            {
                // Chroma precheck this and throws, but we don't care but we also do not want to destroy our PC
                // Also if this value is a lil inaccurate, feel free to change
                if (targetObjects.Count > 100)
                {
                    Debug.LogError(
                        "Extreme value reached, you are attempting to duplicate over 100 objects! Environment enhancements stopped");
                    return;
                }

                // Because we are duplicating, we make a new target list
                var newTargetObjects = new List<ChromaIDMarker>();
                var duplicates = eh.Duplicate.Value;
                foreach (var obj in targetObjects)
                {
                    for (var i = 0; i < duplicates; i++)
                    {
                        var duplicateObject = Instantiate(obj.gameObject, obj.transform.parent);
                        var marker = duplicateObject.GetComponent<ChromaIDMarker>();
                        marker.ChromaID = obj.ChromaID[..obj.ChromaID.LastIndexOf(']')] + marker.name;
                        descriptor.ChromaIDMarkers.Add(marker);
                        newTargetObjects.Add(marker);
                    }
                }

                targetObjects = newTargetObjects;
            }

            // lets pretend this is always valid
            if (eh.Components?.HasKey("BloomFogEnvironment") ?? false)
            {
                var bloomFog = eh.Components["BloomFogEnvironment"];
                if (bloomFog["attenuation"] != null) descriptor.BloomFogParams.Attenuation = bloomFog["attenuation"];
                if (bloomFog["offset"] != null) descriptor.BloomFogParams.Offset = bloomFog["offset"];
                if (bloomFog["startY"] != null) descriptor.BloomFogParams.StartY = bloomFog["startY"];
                if (bloomFog["height"] != null) descriptor.BloomFogParams.Height = bloomFog["height"];
            }

            // Apply enhancements to each target object (original or duplicates)
            foreach (var targetObject in targetObjects)
            {
                if (eh.Active != null) targetObject.gameObject.SetActive(eh.Active.AsBool);

                // Parent to our animator but keep world transform
                if (eh.Track != null)
                {
                    targetObject.transform.SetParent(container.Animator.AnimationThis.transform, true);

                    container.Animator.AnimationThis.transform.SetPositionAndRotation(
                        targetObject.transform.position,
                        targetObject.transform.rotation);
                    container.Animator.AnimationThis.transform.localScale = targetObject.transform.localScale;

                    targetObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    if (BeatSaberSongContainer.Instance.Map.MajorVersion == 2)
                        targetObject.transform.localScale = Vector3.one * (5f / 3);
                    else
                        targetObject.transform.localScale = Vector3.one;

                    // Apply enhancement transforms
                    if (eh.Position != null) container.Animator.AnimationThis.transform.position = eh.Position.Value;
                    if (eh.Rotation != null)
                        container.Animator.AnimationThis.transform.rotation = Quaternion.Euler(eh.Rotation.Value);
                    if (eh.Scale != null) container.Animator.AnimationThis.transform.localScale = eh.Scale.Value;
                    if (eh.LocalPosition != null)
                        container.Animator.AnimationThis.transform.localPosition = eh.LocalPosition.Value;
                    if (eh.LocalRotation != null)
                        container.Animator.AnimationThis.transform.localRotation =
                            Quaternion.Euler(eh.LocalRotation.Value);
                }
                else
                {
                    if (eh.Position != null) targetObject.transform.position = eh.Position.Value;
                    if (eh.Rotation != null) targetObject.transform.rotation = Quaternion.Euler(eh.Rotation.Value);
                    if (eh.Scale != null) targetObject.transform.localScale = eh.Scale.Value;
                    if (eh.LocalPosition != null) targetObject.transform.localPosition = eh.LocalPosition.Value;
                    if (eh.LocalRotation != null)
                        targetObject.transform.localRotation = Quaternion.Euler(eh.LocalRotation.Value);
                }

                // Add colliders to our container
                var colliders = targetObject.GetComponentsInChildren<MeshFilter>();
                foreach (var col in colliders)
                {
                    var intersection = targetObject.gameObject.AddComponent<IntersectionCollider>();
                    intersection.Mesh = col.sharedMesh;
                    container.Colliders.Add(intersection);

                    // Add renderer too
                    if (col.TryGetComponent<MeshRenderer>(out var renderer))
                    {
                        container.ModelRenderers.Add(renderer);
                        container.RendererCount++;
                    }
                }

                // Handle components if needed
                foreach (var controller in targetObject.GetComponentsInChildren<LightController>(true))
                {
                    if (eh.Duplicate == null) descriptor.Unregister(controller);

                    if (controller.Kind == LightController.LightKind.Basic)
                    {
                        controller.Type = eh.LightType ?? controller.Type;
                        controller.ID = eh.LightID ?? controller.ID;
                    }

                    descriptor.Register(controller, false);

                    if (eh.Components?.HasKey("TubeBloomPrePassLight") ?? false)
                    {
                        var ppLight = eh.Components["TubeBloomPrePassLight"];
                        if (controller is not ParametricBloomFogLightController pbflc) continue;
                        if (ppLight["colorAlphaMultiplier"] != null)
                        {
                            if (pbflc.BoxLight != null)
                                pbflc.BoxLight.AlphaMultiplier = ppLight["colorAlphaMultiplier"];
                            if (pbflc.SpriteLight != null)
                                pbflc.SpriteLight.AlphaMultiplier = ppLight["colorAlphaMultiplier"];
                        }

                        if (ppLight["bloomFogIntensityMultiplier"] != null)
                            pbflc.BloomFog.IntensityMultiplier = ppLight["bloomFogIntensityMultiplier"];
                    }
                }

                foreach (var effect in targetObject
                    .GetComponentsInChildren<StateManager<BaseEvent>>())
                    descriptor.BasicEventEffectManager.Register(effect.ID, effect);
            }
        }

        private static bool FindMarker(ChromaIDMarker marker, BaseEnvironmentEnhancement eh) =>
            eh.LookupMethod switch
            {
                EnvironmentLookupMethod.Exact => marker.ChromaID == eh.ID,
                EnvironmentLookupMethod.StartsWith => marker.ChromaID.StartsWith(eh.ID),
                EnvironmentLookupMethod.EndsWith => marker.ChromaID.EndsWith(eh.ID),
                EnvironmentLookupMethod.Contains => marker.ChromaID.Contains(eh.ID),
                EnvironmentLookupMethod.Regex => Regex.IsMatch(marker.ChromaID, eh.ID),
                _ => throw new ArgumentException($"Unknown lookup method {eh.LookupMethod}"),
            };

        /// <summary>
        /// https://answers.unity.com/questions/1594750/is-there-a-premade-triangle-asset.html
        /// </summary>
        private static Mesh CreateTriangleMesh()
        {
            Vector3[] vertices = { new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0), new(0f, 0.5f, 0) };
            Vector2[] uv = { new Vector3(0, 0), new Vector3(1, 0), new Vector3(0.5f, 1) };
            int[] triangles = { 0, 1, 2 };

            var mesh = new Mesh { vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
