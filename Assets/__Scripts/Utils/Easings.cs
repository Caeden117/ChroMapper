using System;
using System.Collections.Generic;
using Beatmap.Enums;
using UnityEngine;

/*
 * Copied from Arti
 *
 * Copied from https://gist.github.com/Fonserbc/3d31a25e87fdaa541ddf
 * Functions taken from Tween.js - Licensed under the MIT license
 * at https://github.com/sole/tween.js
 */
public static class Easing
{
    /// <summary>
    ///     Maps the names found at https://easings.net/en to the matching easing functions.
    ///     Maps "easeLinear" to linear easing (x => x).
    /// </summary>
    public static Dictionary<string, Func<float, float>> ByName = new()
    {
        { "easeLinear", Linear },
        { "easeInQuad", Quadratic.In },
        { "easeOutQuad", Quadratic.Out },
        { "easeInOutQuad", Quadratic.InOut },
        { "easeInCubic", Cubic.In },
        { "easeOutCubic", Cubic.Out },
        { "easeInOutCubic", Cubic.InOut },
        { "easeInQuart", Quartic.In },
        { "easeOutQuart", Quartic.Out },
        { "easeInOutQuart", Quartic.InOut },
        { "easeInQuint", Quintic.In },
        { "easeOutQuint", Quintic.Out },
        { "easeInOutQuint", Quintic.InOut },
        { "easeInSine", Sinusoidal.In },
        { "easeOutSine", Sinusoidal.Out },
        { "easeInOutSine", Sinusoidal.InOut },
        { "easeInExpo", Exponential.In },
        { "easeOutExpo", Exponential.Out },
        { "easeInOutExpo", Exponential.InOut },
        { "easeInCirc", Circular.In },
        { "easeOutCirc", Circular.Out },
        { "easeInOutCirc", Circular.InOut },
        { "easeInBack", Back.In },
        { "easeOutBack", Back.Out },
        { "easeInOutBack", Back.InOut },
        { "easeInElastic", Elastic.In },
        { "easeOutElastic", Elastic.Out },
        { "easeInOutElastic", Elastic.InOut },
        { "easeInBounce", Bounce.In },
        { "easeOutBounce", Bounce.Out },
        { "easeInOutBounce", Bounce.InOut },
        { "easeStep", Step },
        { "easeBeatSaberInOutBack", Back.BeatSaberInOut },
        { "easeBeatSaberInOutElastic", Elastic.BeatSaberInOut },
        { "easeBeatSaberInOutBounce", Bounce.BeatSaberInOut }
    };

    // Chroma/Heck resolves point-data and light-event easings through its own function table
    // (Heck/Animation/Easings.cs): the Back, Bounce, Elastic, and Expo families are custom variants,
    // not the easings.net curves ByName maps. WorldCavesIn's kaleidoscope proved the divergence.
    // Heck's easeOutBack overshoots to ~1.375 where the classic curve peaks at ~1.088, putting the
    // swirl clones ~35 degrees off mid-animation. HeckByName mirrors only the divergent entries;
    // every other name already matches ByName's curve, and unknown names fall back to linear like
    // Named does (Heck's Enum.Parse would fail, but malformed maps should not crash the loader).
    public static readonly Dictionary<string, Func<float, float>> HeckByName = new()
    {
        { "easeInBack", HeckBack.In },
        { "easeOutBack", HeckBack.Out },
        { "easeInOutBack", HeckBack.InOut },
        { "easeInBounce", HeckBounce.In },
        { "easeOutBounce", HeckBounce.Out },
        { "easeInOutBounce", HeckBounce.InOut },
        { "easeInElastic", HeckElastic.In },
        { "easeOutElastic", HeckElastic.Out },
        { "easeInOutElastic", HeckElastic.InOut },
        { "easeInExpo", HeckExponential.In },
        { "easeOutExpo", HeckExponential.Out },
        { "easeInOutExpo", HeckExponential.InOut }
    };

