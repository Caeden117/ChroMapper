using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class BeatmapV3OptionalParamTestEditMode
    {
        // For use in PlayMode
        public void TestEverything()
        {
            V3BpmEventTest();
            V3RotationEventTest();
            V3ColorNoteTest();
            V3BombNoteTest();
            V3ObstacleTest();
            V3ArcTest();
            V3ChainTest();
            V3BasicEventTest();
            V3ColorBoostEventTest();
            V3LightColorEventBoxGroupTest();
            V3LightColorBaseTest();
            V3LightRotationEventBoxGroupTest();
            V3LightRotationBaseTest();
            V3LightTranslationEventBoxGroupTest();
            V3LightTranslationBaseTest();
            V3VfxEventEventBoxGroupTest();
            V3FxEventsCollectionTest();
            V3IndexFilterTest();
        }

        [Test]
        public void V3BpmEventTest()
        {
            Assert.Throws<ArgumentException>(() => V3BpmEvent.GetFromJson(new JSONObject()));

            var json = new JSONObject { ["m"] = 120 };
            var bpmEvent = V3BpmEvent.GetFromJson(json);

            Assert.AreEqual(0, bpmEvent.JsonTime);
            Assert.AreEqual(120, bpmEvent.Bpm);
        }

        [Test]
        public void V3RotationEventTest()
        {
            var json = new JSONObject();
            var rotationEvent = V3RotationEvent.GetFromJson(json);

            Assert.AreEqual(0, rotationEvent.JsonTime);
            Assert.AreEqual(14, rotationEvent.Type); // Is not 0
            Assert.AreEqual(1360, rotationEvent.Value);
            Assert.AreEqual(0, rotationEvent.Rotation);
        }

        [Test]
        public void V3ColorNoteTest()
        {
            var json = new JSONObject();
            var note = V3ColorNote.GetFromJson(json);

            Assert.AreEqual(0, note.JsonTime);
            Assert.AreEqual(0, note.Color);
            Assert.AreEqual(0, note.Type);
            Assert.AreEqual(0, note.CutDirection);
            Assert.AreEqual(0, note.PosX);
            Assert.AreEqual(0, note.PosY);
            Assert.AreEqual(0, note.AngleOffset);
        }

        [Test]
        public void V3BombNoteTest()
        {
            var json = new JSONObject();
            var bomb = V3BombNote.GetFromJson(json);

            Assert.AreEqual(0, bomb.JsonTime);
            Assert.AreEqual(3, bomb.Color); // Is not 0
            Assert.AreEqual(3, bomb.Type); // Is not 0
            Assert.AreEqual(0, bomb.CutDirection);
            Assert.AreEqual(0, bomb.PosX);
            Assert.AreEqual(0, bomb.PosY);
            Assert.AreEqual(0, bomb.AngleOffset);
        }

        [Test]
        public void V3ObstacleTest()
        {
            var json = new JSONObject();
            var obstacle = V3Obstacle.GetFromJson(json);

            Assert.AreEqual(0, obstacle.JsonTime);
            Assert.AreEqual(0, obstacle.PosX);
            Assert.AreEqual(0, obstacle.PosY);
            Assert.AreEqual(0, obstacle.Duration);
            Assert.AreEqual(0, obstacle.Width);
        }

        [Test]
        public void V3ArcTest()
        {
            var json = new JSONObject();
            var arc = V3Arc.GetFromJson(json);

            Assert.AreEqual(0, arc.JsonTime);
            Assert.AreEqual(0, arc.PosX);
            Assert.AreEqual(0, arc.PosY);
            Assert.AreEqual(0, arc.Color);
            Assert.AreEqual(0, arc.CutDirection);
            Assert.AreEqual(0, arc.HeadControlPointLengthMultiplier);
            Assert.AreEqual(0, arc.TailJsonTime);
            Assert.AreEqual(0, arc.TailPosX);
            Assert.AreEqual(0, arc.TailPosY);
            Assert.AreEqual(0, arc.TailCutDirection);
            Assert.AreEqual(0, arc.TailControlPointLengthMultiplier);
            Assert.AreEqual(0, arc.MidAnchorMode);
        }

        [Test]
        public void V3ChainTest()
        {
            var json = new JSONObject();
            var chain = V3Chain.GetFromJson(json);

            Assert.AreEqual(0, chain.JsonTime);
            Assert.AreEqual(0, chain.PosX);
            Assert.AreEqual(0, chain.PosY);
            Assert.AreEqual(0, chain.Color);
            Assert.AreEqual(0, chain.CutDirection);
            Assert.AreEqual(0, chain.TailJsonTime);
            Assert.AreEqual(0, chain.TailPosX);
            Assert.AreEqual(0, chain.TailPosY);
            Assert.AreEqual(0, chain.Squish);
            Assert.AreEqual(0, chain.SliceCount);
        }

        [Test]
        public void V3BasicEventTest()
        {
            var json = new JSONObject();
            var basicEvent = V3BasicEvent.GetFromJson(json);

            Assert.AreEqual(0, basicEvent.JsonTime);
            Assert.AreEqual(0, basicEvent.Type);
            Assert.AreEqual(0, basicEvent.Value);
            Assert.AreEqual(0, basicEvent.FloatValue);
        }

        [Test]
        public void V3ColorBoostEventTest()
        {
            var json = new JSONObject();
            var boostEvent = V3ColorBoostEvent.GetFromJson(json);

            Assert.AreEqual(0, boostEvent.JsonTime);
            Assert.AreEqual(5, boostEvent.Type); // Is not 0
            Assert.AreEqual(0, boostEvent.Value);
            Assert.AreEqual(0, boostEvent.FloatValue);
        }

        [Test]
        public void V3LightColorEventBoxGroupTest()
        {
            Assert.Throws<ArgumentException>(() => V3LightColorEventBoxGroup.GetFromJson(new JSONObject()));
            Assert.Throws<ArgumentException>(() => V3LightColorEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject()
                    }
                }
            }));
            Assert.Throws<ArgumentException>(() => V3LightColorEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["e"] = new JSONArray()
                    }
                }
            }));

            var json = new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject(),
                        ["e"] = new JSONArray()
                    }
                }
            };
            var group = V3LightColorEventBoxGroup.GetFromJson(json);

            AssertBaseEventBoxGroupDefaults(group);
        }

        [Test]
        public void V3LightColorBaseTest()
        {
            var json = new JSONObject();
            var evt = V3LightColorBase.GetFromJson(json);

            Assert.AreEqual(0, evt.JsonTime);
            Assert.AreEqual(0, evt.Color);
            Assert.AreEqual(0, evt.Brightness);
            Assert.AreEqual(0, evt.TransitionType);
        }

        [Test]
        public void V3LightRotationEventBoxGroupTest()
        {
            Assert.Throws<ArgumentException>(() => V3LightRotationEventBoxGroup.GetFromJson(new JSONObject()));
            Assert.Throws<ArgumentException>(() => V3LightRotationEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject()
                    }
                }
            }));
            Assert.Throws<ArgumentException>(() => V3LightRotationEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["l"] = new JSONArray()
                    }
                }
            }));

            var json = new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject(),
                        ["l"] = new JSONArray()
                    }
                }
            };
            var group = V3LightRotationEventBoxGroup.GetFromJson(json);

            AssertBaseEventBoxGroupDefaults(group);
        }

        [Test]
        public void V3LightRotationBaseTest()
        {
            var json = new JSONObject();
            var evt = V3LightRotationBase.GetFromJson(json);

            Assert.AreEqual(0, evt.JsonTime);
            Assert.AreEqual(0, evt.Rotation);
            Assert.AreEqual(0, evt.Direction);
            Assert.AreEqual(0, evt.EaseType);
            Assert.AreEqual(0, evt.Loop);
            Assert.AreEqual(0, evt.UsePrevious);
        }

        [Test]
        public void V3LightTranslationEventBoxGroupTest()
        {
            Assert.Throws<ArgumentException>(() => V3LightTranslationEventBoxGroup.GetFromJson(new JSONObject()));
            Assert.Throws<ArgumentException>(() => V3LightTranslationEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject()
                    }
                }
            }));
            Assert.Throws<ArgumentException>(() => V3LightTranslationEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["l"] = new JSONArray()
                    }
                }
            }));

            var json = new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject(),
                        ["l"] = new JSONArray()
                    }
                }
            };
            var group = V3LightTranslationEventBoxGroup.GetFromJson(json);

            AssertBaseEventBoxGroupDefaults(group);
        }

        [Test]
        public void V3LightTranslationBaseTest()
        {
            var json = new JSONObject();
            var evt = V3LightTranslationBase.GetFromJson(json);

            Assert.AreEqual(0, evt.JsonTime);
            Assert.AreEqual(0, evt.UsePrevious);
            Assert.AreEqual(0, evt.EaseType);
            Assert.AreEqual(0, evt.Translation);
        }

        [Test]
        public void V3VfxEventEventBoxGroupTest()
        {
            Assert.Throws<ArgumentException>(() => V3VfxEventEventBoxGroup.GetFromJson(new JSONObject(), new List<FloatFxEventBase>()));
            Assert.DoesNotThrow(() => V3VfxEventEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject()
                    }
                }
            }, new List<FloatFxEventBase>()));

            var json = new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject(),
                        ["l"] = new JSONArray()
                    }
                }
            };
            var group = V3VfxEventEventBoxGroup.GetFromJson(json, new List<FloatFxEventBase>());

            AssertBaseEventBoxGroupDefaults(group);
        }

        [Test]
        public void V3FxEventsCollectionTest()
        {
            var json = new JSONObject();
            var evt = V3FxEventsCollection.GetFromJson(json);

            Assert.AreEqual(0, evt.IntFxEvents.Length);
            Assert.AreEqual(0, evt.FloatFxEvents.Length);
        }

        [Test]
        public void V3IndexFilterTest()
        {
            var json = new JSONObject();
            var filter = V3IndexFilter.GetFromJson(json);

            AssertIndexFilterDefaults(filter);
        }

        private void AssertBaseEventBoxGroupDefaults<T>(BaseEventBoxGroup<T> boxGroup) where T : BaseEventBox
        {
            Assert.AreEqual(0, boxGroup.JsonTime);
            Assert.AreEqual(0, boxGroup.ID);
            Assert.AreEqual(1, boxGroup.Events.Count);

            var box = boxGroup.Events[0];

            AssertBaseEventBoxDefaults(box);
            AssertIndexFilterDefaults(box.IndexFilter);
        }

        private void AssertBaseEventBoxDefaults(BaseEventBox box)
        {
            Assert.AreEqual(0, box.BeatDistribution);
            Assert.AreEqual(0, box.BeatDistributionType);
            Assert.AreEqual(0, box.Easing);

            if (box is BaseLightColorEventBox lightColorEventBox)
            {
                Assert.AreEqual(0, lightColorEventBox.BrightnessDistribution);
                Assert.AreEqual(0, lightColorEventBox.BrightnessDistributionType);
                Assert.AreEqual(0, lightColorEventBox.BrightnessAffectFirst);
                Assert.AreEqual(0, lightColorEventBox.Events.Length);
            }
            else if (box is BaseLightRotationEventBox lightRotationEventBox)
            {
                Assert.AreEqual(0, lightRotationEventBox.RotationDistribution);
                Assert.AreEqual(0, lightRotationEventBox.RotationDistributionType);
                Assert.AreEqual(0, lightRotationEventBox.RotationAffectFirst);
                Assert.AreEqual(0, lightRotationEventBox.Axis);
                Assert.AreEqual(0, lightRotationEventBox.Flip);
                Assert.AreEqual(0, lightRotationEventBox.Easing);
                Assert.AreEqual(0, lightRotationEventBox.Events.Length);
            }
            else if (box is BaseLightTranslationEventBox lightTranslationEventBox)
            {
                Assert.AreEqual(0, lightTranslationEventBox.TranslationDistribution);
                Assert.AreEqual(0, lightTranslationEventBox.TranslationDistributionType);
                Assert.AreEqual(0, lightTranslationEventBox.TranslationAffectFirst);
                Assert.AreEqual(0, lightTranslationEventBox.Axis);
                Assert.AreEqual(0, lightTranslationEventBox.Flip);
                Assert.AreEqual(0, lightTranslationEventBox.Easing);
                Assert.AreEqual(0, lightTranslationEventBox.Events.Length);
            }
            else if (box is BaseVfxEventEventBox vfxEventEventBox)
            {
                Assert.AreEqual(0, vfxEventEventBox.VfxDistribution);
                Assert.AreEqual(0, vfxEventEventBox.VfxDistributionType);
                Assert.AreEqual(0, vfxEventEventBox.VfxAffectFirst);
                Assert.AreEqual(0, vfxEventEventBox.Easing);
                Assert.AreEqual(0, vfxEventEventBox.FloatFxEvents.Count);
            }
        }

        private void AssertIndexFilterDefaults(BaseIndexFilter filter)
        {
            Assert.AreEqual(0, filter.Type);
            Assert.AreEqual(0, filter.Param0);
            Assert.AreEqual(0, filter.Param1);
            Assert.AreEqual(0, filter.Reverse);
            Assert.AreEqual(0, filter.Chunks);
            Assert.AreEqual(0, filter.Random);
            Assert.AreEqual(0, filter.Seed);
            Assert.AreEqual(0, filter.Limit);
            Assert.AreEqual(0, filter.LimitAffectsType);
        }
    }
}