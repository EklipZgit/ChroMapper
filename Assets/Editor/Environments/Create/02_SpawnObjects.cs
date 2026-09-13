using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public partial class EnvironmentSceneCreator
{
    private static Dictionary<string, GameObject> SpawnObjects(
        CreateContainer container,
        Dictionary<string, GameObject> existingObjects)
    {
        var chromaIdObjects = new Dictionary<string, GameObject>();
        container.ChromaIdObjects = chromaIdObjects;

        foreach (var environmentObject in container.Data.Objects)
        {
            var go = GetOrCreateEnvironmentObject(environmentObject.ChromaID, chromaIdObjects, existingObjects);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (environmentObject.Components.MeshFilter == null
                || string.IsNullOrEmpty(environmentObject.Components.MeshFilter[0].Hash))
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter != null) Object.DestroyImmediate(filter);
            }
            else
            {
                if (container.Library.Meshes.Lookup.TryGetValue(
                        environmentObject.Components.MeshFilter[0].Hash,
                        out var mesh)
                    && mesh != null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf == null) mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;
                    environmentObject.Components.MeshFilter[0].Instance = mf;
                }
                // remove this if statement if u need to search all "invisible" fallback object
                else if (environmentObject.Components.MeshRenderer != null)
                {
                    Debug.LogWarning(
                        $"{environmentObject.ChromaID} mesh not found for:\n{environmentObject.Components.MeshFilter[0].Hash} -- {container.Library.Meshes.list.Find(l => l.Hash == environmentObject.Components.MeshFilter[0].Hash).Name}");
                    var fallback =
                        PrefabUtility.InstantiatePrefab(container.Library.fallbackPrefab, go.transform) as GameObject;
                    var mInfo = container.Library.Meshes.list.First(x =>
                        x.Hash == environmentObject.Components.MeshFilter[0].Hash);
                    fallback.transform.localPosition = mInfo.BoundsCenter;
                    fallback.transform.localScale = mInfo.BoundsSize;
                    // fallback.SetActive(false); // uncomment if u really dont want to see it when testing
                }
            }

            if (environmentObject.Components.MeshRenderer != null
                && environmentObject.Components.MeshRenderer[0].Materials.Any())
            {
                var comp = GetOrCreateMeshRenderer(go);
                var mats = new List<Material>();
                foreach (var matData in environmentObject.Components.MeshRenderer[0].Materials)
                {
                    if (!container.TryGetMaterial(matData, out var mat)) continue;
                    mats.Add(mat);
                }

                comp.sharedMaterials = mats.ToArray();
                environmentObject.Components.MeshRenderer[0].Instance = comp;
            }

            foreach (var component in go.GetComponents<Collider>()) Object.DestroyImmediate(component);

            var compData = new List<EnvironmentComponentData>();
            foreach (var fieldInfo in environmentObject.Components.GetType().GetFields())
            {
                if (!fieldInfo.FieldType.IsArray
                    || !typeof(EnvironmentComponentData).IsAssignableFrom(fieldInfo.FieldType.GetElementType()))
                    continue;
                if (fieldInfo.GetValue(environmentObject.Components) is not EnvironmentComponentData[] data) continue;
                compData.AddRange(data);
            }

            foreach (var data in compData.OrderBy(x => x.Priority)) data.SpawnComponent(go);

            go.name = environmentObject.GameObjectName;
            go.layer = container.Library.LayerMaskLookup[environmentObject.Layer].value.GetBitIndex().FirstOrDefault();
            if (go.name is "DustPS" or "DustBritney") go.AddComponent<FollowCamera>();

            environmentObject.Components.Transform[0].FillComponents(go, go.transform, container);
            go.SetActive(environmentObject.ActiveSelf);
        }

        // Verify render references before saving so regeneration cannot silently produce incomplete scenes.
        ValidateSpawnedRenderAssets(container.Library, container.Data, chromaIdObjects);
        return chromaIdObjects;
    }

    private static string GetParentChromaId(string chromaId)
    {
        var separator = chromaId.LastIndexOf(".[", StringComparison.Ordinal);
        // The prefix before the first indexed object identifies the scene, not a parent GameObject.
        return separator > chromaId.IndexOf("]", StringComparison.Ordinal) ? chromaId[..separator] : null;
    }

    private static GameObject GetOrCreateEnvironmentObject(
        string chromaId,
        Dictionary<string, GameObject> chromaIdObjects,
        Dictionary<string, GameObject> existingObjects)
    {
        if (chromaIdObjects.TryGetValue(chromaId, out var spawned)) return spawned;

        var parentId = GetParentChromaId(chromaId);
        var parent = parentId == null
            ? null
            : GetOrCreateEnvironmentObject(parentId, chromaIdObjects, existingObjects);
        var go = existingObjects.TryGetValue(chromaId, out var existing) ? existing : new GameObject();
        var segmentStart = chromaId.LastIndexOf(".[", StringComparison.Ordinal) + 2;
        go.name = chromaId[(chromaId.IndexOf(']', segmentStart) + 1)..];
        go.transform.SetParent(parent == null ? null : parent.transform, false);

        // Missing export ancestors stay as empty identity transforms; later entries populate the same objects.
        var marker = go.GetComponent<ChromaIDMarker>();
        if (marker == null) marker = go.AddComponent<ChromaIDMarker>();
        marker.ChromaID = chromaId;
        chromaIdObjects.Add(chromaId, go);
        return go;
    }

    private static void ValidateSpawnedRenderAssets(
        EnvironmentLibrarySO library,
        EnvironmentData data,
        Dictionary<string, GameObject> chromaIdObjects)
    {
        var validatedMeshes = 0;
        var validatedMaterials = 0;
        foreach (var envObject in data.Objects)
        {
            var meshData = envObject.Components.MeshFilter?.FirstOrDefault();
            Mesh expectedMesh = null;
            var hasExpectedMesh = meshData != null
                && !string.IsNullOrEmpty(meshData.Hash)
                && library.Meshes.Lookup.TryGetValue(meshData.Hash, out expectedMesh)
                && expectedMesh != null;
            var materialHashes = envObject.Components.MeshRenderer?.FirstOrDefault()?.Materials;
            if (!hasExpectedMesh && (materialHashes == null || materialHashes.Count == 0)) continue;

            if (!chromaIdObjects.TryGetValue(envObject.ChromaID, out var go))
                throw new InvalidOperationException(
                    $"Environment '{data.Data.ID}' did not spawn '{envObject.ChromaID}'.");

            if (hasExpectedMesh)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh != expectedMesh)
                    throw new InvalidOperationException(
                        $"Environment '{data.Data.ID}' failed to assign mesh '{meshData.Hash}' to '{envObject.ChromaID}'.");

                validatedMeshes++;
            }

            if (materialHashes == null || materialHashes.Count == 0) continue;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                throw new InvalidOperationException(
                    $"Environment '{data.Data.ID}' did not create a renderer for '{envObject.ChromaID}'.");

            var assignedMaterials = renderer.sharedMaterials;
            if (assignedMaterials.Length != materialHashes.Count)
                throw new InvalidOperationException(
                    $"Environment '{data.Data.ID}' assigned {assignedMaterials.Length}/{materialHashes.Count} "
                    + $"materials to '{envObject.ChromaID}'.");

            for (var index = 0; index < materialHashes.Count; index++)
            {
                var materialHash = materialHashes[index];
                if (!library.Materials.TryGetMaterial(data.Data.ID, materialHash, out var expectedMaterial)
                    || expectedMaterial == null
                    || assignedMaterials[index] != expectedMaterial)
                    throw new InvalidOperationException(
                        $"Environment '{data.Data.ID}' failed to assign material '{materialHash}' "
                        + $"to '{envObject.ChromaID}'.");

                validatedMaterials++;
            }
        }

        Debug.Log(
            $"Validated {data.Data.ID}: {validatedMeshes} mesh assignments and "
            + $"{validatedMaterials} material assignments.");
    }

    private static MeshRenderer GetOrCreateMeshRenderer(GameObject go)
    {
        var renderer = go.GetOrAddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return renderer;
    }
}
