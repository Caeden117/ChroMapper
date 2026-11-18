using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

// Share serialization details while individual placement owners choose when to load and save their own state.
public static class GLSPlacementEditorState
{
    // New Extension nodes default to Linear (0). Apply this only to placement queues so editing an existing
    // extension preserves its chosen easing.
    public static void ClearExtensionEasing(
        BaseGLSEvent value,
        BeatmapEasingsSelectionInputController inputController)
    {
        if (value.UsePrevious != 1)
            return;

        value.Easing = (int)EaseType.Linear;
        if (value is BaseLightColorBase color)
        {
            // Copied color nodes may also carry Chroma easing overrides; clear those for a new extension.
            color.ChromaColorEasing = null;
            color.ChromaStrobeEasing = null;
            color.ChromaStrobeColorEasing = null;
            color.CustomData?.Remove(Beatmap.V3.V3LightColorBase.CustomKeyEasing);
            color.WriteCustom();
        }

        // Reset the menu's displayed curve without switching away from Extension or mutating hovered map nodes.
        inputController.NotifyEasingChanged(EaseType.Linear, false);
    }

    public static void WriteColor(JSONObject data, BaseLightColorBase value)
    {
        data["color"] = value.Color;
        data["brightness"] = value.Brightness;
        data["frequency"] = value.Frequency;
        data["strobeBrightness"] = value.StrobeBrightness;
        data["strobeFade"] = value.StrobeFade;
        WriteNodeState(data, value);
        GLSColorDistribution.WriteStrings(data, GLSColorDistribution.ColorDistributionsKey, value.ColorDistributions);
        GLSColorDistribution.WriteStrings(data, GLSColorDistribution.StrobeColorDistributionsKey, value.StrobeColorDistributions);
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

        ReadNodeState(data, value);
        if (data.HasKey(GLSColorDistribution.ColorDistributionsKey))
        {
            value.ColorDistributions = GLSColorDistribution.ReadStrings(data, GLSColorDistribution.ColorDistributionsKey);
        }
        if (data.HasKey(GLSColorDistribution.StrobeColorDistributionsKey))
        {
            value.StrobeColorDistributions = GLSColorDistribution.ReadStrings(data, GLSColorDistribution.StrobeColorDistributionsKey);
        }
        value.WriteCustom();
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
        inputController.NotifyColorDistributionsChanged(value.ColorDistributions, false);
        inputController.NotifyColorDistributionsChanged(value.StrobeColorDistributions, true);
        RefreshColorViews(value);
    }

    // Newly placed rotation extensions must not inherit loops from a normal node or copied editor state.
    public static void ClearRotationExtensionLoops(
        BaseLightRotationBase value,
        BeatmapGLSEventRotationInputController inputController)
    {
        if (value.UsePrevious == 1)
        {
            value.Loop = 0;
            inputController.NotifyLoopChanged(0, false);
        }
    }

    public static void WriteRotation(JSONObject data, BaseLightRotationBase value)
    {
        data["rotation"] = value.Rotation;
        data["loop"] = value.Loop;
        data["direction"] = value.Direction;
        WriteNodeState(data, value);
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

        ReadNodeState(data, value);
    }

    public static void WriteTranslation(JSONObject data, BaseLightTranslationBase value)
    {
        data["translation"] = value.Translation;
        WriteNodeState(data, value);
    }

    public static void ReadTranslation(JSONNode data, BaseLightTranslationBase value)
    {
        // Preserve each placement default when loading older metadata that lacks a newer field.
        if (data.HasKey("translation"))
        {
            value.Translation = data["translation"].AsFloat;
        }

        ReadNodeState(data, value);
    }

    public static void WriteFloatFx(JSONObject data, BaseFxEventFloat value)
    {
        data["value"] = value.Value;
        WriteNodeState(data, value);
    }

    public static void ReadFloatFx(JSONNode data, BaseFxEventFloat value)
    {
        // Preserve each placement default when loading older metadata that lacks a newer field.
        if (data.HasKey("value"))
        {
            value.Value = data["value"].AsFloat;
        }

        ReadNodeState(data, value);
    }

    private static void WriteNodeState(JSONObject data, BaseGLSEvent value)
    {
        data["easing"] = value.Easing;
        data["usePrevious"] = value.UsePrevious;
    }

    private static void ReadNodeState(JSONNode data, BaseGLSEvent value)
    {
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
                value.StrobeFade,
                value.ColorDistributions,
                value.StrobeColorDistributions);
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
