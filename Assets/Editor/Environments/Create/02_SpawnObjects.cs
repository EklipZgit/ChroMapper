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
                if (renderer != null) Object.DestroyImmediate(filter);
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
                        if (library.Materials.Lookup.TryGetValue(
                                envObject.Components.MeshRenderer[0].Materials[0],
                                out var mat)
                            && mat != null)
                            renderer.sharedMaterial = mat;
                        else
                        {
                            Debug.LogWarning(
                                $"{envObject.ChromaID} material not found for:\n{envObject.Components.MeshRenderer[0].Materials[0]}");
                        }
                    }
                }
                // remove this if statement if u need to search all "invisible" fallback object
                else if (envObject.Components.MeshRenderer != null)
                {
                    Debug.LogWarning(
                        $"{envObject.ChromaID} mesh not found for:\n{envObject.Components.MeshFilter[0].Hash} -- {library.Meshes.list.FindIndex(l => l.Hash == envObject.Components.MeshFilter[0].Hash)}");
                    var fallback =
                        PrefabUtility.InstantiatePrefab(library.fallbackPrefab, go.transform) as GameObject;
                    var mInfo = library.Meshes.list.First(x => x.Hash == envObject.Components.MeshFilter[0].Hash);
                    fallback.transform.localPosition = mInfo.BoundsCenter;
                    fallback.transform.localScale = mInfo.BoundsSize;
                }
            }

            if (envObject.Components.Collider != null)
            {
                foreach (var component in go.GetComponents<Collider>()) Object.DestroyImmediate(component);
                foreach (var colliderComponent in envObject.Components.Collider)
                {
                    switch (colliderComponent.Type)
                    {
                        case "BoxCollider":
                            var box = go.AddComponent<BoxCollider>();
                            box.center = colliderComponent.BoundsCenter;
                            box.size = colliderComponent.BoundsSize;
                            break;
                        case "MeshCollider":
                            var mf = go.GetComponent<MeshFilter>();
                            if (mf == null) break;
                            var m = go.AddComponent<MeshCollider>();
                            m.sharedMesh = mf.sharedMesh;
                            break;
                    }
                }
            }

            go.name = envObject.GameObjectName;
            go.layer = library.layerMaskLookup[envObject.Layer].value.Get1BitPositions()[0];
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
        }

        return chromaIdObjects;
    }

    private static MeshRenderer GetOrCreateMeshRenderer(GameObject go)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return renderer;
    }
}
