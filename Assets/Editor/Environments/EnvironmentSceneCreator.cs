using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to create a new Unity scene from an EnvironmentInfo JSON file.
/// </summary>
public class EnvironmentSceneCreator
{
    private const string environmentPath = "Assets/__Scenes/Environments";
    private const string assetPath = "Assets/Editor/Environments";

    [MenuItem("Environment/Create from Data", false, 1000)]
    private static void CreateEnvironmentFromData()
    {
        // Check if exactly one object is selected and it's a TextAsset
        // We re-do the check here for safety and to grab a reference to the TextAsset
        if (Selection.objects.Length != 1 || Selection.activeObject is not TextAsset textAsset) return;

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(newScene);

        // Save the scene with the new name (in memory, not on disk yet)
        var assetName = textAsset.name;
        newScene.name = assetName;

        // Oh dear I'm loading stuff at runtime
        var environmentLibrary =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrary>(Path.Combine(assetPath, "EnvironmentLibrary.asset"));
        var environmentData =
            JsonConvert.DeserializeObject<EnvironmentData>(textAsset.text, new Vector3ArrayConverter());
        var environmentBuild =
            AssetDatabase.LoadAssetAtPath<EnvironmentBuildSO>(
                Path.Combine(environmentPath, "Data", $"{assetName}BuildSO.asset"));

        // Move null checks up here so it doesnt ruin the rest of the process
        if (environmentLibrary == null) throw new System.ArgumentNullException(nameof(environmentLibrary));
        if (environmentData == null) throw new System.ArgumentNullException(nameof(environmentData));
        if (environmentBuild == null) throw new System.ArgumentNullException(nameof(environmentBuild));

        // Set the skybox material if specified in the library
        if (environmentLibrary.SkyboxMaterial != null) RenderSettings.skybox = environmentLibrary.SkyboxMaterial;

        // Create the environment in the new scene
        CreateEnvironment(environmentData, environmentLibrary, environmentBuild);

        // Save the scene to disk
        var scenePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(environmentPath, $"{assetName}.unity"));
        if (EditorSceneManager.SaveScene(newScene, scenePath))
        {
            // Select the newly created scene in the Project window
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }
        else
            Debug.LogError("Failed to save the new environment scene.");
    }

    // Validate menu: only show if a single TextAsset is selected
    [MenuItem("Environment/Create from Data", true)]
    private static bool ValidateCreateEnvironmentFromData() =>
        Selection.objects.Length == 1 && Selection.activeObject is TextAsset;

    // Main method which constructs the environment from parsed data
    private static void CreateEnvironment(EnvironmentData data, EnvironmentLibrary library, EnvironmentBuildSO envBuild)
    {
        Transform lastParent = null;
        string lastParentChromaID = null;
        foreach (var envObject in data.Objects)
        {
            // Skip ignored objects
            // if (library.IsIgnored(envObject.ChromaID)) continue; // i pretend to not see

            // If our parents name is not present in the Chroma ID, assume we're a level up
            while (!IsParentByChromaID(lastParentChromaID, envObject.ChromaID))
            {
                if (lastParent == null || lastParent.parent == null) break; // No more parents to check
                lastParent = lastParent.parent;
                lastParentChromaID = GetParentChromaID(lastParentChromaID);
            }

            // Instantiate the environment object from the library
            GameObject prefab;
            if (string.IsNullOrEmpty(envObject.MeshName))
                prefab = new GameObject();
            else
            {
                if (envBuild.prefabLookup.TryGetValue(envObject.MeshName, out var pf) && pf != null)
                    prefab = PrefabUtility.InstantiatePrefab(pf) as GameObject;
                else
                {
                    Debug.LogError($"{envObject.GameObjectName} does not mesh prefab for {envObject.MeshName}");
                    prefab = PrefabUtility.InstantiatePrefab(library.fallbackPrefab) as GameObject;
                }
            }

            prefab.name = envObject.GameObjectName;

            // Set the parent of the instantiated object
            if (lastParent != null) prefab.transform.SetParent(lastParent.transform, false);

            // Copy properties from EnvironmentInfo to the instantiated object
            var components = envObject.Components;
            components.Transform?.CopyTo(prefab.transform);

            // TODO: Add other components (like lights, sprites, and event managers) as needed
            // TODO: Considering the Chroma ID is already given here, we should also store it for future Environemnt Enhancement support

            // Set the last parent to the current object
            lastParent = prefab.transform;
            lastParentChromaID = envObject.ChromaID;
        }
    }

    private static bool IsParentByChromaID(string lastParentChromaID, string chromaID)
    {
        if (string.IsNullOrEmpty(lastParentChromaID) || string.IsNullOrEmpty(chromaID))
            return false; // No parent or invalid Chroma ID

        return GetParentChromaID(chromaID) == lastParentChromaID;
    }

    private static string GetParentChromaID(string chromaID)
    {
        if (string.IsNullOrEmpty(chromaID)) return string.Empty; // No Chroma ID to process

        var splitByPeriods = chromaID.Split('.');
        if (splitByPeriods.Length < 2) return string.Empty; // Not enough segments to determine parent

        // Reconstruct the parent Chroma ID by removing the last segment
        return string.Join(".", splitByPeriods, 0, splitByPeriods.Length - 1);
    }
}
