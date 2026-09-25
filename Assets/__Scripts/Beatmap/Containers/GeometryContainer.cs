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

            // MaterialTrackAnimationTest.TrackedMaterialAnimatesGeometryColor: Give In To You NRE'd at map
            // load because Geometry.prefab's MaterialAnimator was never wired - the AnimationTarget child
            // from 49ca9f0a was lost when the prefab was recreated, so SetGeometryAppearance crashed on any
            // customData.materials entry carrying a "track". Material color animation needs a second
            // ObjectAnimator because AttachToMaterial's ResetData would wipe the transform animator's state;
            // it stays disabled until AttachToMaterial enables it, and its own transform acts as a sink so a
            // track that animates position alongside color cannot NRE on a null LocalTarget.
            var materialAnimatorTarget = new GameObject("AnimationTarget");
            materialAnimatorTarget.layer = container.gameObject.layer;
            materialAnimatorTarget.transform.SetParent(container.transform, false);
            var materialAnimator = materialAnimatorTarget.AddComponent<ObjectAnimator>();
            materialAnimator.LocalTarget = materialAnimatorTarget.transform;
            materialAnimator.WorldTarget = materialAnimatorTarget.transform;
            materialAnimator.Context = context;
            materialAnimator.TracksManager = tracksManager;
            materialAnimator.enabled = false;
            container.MaterialAnimator = materialAnimator;

            container.EnvironmentEnhancement = eh;

            if (eh.Geometry != null)
            {
                // Continue with geometry generation if the Geometry object is defined
                GeneratePrimitiveGeometry(container, eh, context);
                // Primitive geometry is owned by this container, so its existing single-target animator remains valid.
                container.Animator.AttachToGeometry(eh);
            }
            else
            {
                // EnvironmentEnhancementWith*Track* regression tests require OEM hierarchies to remain intact; each
                // matched object receives a data-only track controller instead of sharing the container transform.
                GenerateEnvironmentEnhancement(container, eh, context, tracksManager);
                container.Animator.enabled = false;
            }

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

            var shape = GameObject.CreatePrimitive(type);
            shape.transform.SetParent(container.transform);
            shape.layer = container.gameObject.layer;
            container.MpbController.Renderers = new() { shape.GetComponent<MeshRenderer>() };

            var collider = shape.GetComponentInChildren<Collider>();
            if (collider != null) DestroyImmediate(collider);

            if (eh.Geometry[eh.GeometryKeyType] == "Triangle")
            {
                if (triangleMesh == null) triangleMesh = CreateTriangleMesh();
                shape.GetComponent<MeshFilter>().sharedMesh = triangleMesh;
            }

            // Handle components if needed
            var descriptor = ctx.Descriptor;
            // if (descriptor == null) return;

            if (eh.Components?.HasKey("ILightWithId") ?? false)
            {
                var controller = shape.AddComponent<ParametricBloomFogLightController>();

                var light = shape.AddComponent<ParametricBoxLight>();
                light.UpdateTransform = false;
                light.Renderer = container.MpbController.Renderers[0];
                controller.BoxLight = light;

                var bf = shape.AddComponent<BloomFogObject>();
                controller.BloomFog = bf;

                controller.Type = eh.LightType ?? 0;
                // Chroma's GeometryFactory marks geometry lights for table registration, so the authored
                // ILightWithId.lightID is a requested table key, not the runtime index; Register assigns the
                // append index and records the key (RegisterIndex) instead of pinning the key as the index.
                descriptor.Register(controller, eh.LightID);
            }

            if (eh.Components?.HasKey("TubeBloomPrePassLight") ?? false)
            {
                var ppLight = eh.Components["TubeBloomPrePassLight"];
                var controller = shape.GetComponent<ParametricBloomFogLightController>();
                if (controller == null) return;
                if (ppLight["colorAlphaMultiplier"] != null)
                    controller.ColorAlphaMultiplier = ppLight["colorAlphaMultiplier"];
                if (ppLight["bloomFogIntensityMultiplier"] != null)
                    controller.BloomFogIntensityMultiplier = ppLight["bloomFogIntensityMultiplier"];
            }
        }

        private static void GenerateEnvironmentEnhancement(
            GeometryContainer container,
            BaseEnvironmentEnhancement eh,
            BeatmapRuntimeContext ctx,
            TracksManager tracksManager)
        {
            // Get descriptor of currently loaded environment
            var descriptor = ctx.Descriptor;

            // No environment? No enhancement.
            if (descriptor == null) return;

            // Use the ID / Lookup method to find our target marker
            var chromaIDMarkers = descriptor.ChromaIDMarkers;
            // Yes, all the matching IDs, don't ask me why
            var targetObjects = chromaIDMarkers.Where(marker => FindMarker(marker, eh)).Select(x => (x, x)).ToList();

            // Chroma logs a per-enhancement error when a lookup finds nothing (EnvironmentEnhancementManager's
            // "found nothing" InvalidOperationException); CM silently skipped them, hiding lookup parity gaps like
            // WorldCavesInEnvironmentTest's Ring1..Ring10 regexes matching the game's GameCore.[1..10] ring IDs but
            // not this scene's. Warn with Chroma's message shape so in-game and CM logs compare line-for-line.
            if (targetObjects.Count == 0)
            {
                Debug.LogWarning(
                    $"Environment enhancement ID [\"{eh.ID}\"] using method [{eh.LookupMethod:G}] found nothing.");
            }

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
                var newTargetObjects = new List<(ChromaIDMarker, ChromaIDMarker)>();
                var duplicates = eh.Duplicate.Value;
                foreach (var (original, _) in targetObjects)
                {
                    for (var i = 0; i < duplicates; i++)
                    {
                        // WorldCavesInEnvironmentTest.KaleidoscopeDuplicatesCloneAndSwirlAtBeat266 found duplicates
                        // stranded in the 03_Mapper scene (surviving environment reloads): Chroma's duplicate loop
                        // instantiates each clone unparented, moves it into the original's scene, and only then
                        // parents it (EnvironmentEnhancementManager: Instantiate -> MoveGameObjectToScene ->
                        // SetParent). CM parented at instantiate time, which creates the clone in the parent's
                        // scene - a mapper-scene track parent once an earlier enhancement attached the original -
                        // and SceneManager.MoveGameObjectToScene only accepts root objects, so mirror Chroma's
                        // exact root-instantiate/move/parent sequence instead.
                        var duplicateObject = Instantiate(original.gameObject);
                        SceneManager.MoveGameObjectToScene(duplicateObject, original.gameObject.scene);
                        duplicateObject.transform.SetParent(original.transform.parent, true);
                        var duplicate = duplicateObject.GetComponent<ChromaIDMarker>();
                        var originalParentId = duplicate.ChromaID;
                        duplicate.ChromaID = original.ChromaID[..(original.ChromaID.LastIndexOf(']') + 1)]
                            + duplicate.name;
                        // WorldCavesInEnvironmentTest.KaleidoscopeDuplicatesCloneAndSwirlAtBeat266 found every
                        // duplicate marker ending in "(Clone)(Clone)": the root marker's ChromaID was already
                        // assigned its final prefix + instantiated-name value above, but GetComponentsInChildren
                        // returns that same root marker, so re-running the parent-ID replacement appends a second
                        // suffix. Chroma's duplicate GameObjectInfo derives the clone's ID from the instantiated
                        // GameObject's name with exactly one "(Clone)", so the already-final root must not be
                        // rewritten again.
                        foreach (var childMarker in duplicateObject.GetComponentsInChildren<ChromaIDMarker>())
                        {
                            if (childMarker != duplicate)
                            {
                                childMarker.ChromaID =
                                    childMarker.ChromaID.Replace(originalParentId, duplicate.ChromaID);
                            }

                            descriptor.ChromaIDMarkers.Add(childMarker);
                        }

                        newTargetObjects.Add((original, duplicate));
                    }
                }

                targetObjects = newTargetObjects;
            }

            // BloomFogChromaParityAuditTest.EnhancementOnNonFogObjectKeepsEnvironmentFog:
            // Chroma customizes only BloomFogEnvironment components under matched objects;
            // this scene stores that component's preview state on the environment descriptor root.
            var ownsFog = targetObjects.Any(pair =>
                pair.Item2.transform == descriptor.transform
                || descriptor.transform.IsChildOf(pair.Item2.transform));
            // BloomFogChromaParityAuditTest.EnhancementOnNonFogObjectKeepsEnvironmentFog:
            // keep the skipped-component diagnosis visible in the editor log, as Chroma does.
            if (!ownsFog && (eh.Components?.HasKey("BloomFogEnvironment") ?? false))
                Debug.LogWarning($"BloomFogEnvironment enhancement ID [{eh.ID}] matched no fog component.");
            if (ownsFog && (eh.Components?.HasKey("BloomFogEnvironment") ?? false))
            {
                var bloomFog = eh.Components["BloomFogEnvironment"];
                if (bloomFog["attenuation"] != null) descriptor.BloomFogParams.Attenuation = bloomFog["attenuation"];
                if (bloomFog["offset"] != null) descriptor.BloomFogParams.Offset = bloomFog["offset"];
                if (bloomFog["startY"] != null) descriptor.BloomFogParams.StartY = bloomFog["startY"];
                if (bloomFog["height"] != null) descriptor.BloomFogParams.Height = bloomFog["height"];

                // BloomFogEnvironmentEnhancementUpdatesRenderingState proves descriptor-only mutations leave the
                // already-loaded renderer and its shader globals stale, so publish all final component values now.
                ctx.NotifyBloomFogParamsChanged();
            }

            // Cache the map-version decision once because every matched marker uses the same V2 unit conversion.
            var v2 = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;
            var adjustScale = v2
                ? BeatmapConstant.LaneSize
                : 1f;
            // Apply enhancements to each target object (original or duplicates)
            foreach (var (original, target) in targetObjects)
            {
                if (eh.Active != null) target.gameObject.SetActive(eh.Active.AsBool);

                // ParametricBoxEnhancementTransformTest and HeliovMapParityTest: the native box
                // must perform its first parametric refresh before the enhancement captures any
                // child mesh pose; otherwise Heliov saves the scene's Y=0 instead of Y=1250.
                var boxLight = eh.Position != null || eh.LocalPosition != null || eh.Scale != null || eh.Track != null
                    ? target.GetComponentInChildren<ParametricBoxLight>(true)
                    : null;
                if (boxLight != null)
                    boxLight.InitIfNeeded();

                // Chroma applies authored enhancement transforms before attaching a track and never reparents the
                // matched object, which preserves nested BigTrackLaneRing renderers in all four parity cases.
                if (eh.Scale != null) target.transform.localScale = eh.Scale.Value;
                if (eh.LocalPosition != null)
                    target.transform.localPosition = eh.LocalPosition.Value * adjustScale;
                else if (eh.Position != null) target.transform.position = eh.Position.Value * adjustScale;
                if (eh.LocalRotation != null)
                    target.transform.localRotation = Quaternion.Euler(eh.LocalRotation.Value);
                else if (eh.Rotation != null) target.transform.rotation = Quaternion.Euler(eh.Rotation.Value);

                // ParametricBoxEnhancementTransformTest: Chroma captures the child box mesh after an
                // enhancement is applied, and only authored position/scale may override light refreshes.
                if (boxLight != null)
                {
                    if (eh.Position != null || eh.LocalPosition != null)
                        boxLight.CaptureAuthoredPosition();
                    if (eh.Scale != null)
                        boxLight.CaptureAuthoredScale();
                }

                // A track with no transform property must be inert; a present property is applied directly to each
                // matched transform by its own animator, matching Chroma without flattening the OEM hierarchy.
                if (eh.Track != null)
                {
                    var animator = container.gameObject.AddComponent<ObjectAnimator>();
                    animator.Context = ctx;
                    animator.TracksManager = tracksManager;
                    animator.AttachToEnvironmentObject(
                        target.transform,
                        eh.Track,
                        v2,
                        boxLight);
                    // BloomFogChromaParityAuditTest.AnimateComponentOnNonFogTrackKeepsEnvironmentFog:
                    // only tracks attached to the descriptor's fog owner may animate its four parameters.
                    if (target.transform == descriptor.transform
                        || descriptor.transform.IsChildOf(target.transform))
                        tracksManager.BindFogComponentTarget(eh.Track);
                }

                if (eh.Duplicate != null) HandleDuplicateComponents(original.transform, target.transform);

                foreach (var controller in target.GetComponentsInChildren<LightController>(true))
                {
                    var customized = eh.LightType.HasValue || eh.LightID.HasValue;
                    if (eh.Duplicate != null || customized)
                    {
                        if (eh.Duplicate == null) descriptor.Unregister(controller);
                        if (controller.Kind == LightController.LightKind.Basic)
                            controller.Type = eh.LightType ?? controller.Type;

                        descriptor.Register(controller, eh.LightID);
                    }

                    if (eh.Components?.HasKey("TubeBloomPrePassLight") ?? false)
                    {
                        var ppLight = eh.Components["TubeBloomPrePassLight"];
                        if (controller is not ParametricBloomFogLightController pbflc) continue;
                        if (ppLight["colorAlphaMultiplier"] != null)
                            pbflc.ColorAlphaMultiplier = ppLight["colorAlphaMultiplier"];
                        if (ppLight["bloomFogIntensityMultiplier"] != null)
                            pbflc.BloomFogIntensityMultiplier = ppLight["bloomFogIntensityMultiplier"];
                    }
                }

            }

            return;

            void HandleDuplicateComponents(Transform original, Transform target)
            {
                // var originalComponents = original.GetComponents<MonoBehaviour>();
                var targetComponents = target.GetComponents<MonoBehaviour>();

                for (var i = 0; i < targetComponents.Length; i++)
                {
                    // var originalComponent = originalComponents[i];
                    var targetComponent = targetComponents[i];

                    switch (targetComponent)
                    {
                        case TrackLaneRing trackLaneRing:
                            // var originalTrackLaneRing = (TrackLaneRing)originalComponent;
                            // _trackLaneRingOffset.CopyRing(originalTrackLaneRing, trackLaneRing);

                            if (trackLaneRing.ParentManager != null && !trackLaneRing.ParentManager.SpawnAsChildren)
                                trackLaneRing.ParentManager.Rings.Add(trackLaneRing);

                            break;

                        case StateManager<BaseEvent> effect:
                            descriptor.BasicEventEffectManager.Register(effect.ID, effect);
                            break;
                    }
                }

                foreach (Transform newTarget in target)
                {
                    var index = newTarget.GetSiblingIndex();
                    // in the future, we might need the original
                    HandleDuplicateComponents(newTarget, newTarget);
                }
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
