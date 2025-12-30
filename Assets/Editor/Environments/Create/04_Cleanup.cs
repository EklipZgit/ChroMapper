using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class EnvironmentSceneCreator
{
    private static void Cleanup(Scene scene)
    {
        TraverseAndRemove(scene.GetRootGameObjects());
        var descriptor = GameObject.Find("Environment").GetComponent<EnvironmentDescriptor>();
        descriptor.ChromaIDMarkers = descriptor.GetComponentsInChildren<ChromaIDMarker>(true).ToList();
    }

    private static void TraverseAndRemove(GameObject[] gos)
    {
        foreach (var go in gos)
        {
            TraverseAndRemove(GetChildren(go));
            if (!go.GetComponents<Component>().All(x => x is Transform or ChromaIDMarker)
                || go.transform.childCount > 0)
                continue;
            Object.DestroyImmediate(go);
        }

        return;

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
