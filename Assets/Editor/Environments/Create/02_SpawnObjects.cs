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

        var queue = new Queue<EnvironmentDataObject>(container.Data.Objects);
        var limit = queue.Count;

        var i = 0;
        while (queue.Count > 0)
        {
            var environmentObject = queue.Dequeue();

            var name = environmentObject.ChromaID[
                (environmentObject.ChromaID.IndexOf("]", StringComparison.Ordinal) + 1)..];
            var parentName = name.Contains(".[") ? name[..name.LastIndexOf(".[", StringComparison.Ordinal)] : name;
            var actualParentGoName =
                environmentObject.ChromaID[..environmentObject.ChromaID.LastIndexOf(".[", StringComparison.Ordinal)];

            if (parentName != name && !chromaIdObjects.ContainsKey(actualParentGoName))
            {
                Debug.Log($"Could not find parent object for {environmentObject.ChromaID}, queued for later");
                if (++i == limit) throw new Exception("Queued too long, stuck?");
                queue.Enqueue(environmentObject);
                continue;
            }

            var go = existingObjects.TryGetValue(environmentObject.ChromaID, out var val) ? val : new GameObject();
            if (environmentObject.Components.MeshFilter == null
                || string.IsNullOrEmpty(environmentObject.Components.MeshFilter[0].Hash))
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter != null) Object.DestroyImmediate(filter);

                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null) Object.DestroyImmediate(filter);
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

                    if (environmentObject.Components.MeshRenderer != null
                        && environmentObject.Components.MeshRenderer[0].Materials.Any())
                    {
                        var comp = GetOrCreateMeshRenderer(go);
                        var mats = new List<Material>();
                        foreach (var matData in environmentObject.Components.MeshRenderer[0].Materials)
                        {
                            if (!container.Library.Materials.Lookup.TryGetValue(matData, out var mat)) continue;
                            mats.Add(mat);
                        }

                        comp.sharedMaterials = mats.ToArray();
                        environmentObject.Components.MeshRenderer[0].Instance = comp;
                    }
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

            foreach (var component in go.GetComponents<Collider>()) Object.DestroyImmediate(component);

            foreach (var fieldInfo in environmentObject.Components.GetType().GetFields())
            {
                if (!fieldInfo.FieldType.IsArray
                    || !typeof(EnvironmentComponentData).IsAssignableFrom(fieldInfo.FieldType.GetElementType()))
                    continue;
                if (fieldInfo.GetValue(environmentObject.Components) is not EnvironmentComponentData[] data) continue;
                foreach (var d in data) d.SpawnComponent(go);
            }

            go.name = environmentObject.GameObjectName;
            go.layer = container.Library.LayerMaskLookup[environmentObject.Layer].value.GetBitIndex().FirstOrDefault();
            chromaIdObjects[environmentObject.ChromaID] = go;

            // Add ChromaIDMarker for environment enhancements
            var marker = go.GetComponent<ChromaIDMarker>();
            if (marker == null) marker = go.AddComponent<ChromaIDMarker>();
            marker.ChromaID = environmentObject.ChromaID;

            if (parentName != name)
            {
                go.transform.SetParent(
                    chromaIdObjects[actualParentGoName].transform,
                    false);
            }

            environmentObject.Components.Transform[0].FillComponents(go, go.transform, container);
            go.SetActive(environmentObject.ActiveSelf);
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