    /// <summary>
    ///     Maps UI-friendly display names to the names found at https://easings.net/en.
    ///     Used in conjunction with <seealso cref="ByName" /> to obtain the Easing function from a display name.
    /// </summary>
    public static Dictionary<string, string> DisplayNameToInternalName = new()
    {
        { "Linear", "easeLinear" },
        { "Quadratic In", "easeInQuad" },
        { "Quadratic Out", "easeOutQuad" },
        { "Quadratic In/Out", "easeInOutQuad" },
        { "Cubic In", "easeInCubic" },
        { "Cubic Out", "easeOutCubic" },
        { "Cubic In/Out", "easeInOutCubic" },
        { "Quartic In", "easeInQuart" },
        { "Quartic Out", "easeOutQuart" },
        { "Quartic In/Out", "easeInOutQuart" },
        { "Quintic In", "easeInQuint" },
        { "Quintic Out", "easeOutQuint" },
        { "Quintic In/Out", "easeInOutQuint" },
        { "Sine In", "easeInSine" },
        { "Sine Out", "easeOutSine" },
        { "Sine In/Out", "easeInOutSine" },
        { "Exponential In", "easeInExpo" },
        { "Exponential Out", "easeOutExpo" },
        { "Exponential In/Out", "easeInOutExpo" },
        { "Circular In", "easeInCirc" },
        { "Circular Out", "easeOutCirc" },
        { "Circular In/Out", "easeInOutCirc" },
        { "Back In", "easeInBack" },
        { "Back Out", "easeOutBack" },
        { "Back In/Out", "easeInOutBack" },
        { "Elastic In", "easeInElastic" },
        { "Elastic Out", "easeOutElastic" },
        { "Elastic In/Out", "easeInOutElastic" },
        { "Bounce In", "easeInBounce" },
        { "Bounce Out", "easeOutBounce" },
        { "Bounce In/Out", "easeInOutBounce" },
        { "Step", "easeStep" }
    };

    /// <summary>
    ///     Maps the names found at https://easings.net/en to compact labels used by editor event displays.
    /// </summary>
    public static readonly Dictionary<string, string> InternalNameToShortName = new()
    {
        { "easeLinear", "Lin" },
        { "easeInQuad", "In^2" },
        { "easeOutQuad", "Out^2" },
        { "easeInOutQuad", "IO^2" },
        { "easeInCubic", "In^3" },
        { "easeOutCubic", "Out^3" },
        { "easeInOutCubic", "IO^3" },
        { "easeInQuart", "In^4" },
        { "easeOutQuart", "Out^4" },
        { "easeInOutQuart", "IO^4" },
        { "easeInQuint", "In^5" },
        { "easeOutQuint", "Out^5" },
        { "easeInOutQuint", "IO^5" },
        { "easeInSine", "InSn" },
        { "easeOutSine", "OutSn" },
        { "easeInOutSine", "IOSn" },
        { "easeInExpo", "InEx" },
        { "easeOutExpo", "OutEx" },
        { "easeInOutExpo", "IOEx" },
        { "easeInCirc", "InCr" },
        { "easeOutCirc", "OutCr" },
        { "easeInOutCirc", "IOCr" },
        { "easeInBack", "InBk" },
        { "easeOutBack", "OutBk" },
        { "easeInOutBack", "IOTBk" },
        { "easeInElastic", "InEl" },
        { "easeOutElastic", "OutEl" },
        { "easeInOutElastic", "IOTEl" },
        { "easeInBounce", "InBo" },
        { "easeOutBounce", "OutBo" },
        { "easeInOutBounce", "IOTBo" },
        { "easeStep", "Step" },
        { "easeBeatSaberInOutBack", "IOBk" },
        { "easeBeatSaberInOutElastic", "IOEl" },
        { "easeBeatSaberInOutBounce", "IOBo" }
    };

