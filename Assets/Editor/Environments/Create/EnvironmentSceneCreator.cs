using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to create a new Unity scene from an EnvironmentInfo JSON file.
/// </summary>
public partial class EnvironmentSceneCreator
{
    [MenuItem("Environment/Create from Data", false, 1000)]
    private static void CreateEnvironmentFromDataWithScript() => ReadSelectedAndCreateEnvironment(true);

    [MenuItem("Environment/Create from Data (No Script)", false, 1000)]
    private static void CreateEnvironmentFromDataWithoutScript() => ReadSelectedAndCreateEnvironment(false);

    [MenuItem("Environment/Create All from Data", false, 1000)]
    private static void CreateAllEnvironmentFromData()
    {
        foreach (var ta in CreateUtils.GetEnvironmentDataRaw()) CreateEnvironmentFromData(ta, true);
    }

    private static void ReadSelectedAndCreateEnvironment(bool allowScript)
    {
        var textAsset = Selection.activeObject switch
        {
            TextAsset selectedText => selectedText,
            SceneAsset selectedScene => AssetDatabase.LoadAssetAtPath<TextAsset>(
                Path.Combine(
                    Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedScene))!,
                    "Data",
                    selectedScene.name + ".json")),
            _ => null
        };

        if (textAsset == null)
        {
            var scenePath = SceneManager.GetActiveScene().path;
            var dir = Path.GetDirectoryName(scenePath);
            var name = Path.GetFileNameWithoutExtension(scenePath);

            var textAssetPath = Path.Combine(dir, "Data", name + ".json");
            textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textAssetPath);
        }

        if (textAsset == null) return;
        CreateEnvironmentFromData(textAsset, allowScript);
    }

    private static void CreateEnvironmentFromData(TextAsset textAsset, bool allowScript)
    {
        var assetName = textAsset.name;

        var targetPath = Path.Combine(Constants.ScenesPath, $"{assetName}.unity");
        var exist = AssetDatabase.AssetPathExists(targetPath);

        var scene = exist
            ? EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        // Save the scene with the new name (in memory, not on disk yet)
        if (!exist) scene.name = assetName;

        // Oh dear I'm loading stuff at runtime
        var environmentLibrary =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(
                Path.Combine(Constants.EditorPath, "EnvironmentLibrarySO.asset"));
        var environmentData = CreateUtils.JsonToEnvironmentData(textAsset);

        // Move null checks up here so it doesnt ruin the rest of the process
        if (environmentLibrary == null) throw new ArgumentNullException(nameof(environmentLibrary));
        if (environmentData == null) throw new ArgumentNullException(nameof(environmentData));

        // Set the skybox material if specified in the library
        if (environmentLibrary.SkyboxMaterial != null) RenderSettings.skybox = environmentLibrary.SkyboxMaterial;

        // Create the environment in the new scene
        CreateEnvironment(scene, environmentData, environmentLibrary, allowScript);

        // Save the scene to disk
        if ((exist && EditorSceneManager.SaveScene(scene)) || EditorSceneManager.SaveScene(scene, targetPath))
        {
            // Select the newly created scene in the Project window
            // EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath));
            // Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath);
        }
        else
            Debug.LogError("Failed to save the new environment scene.");
    }

    // Main method which constructs the environment from parsed data
    public static void CreateEnvironment(
        Scene scene,
        EnvironmentData data,
        EnvironmentLibrarySO library,
        bool allowScript)
    {
        var container = new CreateContainer { Library = library };

        // first pass: strip existing object and component
        var existingObjects = StripObjects(scene, data);

        // second pass: spawn object
        container.ChromaIdObjects = SpawnObjects(data, container, existingObjects);

        // third pass: build component
        if (allowScript) BuildComponents(data, container);

        // forth pass: cleanup and remove unused
        if (allowScript) Cleanup(scene, data);
    }
}
