using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Beatmap.Animations
{
    internal static class BaseColorProviderKeys
    {
        internal const string BaseNote0ColorKey = "baseNote0Color";
        internal const string BaseNote1ColorKey = "baseNote1Color";
        internal const string BaseObstaclesColorKey = "baseObstaclesColor";
        internal const string BaseSaberAColorKey = "baseSaberAColor";
        internal const string BaseSaberBColorKey = "baseSaberBColor";
        internal const string BaseEnvironmentColor0Key = "baseEnvironmentColor0";
        internal const string BaseEnvironmentColor1Key = "baseEnvironmentColor1";
        internal const string BaseEnvironmentColorWKey = "baseEnvironmentColorW";
        internal const string BaseEnvironmentColor0BoostKey = "baseEnvironmentColor0Boost";
        internal const string BaseEnvironmentColor1BoostKey = "baseEnvironmentColor1Boost";
        internal const string BaseEnvironmentColorWBoostKey = "baseEnvironmentColorWBoost";
    }

    // Owns live base values and their shared sampling state for every animation consumer.
    internal static class BaseProviderManager
    {
        private static readonly Dictionary<string, ProviderSource> providerSources = new();
        private static readonly Dictionary<string, ProviderValue> providerValues = new();
        private static CameraManager cameraManager;
        private static VariableNJSProvider variableNjsProvider;
        private static AudioTimeSyncController audioTimeSyncController;
        private static int sampleFrame = -1;
        private static float sampleTime;
        private static bool sampleReset;
        internal static int SampleVersion { get; private set; }
        internal static bool ResetSmoothing => sampleReset;

        // Providers share the same clock and smoothing state across typed and untyped curves.
        internal static void BeginSample(float time, bool seek = false)
        {
            if (!seek && sampleFrame == Time.frameCount && sampleTime == time) return;
            sampleFrame = Time.frameCount;
            sampleTime = time;
            sampleReset = seek;
            ++SampleVersion;
            foreach (var source in providerSources.Values) source.Sample();
            foreach (var value in providerValues.Values) value.Sample(seek);
        }

        internal static void BeginSample()
        {
            if (sampleFrame != Time.frameCount)
                BeginSample(audioTimeSyncController != null ? audioTimeSyncController.CurrentJsonTime : sampleTime);
        }

        internal static void Tick() => BeginSample();

        internal static void BindSceneReferences(CameraManager cameras, VariableNJSProvider movement,
            AudioTimeSyncController audioTime = null)
        {
            cameraManager = cameras;
            variableNjsProvider = movement;
            audioTimeSyncController = audioTime;
        }

        // New smoothing chains begin at their native default. Explicit seeks still reset held inputs.
        internal static void ResetForMapLoad()
        {
            providerSources.Clear();
            providerValues.Clear();
            sampleFrame = Time.frameCount;
            sampleTime = audioTimeSyncController != null ? audioTimeSyncController.CurrentJsonTime : 0f;
            sampleReset = false;
            ++SampleVersion;
        }

        internal static IEnumerable<Func<float>> GetBaseGetter(string modifier)
        {
            var parts = modifier.Split('.');
            var name = parts[0];
            if (!providerSources.TryGetValue(name, out var source))
            {
                source = new ProviderSource(name);
                providerSources.Add(name, source);
                source.Sample();
            }
            if (!providerValues.TryGetValue(name, out var provider))
                providerValues.Add(name, provider = new ProviderValue(source));
            var prefix = name;
            for (var part = 1; part < parts.Length; part++)
            {
                var suffix = parts[part];
                // Retain decimal .s0.5 syntax while accepting Heck's .s0_5 form and chained suffixes.
                if (suffix.Length > 1 && suffix[0] == 's' && part + 1 < parts.Length
                    && parts[part + 1].Length > 0 && parts[part + 1].All(char.IsDigit))
                    suffix += "." + parts[++part];
                prefix += "." + suffix;
                if (providerValues.TryGetValue(prefix, out var resolved))
                {
                    provider = resolved;
                    continue;
                }
                if (suffix.Length > 1 && suffix[0] == 's'
                    && float.TryParse(suffix.Substring(1).Replace('_', '.'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var factor))
                    resolved = new ProviderValue(provider, factor);
                else
                {
                    var indices = new int[suffix.Length];
                    if (indices.Length == 0) throw new FormatException($"Base provider [{modifier}] has an empty suffix.");
                    for (var i = 0; i < indices.Length; i++)
                    {
                        indices[i] = "xyzw".IndexOf(suffix[i]);
                        if (indices[i] < 0 || indices[i] >= provider.Dimension)
                            throw new FormatException($"Base provider [{modifier}] has an invalid swizzle [{suffix}].");
                    }
                    resolved = new ProviderValue(provider, indices);
                }
                providerValues.Add(prefix, resolved);
                provider = resolved;
            }
            for (var component = 0; component < provider.Dimension; component++)
            {
                var index = component;
                yield return () =>
                {
                    BeginSample();
                    provider.Sample(sampleReset);
                    return provider.Value[index];
                };
            }
        }

        private sealed class ProviderSource
        {
            private readonly string name;
            public readonly int Dimension;
            public Vector4 Value;

            public ProviderSource(string name)
            {
                this.name = name;
                Dimension = name switch
                {
                    "baseHeadPosition" or "baseHeadLocalPosition" or "baseHeadRotation" or "baseHeadLocalRotation"
                        or "baseHeadLocalScale" or "baseLeftHandPosition" or "baseLeftHandLocalPosition"
                        or "baseLeftHandRotation" or "baseLeftHandLocalRotation" or "baseLeftHandLocalScale"
                        or "baseRightHandPosition" or "baseRightHandLocalPosition" or "baseRightHandRotation"
                        or "baseRightHandLocalRotation" or "baseRightHandLocalScale" => 3,
                    "baseNote0Color" or "baseNote1Color" or "baseSaberAColor" or "baseSaberBColor"
                        or "baseObstaclesColor" or "baseEnvironmentColor0" or "baseEnvironmentColor1"
                        or "baseEnvironmentColorW" or "baseEnvironmentColor0Boost" or "baseEnvironmentColor1Boost"
                        or "baseEnvironmentColorWBoost" => 4,
                    "baseCombo" or "baseMultipliedScore" or "baseImmediateMaxPossibleMultipliedScore"
                        or "baseModifiedScore" or "baseImmediateMaxPossibleModifiedScore" or "baseRelativeScore"
                        or "baseMultiplier" or "baseEnergy" or "baseSongTime" or "baseSongLength"
                        or "baseNoteJumpMovementSpeed" or "baseNoteJumpStartBeatOffset" or "baseJumpDistance"
                        or "basePlayerHeight" => 1,
                    _ => throw new FormatException($"Could not find base provider [{name}].")
                };
            }

            public void Sample()
            {
                var controller = cameraManager != null ? cameraManager.SelectedCameraController : null;
                var camera = controller != null ? controller.Camera : Camera.main;
                var transform = camera != null ? camera.transform : null;
                var colorScheme = PointDataParsers.ColorScheme;
                var song = BeatSaberSongContainer.Instance;
                var difficulty = song != null ? song.MapDifficultyInfo : null;
                var seconds = audioTimeSyncController != null ? audioTimeSyncController.CurrentSeconds : 0f;
                if (song != null && song.Map != null && song.Info != null && song.Info.BeatsPerMinute > 0)
                    seconds = (song.Map.JsonTimeToSongBpmTime(sampleTime) ?? sampleTime) * 60f / song.Info.BeatsPerMinute;
                Value = name switch
                {
                    "baseHeadPosition" => transform != null ? transform.position : Vector3.zero,
                    "baseHeadLocalPosition" => transform != null ? transform.localPosition : Vector3.zero,
                    "baseHeadRotation" => transform != null ? transform.eulerAngles : Vector3.zero,
                    "baseHeadLocalRotation" => transform != null ? transform.localEulerAngles : Vector3.zero,
                    "baseHeadLocalScale" => transform != null ? transform.localScale : Vector3.one,
                    "baseNote0Color" or "baseSaberAColor" => colorScheme != null ? colorScheme.LeftNoteColor : Color.white,
                    "baseNote1Color" or "baseSaberBColor" => colorScheme != null ? colorScheme.RightNoteColor : Color.white,
                    "baseObstaclesColor" => colorScheme != null ? colorScheme.ObstacleColor : Color.white,
                    "baseEnvironmentColor0" => colorScheme != null ? colorScheme.EnvironmentLeftColor : Color.white,
                    "baseEnvironmentColor1" => colorScheme != null ? colorScheme.EnvironmentRightColor : Color.white,
                    "baseEnvironmentColorW" => colorScheme != null ? colorScheme.EnvironmentWhiteColor : Color.white,
                    "baseEnvironmentColor0Boost" => colorScheme != null ? colorScheme.EnvironmentLeftBoostColor : Color.white,
                    "baseEnvironmentColor1Boost" => colorScheme != null ? colorScheme.EnvironmentRightBoostColor : Color.white,
                    "baseEnvironmentColorWBoost" => colorScheme != null ? colorScheme.EnvironmentWhiteBoostColor : Color.white,
                    "baseMultiplier" or "baseEnergy" => new Vector4(1, 0, 0, 0),
                    "baseSongTime" => new Vector4(seconds, 0, 0, 0),
                    "baseSongLength" => new Vector4(song != null && song.LoadedSong != null ? song.LoadedSong.length : 0, 0, 0, 0),
                    "baseNoteJumpMovementSpeed" => new Vector4(variableNjsProvider != null ? variableNjsProvider.NoteJumpSpeed
                        : difficulty != null ? difficulty.NoteJumpSpeed : 0, 0, 0, 0),
                    "baseNoteJumpStartBeatOffset" => new Vector4(difficulty != null ? difficulty.NoteStartBeatOffset : 0, 0, 0, 0),
                    "baseJumpDistance" => new Vector4(variableNjsProvider != null ? variableNjsProvider.JumpDistance : 0, 0, 0, 0),
                    // The preview has no gameplay hands or score.
                    _ => Vector4.zero
                };
            }
        }

        private sealed class ProviderValue
        {
            private readonly ProviderSource source;
            private readonly ProviderValue parent;
            private readonly int[] swizzle;
            private readonly float? smoothing;
            private int sampledVersion = -1;
            public readonly float[] Value;
            public int Dimension => Value.Length;

            public ProviderValue(ProviderSource source)
            {
                this.source = source;
                Value = new float[source.Dimension];
            }

            public ProviderValue(ProviderValue parent, int[] swizzle)
            {
                this.parent = parent;
                this.swizzle = swizzle;
                Value = new float[swizzle.Length];
            }

            public ProviderValue(ProviderValue parent, float smoothing)
            {
                this.parent = parent;
                this.smoothing = smoothing;
                Value = new float[parent.Dimension];
            }

            public void Sample(bool reset)
            {
                if (sampledVersion == SampleVersion) return;
                sampledVersion = SampleVersion;
                parent?.Sample(reset);
                for (var i = 0; i < Value.Length; i++)
                {
                    var input = source != null ? source.Value[i] : parent.Value[swizzle != null ? swizzle[i] : i];
                    // A seek cannot reconstruct the user's prior camera movement. Reset its held input;
                    // ordinary playback starts new smoothing chains at zero, matching Heck.
                    Value[i] = smoothing.HasValue && !reset
                        ? Mathf.Lerp(Value[i], input, smoothing.Value * TimeHelper.DeltaTime)
                        : input;
                }
            }
        }
    }
}
