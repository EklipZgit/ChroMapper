using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public partial class EnvironmentSceneCreator
{
    private static Dictionary<string, GameObject> StripObjects(Scene scene, EnvironmentData data)
    {
        var existingObjects = new Dictionary<string, GameObject>();
        var validObjects = data.Objects.Select(x => x.ChromaID).ToHashSet();
        foreach (var environmentObject in data.Objects)
        {
            var parentId = GetParentChromaId(environmentObject.ChromaID);
            while (parentId != null && validObjects.Add(parentId)) parentId = GetParentChromaId(parentId);
        }
        TraverseAndStrip(scene.GetRootGameObjects());

        return existingObjects;

        void TraverseAndStrip(GameObject[] gos)
        {
            foreach (var go in gos)
            {
                var marker = go.GetComponent<ChromaIDMarker>();
                if (marker == null || !validObjects.Contains(marker.ChromaID))
                {
                    Object.DestroyImmediate(go);
                    continue;
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                var chromaId = marker.ChromaID;
                foreach (var component in go.GetComponents<Component>())
                {
                    // Reset values in place so saved component fileIDs survive regeneration.
                    if (component is Transform transform)
                    {
                        transform.localPosition = Vector3.zero;
                        transform.localRotation = Quaternion.identity;
                        transform.localScale = Vector3.one;
                        continue;
                    }

                    if (component is ParticleSystem particles)
                        particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    Unsupported.SmartReset(component);
                    if (component is Renderer renderer) renderer.SetPropertyBlock(null);
                }
                marker.ChromaID = chromaId;
                marker.MarkUse = false;
                marker.MarkActivator = false;

                existingObjects.Add(chromaId, go);
                TraverseAndStrip(GetChildren(go));
            }
        }

        // messy enumerator, wcyd
        GameObject[] GetChildren(GameObject go)
        {
            var objects = new List<GameObject>();
            var c = go.transform.childCount;
            for (var i = 0; i < c; i++) objects.Add(go.transform.GetChild(i).gameObject);
            return objects.ToArray();
        }
    }
}