    public static readonly Dictionary<int, string> IDToShortName = new()
    {
        { (int)EaseType.None, "N" },
        { (int)EaseType.Linear, "L" },
        { (int)EaseType.InQuadratic, "I^2" },
        { (int)EaseType.OutQuadratic, "O^2" },
        { (int)EaseType.InOutQuadratic, "IO^2" },
        { (int)EaseType.InSinusoidal, "ISn" },
        { (int)EaseType.OutSinusoidal, "OSn" },
        { (int)EaseType.InOutSinusoidal, "IOSn" },
        { (int)EaseType.InCubic, "I^3" },
        { (int)EaseType.OutCubic, "O^3" },
        { (int)EaseType.InOutCubic, "IO^3" },
        { (int)EaseType.InQuartic, "I^4" },
        { (int)EaseType.OutQuartic, "O^4" },
        { (int)EaseType.InOutQuartic, "IO^4" },
        { (int)EaseType.InQuintic, "I^5" },
        { (int)EaseType.OutQuintic, "O^5" },
        { (int)EaseType.InOutQuintic, "IO^5" },
        { (int)EaseType.InExponential, "IEx" },
        { (int)EaseType.OutExponential, "OEx" },
        { (int)EaseType.InOutExponential, "IOEx" },
        { (int)EaseType.InCircular, "ICr" },
        { (int)EaseType.OutCircular, "OCr" },
        { (int)EaseType.InOutCircular, "IOCr" },
        { (int)EaseType.InBack, "IBk" },
        { (int)EaseType.OutBack, "OBk" },
        { (int)EaseType.InOutBack, "IOTBk" },
        { (int)EaseType.InElastic, "IEl" },
        { (int)EaseType.OutElastic, "OEl" },
        { (int)EaseType.InOutElastic, "IOTEl" },
        { (int)EaseType.InBounce, "IBo" },
        { (int)EaseType.OutBounce, "OBo" },
        { (int)EaseType.InOutBounce, "IOTBo" },
        { (int)EaseType.BeatSaberInOutBack, "IOBk" },
        { (int)EaseType.BeatSaberInOutElastic, "IOEl" },
        { (int)EaseType.BeatSaberInOutBounce, "IOBo" }
    };

    public static readonly Dictionary<int, string> IDToInternalName = new()
    {
        { (int)EaseType.None, "easeStep" },
        { (int)EaseType.Linear, "easeLinear" },
        { (int)EaseType.InQuadratic, "easeInQuad" },
        { (int)EaseType.OutQuadratic, "easeOutQuad" },
        { (int)EaseType.InOutQuadratic, "easeInOutQuad" },
        { (int)EaseType.InSinusoidal, "easeInSine" },
        { (int)EaseType.OutSinusoidal, "easeOutSine" },
        { (int)EaseType.InOutSinusoidal, "easeInOutSine" },
        { (int)EaseType.InCubic, "easeInCubic" },
        { (int)EaseType.OutCubic, "easeOutCubic" },
        { (int)EaseType.InOutCubic, "easeInOutCubic" },
        { (int)EaseType.InQuartic, "easeInQuart" },
        { (int)EaseType.OutQuartic, "easeOutQuart" },
        { (int)EaseType.InOutQuartic, "easeInOutQuart" },
        { (int)EaseType.InQuintic, "easeInQuint" },
        { (int)EaseType.OutQuintic, "easeOutQuint" },
        { (int)EaseType.InOutQuintic, "easeInOutQuint" },
        { (int)EaseType.InExponential, "easeInExpo" },
        { (int)EaseType.OutExponential, "easeOutExpo" },
        { (int)EaseType.InOutExponential, "easeInOutExpo" },
        { (int)EaseType.InCircular, "easeInCirc" },
        { (int)EaseType.OutCircular, "easeOutCirc" },
        { (int)EaseType.InOutCircular, "easeInOutCirc" },
        { (int)EaseType.InBack, "easeInBack" },
        { (int)EaseType.OutBack, "easeOutBack" },
        { (int)EaseType.InOutBack, "easeInOutBack" },
        { (int)EaseType.InElastic, "easeInElastic" },
        { (int)EaseType.OutElastic, "easeOutElastic" },
        { (int)EaseType.InOutElastic, "easeInOutElastic" },
        { (int)EaseType.InBounce, "easeInBounce" },
        { (int)EaseType.OutBounce, "easeOutBounce" },
        { (int)EaseType.InOutBounce, "easeInOutBounce" },
        { (int)EaseType.BeatSaberInOutBack, "easeBeatSaberInOutBack" },
        { (int)EaseType.BeatSaberInOutElastic, "easeBeatSaberInOutElastic" },
        { (int)EaseType.BeatSaberInOutBounce, "easeBeatSaberInOutBounce" }
    };

