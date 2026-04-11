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
        EnvironmentData environmentData,
        CreateContainer container,
        Dictionary<string, GameObject> existingObjects)
    {
        var chromaIdObjects = new Dictionary<string, GameObject>();
        container.ChromaIdObjects = chromaIdObjects;

        var queue = new Queue<EnvironmentDataObject>(environmentData.Objects);
        var limit = queue.Count;

        var blacklist = new[] { "Static Batch Component Container", "SaberBurnMarkSparklePS" };

        var i = 0;
        while (queue.Count > 0)
        {
            var envObject = queue.Dequeue();

            if (blacklist.Any(x => x.Contains(envObject.ChromaID) || envObject.ChromaID.Contains(x))) continue;

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
                if (container.Library.Meshes.Lookup.TryGetValue(envObject.Components.MeshFilter[0].Hash, out var mesh)
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
                            if (!container.Library.Materials.Lookup.TryGetValue(matData, out var mat)) continue;
                            mats.Add(mat);
                        }

                        renderer.sharedMaterials = mats.ToArray();
                    }
                }
                // remove this if statement if u need to search all "invisible" fallback object
                else if (envObject.Components.MeshRenderer != null)
                {
                    Debug.LogWarning(
                        $"{envObject.ChromaID} mesh not found for:\n{envObject.Components.MeshFilter[0].Hash} -- {container.Library.Meshes.list.Find(l => l.Hash == envObject.Components.MeshFilter[0].Hash).Name}");
                    var fallback =
                        PrefabUtility.InstantiatePrefab(container.Library.fallbackPrefab, go.transform) as GameObject;
                    var mInfo = container.Library.Meshes.list.First(x =>
                        x.Hash == envObject.Components.MeshFilter[0].Hash);
                    fallback.transform.localPosition = mInfo.BoundsCenter;
                    fallback.transform.localScale = mInfo.BoundsSize;
                    // fallback.SetActive(false); // uncomment if u really dont want to see it when testing
                }
            }

            foreach (var component in go.GetComponents<Collider>()) Object.DestroyImmediate(component);

            if (envObject.Components.BoxCollider != null)
                foreach (var comp in envObject.Components.BoxCollider)
                    comp.Apply(go, container);

            if (envObject.Components.SphereCollider != null)
                foreach (var comp in envObject.Components.SphereCollider)
                    comp.Apply(go, container);

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

            if (envObject.Components.ParticleSystem != null)
                foreach (var comp in envObject.Components.ParticleSystem)
                    comp.Apply(go, container);

            if (envObject.Components.Rigidbody != null)
                foreach (var comp in envObject.Components.Rigidbody)
                    comp.Apply(go, container);

            if (envObject.Components.SpringJoint != null)
                foreach (var comp in envObject.Components.SpringJoint)
                    comp.Apply(go, container);

            if (envObject.Components.SpriteRenderer != null)
                foreach (var comp in envObject.Components.SpriteRenderer)
                    comp.Apply(go, container);

            go.name = envObject.GameObjectName;
            go.layer = container.Library.LayerMaskLookup[envObject.Layer].value.GetBitIndex().FirstOrDefault();
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

        return chromaIdObjects;
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
