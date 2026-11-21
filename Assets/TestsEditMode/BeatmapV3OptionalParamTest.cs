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
            Assert.AreEqual(0, evt.UsePrevious);
        }

        // Extend holds the previous color, so only interpolating nodes export a custom curve.
        [Test]
        public void V3ColorExtendedEasingRoundTripsWithoutChangingTransition(
            [Range(4, 18)] int easing, [Values(0, 1)] int usePrevious)
        {
            var evt = new BaseLightColorBase
            {
                Easing = easing,
                UsePrevious = usePrevious,
                CustomData = new JSONObject { ["unrelated"] = "keep" }
            };
            var json = V3LightColorBase.ToJson(evt);

            Assert.AreEqual(usePrevious == 1 ? 2 : 1, json["i"].AsInt);
            if (usePrevious == 0)
            {
                Assert.IsTrue(json["customData"]["easing"].IsNumber);
                Assert.AreEqual(easing, json["customData"]["easing"].AsInt);
            }
            else
            {
                Assert.IsFalse(json["customData"].HasKey("easing"));
            }
            var loaded = V3LightColorBase.GetFromJson(json);
            Assert.AreEqual(usePrevious == 1 ? 0 : easing, loaded.Easing);
            Assert.AreEqual(usePrevious, loaded.UsePrevious);
            Assert.AreEqual("keep", loaded.CustomData["unrelated"].Value);
            Assert.AreEqual(easing, evt.Easing);
            Assert.AreEqual(usePrevious, evt.UsePrevious);
        }

        // V3ColorOtherKnownEasingsRoundTrip covers curves native on other GLS types but unavailable in
        // V3 color's transition-only schema, so they need the same numeric extension as the new curves.
        [Test]
        public void V3ColorOtherKnownEasingsRoundTrip(
            [Values(1, 2, 3, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 100, 101, 102)] int easing)
        {
            var json = V3LightColorBase.ToJson(new BaseLightColorBase { Easing = easing });
            Assert.AreEqual(easing, json["customData"]["easing"].AsInt);
            Assert.AreEqual(easing, V3LightColorBase.GetFromJson(json).Easing);
        }

        // V3ColorNativeTransitionsDoNotGainCustomEasing keeps unmodified native files free of unnecessary
        // custom data and preserves the established Instant/Interpolate/Extend interpretation.
        [TestCase(-1, 0, 0)]
        [TestCase(0, 0, 1)]
        [TestCase(0, 1, 2)]
        public void V3ColorNativeTransitionsDoNotGainCustomEasing(int easing, int usePrevious, int transition)
        {
            var json = V3LightColorBase.ToJson(new BaseLightColorBase
            {
                Easing = easing, UsePrevious = usePrevious
            });
            Assert.AreEqual(transition, json["i"].AsInt);
            Assert.IsFalse(json.HasKey("customData"));
            var loaded = V3LightColorBase.GetFromJson(json);
            Assert.AreEqual(easing, loaded.Easing);
            Assert.AreEqual(usePrevious, loaded.UsePrevious);
        }

        // Saving an Extend node strips easing from the export without changing its editable metadata.
        [Test]
        public void V3ColorReturningToNativeEasingRemovesStaleExtension(
            [Values(-1, 0)] int easing, [Values(0, 1)] int usePrevious)
        {
            var source = new JSONObject
            {
                ["i"] = 1,
                ["customData"] = new JSONObject { ["easing"] = 4, ["unrelated"] = "keep" }
            };
            var evt = V3LightColorBase.GetFromJson(source);
            evt.Easing = easing;
            evt.UsePrevious = usePrevious;
            var json = V3LightColorBase.ToJson(evt);

            Assert.AreEqual(usePrevious == 1 ? 2 : easing == -1 ? 0 : 1, json["i"].AsInt);
            Assert.IsFalse(json["customData"].HasKey("easing"));
            Assert.AreEqual(usePrevious == 1, evt.CustomData.HasKey("easing"));
            if (usePrevious == 1)
                Assert.AreEqual(4, evt.CustomData["easing"].AsInt);
            Assert.AreEqual("keep", json["customData"]["unrelated"].Value);
            Assert.AreEqual(4, source["customData"]["easing"].AsInt);
            Assert.AreEqual(easing, evt.Easing);
            Assert.AreEqual(usePrevious, evt.UsePrevious);
        }

        // V3ColorEasingSaveUsesModelRatherThanStaleCustomData protects undo/source JSON ownership while
        // replacing only the generated extension with the curve currently selected on the actual node.
        [Test]
        public void V3ColorEasingSaveUsesModelRatherThanStaleCustomData()
        {
            var evt = new BaseLightColorBase
            {
                Easing = 18,
                CustomData = new JSONObject { ["easing"] = 7, ["unrelated"] = "keep" }
            };
            var originalCustom = evt.CustomData;
            var first = V3LightColorBase.ToJson(evt);
            Assert.AreEqual(18, first["customData"]["easing"].AsInt);
            Assert.AreEqual(7, originalCustom["easing"].AsInt);
            Assert.AreEqual("keep", first["customData"]["unrelated"].Value);

            evt.Easing = 4;
            var second = V3LightColorBase.ToJson(evt);
            Assert.AreEqual(4, second["customData"]["easing"].AsInt);
            Assert.AreEqual(18, first["customData"]["easing"].AsInt);
            Assert.AreEqual(4, evt.Easing);
        }

        // V3ColorInstantIgnoresStaleCustomEasing ensures malformed/hand-edited extension data never
        // changes an explicit Instant node into an interpolated event.
        [Test]
        public void V3ColorInstantIgnoresStaleCustomEasing()
        {
            var evt = V3LightColorBase.GetFromJson(new JSONObject
            {
                ["i"] = 0,
                ["customData"] = new JSONObject { ["easing"] = 4 }
            });
            Assert.AreEqual(-1, evt.Easing);
            Assert.AreEqual(0, evt.UsePrevious);
            Assert.IsFalse(V3LightColorBase.ToJson(evt).HasKey("customData"));
        }

        // V3ColorInvalidCustomEasingKeepsNativeTransition rejects strings, fractions, and unknown IDs
        // instead of silently coercing malformed data into a different numeric easing.
        [TestCase("\"4\"")]
        [TestCase("\"easeInSine\"")]
        [TestCase("4.5")]
        [TestCase("-2")]
        [TestCase("31")]
        [TestCase("99")]
        [TestCase("103")]
        [TestCase("true")]
        [TestCase("null")]
        public void V3ColorInvalidCustomEasingKeepsNativeTransition(string value)
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{\"i\":1,\"customData\":{\"easing\":" + value + "}}"));
            Assert.AreEqual(0, evt.Easing);
            Assert.AreEqual(0, evt.UsePrevious);
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
            Assert.AreEqual(0, evt.Easing);
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
            Assert.AreEqual(0, evt.Easing);
            Assert.AreEqual(0, evt.Translation);
        }

        [Test]
        public void V3VfxEventEventBoxGroupTest()
        {
            Assert.Throws<ArgumentException>(() => V3VfxEventEventBoxGroup.GetFromJson(new JSONObject(), new List<BaseFxEventFloat>()));
            Assert.DoesNotThrow(() => V3VfxEventEventBoxGroup.GetFromJson(new JSONObject
            {
                ["e"] = new JSONArray
                {
                    [0] = new JSONObject
                    {
                        ["f"] = new JSONObject()
                    }
                }
            }, new List<BaseFxEventFloat>()));

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
            var group = V3VfxEventEventBoxGroup.GetFromJson(json, new List<BaseFxEventFloat>());

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

        [Test]
        public void V3ColorBoxRoundTripPreservesColorDistributionPayloadAndUnknownCustomData()
        {
            // BaseIndexFilter.ToJson only serializes V3/V4, and earlier fixtures leave ambient MapVersion=2,
            // so pin the version this V3 round-trip requires.
            Settings.Instance.MapVersion = 3;
            var input = JSON.Parse(
                "{\"f\":{\"c\":1,\"f\":0,\"p\":0,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":1," +
                "\"customData\":{\"colorDistributions\":[\"h,-0.2,IO^5\",\"f,0.1,I^2\"]," +
                "\"strobeColorDistributions\":[\"s,-0.2,L\"],\"future\":42}," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}"
            );

            var output = V3LightColorEventBox.ToJson(V3LightColorEventBox.GetFromJson(input));

            Assert.AreEqual("h,-0.2,IO^5", output["customData"]["colorDistributions"][0].Value);
            Assert.AreEqual("f,0.1,I^2", output["customData"]["colorDistributions"][1].Value);
            Assert.AreEqual("s,-0.2,L", output["customData"]["strobeColorDistributions"][0].Value);
            Assert.AreEqual(42, output["customData"]["future"].AsInt);
        }

        [Test]
        public void V3ColorEventRoundTripPreservesColorDistributionPayloadAndUnknownCustomData()
        {
            var input = JSON.Parse(
                "{\"b\":0,\"c\":1,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0," +
                "\"customData\":{\"colorDistributions\":[\"sv,0.3,L\"]," +
                "\"strobeColorDistributions\":[\"f,-0.1,I^2\"],\"future\":\"kept\"}}"
            );

            var output = V3LightColorBase.ToJson(V3LightColorBase.GetFromJson(input));

            Assert.AreEqual("sv,0.3,L", output["customData"]["colorDistributions"][0].Value);
            Assert.AreEqual("f,-0.1,I^2", output["customData"]["strobeColorDistributions"][0].Value);
            Assert.AreEqual("kept", output["customData"]["future"].Value);
        }

        // Inner edits, clipboard copies, and group rebuilds use this copy constructor. Losing the parsed
        // distribution arrays would make SaveCustom erase their keys from the copied customData.
        [Test]
        public void ClonedColorBoxKeepsAuthoredColorDistributionPayloadAliveForSaveCustom()
        {
            // ToJson selects a V3/V4 writer, so pin the version this round-trip requires like the sibling test.
            Settings.Instance.MapVersion = 3;
            var source = V3LightColorEventBox.GetFromJson(JSON.Parse(
                "{\"f\":{\"c\":1,\"f\":0,\"p\":0,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":0,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":1," +
                "\"customData\":{\"colorDistributions\":[\"h,-0.2,IO^5\",\"f,0.1,I^2\"]," +
                "\"strobeColorDistributions\":[\"s,-0.2,L,l\"],\"future\":42}," +
                "\"e\":[{\"b\":0,\"c\":0,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}"
            ));

            var clone = (BaseLightColorEventBox)source.Clone();

            // Clones must retain authored display abbreviations through the next save.
            CollectionAssert.AreEqual(new[] { "h,-0.2,IO^5", "f,0.1,I^2" }, clone.ColorDistributions);
            CollectionAssert.AreEqual(new[] { "s,-0.2,L,l" }, clone.StrobeColorDistributions);
            Assert.AreEqual(2, clone.ParsedColorDistributions.Count);
            Assert.AreEqual(1, clone.ParsedStrobeColorDistributions.Count);
            Assert.True(clone.ParsedStrobeColorDistributions[0].UsesAffectedLightProgress);
            // The clone must own independent arrays and payloads so pastes cannot share or mutate the source.
            Assert.AreNotSame(source.CustomData, clone.CustomData);
            Assert.AreNotSame(source.ColorDistributions, clone.ColorDistributions);
            Assert.AreNotSame(source.StrobeColorDistributions, clone.StrobeColorDistributions);
            Assert.AreNotSame(source.ParsedColorDistributions, clone.ParsedColorDistributions);
            Assert.AreNotSame(source.ParsedStrobeColorDistributions, clone.ParsedStrobeColorDistributions);

            // ToJson drives SaveCustom, which rewrites both keys from the arrays the clone must still own.
            var output = clone.ToJson();
            Assert.AreEqual("h,-0.2,IO^5", output["customData"]["colorDistributions"][0].Value);
            Assert.AreEqual("f,0.1,I^2", output["customData"]["colorDistributions"][1].Value);
            Assert.AreEqual("s,-0.2,L,l", output["customData"]["strobeColorDistributions"][0].Value);
            Assert.AreEqual(42, output["customData"]["future"].AsInt);
        }

        // Group copies must preserve box and node distributions in every filter lane, including during an
        // inner-node edit.
        [Test]
        public void ClonedColorGroupKeepsEveryLaneColorDistributionPayloadAlive()
        {
            Settings.Instance.MapVersion = 3;
            var source = V3LightColorEventBoxGroup.GetFromJson(JSON.Parse(
                "{\"b\":8,\"g\":1,\"e\":[" +
                "{\"f\":{\"c\":1,\"f\":0,\"p\":0,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":1,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":0," +
                "\"customData\":{\"colorDistributions\":[\"h,-0.2,IO^5\",\"f,0.1,I^2\"]," +
                "\"strobeColorDistributions\":[\"s,-0.2,L,l\"],\"future\":42}," +
                "\"e\":[" +
                "{\"b\":0.5,\"c\":0,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0," +
                "\"customData\":{\"colorDistributions\":[\"v,0.4,L\"],\"eventFuture\":\"kept\"}}," +
                "{\"b\":1.5,\"c\":1,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}," +
                "{\"f\":{\"c\":1,\"f\":1,\"p\":2,\"t\":4,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":1,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":0," +
                "\"customData\":{\"strobeColorDistributions\":[\"hs,0.4,L\"]}," +
                "\"e\":[{\"b\":0.75,\"c\":1,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}]}"
            ));

            var clone = (BaseLightColorEventBoxGroup)source.Clone();

            var first = clone.Boxes[0];
            // Every nested clone and serialization path must retain the authored display abbreviations.
            CollectionAssert.AreEqual(new[] { "h,-0.2,IO^5", "f,0.1,I^2" }, first.ColorDistributions);
            CollectionAssert.AreEqual(new[] { "s,-0.2,L,l" }, first.StrobeColorDistributions);
            Assert.AreEqual(2, first.ParsedColorDistributions.Count);
            Assert.AreEqual(1, first.ParsedStrobeColorDistributions.Count);
            Assert.True(first.ParsedStrobeColorDistributions[0].UsesAffectedLightProgress);
            Assert.AreNotSame(source.Boxes[0].CustomData, first.CustomData);
            var clonedNode = first.Events[0];
            CollectionAssert.AreEqual(new[] { "v,0.4,L" }, clonedNode.ColorDistributions);
            Assert.AreEqual(1, clonedNode.ParsedColorDistributions.Count);
            Assert.AreNotSame(source.Boxes[0].Events[0].CustomData, clonedNode.CustomData);
            var second = clone.Boxes[1];
            CollectionAssert.AreEqual(new[] { "hs,0.4,L" }, second.StrobeColorDistributions);
            Assert.AreEqual(1, second.ParsedStrobeColorDistributions.Count);
            Assert.AreNotSame(source.Boxes[1].CustomData, second.CustomData);

            var output = clone.ToJson();
            Assert.AreEqual("h,-0.2,IO^5", output["e"][0]["customData"]["colorDistributions"][0].Value);
            Assert.AreEqual("f,0.1,I^2", output["e"][0]["customData"]["colorDistributions"][1].Value);
            Assert.AreEqual("s,-0.2,L,l", output["e"][0]["customData"]["strobeColorDistributions"][0].Value);
            Assert.AreEqual(42, output["e"][0]["customData"]["future"].AsInt);
            Assert.AreEqual("v,0.4,L", output["e"][0]["e"][0]["customData"]["colorDistributions"][0].Value);
            Assert.AreEqual("kept", output["e"][0]["e"][0]["customData"]["eventFuture"].Value);
            Assert.AreEqual("hs,0.4,L", output["e"][1]["customData"]["strobeColorDistributions"][0].Value);
        }

        private void AssertBaseEventBoxGroupDefaults<T>(BaseEventBoxGroup<T> boxGroup) where T : BaseEventBox
        {
            Assert.AreEqual(0, boxGroup.JsonTime);
            Assert.AreEqual(0, boxGroup.ID);
            Assert.AreEqual(1, boxGroup.Boxes.Count);

            var box = boxGroup.Boxes[0];

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
                Assert.AreEqual(0, vfxEventEventBox.Events.Length);
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