    /// <summary>
    ///     Maps a numeric easing save ID to its internal <see cref="ByName" /> name.
    ///     Unknown IDs fall back to linear so stale metadata cannot select an unintended curve.
    /// </summary>
    /// <param name="id">The numeric easing ID from the beatmap.</param>
    /// <returns>The internal easing name used by gradient and ribbon dispatch.</returns>
    public static string InternalNameForID(int id) =>
        IDToInternalName.TryGetValue(id, out var name) ? name : "easeLinear";

    public static readonly Dictionary<int, string> IDToFullName = new()
    {
        { (int)EaseType.None, "None" },
        { (int)EaseType.Linear, "Linear" },
        { (int)EaseType.InQuadratic, "In Quadratic" },
        { (int)EaseType.OutQuadratic, "Out Quadratic" },
        { (int)EaseType.InOutQuadratic, "In Out Quadratic" },
        { (int)EaseType.InSinusoidal, "In Sinusoidal" },
        { (int)EaseType.OutSinusoidal, "Out Sinusoidal" },
        { (int)EaseType.InOutSinusoidal, "In Out Sinusoidal" },
        { (int)EaseType.InCubic, "In Cubic" },
        { (int)EaseType.OutCubic, "Out Cubic" },
        { (int)EaseType.InOutCubic, "In Out Cubic" },
        { (int)EaseType.InQuartic, "In Quartic" },
        { (int)EaseType.OutQuartic, "Out Quartic" },
        { (int)EaseType.InOutQuartic, "In Out Quartic" },
        { (int)EaseType.InQuintic, "In Quintic" },
        { (int)EaseType.OutQuintic, "Out Quintic" },
        { (int)EaseType.InOutQuintic, "In Out Quintic" },
        { (int)EaseType.InExponential, "In Exponential" },
        { (int)EaseType.OutExponential, "Out Exponential" },
        { (int)EaseType.InOutExponential, "In Out Exponential" },
        { (int)EaseType.InCircular, "In Circular" },
        { (int)EaseType.OutCircular, "Out Circular" },
        { (int)EaseType.InOutCircular, "In Out Circular" },
        { (int)EaseType.InBack, "In Back" },
        { (int)EaseType.OutBack, "Out Back" },
        { (int)EaseType.InOutBack, "In Out Back" },
        { (int)EaseType.InElastic, "In Elastic" },
        { (int)EaseType.OutElastic, "Out Elastic" },
        { (int)EaseType.InOutElastic, "In Out Elastic" },
        { (int)EaseType.InBounce, "In Bounce" },
        { (int)EaseType.OutBounce, "Out Bounce" },
        { (int)EaseType.InOutBounce, "In Out Bounce" },
        { (int)EaseType.BeatSaberInOutBack, "In Out Back (BS)" },
        { (int)EaseType.BeatSaberInOutElastic, "In Out Elastic (BS)" },
        { (int)EaseType.BeatSaberInOutBounce, "In Out Bounce (BS)" }
    };

