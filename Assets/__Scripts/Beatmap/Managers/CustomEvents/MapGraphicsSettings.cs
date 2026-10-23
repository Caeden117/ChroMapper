using System;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// Map-scoped graphics overrides from the selected Info.dat difficulty. Null properties use
/// the editor preference; applying or clearing overrides never changes saved settings.
/// </summary>
public static class MapGraphicsSettings
{
    public static int? MirrorQuality { get; private set; }
    public static bool? Bloom { get; private set; }
    public static bool? ScreenDisplacement { get; private set; }
    public static bool? Smoke { get; private set; }

    /// <summary>Raised after the selected difficulty's graphics overrides are applied or cleared.</summary>
    public static event Action Changed;

    /// <summary>Replaces all overrides with supported _settings._graphics values from this difficulty.</summary>
    public static void Apply(InfoDifficulty difficulty)
    {
        var graphics = difficulty?.CustomData?["_settings"]?["_graphics"];
        MirrorQuality = ReadMirrorQuality(graphics);
        Bloom = ReadBool(graphics, "_mainEffectGraphicsSettings");
        ScreenDisplacement = ReadBool(graphics, "_screenDisplacementEffectsEnabled");
        Smoke = ReadBool(graphics, "_smokeGraphicsSettings");
        Changed?.Invoke();
    }

    /// <summary>Clears map overrides so renderers use the current editor preferences.</summary>
    public static void Reset()
    {
        MirrorQuality = null;
        Bloom = ScreenDisplacement = Smoke = null;
        Changed?.Invoke();
    }

    private static int? ReadMirrorQuality(JSONNode graphics)
    {
        var value = graphics?["_mirrorGraphicsSettings"];
        if (value == null || value.IsNull) return null;
        if (value.IsNumber && value.AsFloat is >= 0f and <= 3f && value.AsFloat == value.AsInt)
        {
            // Beat Saber's Off/Low use no reflection camera. Medium/High use CM's two camera qualities.
            return value.AsInt switch
            {
                2 => (int)MirrorRendererSO.MirrorQuality.Low,
                3 => (int)MirrorRendererSO.MirrorQuality.High,
                _ => (int)MirrorRendererSO.MirrorQuality.None
            };
        }
        Debug.LogWarning("Info.dat _settings._graphics._mirrorGraphicsSettings must be an integer from 0 to 3.");
        return null;
    }

    private static bool? ReadBool(JSONNode graphics, string property)
    {
        var value = graphics?[property];
        if (value == null || value.IsNull) return null;
        if (value.IsBoolean) return value.AsBool;
        if (value.IsNumber && value.AsFloat is 0f or 1f) return value.AsFloat == 1f;
        Debug.LogWarning($"Info.dat _settings._graphics.{property} must be a boolean or 0/1.");
        return null;
    }
}
