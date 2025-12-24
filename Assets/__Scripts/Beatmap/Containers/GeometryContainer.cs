using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

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
            container.Context = context;
            container.Animator.Context = context;
            container.Animator.TracksManager = tracksManager;
            container.EnvironmentEnhancement = eh;

            if (eh.Geometry != null)
            {
                // Continue with geometry generation if the Geometry object is defined
                GeneratePrimitiveGeometry(container, eh);
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

        private static void GeneratePrimitiveGeometry(GeometryContainer container, BaseEnvironmentEnhancement eh)
        {
            PrimitiveType type;
            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                type = PrimitiveType.Quad;
            }
            else
            {
                if (!Enum.TryParse<PrimitiveType>((string)eh.Geometry[eh.GeometryKeyType], out type))
                {
                    Debug.LogError($"Invalid geometry type '{(string)eh.Geometry[eh.GeometryKeyType]}'!");
                }
            }

            container.Shape = GameObject.CreatePrimitive(type);
            container.Shape.layer = 9;

            var collider = container.Shape.GetComponentInChildren<Collider>();
            if (collider != null) DestroyImmediate(collider);

            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                if (triangleMesh == null)
                {
                    triangleMesh = CreateTriangleMesh();
                }

                container.Shape.GetComponent<MeshFilter>().sharedMesh = triangleMesh;
                container.SelectionRenderers[0].transform.localPosition = new Vector3(0, 0, 0.01f);
            }
            else if (type == PrimitiveType.Quad)
            {
                container.SelectionRenderers[0].transform.localPosition = new Vector3(0, 0, -0.01f);
            }

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
            container.Shape.transform.localScale = 1.667f * Vector3.one;
        }

        private static void GenerateEnvironmentEnhancement(GeometryContainer container, BaseEnvironmentEnhancement eh, BeatmapRuntimeContext ctx)
        {
            // Get descriptor of currently loaded environment
            var environmentDescriptor = ctx.Descriptor;

            // No environment? No enhancement.
            if (environmentDescriptor == null) return;

            // Use the ID / Lookup method to find our target marker
            var chromaIDMarkers = environmentDescriptor.ChromaIDMarkers;
            var targetMarker = chromaIDMarkers.Find(marker => FindMarker(marker, eh));

            // Bail out if we couldn't find it
            if (targetMarker == null)
            {
                Debug.LogWarning($"Could not find target marker for environment enhancement {eh.ID}!");
                return;
            }

            container.MaterialPropertyBlock ??= new MaterialPropertyBlock();
            container.RendererCount = 0;

            // Gather a list of target objects.
            // It must be a list because...
            var targetObjects = new List<GameObject>() { targetMarker.gameObject };

            // We need to handle duplicates if defined!
            if (eh.Duplicate != null)
            {
                // Clear the list - we will no longer affect the original object
                targetObjects.Clear();

                // Instantiate a copy and add that to our list.
                var duplicates = eh.Duplicate.Value;
                for (var i = 0; i < duplicates; i++)
                {
                    var duplicateObject = Instantiate(targetMarker.gameObject);
                    targetObjects.Add(duplicateObject);
                }
            }

            // Apply enhancements to each target object (original or duplicates)
            foreach (var targetObject in targetObjects)
            {
                // Parent to our animator but keep world transform
                targetObject.transform.SetParent(container.Animator.AnimationThis.transform, true);

                container.Animator.AnimationThis.transform.SetPositionAndRotation(
                    targetObject.transform.position,
                    targetObject.transform.rotation);
                container.Animator.AnimationThis.transform.localScale = targetObject.transform.localScale;

                targetObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                targetObject.transform.localScale = Vector3.one * (5f / 3);

                // Reset layer to editor object layer
                targetObject.layer = 9;

                // Apply enhancement transforms
                if (eh.Position != null)
                {
                    container.Animator.AnimationThis.transform.position = eh.Position.Value;
                }
                if (eh.Rotation != null)
                {
                    container.Animator.AnimationThis.transform.rotation = Quaternion.Euler(eh.Rotation.Value);
                }
                if (eh.Scale != null)
                {
                    container.Animator.AnimationThis.transform.localScale = eh.Scale.Value;
                }
                if (eh.LocalPosition != null)
                {
                    container.Animator.AnimationThis.transform.localPosition = eh.LocalPosition.Value;
                }
                if (eh.LocalRotation != null)
                {
                    container.Animator.AnimationThis.transform.localRotation = Quaternion.Euler(eh.LocalRotation.Value);
                }

                // Add colliders to our container
                var colliders = targetObject.GetComponentsInChildren<MeshFilter>();
                foreach (var col in colliders)
                {
                    var intersection = targetObject.AddComponent<IntersectionCollider>();
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
                if (eh.Components != null)
                {
                    if (eh.Components.HasKey("ILightWithID") && targetMarker.TryGetComponent<LightController>(out var lightController))
                    {
                        var lightWithID = eh.Components["ILightWithID"];
                        var type = lightWithID["type"].AsInt;
                        var lightID = lightWithID["lightID"].AsInt;

                        environmentDescriptor.BasicEventEffectManager.Register(type, lightID, lightController);
                    }

                    // TODO: Handle TubeBloomPrePassLight to update bloomfog intensity / color alpha multiplier
                    // TODO: Handle BloomFogEnvironment to update environment bloom fog state
                }
            }
        }

        private static bool FindMarker(ChromaIDMarker marker, BaseEnvironmentEnhancement eh)
            => eh.LookupMethod switch
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
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0f, 0.5f, 0)
            };

            Vector2[] uv = { new Vector3(0, 0), new Vector3(1, 0), new Vector3(0.5f, 1) };

            int[] triangles = { 0, 1, 2 };

            var mesh = new Mesh() { vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