    /// <summary>
    ///     Maps the ID to easing found at https://easings.net/en to the matching easing functions.
    ///     Maps 0 to linear easing (x => x).
    /// </summary>
    public static readonly Dictionary<int, Func<float, float>> ByID = new()
    {
        { (int)EaseType.None, Step },
        { (int)EaseType.Linear, Linear },
        { (int)EaseType.InQuadratic, Quadratic.In },
        { (int)EaseType.OutQuadratic, Quadratic.Out },
        { (int)EaseType.InOutQuadratic, Quadratic.InOut },
        { (int)EaseType.InSinusoidal, Sinusoidal.In },
        { (int)EaseType.OutSinusoidal, Sinusoidal.Out },
        { (int)EaseType.InOutSinusoidal, Sinusoidal.InOut },
        { (int)EaseType.InCubic, Cubic.In },
        { (int)EaseType.OutCubic, Cubic.Out },
        { (int)EaseType.InOutCubic, Cubic.InOut },
        { (int)EaseType.InQuartic, Quartic.In },
        { (int)EaseType.OutQuartic, Quartic.Out },
        { (int)EaseType.InOutQuartic, Quartic.InOut },
        { (int)EaseType.InQuintic, Quintic.In },
        { (int)EaseType.OutQuintic, Quintic.Out },
        { (int)EaseType.InOutQuintic, Quintic.InOut },
        { (int)EaseType.InExponential, Exponential.In },
        { (int)EaseType.OutExponential, Exponential.Out },
        { (int)EaseType.InOutExponential, Exponential.InOut },
        { (int)EaseType.InCircular, Circular.In },
        { (int)EaseType.OutCircular, Circular.Out },
        { (int)EaseType.InOutCircular, Circular.InOut },
        { (int)EaseType.InBack, Back.In },
        { (int)EaseType.OutBack, Back.Out },
        { (int)EaseType.InOutBack, Back.InOut },
        { (int)EaseType.InElastic, Elastic.In },
        { (int)EaseType.OutElastic, Elastic.Out },
        { (int)EaseType.InOutElastic, Elastic.InOut },
        { (int)EaseType.InBounce, Bounce.In },
        { (int)EaseType.OutBounce, Bounce.Out },
        { (int)EaseType.InOutBounce, Bounce.InOut },
        { (int)EaseType.BeatSaberInOutBack, Back.BeatSaberInOut },
        { (int)EaseType.BeatSaberInOutElastic, Elastic.BeatSaberInOut },
        { (int)EaseType.BeatSaberInOutBounce, Bounce.BeatSaberInOut }
    };

    private static readonly Dictionary<string, Func<float, float>> byShortName = CreateEasingsByShortName();
    private static readonly Dictionary<int, int> shaderIdByEaseType = CreateShaderIdByEaseType();

    private static Dictionary<string, Func<float, float>> CreateEasingsByShortName()
    {
        var result = new Dictionary<string, Func<float, float>>(IDToShortName.Count, StringComparer.Ordinal);
        foreach (var entry in IDToShortName)
        {
            result.Add(entry.Value, ByID[entry.Key]);
        }

        return result;
    }

    public static bool TryGetByNameOrShortName(string name, out Func<float, float> easing)
    {
        name = name?.Trim() ?? string.Empty;
        return byShortName.TryGetValue(name, out easing) || ByName.TryGetValue(name, out easing);
    }

    /// <summary>
    ///     If an easing named <paramref name="name" /> exists, returns it.
    ///     Otherwise, returns <see cref="Linear(float)" />.
    ///     <seealso cref="ByName" />
    /// </summary>
    /// <param name="name">The name of the desired easing.</param>
    /// <returns>The desired easing, or <see cref="Linear(float)" /> if that easing doesn't exist.</returns>
    public static Func<float, float> Named(string name) => ByName.TryGetValue(name, out var easing) ? easing : Linear;

    /// <summary>
    ///     If an easing ID <paramref name="id" /> exists, returns it.
    ///     Otherwise, returns <see cref="Linear(float)" />.
    ///     <seealso cref="ByName" />
    /// </summary>
    /// <param name="id">The ID of the desired easing.</param>
    /// <returns>The desired easing, or <see cref="Linear(float)" /> if that easing doesn't exist.</returns>
    public static Func<float, float> FromID(int id) => ByID.TryGetValue(id, out var easing) ? easing : Linear;

