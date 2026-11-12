using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnvironmentNativeHierarchy
{
    public static List<EnvironmentDescriptor.NativeSiblingCount> ReadCounts(EnvironmentData data)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in data.Objects)
        {
            var nameStart = item.ChromaID.Length - item.GameObjectName.Length;
            var indexStart = item.ChromaID.LastIndexOf('[', nameStart - 1);
            var parentId = item.ChromaID.Substring(0, indexStart);
            var index = int.Parse(item.ChromaID.Substring(indexStart + 1, nameStart - indexStart - 2), CultureInfo.InvariantCulture);
            counts[parentId] = Math.Max(counts.GetValueOrDefault(parentId), index + 1);
        }
        return counts.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new EnvironmentDescriptor.NativeSiblingCount { ParentId = pair.Key, ChildCount = pair.Value }).ToList();
    }

    [MenuItem("Environment/Update Native Hierarchy Counts")]
    public static void UpdateAllScenes()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var path in Directory.GetFiles("Assets/__Scenes/Environments/Data", "*.json").OrderBy(path => path, StringComparer.Ordinal))
            {
                var scenePath = PathUtils.Combine("Assets", "__Scenes", "Environments", Path.GetFileNameWithoutExtension(path) + ".unity");
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var descriptor = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnvironmentDescriptor>(true)).Single();
                var dataPath = PathUtils.Combine("Assets", "__Scenes", "Environments", "Data", Path.GetFileName(path));
                var data = CreateUtils.JsonToEnvironmentData(AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath));
                descriptor.NativeSiblingCounts = ReadCounts(data);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("Could not save native hierarchy counts: " + scenePath);
                }
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
