using System;
using System.IO;
using Assets.HSVPicker;
using SimpleJSON;
using UnityEngine;

// Centralize map-scoped editor-state persistence outside individual UI and placement controllers.
public static class CmEditorStateData
{
    // Keep the map-local file name owned by the sole CmData serialization service.
    public const string MapDataFileName = "CmData.json";

    private static JSONObject pendingData;
    private static Color? loadedChromaColor;

    // Serialize every map-scoped editor setting into the one CmData document.
    public static string CaptureMapData()
    {
        try
        {
            var mapData = new JSONObject();
            var presets = new JSONObject();
            foreach (var preset in ColorPresetManager.Presets)
            {
                var colors = new JSONArray();
                foreach (var color in preset.Value.Colors)
                {
                    var colorNode = new JSONObject();
                    colorNode.WriteColor(color);
                    colors.Add(colorNode);
                }

                presets.Add(preset.Key, colors);
            }

            mapData.Add("presets", presets);
            if (ColourPicker.ActivePicker != null)
            {
                var chromaColor = new JSONObject();
                chromaColor.WriteColor(ColourPicker.ActivePicker.CurrentColor);
                mapData.Add("chromaColor", chromaColor);
            }

            var strobeColor = new JSONObject();
            strobeColor.WriteColor(StrobeColorPickerController.LoadedColor);
            mapData.Add("strobeColor", strobeColor);
            mapData.Add("strobeEnabled", Settings.Instance.PlaceGLSStrobeColor);
            mapData.Add("editorState", Capture());
            return mapData.ToString();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CmData] Failed to capture map state for saving: {exception}");
            return null;
        }
    }

    // Write the single CmData document after the destination directory exists, including autosaves.
    public static void SaveMapData(string directory, string mapData)
    {
        try
        {
            if (string.IsNullOrEmpty(mapData))
            {
                Debug.LogWarning("[CmData] Skipped writing map state because capture failed.");
                return;
            }

            var path = PathUtils.Combine(directory, MapDataFileName);
            File.WriteAllText(path, mapData);
            Debug.Log($"[CmData] Saved map state to '{path}'.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CmData] Failed to save map state in '{directory}': {exception}");
        }
    }

    // Read the single CmData document and apply its in-memory settings without interrupting map loading.
    public static void LoadMapData(string directory)
    {
        try
        {
            ClearPendingState();
            var path = PathUtils.Combine(directory, MapDataFileName);
            if (!File.Exists(path))
            {
                Debug.Log($"[CmData] No map state file found at '{path}'; using current editor state.");
                return;
            }

            var mapData = JSON.Parse(File.ReadAllText(path));
            RestoreColorPresets(mapData["presets"].AsObject);
            RestoreChromaColor(mapData["chromaColor"]);
            if (mapData["strobeColor"].IsObject)
            {
                StrobeColorPickerController.SetLoadedColor(mapData["strobeColor"].ReadColor(Color.black));
            }

            if (mapData.HasKey("strobeEnabled"))
            {
                StrobeColorPickerController.SetLoadedEnabled(mapData["strobeEnabled"].AsBool);
            }

            Restore(mapData.HasKey("editorState") ? mapData["editorState"].AsObject : mapData["glsPlacement"].AsObject);
            Debug.Log($"[CmData] Loaded map state from '{path}'.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CmData] Failed to load map state from '{directory}\\{MapDataFileName}': {exception}");
        }
    }

    // Apply a picker color deferred during map loading after the picker has initialized its controls.
    public static void ApplyLoadedChromaColor(ColorPicker picker)
    {
        try
        {
            if (loadedChromaColor is not Color color)
            {
                return;
            }

            picker.CurrentColor = color;
            loadedChromaColor = null;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CmData] Failed to apply the deferred Chroma color: {exception}");
        }
    }

    // Restore palette data into existing lists so open Chroma controls keep their event subscriptions.
    private static void RestoreColorPresets(JSONObject presets)
    {
        if (presets == null)
        {
            return;
        }

        foreach (var preset in presets)
        {
            var colors = new System.Collections.Generic.List<Color>();
            foreach (JSONNode colorNode in preset.Value.AsArray)
            {
                colors.Add(colorNode.ReadColor(Color.black));
            }

            ColorPresetManager.Get(preset.Key).UpdateList(colors);
        }
    }

    // Defer applying the active Chroma color until its picker has completed Unity initialization.
    private static void RestoreChromaColor(JSONNode chromaColor)
    {
        if (!chromaColor.IsObject)
        {
            return;
        }

        loadedChromaColor = chromaColor.ReadColor(Color.black);
        if (ColourPicker.ActivePicker != null)
        {
            ApplyLoadedChromaColor(ColourPicker.ActivePicker);
        }
    }

    public static JSONObject Capture()
    {
        var data = new JSONObject();
        AddColor(data, "colorEvent", FindObject<GLSEventColorPlacement>()?.QueuedData);
        AddColor(data, "colorGroup", FindObject<GLSGroupColorPlacement>()?.QueuedData.Boxes[0].Events[0]);
        AddRotation(data, "rotationEvent", FindObject<GLSEventRotationPlacement>()?.QueuedData);
        AddRotation(data, "rotationGroup", FindObject<GLSGroupRotationPlacement>()?.QueuedData.Boxes[0].Events[0]);
        AddTranslation(data, "translationEvent", FindObject<GLSEventTranslationPlacement>()?.QueuedData);
        AddTranslation(data, "translationGroup", FindObject<GLSGroupTranslationPlacement>()?.QueuedData.Boxes[0].Events[0]);
        AddFloatFx(data, "floatFxEvent", FindObject<GLSEventFloatFXPlacement>()?.QueuedData);
        AddFloatFx(data, "floatFxGroup", FindObject<GLSGroupFloatFXPlacement>()?.QueuedData.Boxes[0].Events[0]);

        var colorType = FindObject<ColorTypeController>();
        if (colorType != null)
        {
            data.Add("colorType", colorType.SelectedColorType);
        }

        var easingInput = FindObject<BeatmapEasingsSelectionInputController>();
        if (easingInput != null)
        {
            data.Add("menuEasing", easingInput.CurrentEasing);
            data.Add("menuExtension", easingInput.CurrentExtension);
        }

        // Capture basic-event placement settings alongside GLS settings in the shared map-scoped editor state.
        CaptureBasicEventState(data);

        // Capture workspace controllers so reopening a map resumes the same editor view without selecting a map node.
        CaptureWorkspaceState(data);

        // Log the captured source values so CmData save issues can be separated from later UI restoration.
        LogCapturedState(data);

        return data;
    }

    public static void Restore(JSONObject data)
    {
        pendingData = data;
        ApplyPendingState();
    }

    public static void ApplyPendingState()
    {
        // Deferred CmData application runs after loading, so isolate any malformed state here too.
        try
        {
            ApplyPendingStateUnsafe();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CmData] Failed to apply deferred GLS placement state: {exception}");
        }
    }

    private static void ApplyPendingStateUnsafe()
    {
        var data = pendingData;
        if (data == null)
        {
            return;
        }

        RestoreColor(GetObject(data, "colorEvent"), FindObject<GLSEventColorPlacement>()?.QueuedData);
        RestoreColor(GetObject(data, "colorGroup"), FindObject<GLSGroupColorPlacement>()?.QueuedData.Boxes[0].Events[0]);
        RestoreRotation(GetObject(data, "rotationEvent"), FindObject<GLSEventRotationPlacement>()?.QueuedData);
        RestoreRotation(GetObject(data, "rotationGroup"), FindObject<GLSGroupRotationPlacement>()?.QueuedData.Boxes[0].Events[0]);
        RestoreTranslation(GetObject(data, "translationEvent"), FindObject<GLSEventTranslationPlacement>()?.QueuedData);
        RestoreTranslation(GetObject(data, "translationGroup"), FindObject<GLSGroupTranslationPlacement>()?.QueuedData.Boxes[0].Events[0]);
        RestoreFloatFx(GetObject(data, "floatFxEvent"), FindObject<GLSEventFloatFXPlacement>()?.QueuedData);
        RestoreFloatFx(GetObject(data, "floatFxGroup"), FindObject<GLSGroupFloatFXPlacement>()?.QueuedData.Boxes[0].Events[0]);

        var colorType = FindObject<ColorTypeController>();
        if (colorType != null && data.HasKey("colorType"))
        {
            colorType.UpdateValue(data["colorType"].AsInt);
        }

        // Replay the saved values through the input controllers so the visible placement menu matches its queued data.
        RefreshVisibleMenu(data);
        // Restore workspace state last so tab changes do not overwrite menu state during their callbacks.
        RestoreWorkspaceState(data);
        // Restore basic-event placement state after tab selection has initialized its controls.
        RestoreBasicEventState(data);
        // Restore the strobe checkbox after all menu initialization has completed.
        StrobeColorPickerController.RefreshLoadedEnabledUi();
    }

    public static void ClearPendingState() => pendingData = null;

    private static T FindObject<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

    private static JSONObject GetObject(JSONObject data, string key) => data.HasKey(key) ? data[key].AsObject : null;

    private static void LogCapturedState(JSONObject data)
    {
        var color = GetObject(data, "colorEvent");
        var rotation = GetObject(data, "rotationEvent");
        var translation = GetObject(data, "translationEvent");
        var floatFx = GetObject(data, "floatFxEvent");
        Debug.Log(
            $"[CmData] Captured GLS state: brightness={color?["brightness"].AsFloat}, " +
            $"strobeBrightness={color?["strobeBrightness"].AsFloat}, frequency={color?["frequency"].AsInt}, " +
            $"rotation={rotation?["rotation"].AsFloat}, loop={rotation?["loop"].AsInt}, " +
            $"direction={rotation?["direction"].AsInt}, translation={translation?["translation"].AsFloat}, " +
            $"floatFx={floatFx?["value"].AsFloat}.");
    }

    private static void CaptureBasicEventState(JSONObject data)
    {
        var placement = FindObject<EventPlacement>();
        if (placement == null)
        {
            return;
        }

        var basicEvent = new JSONObject
        {
            ["value"] = placement.QueuedValue,
            ["floatValue"] = placement.QueuedFloatValue,
            ["laserSpeed"] = placement.LaserSpeedText,
        };
        var lightingMode = FindObject<LightingModeController>();
        if (lightingMode != null)
        {
            basicEvent["lightingMode"] = (int)lightingMode.CurrentMode;
        }

        data["basicEventPlacement"] = basicEvent;
        Debug.Log(
            $"[CmData] Captured basic event state: value={placement.QueuedValue}, floatValue={placement.QueuedFloatValue}, " +
            $"laserSpeed='{placement.LaserSpeedText}', mode={basicEvent["lightingMode"].AsInt}.");
    }

    private static void RestoreBasicEventState(JSONObject data)
    {
        var basicEvent = GetObject(data, "basicEventPlacement");
        if (basicEvent == null)
        {
            return;
        }

        var lightingMode = FindObject<LightingModeController>();
        if (lightingMode != null && basicEvent.HasKey("lightingMode"))
        {
            lightingMode.RestoreCmDataState((LightingModeController.LightingMode)basicEvent["lightingMode"].AsInt);
        }

        var placement = FindObject<EventPlacement>();
        var floatValue = basicEvent["floatValue"].AsFloat;
        if (placement != null)
        {
            placement.RestoreCmDataState(
                basicEvent["value"].AsInt,
                floatValue,
                basicEvent["laserSpeed"].Value);
        }

        var floatValueController = FindObject<FloatValueController>();
        floatValueController?.RestoreCmDataState(floatValue);
    }

    private static void RefreshVisibleMenu(JSONObject data)
    {
        var colorData = GetObject(data, "colorEvent");
        var colorInput = FindObject<BeatmapGLSEventColorInputController>();
        if (colorData != null && colorInput != null)
        {
            colorInput.NotifyBrightnessChanged(colorData["brightness"].AsFloat);
            colorInput.NotifyFadeChanged(colorData["easing"].AsInt >= 0 ? 0 : -1);
            colorInput.NotifyStrobeFrequencyChanged(colorData["frequency"].AsInt);
            colorInput.NotifyStrobeBrightnessChanged(colorData["strobeBrightness"].AsFloat);
            colorInput.NotifySoftStrobeChanged(colorData["strobeFade"].AsInt);
        }

        var rotationData = GetObject(data, "rotationEvent");
        var rotationInput = FindObject<BeatmapGLSEventRotationInputController>();
        if (rotationData != null && rotationInput != null)
        {
            rotationInput.NotifyValueChanged(rotationData["rotation"].AsFloat);
            rotationInput.NotifyLoopChanged(rotationData["loop"].AsInt);
            rotationInput.NotifyDirectionChanged(rotationData["direction"].AsInt);
        }

        var translationData = GetObject(data, "translationEvent");
        var translationInput = FindObject<BeatmapGLSEventTranslationInputController>();
        if (translationData != null && translationInput != null)
        {
            translationInput.NotifyValueChanged(translationData["translation"].AsFloat);
        }

        var floatFxData = GetObject(data, "floatFxEvent");
        var floatFxInput = FindObject<BeatmapGLSEventFloatFXInputController>();
        if (floatFxData != null && floatFxInput != null)
        {
            floatFxInput.NotifyValueChanged(floatFxData["value"].AsFloat);
        }

        // Input notifications reset extension nodes, so restore their saved values after the UI has refreshed.
        RestoreColor(GetObject(data, "colorEvent"), FindObject<GLSEventColorPlacement>()?.QueuedData);
        RestoreColor(GetObject(data, "colorGroup"), FindObject<GLSGroupColorPlacement>()?.QueuedData.Boxes[0].Events[0]);
        RestoreRotation(GetObject(data, "rotationEvent"), FindObject<GLSEventRotationPlacement>()?.QueuedData);
        RestoreRotation(GetObject(data, "rotationGroup"), FindObject<GLSGroupRotationPlacement>()?.QueuedData.Boxes[0].Events[0]);
        RestoreTranslation(GetObject(data, "translationEvent"), FindObject<GLSEventTranslationPlacement>()?.QueuedData);
        RestoreTranslation(GetObject(data, "translationGroup"), FindObject<GLSGroupTranslationPlacement>()?.QueuedData.Boxes[0].Events[0]);
        RestoreFloatFx(GetObject(data, "floatFxEvent"), FindObject<GLSEventFloatFXPlacement>()?.QueuedData);
        RestoreFloatFx(GetObject(data, "floatFxGroup"), FindObject<GLSGroupFloatFXPlacement>()?.QueuedData.Boxes[0].Events[0]);

        // Replay the checkbox notifications after direct data restoration so their visible states cannot drift.
        if (colorData != null && colorInput != null)
        {
            colorInput.NotifyFadeChanged(colorData["easing"].AsInt >= 0 ? 0 : -1);
            colorInput.NotifySoftStrobeChanged(colorData["strobeFade"].AsInt);
        }

        var easingInput = FindObject<BeatmapEasingsSelectionInputController>();
        var easing = data.HasKey("menuEasing")
            ? data["menuEasing"].AsInt
            : colorData != null ? colorData["easing"].AsInt : 0;
        var extension = data.HasKey("menuExtension")
            ? data["menuExtension"].AsInt
            : colorData != null ? colorData["usePrevious"].AsInt : 0;
        if (easingInput != null)
        {
            easingInput.RestoreMenuState(easing, extension);
        }

        // Assign the rendered toggle components directly; placement data alone does not redraw these checkmarks.
        var colorToggleViews = UnityEngine.Object.FindObjectsByType<GLSInputColorViewController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var view in colorToggleViews)
        {
            // RestoreMenuState above is the final writer of the queued preview's easing, so draw Fade from it too.
            view.ApplyCmDataState(
                colorData != null ? colorData["brightness"].AsFloat : 0f,
                colorData != null ? colorData["strobeBrightness"].AsFloat : 0f,
                colorData != null ? colorData["frequency"].AsInt : 0,
                easing,
                colorData != null ? colorData["strobeFade"].AsInt : 0);
        }

        var easingToggleViews = UnityEngine.Object.FindObjectsByType<InputEasingViewController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var view in easingToggleViews)
        {
            view.ApplyCmDataToggleState(easing, extension);
        }

        // Cache the numeric GLS controls too, because their own delayed Start methods otherwise repaint default values.
        var rotationViews = UnityEngine.Object.FindObjectsByType<GLSInputRotationViewController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var view in rotationViews)
        {
            view.ApplyCmDataState(
                rotationData != null ? rotationData["rotation"].AsFloat : 0f,
                rotationData != null ? rotationData["loop"].AsInt : 0,
                rotationData != null ? rotationData["direction"].AsInt : 0);
        }

        var translationViews = UnityEngine.Object.FindObjectsByType<GLSInputTranslationViewController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var view in translationViews)
        {
            view.ApplyCmDataState(translationData != null ? translationData["translation"].AsFloat : 0f);
        }

        var floatFxViews = UnityEngine.Object.FindObjectsByType<GLSInputFloatFXViewController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (var view in floatFxViews)
        {
            view.ApplyCmDataState(floatFxData != null ? floatFxData["value"].AsFloat : 0f);
        }

        var fadeEnabled = colorData != null && colorData["easing"].AsInt >= 0;
        var strobeFadeEnabled = colorData != null && colorData["strobeFade"].AsInt == 1;
        Debug.Log(
            $"[CmData] Restored views: color={colorToggleViews.Length}, easing={easingToggleViews.Length}, " +
            $"rotation={rotationViews.Length}, translation={translationViews.Length}, floatFx={floatFxViews.Length}, " +
            $"fade={fadeEnabled}, strobeFade={strobeFadeEnabled}, extension={extension == 1}.");
    }

    private static void CaptureWorkspaceState(JSONObject data)
    {
        var editMode = FindObject<EditModeContext>();
        if (editMode != null)
        {
            // EventBox requires a selected group/node, which is intentionally not restored with map-scoped state.
            data.Add("editingMode", editMode.EditingMode == EditingMode.EventBox ? (int)EditingMode.GLS : (int)editMode.EditingMode);
        }

        var glsGroups = FindObject<GLSGroupGridProvider>();
        if (glsGroups != null && !string.IsNullOrEmpty(glsGroups.CurrentGroup))
        {
            data.Add("glsGroupPage", glsGroups.CurrentGroup);
        }

        var basicEvents = FindObject<EventGridContainer>();
        if (basicEvents != null)
        {
            data.Add("basicEventType", basicEvents.EventTypeToPropagate);
            data.Add("basicEventView", (int)basicEvents.PropagationEditing);
        }

        var timeSync = FindObject<AudioTimeSyncController>();
        if (timeSync != null)
        {
            data.Add("currentJsonTime", timeSync.CurrentJsonTime);
        }

        var cameraManager = FindObject<CameraManager>();
        var editingCamera = cameraManager?.CameraControllers[0] ?? cameraManager?.SelectedCameraController;
        if (editingCamera != null)
        {
            var camera = new JSONObject();
            var position = editingCamera.transform.position;
            var rotation = editingCamera.transform.rotation;
            // SimpleJSON's JSONArray is not enumerable, so construct its persisted camera vectors explicitly.
            var persistedPosition = new JSONArray();
            persistedPosition.Add(position.x);
            persistedPosition.Add(position.y);
            persistedPosition.Add(position.z);
            var persistedRotation = new JSONArray();
            persistedRotation.Add(rotation.x);
            persistedRotation.Add(rotation.y);
            persistedRotation.Add(rotation.z);
            persistedRotation.Add(rotation.w);
            camera["position"] = persistedPosition;
            camera["rotation"] = persistedRotation;
            data.Add("editingCamera", camera);
        }
    }

    private static void RestoreWorkspaceState(JSONObject data)
    {
        var glsGroups = FindObject<GLSGroupGridProvider>();
        if (glsGroups != null && data.HasKey("glsGroupPage"))
        {
            glsGroups.SetGroupPage(data["glsGroupPage"]);
        }

        var basicEvents = FindObject<EventGridContainer>();
        if (basicEvents != null)
        {
            if (data.HasKey("basicEventType")) basicEvents.EventTypeToPropagate = data["basicEventType"].AsInt;
            if (data.HasKey("basicEventView")) basicEvents.PropagationEditing = (EventGridContainer.PropMode)data["basicEventView"].AsInt;
        }

        var timeSync = FindObject<AudioTimeSyncController>();
        if (timeSync != null && data.HasKey("currentJsonTime"))
        {
            timeSync.MoveToJsonTime(data["currentJsonTime"].AsFloat);
        }

        var camera = GetObject(data, "editingCamera");
        var cameraManager = FindObject<CameraManager>();
        var editingCamera = cameraManager?.CameraControllers[0] ?? cameraManager?.SelectedCameraController;
        if (camera != null && editingCamera != null)
        {
            var position = camera["position"].AsArray;
            var rotation = camera["rotation"].AsArray;
            if (position.Count == 3 && rotation.Count == 4)
            {
                editingCamera.transform.SetPositionAndRotation(
                    new Vector3(position[0].AsFloat, position[1].AsFloat, position[2].AsFloat),
                    new Quaternion(rotation[0].AsFloat, rotation[1].AsFloat, rotation[2].AsFloat, rotation[3].AsFloat));
            }
        }

        var editMode = FindObject<EditModeContext>();
        if (editMode != null && data.HasKey("editingMode"))
        {
            editMode.EditingMode = (EditingMode)data["editingMode"].AsInt;
        }
    }

    private static void AddColor(JSONObject data, string key, Beatmap.Base.BaseLightColorBase value)
    {
        if (value == null) return;
        data[key] = new JSONObject
        {
            ["color"] = value.Color,
            ["brightness"] = value.Brightness,
            ["frequency"] = value.Frequency,
            ["strobeBrightness"] = value.StrobeBrightness,
            ["strobeFade"] = value.StrobeFade,
            ["easing"] = value.Easing,
            ["usePrevious"] = value.UsePrevious,
        };
    }

    private static void AddRotation(JSONObject data, string key, Beatmap.Base.BaseLightRotationBase value)
    {
        if (value == null) return;
        data[key] = new JSONObject
        {
            ["rotation"] = value.Rotation,
            ["loop"] = value.Loop,
            ["direction"] = value.Direction,
            ["easing"] = value.EaseType,
            ["usePrevious"] = value.UsePrevious,
        };
    }

    private static void AddTranslation(JSONObject data, string key, Beatmap.Base.BaseLightTranslationBase value)
    {
        if (value == null) return;
        data[key] = new JSONObject
        {
            ["translation"] = value.Translation,
            ["easing"] = value.EaseType,
            ["usePrevious"] = value.UsePrevious,
        };
    }

    private static void AddFloatFx(JSONObject data, string key, Beatmap.Base.BaseFxEventFloat value)
    {
        if (value == null) return;
        data[key] = new JSONObject
        {
            ["value"] = value.Value,
            ["easing"] = value.Easing,
            ["usePrevious"] = value.UsePrevious,
        };
    }

    private static void RestoreColor(JSONObject data, Beatmap.Base.BaseLightColorBase value)
    {
        if (data == null || value == null) return;
        value.Color = data["color"].AsInt;
        value.Brightness = data["brightness"].AsFloat;
        value.Frequency = data["frequency"].AsInt;
        value.StrobeBrightness = data["strobeBrightness"].AsFloat;
        value.StrobeFade = data["strobeFade"].AsInt;
        value.Easing = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }

    private static void RestoreRotation(JSONObject data, Beatmap.Base.BaseLightRotationBase value)
    {
        if (data == null || value == null) return;
        value.Rotation = data["rotation"].AsFloat;
        value.Loop = data["loop"].AsInt;
        value.Direction = data["direction"].AsInt;
        value.EaseType = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }

    private static void RestoreTranslation(JSONObject data, Beatmap.Base.BaseLightTranslationBase value)
    {
        if (data == null || value == null) return;
        value.Translation = data["translation"].AsFloat;
        value.EaseType = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }

    private static void RestoreFloatFx(JSONObject data, Beatmap.Base.BaseFxEventFloat value)
    {
        if (data == null || value == null) return;
        value.Value = data["value"].AsFloat;
        value.Easing = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }
}
