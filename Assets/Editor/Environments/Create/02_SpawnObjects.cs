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
        EnvironmentLibrarySO library,
        EnvData data,
        Dictionary<string, GameObject> existingObjects)
    {
        var chromaIdObjects = new Dictionary<string, GameObject>();

        var queue = new Queue<EnvDataObject>(data.Objects);
        var limit = queue.Count;

        var i = 0;
        while (queue.Count > 0)
        {
            var envObject = queue.Dequeue();

            // static mesh or whatever, we dont need this
            if (envObject.ChromaID.Contains("Static Batch Component Container")) continue;

            var name = envObject.ChromaID[(envObject.ChromaID.IndexOf("]", StringComparison.Ordinal) + 1)..];
            var parentName = name.Contains(".[") ? name[..name.LastIndexOf(".[", StringComparison.Ordinal)] : name;
            var actualParentGoName =
                envObject.ChromaID[..envObject.ChromaID.LastIndexOf(".[", StringComparison.Ordinal)];

            if (parentName != name && !chromaIdObjects.ContainsKey(actualParentGoName))
            {
                Debug.Log($"Could not find parent object for {envObject.ChromaID}, queued for later");
                if (++i == limit) throw new Exception("Queued too long, stuck?");
                queue.Enqueue(envObject);
                continue;
            }

            var go = existingObjects.TryGetValue(envObject.ChromaID, out var val) ? val : new GameObject();
            if (envObject.Components.MeshFilter == null
                || string.IsNullOrEmpty(envObject.Components.MeshFilter[0].Hash))
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter != null) Object.DestroyImmediate(filter);

                var renderer = go.GetComponent<MeshRenderer>();
                // Remove the stale renderer too when regenerated data no longer describes renderable geometry.
                if (renderer != null) Object.DestroyImmediate(renderer);
            }
            else
            {
                if (library.Meshes.Lookup.TryGetValue(envObject.Components.MeshFilter[0].Hash, out var mesh)
                    && mesh != null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf == null) mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    if (envObject.Components.MeshRenderer != null
                        && envObject.Components.MeshRenderer[0].Materials.Any())
                    {
                        var renderer = GetOrCreateMeshRenderer(go);
                        var mats = new List<Material>();
                        foreach (var matData in envObject.Components.MeshRenderer[0].Materials)
                        {
                            if (!library.Materials.Lookup.TryGetValue(matData, out var mat)) continue;
                            mats.Add(mat);
                        }

                        renderer.sharedMaterials = mats.ToArray();
                    }
                }
                // remove this if statement if u need to search all "invisible" fallback object
                else if (envObject.Components.MeshRenderer != null)
                {
                    Debug.LogWarning(
                        $"{envObject.ChromaID} mesh not found for:\n{envObject.Components.MeshFilter[0].Hash} -- {library.Meshes.list.Find(l => l.Hash == envObject.Components.MeshFilter[0].Hash).Name}");
                    var fallback =
                        PrefabUtility.InstantiatePrefab(library.fallbackPrefab, go.transform) as GameObject;
                    var mInfo = library.Meshes.list.First(x => x.Hash == envObject.Components.MeshFilter[0].Hash);
                    fallback.transform.localPosition = mInfo.BoundsCenter;
                    fallback.transform.localScale = mInfo.BoundsSize;
                    // fallback.SetActive(false); // uncomment if u really dont want to see it when testing
                }
            }

            foreach (var component in go.GetComponents<Collider>()) Object.DestroyImmediate(component);

            if (envObject.Components.BoxCollider != null)
            {
                foreach (var comp in envObject.Components.BoxCollider)
                {
                    var box = go.AddComponent<BoxCollider>();
                    box.center = comp.Center;
                    box.size = comp.Size;
                }
            }

            if (envObject.Components.SphereCollider != null)
            {
                foreach (var comp in envObject.Components.SphereCollider)
                {
                    var box = go.AddComponent<SphereCollider>();
                    box.center = comp.Center;
                    box.radius = comp.Radius;
                }
            }

            if (envObject.Components.MeshCollider != null)
            {
                foreach (var _ in envObject.Components.MeshCollider)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf == null) break;
                    var m = go.AddComponent<MeshCollider>();
                    m.sharedMesh = mf.sharedMesh;
                    break;
                }
            }

            go.name = envObject.GameObjectName;
            go.layer = library.LayerMaskLookup[envObject.Layer].value.Get1BitPositions()[0];
            chromaIdObjects[envObject.ChromaID] = go;

            // Add ChromaIDMarker for environment enhancements
            var marker = go.GetComponent<ChromaIDMarker>();
            if (marker == null) marker = go.AddComponent<ChromaIDMarker>();
            marker.ChromaID = envObject.ChromaID;

            if (parentName != name)
            {
                go.transform.SetParent(
                    chromaIdObjects[actualParentGoName].transform,
                    false);
            }

            envObject.Components.Transform[0].CopyTo(go.transform);
            go.SetActive(envObject.ActiveSelf);
        }

        // Verify render references before saving so regeneration cannot silently produce incomplete scenes.
        ValidateSpawnedRenderAssets(library, data, chromaIdObjects);
        return chromaIdObjects;
    }

    private static void ValidateSpawnedRenderAssets(
        EnvironmentLibrarySO library,
        EnvData data,
        Dictionary<string, GameObject> chromaIdObjects)
    {
        var validatedMeshes = 0;
        var validatedMaterials = 0;
        foreach (var envObject in data.Objects)
        {
            var meshData = envObject.Components.MeshFilter?.FirstOrDefault();
            if (meshData == null
                || string.IsNullOrEmpty(meshData.Hash)
                || !library.Meshes.Lookup.TryGetValue(meshData.Hash, out var expectedMesh)
                || expectedMesh == null)
                continue;

            if (!chromaIdObjects.TryGetValue(envObject.ChromaID, out var go))
                throw new InvalidOperationException(
                    $"Environment '{data.Data.ID}' did not spawn mesh object '{envObject.ChromaID}'.");

            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh != expectedMesh)
                throw new InvalidOperationException(
                    $"Environment '{data.Data.ID}' failed to assign mesh '{meshData.Hash}' to '{envObject.ChromaID}'.");

            validatedMeshes++;
            var materialHashes = envObject.Components.MeshRenderer?.FirstOrDefault()?.Materials;
            if (materialHashes == null || materialHashes.Count == 0) continue;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                throw new InvalidOperationException(
                    $"Environment '{data.Data.ID}' did not create a renderer for '{envObject.ChromaID}'.");

            var assignedMaterials = renderer.sharedMaterials;
            if (assignedMaterials.Length != materialHashes.Count)
                throw new InvalidOperationException(
                    $"Environment '{data.Data.ID}' assigned {assignedMaterials.Length}/{materialHashes.Count} " +
                    $"materials to '{envObject.ChromaID}'.");

            for (var index = 0; index < materialHashes.Count; index++)
            {
                var materialHash = materialHashes[index];
                if (!library.Materials.Lookup.TryGetValue(materialHash, out var expectedMaterial)
                    || expectedMaterial == null
                    || assignedMaterials[index] != expectedMaterial)
                    throw new InvalidOperationException(
                        $"Environment '{data.Data.ID}' failed to assign material '{materialHash}' " +
                        $"to '{envObject.ChromaID}'.");

                validatedMaterials++;
            }
        }

        Debug.Log(
            $"Validated {data.Data.ID}: {validatedMeshes} mesh assignments and " +
            $"{validatedMaterials} material assignments.");
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
