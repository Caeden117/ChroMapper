using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beatmap.Animations
{
    // Resolves base* point values from the editor's current camera, colors, song time, and movement settings.
    internal static class BaseProviderManager
    {
        private static readonly Dictionary<string, IPointDefinition.IValueSegment> BaseSegments = BuildSegments();
        private static readonly Dictionary<string, IPointDefinition.IValueSegment> ResolvedSegments = new();
        private static readonly List<SmoothedValues> Smoothed = new();
        private static int lastTickFrame = -1;
        private static CameraManager boundCameraManager;
        private static VariableNJSProvider variableNjsProvider;

        // Advance smoothing once per frame, even when point definitions are reused.
        internal static void Tick()
        {
            if (Time.frameCount == lastTickFrame) return;
            lastTickFrame = Time.frameCount;
            foreach (var segment in Smoothed)
            {
                segment.Tick();
            }
        }

        // Bind scene references once so evaluating points does not search for objects each frame.
        internal static void BindSceneReferences(CameraManager cameraManager, VariableNJSProvider njsProvider)
        {
            boundCameraManager = cameraManager;
            variableNjsProvider = njsProvider;
        }

        // Discard cached provider chains on map load; camera and movement references belong to the scene.
        internal static void ResetForMapLoad()
        {
            ResolvedSegments.Clear();
            Smoothed.Clear();
            lastTickFrame = Time.frameCount;
        }

        // Unknown providers are logged and skipped rather than aborting map loading.
        internal static IPointDefinition.IValueSegment Resolve(string key)
        {
            if (BaseSegments.TryGetValue(key, out var segment)) return segment;
            if (ResolvedSegments.TryGetValue(key, out segment)) return segment;

            var dot = key.IndexOf('.');
            if (dot <= 0 || !BaseSegments.TryGetValue(key[..dot], out var source))
            {
                Debug.LogError($"Could not find base provider [{key}]; the point was skipped.");
                return null;
            }

            IPointDefinition.IValueSegment current = source;
            var remaining = key[(dot + 1)..];
            while (remaining.Length > 0)
            {
                var nextDot = remaining.IndexOf('.');
                var suffix = nextDot < 0 ? remaining : remaining[..nextDot];
                if (suffix.Length > 1 && suffix[0] == 's'
                    && float.TryParse(suffix[1..].Replace('_', '.'), out var smoothMult))
                {
                    // The .s suffix uses underscores for decimal places. Cache each chain so its points share smoothing state.
                    var smoothedSegment = new SmoothedValues(current, smoothMult);
                    Smoothed.Add(smoothedSegment);
                    current = smoothedSegment;
                }
                else
                {
                    var indices = new int[suffix.Length];
                    for (var i = 0; i < suffix.Length; ++i)
                    {
                        indices[i] = suffix[i] switch
                        {
                            'x' => 0,
                            'y' => 1,
                            'z' => 2,
                            'w' => 3,
                            _ => -1
                        };
                        if (indices[i] < 0 || indices[i] >= current.Dimension)
                        {
                            Debug.LogError($"Base provider [{key}] has an invalid swizzle [{suffix}]; the point was skipped.");
                            return null;
                        }
                    }

                    current = new SwizzledValues(current, indices);
                }

                remaining = nextDot < 0 ? string.Empty : remaining[(nextDot + 1)..];
            }

            ResolvedSegments[key] = current;
            return current;
        }

        private static Transform HeadTransform()
        {
            // Head values follow the selected editing or playback camera.
            var controller = boundCameraManager != null ? boundCameraManager.SelectedCameraController : null;
            var camera = controller != null ? controller.Camera : null;
            return camera != null ? camera.transform : null;
        }

        private static AudioTimeSyncController Atsc => AudioTimeSyncController.Instance;

        private static float SongLength => BeatSaberSongContainer.Instance.LoadedSong.length;

        private static float NoteJumpStartBeatOffset => BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteStartBeatOffset;

        private static float NoteJumpSpeed =>
            variableNjsProvider != null
                ? variableNjsProvider.NoteJumpSpeed
                : BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;

        private static float JumpDistance =>
            variableNjsProvider != null ? variableNjsProvider.JumpDistance : 0f;

        private static IPointDefinition.IValueSegment Live(int dimension, Action<float[], int> append) =>
            new LiveValues(dimension, append);

        private static Dictionary<string, IPointDefinition.IValueSegment> BuildSegments()
        {
            // The editor has no VR controllers, so hand providers return zero.
            var zero3 = Live(3, WriteZeros(3));
            var zero1 = Live(1, WriteZeros(1));
            return new Dictionary<string, IPointDefinition.IValueSegment>
            {
                ["baseHeadPosition"] = Live(3, WriteHeadPosition),
                ["baseHeadLocalPosition"] = Live(3, WriteHeadLocalPosition),
                ["baseHeadRotation"] = Live(3, WriteHeadRotation),
                ["baseHeadLocalRotation"] = Live(3, WriteHeadLocalRotation),
                ["baseHeadLocalScale"] = Live(3, WriteHeadLocalScale),
                ["baseLeftHandPosition"] = zero3,
                ["baseLeftHandLocalPosition"] = zero3,
                ["baseLeftHandRotation"] = zero3,
                ["baseLeftHandLocalRotation"] = zero3,
                ["baseLeftHandLocalScale"] = zero3,
                ["baseRightHandPosition"] = zero3,
                ["baseRightHandLocalPosition"] = zero3,
                ["baseRightHandRotation"] = zero3,
                ["baseRightHandLocalRotation"] = zero3,
                ["baseRightHandLocalScale"] = zero3,

                // Read the original scheme colors, before the editor mirrors note colors for left-handed mode.
                ["baseNote0Color"] = Live(4, (t, o) => WriteSchemeColor(scheme => scheme.LeftNoteColor, t, o)),
                ["baseNote1Color"] = Live(4, (t, o) => WriteSchemeColor(s => s.RightNoteColor, t, o)),
                ["baseObstaclesColor"] = Live(4, (t, o) => WriteSchemeColor(s => s.ObstacleColor, t, o)),
                ["baseSaberAColor"] = Live(4, (t, o) => WriteSchemeColor(s => s.LeftNoteColor, t, o)),
                ["baseSaberBColor"] = Live(4, (t, o) => WriteSchemeColor(s => s.RightNoteColor, t, o)),
                ["baseEnvironmentColor0"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentLeftColor, t, o)),
                ["baseEnvironmentColor1"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentRightColor, t, o)),
                ["baseEnvironmentColorW"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentWhiteColor, t, o)),
                ["baseEnvironmentColor0Boost"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentLeftBoostColor, t, o)),
                ["baseEnvironmentColor1Boost"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentRightBoostColor, t, o)),
                ["baseEnvironmentColorWBoost"] = Live(4, (t, o) => WriteSchemeColor(s => s.EnvironmentWhiteBoostColor, t, o)),

                // The preview has no gameplay score. Keep score values at their initial state while song time stays live.
                ["baseCombo"] = zero1,
                ["baseMultipliedScore"] = zero1,
                ["baseImmediateMaxPossibleMultipliedScore"] = zero1,
                ["baseModifiedScore"] = zero1,
                ["baseImmediateMaxPossibleModifiedScore"] = zero1,
                ["baseRelativeScore"] = zero1,
                ["baseMultiplier"] = Live(1, (t, o) => t[o] = 1f),
                ["baseEnergy"] = Live(1, (t, o) => t[o] = 1f),
                ["baseSongTime"] = Live(1, (t, o) => t[o] = Atsc != null ? Atsc.CurrentSeconds : 0f),
                ["baseSongLength"] = Live(1, (t, o) => t[o] = SongLength),

                ["baseNoteJumpMovementSpeed"] = Live(1, (t, o) => t[o] = NoteJumpSpeed),
                ["baseNoteJumpStartBeatOffset"] = Live(1, (t, o) => t[o] = NoteJumpStartBeatOffset),
                ["baseJumpDistance"] = Live(1, (t, o) => t[o] = JumpDistance),
                ["basePlayerHeight"] = zero1
            };
        }

        private static void WriteHeadPosition(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) Copy(head.position, target, offset);
        }

        private static void WriteHeadLocalPosition(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) Copy(head.localPosition, target, offset);
        }

        private static void WriteHeadRotation(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) CopyEuler(head.rotation, target, offset);
        }

        private static void WriteHeadLocalRotation(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) CopyEuler(head.localRotation, target, offset);
        }

        private static void WriteHeadLocalScale(float[] target, int offset)
        {
            var head = HeadTransform();
            if (head != null) Copy(head.localScale, target, offset);
        }

        private static void WriteSchemeColor(Func<ColorSchemeSO, Color> selector, float[] target, int offset)
        {
            var scheme = PointDataParsers.ColorScheme;
            WriteColor(scheme == null ? DefaultColors.White : selector(scheme), target, offset);
        }

        private static void Copy(Vector3? value, float[] target, int offset)
        {
            if (value.HasValue)
            {
                target[offset] = value.Value.x;
                target[offset + 1] = value.Value.y;
                target[offset + 2] = value.Value.z;
            }
        }

        private static void CopyEuler(Quaternion? rotation, float[] target, int offset)
        {
            if (rotation.HasValue)
            {
                var euler = rotation.Value.eulerAngles;
                target[offset] = euler.x;
                target[offset + 1] = euler.y;
                target[offset + 2] = euler.z;
            }
        }

        private static void WriteColor(Color? color, float[] target, int offset)
        {
            var value = color ?? DefaultColors.White;
            target[offset] = value.r;
            target[offset + 1] = value.g;
            target[offset + 2] = value.b;
            target[offset + 3] = value.a;
        }

        private static Action<float[], int> WriteZeros(int count)
        {
            void Write(float[] target, int offset)
            {
                for (var i = 0; i < count; ++i)
                {
                    target[offset + i] = 0f;
                }
            }

            return Write;
        }

        private sealed class LiveValues : IPointDefinition.IValueSegment
        {
            private readonly int dimension;
            private readonly Action<float[], int> append;

            internal LiveValues(int dimension, Action<float[], int> append)
            {
                this.dimension = dimension;
                this.append = append;
            }

            public int Dimension => dimension;

            public void Append(float[] target, ref int offset)
            {
                append(target, offset);
                offset += dimension;
            }
        }

        private sealed class SwizzledValues : IPointDefinition.IValueSegment
        {
            private readonly IPointDefinition.IValueSegment source;
            private readonly int[] parts;
            private readonly float[] scratch;

            internal SwizzledValues(IPointDefinition.IValueSegment source, int[] parts)
            {
                this.source = source;
                this.parts = parts;
                scratch = new float[source.Dimension];
            }

            public int Dimension => parts.Length;

            public void Append(float[] target, ref int offset)
            {
                var innerOffset = 0;
                source.Append(scratch, ref innerOffset);
                for (var i = 0; i < parts.Length; ++i)
                {
                    target[offset + i] = scratch[parts[i]];
                }

                offset += parts.Length;
            }
        }

        internal sealed class SmoothedValues : IPointDefinition.IValueSegment
        {
            private readonly IPointDefinition.IValueSegment source;
            private readonly float mult;
            private readonly float[] state;
            private readonly float[] scratch;

            internal SmoothedValues(IPointDefinition.IValueSegment source, float mult)
            {
                this.source = source;
                this.mult = mult;
                state = new float[source.Dimension];
                scratch = new float[source.Dimension];
            }

            public int Dimension => state.Length;

            public void Append(float[] target, ref int offset)
            {
                for (var i = 0; i < state.Length; ++i)
                {
                    target[offset + i] = state[i];
                }

                offset += state.Length;
            }

            internal void Tick()
            {
                var delta = Time.deltaTime * mult;
                var offset = 0;
                source.Append(scratch, ref offset);
                for (var i = 0; i < state.Length; ++i)
                {
                    state[i] = Mathf.Lerp(state[i], scratch[i], delta);
                }
            }
        }
    }
}
