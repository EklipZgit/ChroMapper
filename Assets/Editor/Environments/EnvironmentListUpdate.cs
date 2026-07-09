using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EnvironmentListUpdate
{
    [MenuItem("Environment/Update Environment List", false, 800)]
    private static void PopulateBuildData()
    {
        var listingPath = $"{Constants.ScriptsPath}/EnvironmentListSO.asset";
        var environmentListing = AssetDatabase.LoadAssetAtPath<EnvironmentListSO>(listingPath);
        if (environmentListing == null)
        {
            Debug.LogError($"[EnvironmentTools] EnvironmentListSO not found at '{listingPath}'.");
            return;
        }

        var assetToReserialize = new List<Object> { environmentListing };

        foreach (var data in CreateUtils.GetEnvironmentData())
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                $"{Constants.ScenesPath}/{data.Data.ID}.unity");

            if (scene == null)
            {
                Debug.LogWarning($"[EnvironmentTools] Scene not found at '{Constants.ScenesPath}/{data.Data.ID}.unity'. Run 'Create All from Data' first.");
                continue;
            }

            var colorScheme = $"{Constants.ScriptsPath}/ColorSchemes/{data.Data.ID}ColorScheme.asset"
                .GetOrCreateScriptableObject<ColorSchemeSO>();
            assetToReserialize.Add(colorScheme);

            var trackDefinitions = $"{Constants.ScriptsPath}/TrackDefinitions/{data.Data.ID}TrackDefinitions.asset"
                .GetOrCreateScriptableObject<TrackDefinitionsSO>();
            assetToReserialize.Add(trackDefinitions);

            data.Data.ColorScheme.CopyTo(colorScheme);
            if (data.Data.LightTracks != null)
                data.Data.LightTracks.CopyTo(trackDefinitions);
            else
            {
                trackDefinitions.UnregisterAll();
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
                    .ForEach(trackDefinitions.Register);
            }

            if (environmentListing.List.Exists(x => x.ID == data.Data.ID))
            {
                var d = environmentListing.List.First(x => x.ID == data.Data.ID);
                d.Name = data.Data.Title;
                d.ColorScheme = colorScheme;
                d.TrackDefinitions = trackDefinitions;
            }
            else
            {
                environmentListing.List.Add(
                    new EnvironmentListInfo
                    {
                        Name = data.Data.Title,
                        ID = data.Data.ID,
                        ColorScheme = colorScheme,
                        TrackDefinitions = trackDefinitions
                    });
            }
        }

        environmentListing.Sort();

        foreach (var o in assetToReserialize) EditorUtility.SetDirty(o);
        AssetDatabase.SaveAssets();
    }
}
