using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class EnvironmentSceneCreator
{
    private static Dictionary<string, GameObject> SpawnObjects(EnvironmentLibrarySO library, List<EnvDataObject> objectsToUse)
    {
        var chromaIdObjects = new Dictionary<string, GameObject>();

        var queue = new Queue<EnvDataObject>(objectsToUse);
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

            GameObject go;
            if (envObject.Components.MeshFilter == null || string.IsNullOrEmpty(envObject.Components.MeshFilter.Hash))
                go = new GameObject();
            else
            {
                if (library.Meshes.Lookup.TryGetValue(envObject.Components.MeshFilter.Hash, out var mesh)
                    && mesh != null)
                {
                    go = new GameObject();
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    var renderer = go.AddComponent<MeshRenderer>();
                    if (envObject.Components.MeshRenderer != null
                        && envObject.Components.MeshRenderer.Materials.Any())
                    {
                        if (library.Materials.Lookup.TryGetValue(
                                envObject.Components.MeshRenderer.Materials[0],
                                out var mat)
                            && mat != null)
                            renderer.sharedMaterial = mat;
                        else
                        {
                            Debug.LogWarning(
                                $"{envObject.ChromaID} material not found for:\n{envObject.Components.MeshRenderer.Materials[0]}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"{envObject.ChromaID} mesh not found for:\n{envObject.Components.MeshFilter.Hash} -- {library.Meshes.list.FindIndex(l => l.Hash == envObject.Components.MeshFilter.Hash)}");
                    go = new GameObject();
                    var fallback =
                        PrefabUtility.InstantiatePrefab(library.fallbackPrefab, go.transform) as GameObject;
                    var mInfo = library.Meshes.list.First(x => x.Hash == envObject.Components.MeshFilter.Hash);
                    fallback.transform.localPosition = mInfo.BoundsCenter;
                    fallback.transform.localScale = mInfo.BoundsSize;
                }
            }

            go.name = envObject.GameObjectName;
            go.layer = library.layerMaskLookup[envObject.Layer].value.Get1BitPositions()[0];
            chromaIdObjects[envObject.ChromaID] = go;

            // Add ChromaIDMarker for environment enhancements
            var marker = go.AddComponent<ChromaIDMarker>();
            marker.ChromaID = envObject.ChromaID;

            if (parentName != name)
            {
                go.transform.SetParent(
                    chromaIdObjects[actualParentGoName].transform,
                    false);
            }

            envObject.Components.Transform?.CopyTo(go.transform);
        }

        return chromaIdObjects;
    }
}
