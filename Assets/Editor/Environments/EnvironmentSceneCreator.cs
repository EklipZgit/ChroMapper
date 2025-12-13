using System;
using System.IO;
using System.Linq;
using Beatmap.Enums;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
        var textAsset = Selection.activeObject switch
        {
            TextAsset tempTextAsset => tempTextAsset,
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

        var assetName = textAsset.name;

        var targetPath = Path.Combine(environmentPath, $"{assetName}.unity");
        var exist = AssetDatabase.AssetPathExists(targetPath);

        var scene = exist
            ? EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        // Remove everything from scene
        foreach (var go in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(go);
        }

        // Save the scene with the new name (in memory, not on disk yet)
        if (!exist) scene.name = assetName;

        // Oh dear I'm loading stuff at runtime
        var environmentLibrary =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(Path.Combine(assetPath, "EnvironmentLibrarySO.asset"));
        var environmentData =
            JsonConvert.DeserializeObject<EnvironmentData>(textAsset.text, new Vector3ArrayConverter());

        // Move null checks up here so it doesnt ruin the rest of the process
        if (environmentLibrary == null) throw new ArgumentNullException(nameof(environmentLibrary));
        if (environmentData == null) throw new ArgumentNullException(nameof(environmentData));

        // Set the skybox material if specified in the library
        if (environmentLibrary.SkyboxMaterial != null) RenderSettings.skybox = environmentLibrary.SkyboxMaterial;

        // Create the environment in the new scene
        CreateEnvironment(environmentData, environmentLibrary);

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
    private static void CreateEnvironment(
        EnvironmentData data,
        EnvironmentLibrarySO library)
    {
        Transform lastParent = null;
        string lastParentChromaID = null;

        // first pass: spawn object
        foreach (var envObject in data.Objects)
        {
            // Skip ignored objects (Exclude Environment node of ID)
            if (library.IsIgnored(
                envObject.ChromaID.Substring(envObject.ChromaID.IndexOf("]", StringComparison.Ordinal) + 1)))
                continue;

            // If our parents name is not present in the Chroma ID, assume we're a level up
            while (!IsParentByChromaID(lastParentChromaID, envObject.ChromaID))
            {
                if (lastParent == null || lastParent.parent == null) break; // No more parents to check
                lastParent = lastParent.parent;
                lastParentChromaID = GetParentChromaID(lastParentChromaID);
            }

            // Instantiate the environment object from the library
            GameObject prefab;
            if (envObject.Components.MeshFilter == null || string.IsNullOrEmpty(envObject.Components.MeshFilter.Hash))
                prefab = new GameObject();
            else
            {
                if (library.Meshes.Lookup.TryGetValue(envObject.Components.MeshFilter.Hash, out var mesh)
                    && mesh != null)
                {
                    prefab = new GameObject();
                    var mf = prefab.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    var renderer = prefab.AddComponent<MeshRenderer>();
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
                    prefab = new GameObject();
                    var fallback =
                        PrefabUtility.InstantiatePrefab(library.fallbackPrefab, prefab.transform) as GameObject;
                    var mInfo = library.Meshes.list.First(x => x.Hash == envObject.Components.MeshFilter.Hash);
                    fallback.transform.localPosition = mInfo.BoundsCenter;
                    fallback.transform.localScale = mInfo.BoundsSize;
                }
            }

            prefab.name = envObject.GameObjectName;
            prefab.layer = library.layerMaskLookup[envObject.Layer].value.Get1BitPositions()[0];
            prefab.AddComponent<ChromaIDMarker>().ID = envObject.ChromaID;

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

        // second pass: build component
        var chromaIdMarkers = Object.FindObjectsByType<ChromaIDMarker>(FindObjectsSortMode.None);
        var descriptor = GameObject.Find("Environment").AddComponent<PlatformDescriptor>();

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectController>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.basicEventEffectController = beec;

        beec.TryInit<ColorBoostManager>((int)EventTypeValue.ColorBoost);

        foreach (var envObject in data.Objects)
        {
            if (library.IsIgnored(
                envObject.ChromaID.Substring(envObject.ChromaID.IndexOf("]", StringComparison.Ordinal) + 1)))
                continue;

            var marker = chromaIdMarkers.First(x => x.ID == envObject.ChromaID);
            var go = marker.gameObject;

            if (envObject.Components.TubeBloomPrePassLightWithId != null)
            {
                foreach (var tubeBloomPrePass in envObject.Components.TubeBloomPrePassLightWithId)
                {
                    if (tubeBloomPrePass.TubeBloomPrePassLight == null
                        || tubeBloomPrePass.ChromaLight == null
                        || string.IsNullOrEmpty(tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId)
                        || tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId == "null")
                        continue;

                    var blc = go.AddComponent<BasicLightController>();
                    var boxLight = chromaIdMarkers.First(x =>
                            x.ID == tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId)
                        .gameObject;
                    blc.MainLight = boxLight.AddComponent<LightObject>();
                    blc.MainLight.Renderer = boxLight.GetComponent<Renderer>();
                    blc.MainLight.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.ColorAlphaMultiplier;

                    // var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    // quad.transform.SetParent(go.transform, false);
                    // quad.transform.localScale = new(
                    //    tubeBloomPrePass.TubeBloomPrePassLight.TubeWidth
                    //    * 10f
                    //    * tubeBloomPrePass.TubeBloomPrePassLight.LightWidthMultiplier,
                    //    tubeBloomPrePass.TubeBloomPrePassLight.TubeLength,
                    //    0f);
                    // quad.transform.localPosition = new(0f, 0f, 0f);
                    // var lobf = quad.AddComponent<LightObjectBloomFog>();
                    // lobf.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.BloomFogIntensityMultiplier;
                    // quad.layer = LayerMask.NameToLayer("Lighting Events");
                    // quad.GetComponent<Renderer>().sharedMaterial = library.BloomFogMaterial;

                    blc.ID = tubeBloomPrePass.ChromaLight.LightId;

                    beec.TryInit<BasicLightManager>(tubeBloomPrePass.ChromaLight.Type);
                    beec.Add(tubeBloomPrePass.ChromaLight.Type, blc);
                }
            }
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
