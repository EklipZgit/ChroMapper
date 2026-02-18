using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
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
            if (data.Data.LightTracks != null)
                data.Data.LightTracks.CopyTo(tracksDefinition);
            else
            {
                tracksDefinition.UnregisterAll();
                new TrackDefinitionBasic[]
                    {
                        new() { Kind = BasicEventKind.Lights, Type = 0, Name = "Back Light" },
                        new() { Kind = BasicEventKind.Lights, Type = 1, Name = "Ring" },
                        new() { Kind = BasicEventKind.Lights, Type = 2, Name = "Left Lasers" },
                        new() { Kind = BasicEventKind.Lights, Type = 3, Name = "Right Lasers" },
                        new() { Kind = BasicEventKind.Lights, Type = 4, Name = "Center Light" },
                        new() { Kind = BasicEventKind.Toggle, Type = 5, Name = "Boost" },
                        new() { Kind = BasicEventKind.IntValue, Type = 12, Name = "Left Speed" },
                        new() { Kind = BasicEventKind.IntValue, Type = 13, Name = "Right Speed" }
                    }
                    .ToList()
                    .ForEach(tracksDefinition.Register);
            }

            if (listSo.List.Exists(x => x.ID == data.Data.ID))
            {
                var d = listSo.List.First(x => x.ID == data.Data.ID);
                d.Name = data.Data.Title;
                d.ColorScheme = colorScheme;
                d.TracksDefinition = tracksDefinition;
            }
            else
            {
                listSo.List.Add(
                    new EnvironmentListInfo
                    {
                        Name = data.Data.Title,
                        ID = data.Data.ID,
                        ColorScheme = colorScheme,
                        TracksDefinition = tracksDefinition
                    });
            }

            if (!AssetDatabase.AssetPathExists(colorSchemePath))
                AssetDatabase.CreateAsset(colorScheme, colorSchemePath);
            if (!AssetDatabase.AssetPathExists(tracksDefinitionPath))
                AssetDatabase.CreateAsset(tracksDefinition, tracksDefinitionPath);
        }

        listSo.List = listSo.List.OrderBy(x => x.ID).ToList();

        foreach (var o in assetToReserialize) EditorUtility.SetDirty(o);
        AssetDatabase.SaveAssets();
    }
}
