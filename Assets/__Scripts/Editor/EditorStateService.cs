using System;
using System.Collections.Generic;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;

// Keep only the shared metadata cache and save-time provider dispatch outside UI owners.
public static class EditorStateService
{
    // Reserve one ChroMapper-owned metadata property so unrelated editor metadata remains untouched.
    private const string EditorStateKey = "editorState";
    // Isolate component schemas from one another.
    private const string ComponentStatesKey = "components";

    private static JSONObject loadedData;
    private static readonly List<IEditorStateProvider> stateProviders = new();

    // Late-starting UI views use this to pull their own cached node after a tab becomes active.
    public static event Action OnMapDataLoaded;

    // Components own their schemas, startup restoration, and current-value capture.
    public interface IEditorStateProvider
    {
        string StateKey { get; }
        void CaptureEditorState(JSONObject data);
        void LoadEditorState(JSONNode data);
    }

    // Register an owner and return only its cached node for Start-time restoration.
    public static JSONNode Register(IEditorStateProvider provider)
    {
        if (provider == null || stateProviders.Contains(provider))
        {
            return null;
        }

        stateProviders.Add(provider);
        if (loadedData == null || string.IsNullOrEmpty(provider.StateKey))
        {
            return null;
        }

        var componentStates = loadedData[ComponentStatesKey].AsObject;
        return componentStates != null && componentStates.HasKey(provider.StateKey)
            ? componentStates[provider.StateKey]
            : null;
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
        // Notify late-starting UI owners to pull their own nodes without a global UI restore pass.
        OnMapDataLoaded?.Invoke();
    }

    // Return one cached component node so a UI owner can restore itself during Start.
    public static JSONNode GetState(string stateKey)
    {
        var componentStates = loadedData != null ? loadedData[ComponentStatesKey].AsObject : null;
        return componentStates != null && componentStates.HasKey(stateKey)
            ? componentStates[stateKey]
            : null;
    }
}
