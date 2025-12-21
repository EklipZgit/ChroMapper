using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EnvironmentListUpdate
{
    private const string environmentPath = "Assets/__Scenes/Environments";
    private const string scriptPath = "Assets/__Scripts/Environments";

    [MenuItem("Environment/Update Environment List", false, 800)]
    private static void PopulateBuildData()
    {
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(Path.Combine(environmentPath, "Data")) && x.EndsWith(".json"));

        var listSo =
            AssetDatabase.LoadAssetAtPath<EnvironmentListSO>(Path.Combine(scriptPath, "EnvironmentListSO.asset"));
        var assetToReserialize = new List<Object> { listSo };

        listSo.list.Clear();

        foreach (var dataPath in envDataPaths)
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            var data = JsonConvert.DeserializeObject<EnvData>(
                dataAsset.text,
                new Vector3ArrayConverter());

            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                Path.Combine(environmentPath, data.Data.ID + ".unity"));

            if (scene == null) continue;

            var colorSchemePath = Path.Combine(scriptPath, "ColorSchemes", data.Data.ID + "ColorScheme.asset");
            var colorScheme = AssetDatabase.AssetPathExists(colorSchemePath)
                ? AssetDatabase.LoadAssetAtPath<ColorSchemeSO>(colorSchemePath)
                : ScriptableObject.CreateInstance<ColorSchemeSO>();

            var tracksDefinitionPath = Path.Combine(
                scriptPath,
                "TracksDefinitions",
                data.Data.ID + "TracksDefinition.asset");
            var tracksDefinition = AssetDatabase.AssetPathExists(tracksDefinitionPath)
                ? AssetDatabase.LoadAssetAtPath<TracksDefinitionSO>(tracksDefinitionPath)
                : ScriptableObject.CreateInstance<TracksDefinitionSO>();

            assetToReserialize.Add(colorScheme);
            assetToReserialize.Add(tracksDefinition);

            data.Data.ColorScheme.CopyTo(colorScheme);
            data.Data.LightTracks.CopyTo(tracksDefinition);

            listSo.list.Add(
                new EnvironmentListInfo
                {
                    Name = data.Data.Title,
                    ID = data.Data.ID,
                    ColorScheme = colorScheme,
                    TracksDefinition = tracksDefinition
                });

            if (!AssetDatabase.AssetPathExists(colorSchemePath))
                AssetDatabase.CreateAsset(colorScheme, colorSchemePath);
            if (!AssetDatabase.AssetPathExists(tracksDefinitionPath))
                AssetDatabase.CreateAsset(tracksDefinition, tracksDefinitionPath);
        }

        listSo.list = listSo.list.OrderBy(x => x.ID).ToList();
        
        AssetDatabase.ForceReserializeAssets(
            assetToReserialize.Select(AssetDatabase.GetAssetPath).ToArray(),
            ForceReserializeAssetsOptions.ReserializeAssets);
    }
}
