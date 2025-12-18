using System;
using System.Collections;
using System.Collections.Generic;
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
        var objectsToUse = data
            .Objects.Where(obj =>
                !library.IsIgnored(obj.ChromaID[(obj.ChromaID.IndexOf("]", StringComparison.Ordinal) + 1)..]))
            .ToList();

        var chromaIdObjects = new Dictionary<string, GameObject>();

        // first pass: spawn object
        var queue = new Queue<EnvironmentObject>(objectsToUse);
        var limit = queue.Count;
        var i = 0;
        while (queue.Count > 0)
        {
            var envObject = queue.Dequeue();
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

            GameObject go;
            if (envObject.Components.MeshFilter == null || string.IsNullOrEmpty(envObject.Components.MeshFilter.Hash))
                go = new GameObject();
            else
            {
                if (library.Meshes.Lookup.TryGetValue(envObject.Components.MeshFilter.Hash, out var mesh)
                    && mesh != null)
                {
                    go = new GameObject();
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    var renderer = go.AddComponent<MeshRenderer>();
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
                    go = new GameObject();
                    var fallback =
                        PrefabUtility.InstantiatePrefab(library.fallbackPrefab, go.transform) as GameObject;
                    var mInfo = library.Meshes.list.First(x => x.Hash == envObject.Components.MeshFilter.Hash);
                    fallback.transform.localPosition = mInfo.BoundsCenter;
                    fallback.transform.localScale = mInfo.BoundsSize;
                }
            }

            go.name = envObject.GameObjectName;
            go.layer = library.layerMaskLookup[envObject.Layer].value.Get1BitPositions()[0];
            chromaIdObjects[envObject.ChromaID] = go;

            if (parentName != name)
            {
                go.transform.SetParent(
                    chromaIdObjects[actualParentGoName].transform,
                    false);
            }

            envObject.Components.Transform?.CopyTo(go.transform);
        }

        // second pass: build component
        var descriptor = GameObject.Find("Environment").AddComponent<PlatformDescriptor>();

        data.Data.ColorScheme.CopyTo(descriptor.ColorScheme);
        data.Data.LightTracks.CopyTo(descriptor.TrackDefinition);
        data.Data.FogParameters.CopyTo(descriptor.BloomFogParams);

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.BasicEventEffectManager = beec;

        beec.Register<ColorBoostManager>((int)EventTypeValue.ColorBoost);

        foreach (var envObject in objectsToUse)
        {
            var marker = chromaIdObjects[envObject.ChromaID];
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

                    var blc = go.AddComponent<LightController>();

                    var boxLight = chromaIdObjects[tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId];
                    blc.LightObject = boxLight.AddComponent<LightObject>();
                    blc.LightObject.Renderer = boxLight.GetComponent<Renderer>();
                    blc.LightObject.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.ColorAlphaMultiplier;

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

                    beec.Register<BasicLightManager>(tubeBloomPrePass.ChromaLight.Type);
                    beec.Register(tubeBloomPrePass.ChromaLight.Type, tubeBloomPrePass.ChromaLight.LightId, blc);
                }
            }
        }
    }
}
