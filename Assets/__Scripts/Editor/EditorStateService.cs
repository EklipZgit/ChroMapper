using System;
using System.Collections.Generic;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;

// Components own their schemas, startup restoration, and current-value capture.
public interface IEditorStateProvider
{
    string StateKey { get; }
    void CaptureEditorState(JSONObject data);
    void LoadEditorState(JSONNode data);
}

// Keep only the shared metadata cache and save-time provider dispatch outside UI owners.
public static class EditorStateService
{
    // Reserve one ChroMapper-owned metadata property so unrelated editor metadata remains untouched.
    private const string EditorStateKey = "editorState";
    // Isolate component schemas from one another.
    private const string ComponentStatesKey = "components";

    private static JSONObject loadedData;
    private static readonly List<IEditorStateProvider> stateProviders = new();

    // Register an owner and return only its cached node for Start-time restoration.
    public static void Register(IEditorStateProvider provider)
    {
        if (stateProviders.Contains(provider))
        {
            return;
        }

        stateProviders.Add(provider);

        // Hydrate views that start after LoadMapData so they do not display prefab defaults over restored placement state.
        if (loadedData == null || string.IsNullOrEmpty(provider.StateKey))
        {
            return;
        }

        var componentStates = loadedData[ComponentStatesKey].AsObject;
        if (componentStates != null && componentStates.HasKey(provider.StateKey))
        {
            provider.LoadEditorState(componentStates[provider.StateKey]);
        }
    }


    // Remove destroyed providers so a later map save cannot retain a stale Unity component reference.
    public static void Unregister(IEditorStateProvider provider)
    {
        if (provider != null)
        {
            stateProviders.Remove(provider);
        }
    }


    // Snapshot registered component state on the main thread before the existing Info.dat save flow.
    public static void CaptureMapData(BaseInfo info)
    {
        try
        {
            var componentStates = new JSONObject();
            foreach (var provider in stateProviders)
            {
                if (provider == null || string.IsNullOrEmpty(provider.StateKey))
                {
                    continue;
                }

                var componentState = new JSONObject();
                provider.CaptureEditorState(componentState);
                componentStates[provider.StateKey] = componentState;
            }

            info.CustomEditorsData.SetEditorData(EditorStateKey, new JSONObject
            {
                [ComponentStatesKey] = componentStates,
            });
        }
        catch (Exception exception)
        {
            Debug.LogError($"[EditorState] Failed to capture editor metadata for Info.dat: {exception}");
        }
    }

    // Cache metadata while map loading so every owner can pull its own node from Start.
    public static void LoadMapData(BaseInfo info)
    {
        var mapData = info.CustomEditorsData.GetEditorData(EditorStateKey);
        loadedData = mapData != null && mapData.IsObject ? mapData.AsObject : null;
        var componentStates = loadedData != null ? loadedData[ComponentStatesKey].AsObject : null;
        foreach (var provider in stateProviders)
        {
            if (provider == null || componentStates == null || string.IsNullOrEmpty(provider.StateKey))
            {
                continue;
            }

            if (componentStates.HasKey(provider.StateKey))
            {
                // Each registered owner applies only its own node after map metadata becomes available.
                provider.LoadEditorState(componentStates[provider.StateKey]);
            }
        }
    }

    // Clear the previous map's cache before the next scene's Start callbacks can register against stale nodes.
    public static void BeginMapLoad() => loadedData = null;
}
