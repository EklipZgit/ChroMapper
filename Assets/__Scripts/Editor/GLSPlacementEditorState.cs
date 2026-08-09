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
        // Preserve each placement default when loading older metadata that lacks a newer field.
        if (data.HasKey("color"))
        {
            value.Color = data["color"].AsInt;
        }
        if (data.HasKey("brightness"))
        {
            value.Brightness = data["brightness"].AsFloat;
        }
        if (data.HasKey("frequency"))
        {
            value.Frequency = data["frequency"].AsInt;
        }
        if (data.HasKey("strobeBrightness"))
        {
            value.StrobeBrightness = data["strobeBrightness"].AsFloat;
        }
        if (data.HasKey("strobeFade"))
        {
            value.StrobeFade = data["strobeFade"].AsInt;
        }
        if (data.HasKey("easing"))
        {
            value.Easing = data["easing"].AsInt;
        }
        if (data.HasKey("usePrevious"))
        {
            value.UsePrevious = data["usePrevious"].AsInt;
        }
    }

    // Restore every color control through its shared controller so placement data and delayed GLS views cannot diverge.
    public static void RestoreColorPlacementState(
        JSONNode data,
        BaseLightColorBase value,
        BeatmapGLSEventColorInputController inputController)
    {
        ReadColor(data, value);
        inputController.NotifyFadeChanged(value.Easing >= 0 ? 0 : -1);
        inputController.NotifyBrightnessChanged(value.Brightness);
        inputController.NotifyStrobeFrequencyChanged(value.Frequency);
        inputController.NotifyStrobeBrightnessChanged(value.StrobeBrightness);
        inputController.NotifySoftStrobeChanged(value.StrobeFade);
        RefreshColorViews(value);
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
        // Preserve each placement default when loading older metadata that lacks a newer field.
        if (data.HasKey("rotation"))
        {
            value.Rotation = data["rotation"].AsFloat;
        }
        if (data.HasKey("loop"))
        {
            value.Loop = data["loop"].AsInt;
        }
        if (data.HasKey("direction"))
        {
            value.Direction = data["direction"].AsInt;
        }
        if (data.HasKey("easing"))
        {
            value.EaseType = data["easing"].AsInt;
        }
        if (data.HasKey("usePrevious"))
        {
            value.UsePrevious = data["usePrevious"].AsInt;
        }
    }

    public static void WriteTranslation(JSONObject data, BaseLightTranslationBase value)
    {
        data["translation"] = value.Translation;
        data["easing"] = value.EaseType;
        data["usePrevious"] = value.UsePrevious;
    }

    public static void ReadTranslation(JSONNode data, BaseLightTranslationBase value)
    {
        // Preserve each placement default when loading older metadata that lacks a newer field.
        if (data.HasKey("translation"))
        {
            value.Translation = data["translation"].AsFloat;
        }
        if (data.HasKey("easing"))
        {
            value.EaseType = data["easing"].AsInt;
        }
        if (data.HasKey("usePrevious"))
        {
            value.UsePrevious = data["usePrevious"].AsInt;
        }
    }

    public static void WriteFloatFx(JSONObject data, BaseFxEventFloat value)
    {
        data["value"] = value.Value;
        data["easing"] = value.Easing;
        data["usePrevious"] = value.UsePrevious;
    }

    public static void ReadFloatFx(JSONNode data, BaseFxEventFloat value)
    {
        // Preserve each placement default when loading older metadata that lacks a newer field.
        if (data.HasKey("value"))
        {
            value.Value = data["value"].AsFloat;
        }
        if (data.HasKey("easing"))
        {
            value.Easing = data["easing"].AsInt;
        }
        if (data.HasKey("usePrevious"))
        {
            value.UsePrevious = data["usePrevious"].AsInt;
        }
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