    /// <summary>
    ///     If an easing named <paramref name="name" /> exists in Chroma's function table, returns it.
    ///     Otherwise, returns <see cref="Linear(float)" />.
    ///     <seealso cref="HeckByName" />
    /// </summary>
    /// <param name="name">The name of the desired easing.</param>
    /// <returns>The desired easing, or <see cref="Linear(float)" /> if that easing doesn't exist.</returns>
    public static Func<float, float> HeckNamed(string name) =>
        HeckByName.TryGetValue(name, out var easing) ? easing : Named(name);

    /// <summary>
    /// Returns the shader ID for a given easing.
    /// </summary>
    /// <param name="easingId">Internal easing ID (what Chroma uses)</param>
    /// <returns>Numerical ID used for the basic gradient shader.</returns>
    public static int EasingShaderId(string easingId)
    {
        var i = 0;
        foreach (var easing in ByName.Keys)
        {
            if (easing == easingId) return i;
            i++;
        }

        return 0;
    }

    public static int EasingShaderId(int easeType) => shaderIdByEaseType.GetValueOrDefault(easeType);

    private static Dictionary<int, int> CreateShaderIdByEaseType()
    {
        var result = new Dictionary<int, int>();
        foreach (var easingById in ByID)
        {
            var shaderId = 0;
            foreach (var shaderEasing in ByName)
            {
                if (shaderEasing.Value == easingById.Value)
                {
                    result[easingById.Key] = shaderId;
                    break;
                }

                shaderId++;
            }
        }

        return result;
    }

    public static float StrobeFade(float phase, float pulseWidth, Func<float, float> easing) =>
        phase < pulseWidth
            ? easing(phase / pulseWidth)
            : 1f - easing((phase - pulseWidth) / (1f - pulseWidth));

    public static float Linear(float k) => k;

    public static float Step(float k) => Mathf.Floor(k);

    public static class Quadratic
    {
        public static float In(float k) => k * k;

        public static float Out(float k) => k * (2f - k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k
                : -0.5f * (((k -= 1f) * (k - 2f)) - 1f);
        }
    }

    public static class Cubic
    {
        public static float In(float k) => k * k * k;

        public static float Out(float k) => 1f + ((k -= 1f) * k * k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k * k
                : 0.5f * (((k -= 2f) * k * k) + 2f);
        }
    }

    public static class Quartic
    {
        public static float In(float k) => k * k * k * k;

        public static float Out(float k) => 1f - ((k -= 1f) * k * k * k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k * k * k
                : -0.5f * (((k -= 2f) * k * k * k) - 2f);
        }
    }

    public static class Quintic
    {
        public static float In(float k) => k * k * k * k * k;

