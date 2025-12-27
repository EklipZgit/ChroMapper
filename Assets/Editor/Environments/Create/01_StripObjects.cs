using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public partial class EnvironmentSceneCreator
{
    private static Dictionary<string, GameObject> StripObjects(Scene scene, EnvData data)
    {
        var existingObjects = new Dictionary<string, GameObject>();
        TraverseAndStrip(scene.GetRootGameObjects());

        return existingObjects;

        void TraverseAndStrip(GameObject[] gos)
        {
            foreach (var go in gos)
            {
                var marker = go.GetComponent<ChromaIDMarker>();
                if (marker == null || !data.Objects.Exists(d => d.ChromaID == marker.ChromaID))
                {
                    Object.DestroyImmediate(go);
                    continue;
                }

                foreach (var component in go.GetComponents<Component>())
                {
                    if (component is not (Transform or MeshFilter or MeshRenderer or ChromaIDMarker)) Object.DestroyImmediate(component);
                }

                existingObjects.Add(marker.ChromaID, go);
                TraverseAndStrip(GetChildren(go));
            }
        }

        GameObject[] GetChildren(GameObject go)
        {
            var objects = new List<GameObject>();
            var c = go.transform.childCount;
            for (var i = 0; i < c; i++) objects.Add(go.transform.GetChild(i).gameObject);

            return objects.ToArray();
        }
    }
}
