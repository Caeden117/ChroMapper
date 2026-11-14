using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Info;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TestsEditMode
{
    public class FireGroupEffectTest
    {
        private static readonly PropertyInfo songInstance = typeof(BeatSaberSongContainer)
            .GetProperty(nameof(BeatSaberSongContainer.Instance));
        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int startTimeId = Shader.PropertyToID("_StartTime");
        private static readonly int pointColorId = Shader.PropertyToID("_PrivatePointLightColor");
        private static readonly int emissionColorId = Shader.PropertyToID("_EmissionTexColor");
        private static readonly int writeMarkerId = Shader.PropertyToID("_FireTestWriteMarker");
        private static readonly FieldInfo currentBeatField = typeof(AudioTimeSyncController)
            .GetField("currentSongBpmTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly MaterialPropertyBlock block = new();
        private GameObject root;
        private BeatSaberSongContainer previousSong;
        private AudioTimeSyncController clock;
        private LightColorGroupEffectManager manager;
        private IBeatmapUpdate firePlayback;
        private ColorBoostEffect boost;
        private AudioClip clip;

        [SetUp]
        public void SetUp()
        {
            previousSong = BeatSaberSongContainer.Instance;
            root = new GameObject(nameof(FireGroupEffectTest));
            // Song/audio lifecycle stays dormant; samples are delivered explicitly by the fixture.
            var songObject = new GameObject("Dormant song and clock");
            songObject.SetActive(false);
            songObject.transform.SetParent(root.transform, false);
            var song = songObject.AddComponent<BeatSaberSongContainer>();
            song.Info = new BaseInfo { BeatsPerMinute = 120 };
            song.Map = new BaseDifficulty();
            song.Map.BootstrapBpmEvents(120);
            songInstance.SetValue(null, song);
            clock = songObject.AddComponent<AudioTimeSyncController>();
            clock.MapInfo = song.Info;
            clock.SongAudioSource = songObject.AddComponent<AudioSource>();
            clip = AudioClip.Create("Fire test song", 20000, 1, 1000, false);
            clock.SongAudioSource.clip = clip;
            manager = root.AddComponent<LightColorGroupEffectManager>();
            boost = root.AddComponent<ColorBoostEffect>();
        }

        [TearDown]
        public void TearDown()
        {
            songInstance.SetValue(null, previousSong);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(clip);
        }

        [Category("CMFirePreview")]
        [TestCase(false)]
        [TestCase(true)]
        public void PlaybackAndRepeatedReverseSeeksProduceTheSameRendererState(bool burst)
        {
            var fire = CreateFire(burst);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (6, 0, false), (10, 1, false), (14, 0, false));
            var beats = new[] { 0f, 1.99f, 2, 2.5f, 3, 3.5f, 4, 5.5f, 6, 9, 10, 10.5f, 11.5f, 12, 14, 39 };
            var played = new Dictionary<float, Snapshot>();
            foreach (var beat in beats)
            {
                firePlayback.UpdateTime(true, beat);
                played.Add(beat, Read(fire));
            }
            Assert.That(played[3].Visible, Is.True, "The playback fixture must actually show fire.");
            Assert.That(played[3].Flipbook.a, Is.GreaterThan(0));
            Assert.That(played[0].Visible, Is.False);
            Assert.That(played[39].Visible, Is.False);

            foreach (var beat in beats.Reverse().Concat(beats).Concat(beats.Reverse()))
            {
                firePlayback.UpdateTime(false, beat);
                Assert.That(Read(fire), Is.EqualTo(played[beat]), $"Paused seek to beat {beat}");
                firePlayback.UpdateTime(false, beat);
                Assert.That(Read(fire), Is.EqualTo(played[beat]), $"Repeated seek to beat {beat}");
            }
        }

        [Category("CMFirePreview")]
        [TestCase(1.99f, false, 0f)]
        [TestCase(2f, true, 0f)]
        [TestCase(2.5f, true, .75f)]
        [TestCase(3f, true, 1f)]
        [TestCase(5.5f, true, .5f)]
        [TestCase(6f, false, 0f)]
        public void ContinuousFireUsesSongSecondsForFadeAndVisibility(float beat, bool visible, float alpha)
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (6, 0, false));
            firePlayback.UpdateTime(false, beat);
            AssertFire(fire, visible, beat < 2 ? -1000f : 1f, alpha, alpha);
        }

        [Category("CMFirePreview")]
        [Test]
        public void ShortContinuousIntervalCombinesFadeInAndFadeOut()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (2.5f, 0, false));
            firePlayback.UpdateTime(false, 2.25f);
            AssertFire(fire, true, 1, .375f, .375f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void SustainCurvesContinueChangingWithoutRestartingTheFlipbook()
        {
            var fire = (ContinuousFireEffect)CreateFire(false);
            fire.FadeInDuration = fire.FadeOutDuration = .1f;
            fire.SustainDuration = 2;
            fire.FlipbookSustainCurve = AnimationCurve.Linear(0, 0, 1, 1);
            fire.BloomSustainCurve = AnimationCurve.Linear(0, .5f, 1, .5f);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (10, 0, false));
            firePlayback.UpdateTime(true, 4);
            AssertFire(fire, true, 1, .5f, .5f);
            firePlayback.UpdateTime(true, 5);
            AssertFire(fire, true, 1, .75f, .5f);
            firePlayback.UpdateTime(false, 4);
            AssertFire(fire, true, 1, .5f, .5f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void BpmChangesConvertAuthoredBeatsToFireSongSeconds()
        {
            var map = BeatSaberSongContainer.Instance.Map;
            map.BpmEvents.Add(new BaseBpmEvent(4, 60));
            map.BootstrapBpmEvents(120);
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (6, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, (float)map.JsonTimeToSongBpmTime(6.25f));
            AssertFire(fire, true, 4, .75f, .75f);
            firePlayback.UpdateTime(false, (float)map.JsonTimeToSongBpmTime(8));
            AssertFire(fire, false, 4, 0, 0);
        }

        [Category("CMFirePreview")]
        [Test]
        public void BurstIgnoresOffAndRetriggerUntilItsDurationEnds()
        {
            var fire = CreateFire(true);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (2.5f, 0, false), (3, 1, false), (4, 1, false));
            foreach (var beat in new[] { 2.5f, 3f, 3.5f, 4f, 4.5f, 6f, 3.5f, 4f })
            {
                firePlayback.UpdateTime(false, beat);
                var visible = beat < 4;
                var start = 1f;
                var alpha = visible ? 1f - (beat / 2f - start) : 0f;
                AssertFire(fire, visible, start, alpha, alpha);
            }
        }

        [Category("CMFirePreview")]
        [Test]
        public void FractionalBurstDeadlineTerminatesDirectAndReverseSeeks()
        {
            var fire = (BurstFireEffect)CreateFire(true);
            fire.FadeOutDuration = 1.3f;
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (32, 1, false));

            // 16s + 1.3s rounds down as a float. A recomputed Mono sum can remain
            // above that rounded deadline and leave preview completion looping forever.
            firePlayback.UpdateTime(false, 36);
            AssertFire(fire, false, 16, 0, 0);
            firePlayback.UpdateTime(false, 33.3f);
            AssertFire(fire, true, 16, .5f, .5f);
            firePlayback.UpdateTime(false, 36);
            AssertFire(fire, false, 16, 0, 0);
        }

        [Category("CMFirePreview")]
        [Test]
        public void BurstTargetsOnOneElementRetainTheirOwnRetriggerDurations()
        {
            var shortBurst = (BurstFireEffect)CreateFire(true);
            var longBurst = (BurstFireEffect)CreateFire(true);
            longBurst.FadeOutDuration = 2;
            var group = CreateGroup(0, shortBurst, longBurst);
            Initialize();
            AddEvents(group, (2, 1, false), (5, 1, false));
            firePlayback.UpdateTime(false, 5.5f);
            AssertFire(shortBurst, true, 2.5f, .75f, .75f);
            AssertFire(longBurst, true, 1, .125f, .125f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void RefreshRegroupsBurstTargetsAfterDurationEdit()
        {
            var first = (BurstFireEffect)CreateFire(true);
            var second = (BurstFireEffect)CreateFire(true);
            var group = CreateGroup(0, first, second);
            Initialize();
            AddEvents(group, (2, 1, false), (5, 1, false));
            firePlayback.UpdateTime(false, 5.5f);
            AssertFire(first, true, 2.5f, .75f, .75f);
            AssertFire(second, true, 2.5f, .75f, .75f);
            second.FadeOutDuration = 2;
            currentBeatField.SetValue(clock, 5.5f);
            group.Refresh();
            firePlayback.UpdateTime(false, clock.CurrentSongBpmTime);
            AssertFire(first, true, 2.5f, .75f, .75f);
            AssertFire(second, true, 1, .125f, .125f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void FirstUsePreviousKeepsAuthoredBrightnessAndNaturalEndKeepsRendererState()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, true), (4, 1, false));
            firePlayback.UpdateTime(false, 3);
            AssertFire(fire, true, 1, 1, 1);
            firePlayback.UpdateTime(false, 39);
            AssertFire(fire, true, 1, 0, 0);
            firePlayback.UpdateTime(false, 40);
            AssertFire(fire, true, 1, 0, 0);
        }

        [Category("CMFirePreview")]
        [Test]
        public void EmptyTrackStaysHiddenAfterInitializationAndRefresh()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            foreach (var beat in new[] { 0f, 12, 39, 0 })
            {
                firePlayback.UpdateTime(false, beat);
                AssertFire(fire, false, -1000, 0, 0);
            }
            group.Refresh();
            firePlayback.UpdateTime(false, clock.CurrentSongBpmTime);
            AssertFire(fire, false, -1000, 0, 0);
        }

        [Category("CMFirePreview")]
        [TestCase(false)]
        [TestCase(true)]
        public void SharedPointLightUsesCallbackWriteOrderAcrossGroupRegistrationOrder(bool reverseOrder)
        {
            var first = CreateFire(false);
            var second = CreateFire(false);
            second.PrivatePointLightPropertyBlockController = first.PrivatePointLightPropertyBlockController;
            first.PointLightColor = Color.red;
            second.PointLightColor = Color.blue;
            FireGroupEffect a, b;
            if (reverseOrder)
            {
                b = CreateGroup(1, second);
                a = CreateGroup(0, first);
            }
            else
            {
                a = CreateGroup(0, first);
                b = CreateGroup(1, second);
            }
            Initialize();
            AddEvents(a, (2, 1, false), (4, 0, false));
            AddEvents(b, (3, 1, false), (8, 0, false));
            foreach (var (beat, expected) in new[]
            {
                (2.5f, Color.red * .75f), (4f, Color.clear), (4.01f, Color.blue), (1f, Color.clear),
                (4f, Color.clear), (2.5f, Color.red * .75f)
            })
            {
                firePlayback.UpdateTime(false, beat);
                AssertColor(ReadPoint(first), expected);
            }
        }

        [Category("CMFirePreview")]
        [Test]
        public void SharedControllerKeepsEmissionAndPrivatePointLightChannelsIndependent()
        {
            var first = CreateFire(false);
            var second = CreateFire(false);
            second.UseEmissionColor = true;
            second.EmissionTextureColorPropertyBlockController = first.PrivatePointLightPropertyBlockController;
            first.PointLightColor = Color.red;
            second.PointLightColor = Color.blue;
            var a = CreateGroup(0, first);
            var b = CreateGroup(1, second);
            Initialize();
            AddEvents(a, (2, 1, false), (8, 0, false));
            AddEvents(b, (4, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, 5);
            AssertColor(ReadPoint(first), Color.red);
            AssertColor(ReadPoint(second), Color.blue);
        }

        [Category("CMFirePreview")]
        [TestCase(false)]
        [TestCase(true)]
        public void UnchangedOffAndFullBrightnessSkipWritesAndContributionCallbacks(bool visible)
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            var light = root.AddComponent<FireContributionProbe>();
            light.ID = 0;
            group.Register(light);
            light.Start();
            Initialize();
            AddEvents(group, (2, 1, false), (16, 0, false));
            var beat = visible ? 4f : 0f;
            firePlayback.UpdateTime(false, beat);
            AssertFire(fire, visible, visible ? 1f : -1000f, visible ? 1 : 0, visible ? 1 : 0);
            var calls = light.Calls;
            var controllers = new[] { fire.FlipBookPropertyBlockController, fire.BloomPropertyBlockController,
                fire.PrivatePointLightPropertyBlockController };
            foreach (var controller in controllers)
            {
                var renderer = controller.Renderers[0];
                renderer.GetPropertyBlock(block);
                // A subsequent ApplyChanges overwrites this renderer-only marker. No production counters are needed.
                block.SetFloat(writeMarkerId, 123);
                renderer.SetPropertyBlock(block);
            }
            for (var i = 0; i < 100; i++) firePlayback.UpdateTime(true, beat + i * .001f);
            Assert.That(light.Calls, Is.EqualTo(calls));
            foreach (var controller in controllers)
            {
                controller.Renderers[0].GetPropertyBlock(block);
                Assert.That(block.GetFloat(writeMarkerId), Is.EqualTo(123));
            }
            currentBeatField.SetValue(clock, beat);
            group.Refresh();
            firePlayback.UpdateTime(false, clock.CurrentSongBpmTime);
            Assert.That(light.Calls, Is.GreaterThan(calls), "Refresh must invalidate the contribution cache.");
            AssertFire(fire, visible, visible ? 1f : -1000f, visible ? 1 : 0, visible ? 1 : 0);
            foreach (var controller in controllers)
            {
                controller.Renderers[0].GetPropertyBlock(block);
                Assert.That(block.GetFloat(writeMarkerId), Is.Zero, "Refresh must reapply renderer state.");
            }

        }

        [Category("CMFirePreview")]
        [Test]
        public void RepeatedPausedSamplesRetainTheSharedCompletionWriterAndLiveColors()
        {
            var burst = (BurstFireEffect)CreateFire(true);
            var continuous = CreateFire(false);
            burst.BloomFadeOutCurve = AnimationCurve.Linear(0, 1, 1, .25f);
            burst.PointLightColor = Color.red;
            continuous.PointLightColor = Color.blue;
            continuous.PrivatePointLightPropertyBlockController = burst.PrivatePointLightPropertyBlockController;
            var a = CreateGroup(0, burst);
            var b = CreateGroup(1, continuous);
            Initialize();
            AddEvents(a, (2, 1, false));
            AddEvents(b, (2, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, 4);
            var completed = Read(burst);
            Assert.That(completed.Visible, Is.False);
            Assert.That(completed.BloomVisible, Is.False);
            Assert.That(completed.Start, Is.EqualTo(1));
            Assert.That(completed.Flipbook.a, Is.Zero);
            Assert.That(completed.Bloom.a, Is.EqualTo(.25f));
            AssertColor(completed.Contribution, burst.CustomLightColor.WithAlpha(0));
            AssertColor(ReadPoint(burst), Color.red * .25f);
            var renderer = burst.PrivatePointLightPropertyBlockController.Renderers[0];
            renderer.GetPropertyBlock(block);
            block.SetFloat(writeMarkerId, 123);
            renderer.SetPropertyBlock(block);
            for (var i = 0; i < 100; i++) firePlayback.UpdateTime(false, 4);
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetFloat(writeMarkerId), Is.EqualTo(123));
            AssertColor(ReadPoint(burst), Color.red * .25f);
            burst.PointLightColor = Color.green;
            firePlayback.UpdateTime(false, 4);
            AssertColor(ReadPoint(burst), Color.green * .25f);
            GameFrame(2); // A live frame lets the continuous writer take over after completion.
            AssertColor(ReadPoint(burst), Color.blue);
            firePlayback.UpdateTime(false, 4); // Pausing reconstructs the authored completion frame again.
            AssertColor(ReadPoint(burst), Color.green * .25f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void ContributionBeforeLightStartIsRetriedAfterInitialization()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            var light = root.AddComponent<FireContributionProbe>();
            light.ID = 0;
            group.Register(light);
            Initialize();
            AddEvents(group, (2, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, 4);
            Assert.That(light.HasInitialized, Is.False);
            AssertColor(light.DeliveredColor, Color.clear);
            light.Start();
            var calls = light.Calls;
            firePlayback.UpdateTime(false, 4);
            Assert.That(light.Calls, Is.EqualTo(calls + 1));
            AssertColor(light.DeliveredColor, fire.CustomLightColor);
        }

        [Category("CMFirePreview")]
        [Test]
        public void LiveColorAndBloomEditsReachRendererAndLighting()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, 4);
            fire.BloomIntensityMultiplier = 2.5f;
            fire.CustomLightColor = Color.cyan;
            fire.PointLightColor = Color.magenta;
            firePlayback.UpdateTime(false, 4);
            AssertFire(fire, true, 1, 1, 1);
            AssertColor(fire.ContributionColor, Color.cyan);
            AssertColor(ReadPoint(fire), Color.magenta);
        }

        [Category("CMFirePreview")]
        [Test]
        public void PausedContinuousFadeEditResamplesAtUnchangedTime()
        {
            var fire = (ContinuousFireEffect)CreateFire(false);
            fire.FadeInDuration = 1;
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (12, 0, false));
            firePlayback.UpdateTime(false, 3);
            AssertFire(fire, true, 1, .75f, .75f);

            fire.FadeInDuration = 2;
            firePlayback.UpdateTime(false, 3);
            AssertFire(fire, true, 1, .4375f, .4375f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void PausedCurveEditsResampleInPlaceAndAfterReplacement()
        {
            var fire = (ContinuousFireEffect)CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, 4);
            AssertFire(fire, true, 1, 1, 1);

            fire.FlipbookSustainCurve = AnimationCurve.Linear(0, .25f, 1, .25f);
            fire.BloomSustainCurve.MoveKey(1, new Keyframe(1, .75f));
            firePlayback.UpdateTime(false, 4);
            AssertFire(fire, true, 1, .25f, .75f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void PausedCurveWrapAndWeightedTangentEditsInvalidateTheCachedSample()
        {
            var fire = (ContinuousFireEffect)CreateFire(false);
            fire.FlipbookSustainCurve = AnimationCurve.Linear(0, 0, 1, 1);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (16, 0, false));
            firePlayback.UpdateTime(false, 5);
            AssertFire(fire, true, 1, 1, 1);

            fire.FlipbookSustainCurve.postWrapMode = WrapMode.Loop;
            firePlayback.UpdateTime(false, 5);
            AssertFire(fire, true, 1, .5f, 1);

            var key = fire.FlipbookSustainCurve[0];
            key.weightedMode = WeightedMode.Out;
            key.outWeight = .8f;
            key.outTangent = 0;
            fire.FlipbookSustainCurve.MoveKey(0, key);
            var expected = fire.FlipbookSustainCurve.Evaluate(1.5f);
            Assert.That(expected, Is.Not.EqualTo(.5f).Within(.001f));
            firePlayback.UpdateTime(false, 5);
            AssertFire(fire, true, 1, expected, 1);
        }

        [Category("CMFirePreview")]
        [Test]
        public void EditingOneGroupKeepsUnchangedGroupsAppliedState()
        {
            var first = CreateFire(false);
            var second = CreateFire(false);
            var a = CreateGroup(0, first);
            var b = CreateGroup(1, second);
            Initialize();
            var events = AddEvents(a, (2, 1, false), (16, 0, false));
            AddEvents(b, (2, 1, false), (16, 0, false));
            firePlayback.UpdateTime(false, 4);
            var controllers = new[] { second.FlipBookPropertyBlockController,
                second.BloomPropertyBlockController, second.PrivatePointLightPropertyBlockController };
            foreach (var controller in controllers)
            {
                controller.Renderers[0].GetPropertyBlock(block);
                block.SetFloat(writeMarkerId, 123);
                controller.Renderers[0].SetPropertyBlock(block);
            }

            manager.RemoveData(events, events);
            AssertFire(first, false, -1000, 0, 0);
            AssertFire(second, true, 1, 1, 1);
            foreach (var controller in controllers)
            {
                controller.Renderers[0].GetPropertyBlock(block);
                Assert.That(block.GetFloat(writeMarkerId), Is.EqualTo(123),
                    "An unrelated group edit must retain the already-applied renderer and point-light state.");
            }
            manager.InsertData(events);
            AssertFire(first, true, 1, 1, 1);
            AssertFire(second, true, 1, 1, 1);
        }

        [Category("CMFirePreview")]
        [TestCase(false)]
        [TestCase(true)]
        public void EqualTimeTriggersKeepContainerOrderAfterAnEventRebuild(bool finalOn)
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            var events = AddEvents(group, (2, finalOn ? 0 : 1, false),
                (2, finalOn ? 1 : 0, false), (8, 0, false));
            firePlayback.UpdateTime(false, 4);
            AssertFire(fire, finalOn, 1, finalOn ? 1 : 0, finalOn ? 1 : 0);

            manager.RemoveData(events, events);
            manager.InsertData(events);
            AssertFire(fire, finalOn, 1, finalOn ? 1 : 0, finalOn ? 1 : 0);
            firePlayback.UpdateTime(false, 1);
            firePlayback.UpdateTime(false, 4);
            AssertFire(fire, finalOn, 1, finalOn ? 1 : 0, finalOn ? 1 : 0);
        }

        [Category("CMFirePreview")]
        [Test]
        public void PausedBurstDurationEditReconsidersIgnoredTriggersAtUnchangedTime()
        {
            var fire = (BurstFireEffect)CreateFire(true);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (5, 1, false));
            firePlayback.UpdateTime(false, 5.5f);
            AssertFire(fire, true, 2.5f, .75f, .75f);

            fire.FadeOutDuration = 2;
            firePlayback.UpdateTime(false, 5.5f);
            AssertFire(fire, true, 1, .125f, .125f);
        }

        [Category("CMFirePreview")]
        [Test]
        public void EventRemovalAndReinsertionRebuildPausedFireImmediately()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            var events = AddEvents(group, (0, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, 0);
            Assert.That(Read(fire).Visible, Is.True);
            manager.RemoveData(events, events);
            AssertFire(fire, false, -1000, 0, 0);
            manager.InsertData(events);
            Assert.That(Read(fire).Visible, Is.True);
            events.Boxes[0].Events[0].Brightness = 0;
            manager.RemoveData(events, events);
            manager.InsertData(events);
            AssertFire(fire, false, -1000, 0, 0);
        }

        [Category("CMFirePreview")]
        [Test]
        public void ReinitializationDropsOldEventsAndAllowsNewPlayback()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (8, 0, false));
            firePlayback.UpdateTime(false, 4);
            Assert.That(Read(fire).Visible, Is.True);
            manager.Reinitialize();
            manager.Refresh();
            firePlayback.UpdateTime(false, 4);
            AssertFire(fire, false, -1000, 0, 0);
            AddEvents(group, (4, 1, false), (10, 0, false));
            firePlayback.UpdateTime(false, 6);
            AssertFire(fire, true, 2, 1, 1);
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145ContinuousPlaybackUsesAuthoredFadeAndSustainCurves()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (8, 0, false));

            foreach (var (seconds, alpha) in new[]
            {
                (1f, 0f), (1.25f, .75f), (1.5f, 1f), (3.5f, 1f), (3.75f, .5f), (4f, 0f)
            })
            {
                GameFrame(seconds);
                AssertFire(fire, seconds < 4, 1, alpha, alpha);
            }
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145SharedLightFollowsBurstLateWriteAndCompletionBeforeContinuousRestoresIt()
        {
            var burst = CreateFire(true);
            var continuous = CreateFire(false);
            burst.PointLightColor = Color.red;
            continuous.PointLightColor = Color.blue;
            burst.UseEmissionColor = continuous.UseEmissionColor = true;
            continuous.EmissionTextureColorPropertyBlockController = burst.EmissionTextureColorPropertyBlockController;
            var burstGroup = CreateGroup(0, burst);
            var continuousGroup = CreateGroup(1, continuous);
            Initialize();
            AddEvents(burstGroup, (2, 1, false));
            AddEvents(continuousGroup, (2, 1, false), (8, 0, false));

            GameFrame(0);
            GameFrame(1);
            AssertColor(ReadPoint(burst), Color.red);
            GameFrame(1.25f);
            AssertColor(ReadPoint(burst), Color.red * .75f);
            GameFrame(1.75f);
            AssertFire(continuous, true, 1, 1, 1);
            AssertColor(ReadPoint(burst), Color.red * .25f,
                "The dimmer burst writes after the brighter continuous fire.");
            GameFrame(2);
            AssertFire(burst, false, 1, 0, 0);
            AssertColor(ReadPoint(burst), Color.clear, "The completion-frame endpoint is the last write.");
            GameFrame(2.01f);
            AssertColor(ReadPoint(burst), Color.blue, "Continuous Update restores the light next frame.");
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145DelayedBurstUsesDeliveryTimeForTweenAndAuthoredTimeForShader()
        {
            var fire = CreateFire(true);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false));

            GameFrame(0);
            GameFrame(1.2f);
            AssertFire(fire, true, 1, 1, 1);
            GameFrame(1.7f);
            AssertFire(fire, true, 1, .5f, .5f);
            GameFrame(2.2f);
            AssertFire(fire, false, 1, 0, 0);
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145BurstIgnoresRetriggerOnCompletionFrameButAcceptsTheFollowingTrigger()
        {
            var fire = CreateFire(true);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (2.5f, 0, false), (4, 1, false), (4.2f, 1, false));

            GameFrame(0);
            GameFrame(1);
            GameFrame(1.25f);
            AssertFire(fire, true, 1, .75f, .75f);
            GameFrame(1.5f);
            GameFrame(2);
            AssertFire(fire, false, 1, 0, 0);
            GameFrame(2.1f);
            AssertFire(fire, true, 2.1f, 1, 1);
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145MultipleCallbacksBeforeFirstTweenAdvanceCanRestartTheBurst()
        {
            var fire = CreateFire(true);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (2.2f, 1, false));

            GameFrame(0);
            GameFrame(1.2f);
            AssertFire(fire, true, 1.1f, 1, 1);
            GameFrame(1.7f);
            AssertFire(fire, true, 1.1f, .5f, .5f);
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145ContinuousNaturalEndKeepsEnabledStateUntilExplicitOff()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (4, 1, false), (8, 0, false), (10, 1, false));

            GameFrame(0);
            GameFrame(1);
            GameFrame(1.5f);
            AssertFire(fire, true, 1, 1, 1);
            GameFrame(2);
            AssertFire(fire, true, 1, 0, 0);
            GameFrame(3);
            AssertFire(fire, true, 1, 0, 0);
            GameFrame(4);
            AssertFire(fire, false, 1, 0, 0);
            GameFrame(5);
            AssertFire(fire, true, 5, 0, 0);
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145FirstUsePreviousDoesNotReplaceAuthoredBrightnessWithSyntheticOff()
        {
            var fire = CreateFire(false);
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, true), (8, 0, false));

            GameFrame(0);
            GameFrame(1);
            GameFrame(1.5f);
            AssertFire(fire, true, 1, 1, 1);
        }

        [Category("BeatSaber145FireParity")]
        [TestCase(0f, 0f)]
        [TestCase(.5f, .75f)]
        public void Game145ZeroFadeInUsesInverseLerpEqualEndpointBehavior(float duration, float alpha)
        {
            var fire = (ContinuousFireEffect)CreateFire(false);
            fire.FadeInDuration = duration;
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false), (8, 0, false));

            GameFrame(0);
            GameFrame(1);
            GameFrame(1.25f);
            AssertFire(fire, true, 1, alpha, alpha);
        }

        [Category("BeatSaber145FireParity")]
        [Test]
        public void Game145BurstCompletionRetainsCurveEndpointsButClearsCustomContribution()
        {
            var fire = (BurstFireEffect)CreateFire(true);
            fire.FlipbookFadeOutCurve = AnimationCurve.Linear(0, 1, 1, .3f);
            fire.BloomFadeOutCurve = AnimationCurve.Linear(0, 1, 1, .2f);
            fire.BloomIntensityMultiplier = 2;
            var group = CreateGroup(0, fire);
            Initialize();
            AddEvents(group, (2, 1, false));

            GameFrame(0);
            GameFrame(1);
            GameFrame(1.5f);
            GameFrame(2);
            var state = Read(fire);
            Assert.That(state.Visible, Is.False);
            Assert.That(state.BloomVisible, Is.False);
            Assert.That(state.Start, Is.EqualTo(1));
            Assert.That(state.Flipbook.a, Is.EqualTo(.3f).Within(.0001f));
            Assert.That(state.Bloom.a, Is.EqualTo(.4f).Within(.0001f));
            AssertColor(ReadPoint(fire), Color.yellow * .2f);
            AssertColor(state.Contribution, fire.CustomLightColor.WithAlpha(0));
        }

        private void GameFrame(float seconds)
        {
            var beat = clock.GetBeatFromSeconds(seconds);
            currentBeatField.SetValue(clock, beat);
            firePlayback.UpdateTime(true, beat);
        }

        private FireEffect CreateFire(bool burst)
        {
            var go = new GameObject(burst ? "Burst fire" : "Continuous fire");
            go.transform.SetParent(root.transform, false);
            var fire = (FireEffect)go.AddComponent(burst ? typeof(BurstFireEffect) : typeof(ContinuousFireEffect));
            fire.FlipBookPropertyBlockController = CreateController(go, "Flipbook");
            fire.BloomPropertyBlockController = CreateController(go, "Bloom");
            fire.PrivatePointLightPropertyBlockController = CreateController(go, "Point light");
            fire.EmissionTextureColorPropertyBlockController = fire.PrivatePointLightPropertyBlockController;
            fire.BloomPrePassRenderer = fire.BloomPropertyBlockController.gameObject
                .AddComponent<BloomPrePassBackgroundNonLightRenderer>();
            fire.BloomPrePassRenderer.Renderer = fire.BloomPropertyBlockController.Renderers[0];
            fire.BloomPrePassRenderer.Renderer.enabled = false;
            fire.PointLightColor = Color.yellow;
            fire.CustomLightColor = new Color(.2f, .4f, .8f, 1);
            if (fire is ContinuousFireEffect continuous)
            {
                continuous.FadeInDuration = .5f;
                continuous.FadeOutDuration = .5f;
            }
            else if (fire is BurstFireEffect burstFire)
            {
                burstFire.FadeOutDuration = 1;
                burstFire.BloomFadeOutCurve = AnimationCurve.Linear(0, 1, 1, 0);
            }
            return fire;
        }

        private static MaterialPropertyBlockController CreateController(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var controller = go.AddComponent<MaterialPropertyBlockController>();
            controller.Renderers.Add(go.AddComponent<MeshRenderer>());
            return controller;
        }

        private FireGroupEffect CreateGroup(int id, params FireEffect[] fires)
        {
            var group = root.AddComponent<FireGroupEffect>();
            group.EffectManager = manager;
            group.ID = id;
            group.Count = 1;
            group.Atsc = clock;
            group.ColorBoostEffect = boost;
            foreach (var fire in fires) group.Register(0, fire);
            manager.IdToEffect.Add(id, group);
            return group;
        }

        private void Initialize()
        {
            manager.Initialize(clock);
            firePlayback = manager.IdToEffect.Values.OfType<FireGroupEffect>().First();
            firePlayback.Refresh();
            firePlayback.UpdateTime(false, clock.CurrentSongBpmTime);
        }

        private BaseLightColorEventBoxGroup AddEvents(FireGroupEffect group,
            params (float beat, float brightness, bool usePrevious)[] events)
        {
            var data = new BaseLightColorEventBoxGroup
            {
                ID = group.ID,
                JsonTime = 0,
                Boxes = new List<BaseLightColorEventBox>
                {
                    new()
                    {
                        IndexFilter = new BaseIndexFilter(),
                        Events = events.Select(evt => new BaseLightColorBase
                        {
                            RelativeJsonTime = evt.beat,
                            Brightness = evt.brightness,
                            UsePrevious = evt.usePrevious ? 1 : 0
                        }).ToArray()
                    }
                }
            };
            Assert.That(manager.InsertData(data), Is.True);
            firePlayback.UpdateTime(false, clock.CurrentSongBpmTime);
            return data;
        }

        private Snapshot Read(FireEffect fire)
        {
            fire.FlipBookPropertyBlockController.Renderers[0].GetPropertyBlock(block);
            var start = block.GetFloat(startTimeId);
            var flipbook = block.GetColor(colorId);
            fire.BloomPropertyBlockController.Renderers[0].GetPropertyBlock(block);
            return new Snapshot(fire.FlipBookPropertyBlockController.Renderers[0].enabled,
                fire.BloomPrePassRenderer.enabled, start, flipbook, block.GetColor(colorId),
                ReadPoint(fire), fire.ContributionColor);
        }

        private Color ReadPoint(FireEffect fire)
        {
            var controller = fire.UseEmissionColor ? fire.EmissionTextureColorPropertyBlockController
                : fire.PrivatePointLightPropertyBlockController;
            controller.Renderers[0].GetPropertyBlock(block);
            return block.GetColor(fire.UseEmissionColor ? emissionColorId : pointColorId);
        }

        private void AssertFire(FireEffect fire, bool visible, float start, float flipbookAlpha, float bloomAlpha)
        {
            var state = Read(fire);
            Assert.That(state.Visible, Is.EqualTo(visible));
            Assert.That(state.BloomVisible, Is.EqualTo(visible));
            Assert.That(state.Start, Is.EqualTo(start).Within(.0001f));
            Assert.That(state.Flipbook.a, Is.EqualTo(flipbookAlpha).Within(.0001f));
            Assert.That(state.Bloom.a, Is.EqualTo(bloomAlpha * fire.BloomIntensityMultiplier).Within(.0001f));
            AssertColor(state.Contribution, fire.CustomLightColor.WithAlpha(bloomAlpha));
        }

        private static void AssertColor(Color actual, Color expected, string message = null)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.0001f), message);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.0001f), message);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.0001f), message);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.0001f), message);
        }

        private readonly struct Snapshot
        {
            public readonly bool Visible;
            public readonly bool BloomVisible;
            public readonly float Start;
            public readonly Color Flipbook;
            public readonly Color Bloom;
            public readonly Color Point;
            public readonly Color Contribution;

            public Snapshot(bool visible, bool bloomVisible, float start, Color flipbook, Color bloom,
                Color point, Color contribution)
            {
                Visible = visible;
                BloomVisible = bloomVisible;
                Start = start;
                Flipbook = flipbook;
                Bloom = bloom;
                Point = point;
                Contribution = contribution;
            }
        }
    }

    public class FireContributionProbe : LightController
    {
        public int Calls;
        public Color DeliveredColor;
        protected override bool Initialize() => true;
        public override void SetColor(Color color)
        {
            Calls++;
            if (HasInitialized) DeliveredColor = color;
        }
    }
}