        public static float Out(float k) => 1f + ((k -= 1f) * k * k * k * k);

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? 0.5f * k * k * k * k * k
                : 0.5f * (((k -= 2f) * k * k * k * k) + 2f);
        }
    }

    public static class Sinusoidal
    {
        public static float In(float k) => 1f - Mathf.Cos(k * Mathf.PI / 2f);

        public static float Out(float k) => Mathf.Sin(k * Mathf.PI / 2f);

        public static float InOut(float k) => 0.5f * (1f - Mathf.Cos(Mathf.PI * k));
    }

    public static class Exponential
    {
        public static float In(float k) => k == 0f ? 0f : Mathf.Pow(1024f, k - 1f);

        public static float Out(float k) => k == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * k);

        public static float InOut(float k)
        {
            if (k == 0f) return 0f;
            if (k == 1f) return 1f;
            if ((k *= 2f) < 1f) return 0.5f * Mathf.Pow(1024f, k - 1f);
            return 0.5f * (-Mathf.Pow(2f, -10f * (k - 1f)) + 2f);
        }
    }

    public static class Circular
    {
        public static float In(float k) => 1f - Mathf.Sqrt(1f - (k * k));

        public static float Out(float k) => Mathf.Sqrt(1f - ((k -= 1f) * k));

        public static float InOut(float k)
        {
            return (k *= 2f) < 1f
                ? -0.5f * (Mathf.Sqrt(1f - (k * k)) - 1)
                : 0.5f * (Mathf.Sqrt(1f - ((k -= 2f) * k)) + 1f);
        }
    }

    public static class Elastic
    {
        public static float In(float t)
        {
            if (t == 0f || t == 1f)
            {
                return t;
            }

            return -Mathf.Pow(2f, (10f * t) - 10f)
                * Mathf.Sin(((10f * t) - 10.75f) * (Mathf.PI * 2f / 3f));
        }

        public static float Out(float t)
        {
            if (t == 0f || t == 1f)
            {
                return t;
            }

            return (Mathf.Pow(2f, -10f * t)
                * Mathf.Sin(((10f * t) - 0.75f) * (Mathf.PI * 2f / 3f))) + 1f;
        }

        public static float InOut(float t)
        {
            if (t == 0f || t == 1f)
            {
                return t;
            }

            return t < 0.5f
                ? -(Mathf.Pow(2f, (20f * t) - 10f)
                    * Mathf.Sin(((20f * t) - 11.125f) * (Mathf.PI * 4f / 9f))) / 2f
                : (Mathf.Pow(2f, (-20f * t) + 10f)
                    * Mathf.Sin(((20f * t) - 11.125f) * (Mathf.PI * 4f / 9f)) / 2f) + 1f;
        }

        public static float BeatSaberInOut(float t)
        {
            return t < 0.3f
                ? 37.037f * t * t * t
                : (Mathf.Pow(2f, -10f * (t - 0.2f)) * Mathf.Sin(t * 10f * (MathF.PI * 2f / 3f))) + 1f;
        }
    }

    public static class Back
    {
        private static readonly float s = 1.70158f;
        private static readonly float s2 = 2.5949095f;

        public static float In(float k) => k * k * (((s + 1f) * k) - s);

        public static float Out(float k) => ((k -= 1f) * k * (((s + 1f) * k) + s)) + 1f;

        public static float InOut(float k)
        {
            if ((k *= 2f) < 1f) return 0.5f * (k * k * (((s2 + 1f) * k) - s2));
            return 0.5f * (((k -= 2f) * k * (((s2 + 1f) * k) + s2)) + 2f);
        }

        public static float BeatSaberInOut(float t)
        {
            if (t < 0.517f) return 5.014f * t * t * t;

            return 1f
                + (2.70158f * Mathf.Pow((1.665f * (t + -0.4f)) - 1f, 3f))
                + (1.70158f * Mathf.Pow((1.665f * (t + -0.4f)) - 1f, 2f));
        }
    }

    public static class Bounce
    {
        public static float In(float k) => 1f - Out(1f - k);

        public static float Out(float k)
        {
            return k switch
            {
                < 1f / 2.75f => 7.5625f * k * k,
                < 2f / 2.75f => (7.5625f * (k -= 1.5f / 2.75f) * k) + 0.75f,
                < 2.5f / 2.75f => (7.5625f * (k -= 2.25f / 2.75f) * k) + 0.9375f,
                _ => (7.5625f * (k -= 2.625f / 2.75f) * k) + 0.984375f
            };
        }

        public static float InOut(float k)
        {
            if (k < 0.5f) return In(k * 2f) * 0.5f;
            return (Out((k * 2f) - 1f) * 0.5f) + 0.5f;
        }

        public static float BeatSaberInOut(float t)
        {
            return t switch
            {
                < 0.72727275f and < 0.36363637f => 20.796f * t * t * t,
                < 0.72727275f => (7.5625f * (t -= 0.54545456f) * t) + 0.75f,
                < 0.90909094f => (7.5625f * (t -= 0.8181818f) * t) + 0.9375f,
                _ => (7.5625f * (t -= 21f / 22f) * t) + (63f / 64f)
            };
        }
    }

    // Chroma's Back family (Heck/Animation/Easings.cs) is a sine-overshoot variant, not the
    // easings.net cubic the classic Back class implements; point-data easings must use these.
    public static class HeckBack
    {
        public static float In(float k) => (k * k * k) - (k * Mathf.Sin(k * Mathf.PI));

        public static float Out(float k)
        {
            var f = 1f - k;
            return 1f - ((f * f * f) - (f * Mathf.Sin(f * Mathf.PI)));
        }

        public static float InOut(float k)
        {
            if (k < 0.5f)
            {
                var f = 2f * k;
                return 0.5f * ((f * f * f) - (f * Mathf.Sin(f * Mathf.PI)));
            }

            var g = 1f - ((2f * k) - 1f);
            return (0.5f * (1f - ((g * g * g) - (g * Mathf.Sin(g * Mathf.PI))))) + 0.5f;
        }
    }

    // Chroma's Bounce family takes the minimum of four parabolas (collision points 4/11, 8/11,
    // 9/11, 1) rather than Penner's piecewise bounce used by the classic Bounce class.
    public static class HeckBounce
    {
        public static float In(float k) => 1f - Out(1f - k);

        public static float Out(float k)
        {
            var x = (121f / 16f) * k * k;

            var q1 = k - (6f / 11f);
            var b = ((363f / 40f) * q1 * q1) + (7f / 10f);
            if (b < x) x = b;

            var q2 = k - (179f / 220f);
            var c = ((4356f / 361f) * q2 * q2) + (91f / 100f);
            if (c < x) x = c;

            var q3 = k - (19f / 20f);
            var d = ((54f / 5f) * q3 * q3) + (973f / 1000f);
            if (d < x) x = d;

            return x;
        }

        public static float InOut(float k)
        {
            if (k < 0.5f) return 0.5f * In(k * 2f);
            return (0.5f * Out((k * 2f) - 1f)) + 0.5f;
        }
    }

    // Chroma's Elastic family uses a fixed sin(13pi/2 * t) frequency where the classic Elastic
    // class uses the easings.net phase-shifted form.
    public static class HeckElastic
    {
        private const float HalfPi = Mathf.PI / 2f;

        public static float In(float k) => Mathf.Sin(13f * HalfPi * k) * Mathf.Pow(2f, 10f * (k - 1f));

        public static float Out(float k) =>
            (Mathf.Sin(-13f * HalfPi * (k + 1f)) * Mathf.Pow(2f, -10f * k)) + 1f;

        public static float InOut(float k)
        {
            if (k < 0.5f)
            {
                return 0.5f * Mathf.Sin(13f * HalfPi * (2f * k)) * Mathf.Pow(2f, 10f * ((2f * k) - 1f));
            }

            return 0.5f * ((Mathf.Sin(-13f * HalfPi * (2f * k)) * Mathf.Pow(2f, -10f * ((2f * k) - 1f))) + 2f);
        }
    }

    // Chroma's Expo family normalizes with 1023-scale constants instead of the classic
    // 1024-scale forms, so endpoints land exactly on 0 and 1.
    public static class HeckExponential
    {
        public static float In(float k) =>
            k <= 0f ? k : (Mathf.Pow(2f, 10f * k) * (1f / 1023f)) - (1f / 1023f);

        public static float Out(float k) =>
            k > 1f ? k : (1024f / 1023f) - ((1024f / 1023f) * Mathf.Pow(2f, -10f * k));

        public static float InOut(float k)
        {
            if (k > 1f) return k;
            var p = (k * 20f) - 10f;
            const float s = 512f / 1023f;
            if (p < 0f) return 0.5f - (s - (s * Mathf.Pow(2f, p)));
            return 0.5f + (s - (s * Mathf.Pow(2f, -p)));
        }
    }
}
