using Beatmap.Base;
using SimpleJSON;
using UnityEngine;

// Share serialization details while individual placement owners choose when to load and save their own state.
public static class GLSPlacementEditorState
{
    public static void WriteColor(JSONObject data, BaseLightColorBase value)
    {
        data["color"] = value.Color;
        data["brightness"] = value.Brightness;
        data["frequency"] = value.Frequency;
        data["strobeBrightness"] = value.StrobeBrightness;
        data["strobeFade"] = value.StrobeFade;
        data["easing"] = value.Easing;
        data["usePrevious"] = value.UsePrevious;
    }

    public static void ReadColor(JSONNode data, BaseLightColorBase value)
    {
        value.Color = data["color"].AsInt;
        value.Brightness = data["brightness"].AsFloat;
        value.Frequency = data["frequency"].AsInt;
        value.StrobeBrightness = data["strobeBrightness"].AsFloat;
        value.StrobeFade = data["strobeFade"].AsInt;
        value.Easing = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }

    public static void WriteRotation(JSONObject data, BaseLightRotationBase value)
    {
        data["rotation"] = value.Rotation;
        data["loop"] = value.Loop;
        data["direction"] = value.Direction;
        data["easing"] = value.EaseType;
        data["usePrevious"] = value.UsePrevious;
    }

    public static void ReadRotation(JSONNode data, BaseLightRotationBase value)
    {
        value.Rotation = data["rotation"].AsFloat;
        value.Loop = data["loop"].AsInt;
        value.Direction = data["direction"].AsInt;
        value.EaseType = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }

    public static void WriteTranslation(JSONObject data, BaseLightTranslationBase value)
    {
        data["translation"] = value.Translation;
        data["easing"] = value.EaseType;
        data["usePrevious"] = value.UsePrevious;
    }

    public static void ReadTranslation(JSONNode data, BaseLightTranslationBase value)
    {
        value.Translation = data["translation"].AsFloat;
        value.EaseType = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }

    public static void WriteFloatFx(JSONObject data, BaseFxEventFloat value)
    {
        data["value"] = value.Value;
        data["easing"] = value.Easing;
        data["usePrevious"] = value.UsePrevious;
    }

    public static void ReadFloatFx(JSONNode data, BaseFxEventFloat value)
    {
        value.Value = data["value"].AsFloat;
        value.Easing = data["easing"].AsInt;
        value.UsePrevious = data["usePrevious"].AsInt;
    }

    // Let a GLS placement owner redraw every matching view after it restores its queued node.
    public static void RefreshColorViews(BaseLightColorBase value)
    {
        foreach (var view in Object.FindObjectsByType<GLSInputColorViewController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            view.ApplyEditorState(
                value.Brightness,
                value.StrobeBrightness,
                value.Frequency,
                value.Easing,
                value.StrobeFade);
        }
    }

    // Let a GLS placement owner redraw every matching view after it restores its queued node.
    public static void RefreshRotationViews(BaseLightRotationBase value)
    {
        foreach (var view in Object.FindObjectsByType<GLSInputRotationViewController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            view.ApplyEditorState(value.Rotation, value.Loop, value.Direction);
        }
    }

    // Let a GLS placement owner redraw every matching view after it restores its queued node.
    public static void RefreshTranslationViews(BaseLightTranslationBase value)
    {
        foreach (var view in Object.FindObjectsByType<GLSInputTranslationViewController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            view.ApplyEditorState(value.Translation);
        }
    }

    // Let a GLS placement owner redraw every matching view after it restores its queued node.
    public static void RefreshFloatFxViews(BaseFxEventFloat value)
    {
        foreach (var view in Object.FindObjectsByType<GLSInputFloatFXViewController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            view.ApplyEditorState(value.Value);
        }
    }
}
