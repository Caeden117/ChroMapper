using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSPlaybackLatencyTest : GLSColorPlaybackTestBase
    {
        public enum PreviewKind { Color, Rotation, Translation, FloatFx }

        private AudioTimeSyncController atsc;
        private bool previousEnabled;
        private int previousSnapping;
        private GameObject transformRoot;
        private LightRotationGroupEffect rotation;
        private LightTranslationGroupEffect translation;
        private FloatFxGroupEffect floatFx;
        private Transform rotationTarget;
        private Transform translationTarget;
        private RecordingFxTarget fxTarget;

        protected override EditingMode InitialEditingMode => EditingMode.GLS;

        [SetUp]
        public void AttachPreviewClock()
        {
            atsc = playback.Atsc;
            previousEnabled = atsc.enabled;
            previousSnapping = atsc.GridMeasureSnapping;
            atsc.GridMeasureSnapping = 4;
            atsc.enabled = false;
            LoadPlayback(@"{
                ""version"":""3.3.0"",
                ""lightColorEventBoxGroups"":[{
                    ""b"":0,""g"":1,""e"":[{
                        ""f"":{""f"":1,""p"":1},
                        ""e"":[
                            {""b"":0,""c"":0,""s"":1,""i"":0,""customData"":{""color"":[0,0,1]}},
                            {""b"":2,""c"":0,""s"":1,""i"":0,""customData"":{""color"":[1,0,0]}}
                        ]
                    }]
                }]
            }");
            CreateTransformPreviews();
            atsc.OnTimeChanged += UpdatePreview;
        }

        [TearDown]
        public void DetachPreviewClock()
        {
            atsc.OnTimeChanged -= UpdatePreview;
            if (atsc.IsPlaying)
                atsc.CancelPlaying();

            atsc.StopScheduled = false;
            atsc.enabled = previousEnabled;
            atsc.GridMeasureSnapping = previousSnapping;
            Object.DestroyImmediate(transformRoot);
        }

        [Test]
        public void ResumeAtNodeBoundaryMatchesRefreshAtSameCompensatedTime(
            [Values] PreviewKind kind,
            [Values(0, 32)] int latency)
        {
            Settings.Instance.AudioLatencyCompensation = latency;
            atsc.MoveToJsonTime(2f);
            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            atsc.TogglePlaying();
            Assert.That(atsc.IsPlaying, Is.False);
            Assert.That(atsc.IsSnapped, Is.True);
            Assert.That(ReadPreview(kind), Is.GreaterThan(0.5f));
            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            Assert.That(atsc.CurrentSeconds, Is.EqualTo(1.2f - (latency / 1000f)).Within(0.00001f));
            var resumed = ReadPreview(kind);
            var time = atsc.CurrentSeconds;

            // Rebuilding a preview at the same clock position must not reveal a different cached state.
            playback.Refresh();
            rotation.Refresh();
            translation.Refresh();
            floatFx.Refresh();

            Assert.That(atsc.CurrentSeconds, Is.EqualTo(time));
            Assert.That(resumed, Is.EqualTo(ReadPreview(kind)).Within(0.00001f),
                "Resume kept the paused node's future state until a cache refresh corrected it to the latency-adjusted time.");
        }

        private float ReadPreview(PreviewKind kind) => kind switch
        {
            PreviewKind.Color => containers[0].Tween.Color.r,
            PreviewKind.Rotation => rotationTarget.localEulerAngles.x,
            PreviewKind.Translation => translationTarget.localPosition.x,
            _ => fxTarget.Value
        };

        [Test]
        public void LatencyPrerollBeforeFirstNodeMatchesRefreshedPreview()
        {
            Settings.Instance.AudioLatencyCompensation = 32;
            atsc.MoveToJsonTime(0f);
            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            var resumed = containers[0].Tween.Color;
            playback.Refresh();

            Assert.That(atsc.CurrentSeconds, Is.EqualTo(-0.032f).Within(0.00001f),
                "Preparing the cache must not clamp away the audio-compensation preroll.");
            Assert.That(resumed, Is.EqualTo(containers[0].Tween.Color),
                "The cached first node lit up before its latency-compensated start time.");
        }

        private void UpdatePreview()
        {
            playback.UpdateTime(atsc.IsPlaying, atsc.CurrentSongBpmTime);
            rotation.UpdateTime(atsc.IsPlaying, atsc.CurrentSongBpmTime);
            translation.UpdateTime(atsc.IsPlaying, atsc.CurrentSongBpmTime);
            floatFx.UpdateTime(atsc.IsPlaying, atsc.CurrentSongBpmTime);
        }

        private void CreateTransformPreviews()
        {
            transformRoot = new GameObject("Latency preview targets");
            transformRoot.SetActive(false);
            rotationTarget = new GameObject("Rotation target").transform;
            rotationTarget.SetParent(transformRoot.transform, false);
            translationTarget = new GameObject("Translation target").transform;
            translationTarget.SetParent(transformRoot.transform, false);

            rotation = transformRoot.AddComponent<LightRotationGroupEffect>();
            rotation.Atsc = atsc;
            rotation.Count = 1;
            rotation.Register(0, Axis.X, false, rotationTarget);
            rotation.Initialize();
            var rotationGroup = new BaseLightRotationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightRotationEventBox
                    {
                        Axis = (int)Axis.X,
                        IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                        Events = new[]
                        {
                            new BaseLightRotationBase { Easing = (int)EaseType.None },
                            new BaseLightRotationBase
                            {
                                RelativeJsonTime = 2f,
                                Rotation = 90f,
                                Easing = (int)EaseType.None
                            }
                        }
                    }
                }
            };
            rotationGroup.SetMap(map);
            rotationGroup.RecomputeSongBpmTime();
            rotation.InsertData(rotationGroup);

            translation = transformRoot.AddComponent<LightTranslationGroupEffect>();
            translation.Atsc = atsc;
            translation.Count = 1;
            translation.TranslationLimits = new[] { new Vector2(-1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, 1f) };
            translation.DistributionLimits = new[] { Vector2.zero, Vector2.zero, Vector2.zero };
            translation.Register(0, Axis.X, false, translationTarget);
            translation.Initialize();
            var translationGroup = new BaseLightTranslationEventBoxGroup
            {
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = (int)Axis.X,
                        IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                        Events = new[]
                        {
                            new BaseLightTranslationBase { Easing = (int)EaseType.None },
                            new BaseLightTranslationBase
                            {
                                RelativeJsonTime = 2f,
                                Translation = 1f,
                                Easing = (int)EaseType.None
                            }
                        }
                    }
                }
            };
            translationGroup.SetMap(map);
            translationGroup.RecomputeSongBpmTime();
            translation.InsertData(translationGroup);

            floatFx = transformRoot.AddComponent<FloatFxGroupEffect>();
            floatFx.Atsc = atsc;
            floatFx.Count = 1;
            floatFx.Trigger = false;
            fxTarget = transformRoot.AddComponent<RecordingFxTarget>();
            floatFx.Register(0, fxTarget);
            floatFx.Initialize();
            var fxGroup = new BaseVfxEventEventBoxGroup
            {
                Boxes =
                {
                    new BaseVfxEventEventBox
                    {
                        IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                        Events = new[]
                        {
                            new BaseFxEventFloat { Easing = (int)EaseType.None },
                            new BaseFxEventFloat
                            {
                                RelativeJsonTime = 2f,
                                Value = 1f,
                                Easing = (int)EaseType.None
                            }
                        }
                    }
                }
            };
            fxGroup.SetMap(map);
            fxGroup.RecomputeSongBpmTime();
            floatFx.InsertData(fxGroup);
        }

        private class RecordingFxTarget : FxTarget
        {
            public float Value;
            public override void SetValue(int group, int id, float value) => Value = value;
            public override void TriggerValue(int group, int id, float value)
            {
            }
        }
    }
}
