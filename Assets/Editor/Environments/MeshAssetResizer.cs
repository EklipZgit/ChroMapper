using UnityEditor;
using UnityEngine;

public class MeshAssetResizer
{
    [MenuItem("Environment/Resize Mesh by 100x", false, 1200)]
    private static void CreateEnvironmentFromData()
    {
        if (Selection.activeObject == null || Selection.activeObject is not Mesh) return;

        var mesh = Selection.activeObject as Mesh;
        var vertices = mesh.vertices;
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            vertex *= 100f;
            vertices[i] = vertex;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Environment/Resize Mesh by 100x", true)]
    private static bool Validate() => Selection.objects.Length == 1 && Selection.activeObject is Mesh;
}
