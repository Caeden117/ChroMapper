using System;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4CommonData
    {
        public struct Note : IEquatable<Note>
        {
            public int PosX { get; set; }
            public int PosY { get; set; }
            public int Color { get; set; }
            public int CutDirection { get; set; }
            public int AngleOffset { get; set; }
            
            public static Note GetFromJson(JSONNode node) => new()
            {
                PosX = node["x"].AsInt,
                PosY = node["y"].AsInt,
                Color = node["c"].AsInt,
                CutDirection = node["d"].AsInt,
                AngleOffset = node["a"].AsInt,
            };
            
            public static Note FromBaseNote(BaseNote baseNote) => new()
            {
                PosX = baseNote.PosX,
                PosY = baseNote.PosY,
                Color = baseNote.Color,
                CutDirection = baseNote.CutDirection,
                AngleOffset = baseNote.AngleOffset
            };
            
            public static Note FromBaseSliderHead(BaseSlider baseSlider) => new()
            {
                PosX = baseSlider.PosX,
                PosY = baseSlider.PosY,
                Color = baseSlider.Color,
                CutDirection = baseSlider.CutDirection,
                AngleOffset = baseSlider.AngleOffset
            };
            
            public static Note FromBaseArcTail(BaseArc baseArc) => new()
            {
                PosX = baseArc.TailPosX,
                PosY = baseArc.TailPosY,
                CutDirection = baseArc.TailCutDirection
            };
            
            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["x"] = PosX;
                node["y"] = PosY;
                node["c"] = Color;
                node["d"] = CutDirection;
                node["a"] = AngleOffset;

                return node;
            }

            public bool Equals(Note other) => PosX == other.PosX 
                                              && PosY == other.PosY 
                                              && Color == other.Color 
                                              && CutDirection == other.CutDirection 
                                              && AngleOffset == other.AngleOffset;

            public override bool Equals(object obj) => obj is Note other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = PosX;
                    hashCode = (hashCode * 397) ^ PosY;
                    hashCode = (hashCode * 397) ^ Color;
                    hashCode = (hashCode * 397) ^ CutDirection;
                    hashCode = (hashCode * 397) ^ AngleOffset;
                    return hashCode;
                }
            }
        }

        public struct Bomb : IEquatable<Bomb>
        {
            public int PosX { get; set; }
            public int PosY { get; set; }

            public static Bomb GetFromJson(JSONNode node) => new()
            {
                PosX = node["x"].AsInt, PosY = node["y"].AsInt
            };

            public static Bomb FromBaseNote(BaseNote baseNote) => new() { PosX = baseNote.PosX, PosY = baseNote.PosY };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["x"] = PosX;
                node["y"] = PosY;
                
                return node;
            }

            public bool Equals(Bomb other) => PosX == other.PosX && PosY == other.PosY;

            public override bool Equals(object obj) => obj is Bomb other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (PosX * 397) ^ PosY;
                }
            }
        }

        public struct Obstacle : IEquatable<Obstacle>
        {
            public int PosX { get; set; }
            public int PosY { get; set; }
            public float Duration { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            
            public static Obstacle GetFromJson(JSONNode node) => new()
            {
                PosX = node["x"].AsInt,
                PosY = node["y"].AsInt,
                Duration = node["d"].AsFloat,
                Width = node["w"].AsInt,
                Height = node["h"].AsInt
            };

            public static Obstacle FromBaseObstacle(BaseObstacle baseObstacle) => new()
            {
                PosX = baseObstacle.PosX,
                PosY = baseObstacle.PosY,
                Duration = baseObstacle.Duration,
                Width = baseObstacle.Width,
                Height = baseObstacle.Height
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["x"] = PosX;
                node["y"] = PosY;
                node["d"] = Duration;
                node["w"] = Width;
                node["h"] = Height;
                
                return node;
            }

            public bool Equals(Obstacle other) => PosX == other.PosX 
                                                  && PosY == other.PosY
                                                  && Duration.Equals(other.Duration) 
                                                  && Width == other.Width 
                                                  && Height == other.Height;

            public override bool Equals(object obj) => obj is Obstacle other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = PosX;
                    hashCode = (hashCode * 397) ^ PosY;
                    hashCode = (hashCode * 397) ^ Duration.GetHashCode();
                    hashCode = (hashCode * 397) ^ Width;
                    hashCode = (hashCode * 397) ^ Height;
                    return hashCode;
                }
            }
        }

        public struct Arc : IEquatable<Arc>
        {
            public float HeadControlPointLengthMultiplier { get; set; }
            public float TailControlPointLengthMultiplier { get; set; }
            public int MidAnchorMode { get; set; }
            
            public static Arc GetFromJson(JSONNode node) => new()
            {
                HeadControlPointLengthMultiplier = node["m"].AsFloat,
                TailControlPointLengthMultiplier = node["tm"].AsFloat,
                MidAnchorMode = node["a"].AsInt
            };

            public static Arc FromBaseArc(BaseArc baseArc) => new()
            {
                HeadControlPointLengthMultiplier = baseArc.HeadControlPointLengthMultiplier,
                TailControlPointLengthMultiplier = baseArc.TailControlPointLengthMultiplier,
                MidAnchorMode = baseArc.MidAnchorMode
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["m"] = HeadControlPointLengthMultiplier;
                node["tm"] = TailControlPointLengthMultiplier;
                node["a"] = MidAnchorMode;
                
                return node;
            }

            public bool Equals(Arc other) => HeadControlPointLengthMultiplier.Equals(other.HeadControlPointLengthMultiplier) 
                                             && TailControlPointLengthMultiplier.Equals(other.TailControlPointLengthMultiplier) 
                                             && MidAnchorMode == other.MidAnchorMode;

            public override bool Equals(object obj) => obj is Arc other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = HeadControlPointLengthMultiplier.GetHashCode();
                    hashCode = (hashCode * 397) ^ TailControlPointLengthMultiplier.GetHashCode();
                    hashCode = (hashCode * 397) ^ MidAnchorMode;
                    return hashCode;
                }
            }
        }

        public struct Chain : IEquatable<Chain>
        {
            public int TailPosX { get; set; }
            public int TailPosY { get; set; }
            public int SliceCount { get; set; }
            public float Squish { get; set; }

            public static Chain GetFromJson(JSONNode node) => new()
            {
                TailPosX = node["tx"].AsInt,
                TailPosY = node["ty"].AsInt,
                SliceCount = node["c"].AsInt,
                Squish = node["s"].AsFloat
            };

            public static Chain FromBaseChain(BaseChain baseChain) => new()
            {
                TailPosX = baseChain.TailPosX,
                TailPosY = baseChain.TailPosY,
                SliceCount = baseChain.SliceCount,
                Squish = baseChain.Squish
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["tx"] = TailPosX;
                node["ty"] = TailPosY;
                node["c"] = SliceCount;
                node["s"] = Squish;
                
                return node;
            }

            public bool Equals(Chain other) => TailPosX == other.TailPosX 
                                               && TailPosY == other.TailPosY 
                                               && SliceCount == other.SliceCount 
                                               && Squish.Equals(other.Squish);

            public override bool Equals(object obj) => obj is Chain other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = TailPosX;
                    hashCode = (hashCode * 397) ^ TailPosY;
                    hashCode = (hashCode * 397) ^ SliceCount;
                    hashCode = (hashCode * 397) ^ Squish.GetHashCode();
                    return hashCode;
                }
            }
        }

        public struct RotationEvent : IEquatable<RotationEvent>
        {
            public int Type { get; set; }
            public float Rotation { get; set; }

            public static RotationEvent GetFromJson(JSONNode node) => new()
            {
                Type = node["t"].AsInt,
                Rotation = node["r"].AsFloat
            };

            public static RotationEvent FromBaseEvent(BaseEvent baseEvent) => new()
            {
                Type = baseEvent.Type == (int)EventTypeValue.EarlyLaneRotation ? 0 : 1,
                Rotation = baseEvent.Rotation
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["t"] = Type;
                node["r"] = Rotation;
                
                return node;
            }

            public bool Equals(RotationEvent other) => Type == other.Type && Rotation.Equals(other.Rotation);

            public override bool Equals(object obj) => obj is RotationEvent other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Type * 397) ^ Rotation.GetHashCode();
                }
            }
        }

        public struct BasicEvent : IEquatable<BasicEvent>
        {
            public int Type { get; set; }
            public int Value { get; set; }
            public float FloatValue { get; set; }

            public static BasicEvent GetFromJson(JSONNode node) => new()
            {
                Type = node["t"].AsInt, Value = node["i"].AsInt, FloatValue = node["f"].AsFloat
            };

            public static BasicEvent FromBaseEvent(BaseEvent baseEvent) => new()
            {
                Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["t"] = Type;
                node["i"] = Value;
                node["f"] = FloatValue;
                
                return node;
            }

            public bool Equals(BasicEvent other) => Type == other.Type && Value == other.Value && FloatValue.Equals(other.FloatValue);

            public override bool Equals(object obj) => obj is BasicEvent other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Type;
                    hashCode = (hashCode * 397) ^ Value;
                    hashCode = (hashCode * 397) ^ FloatValue.GetHashCode();
                    return hashCode;
                }
            }
        }

        public struct ColorBoostEvent : IEquatable<ColorBoostEvent>
        {
            public int Boost { get; set; }

            public static ColorBoostEvent GetFromJson(JSONNode node) => new() { Boost = node["b"].AsInt };

            public static ColorBoostEvent FromBaseEvent(BaseEvent baseEvent) => new()
            {
                Boost = baseEvent.Value == 1 ? 1 : 0
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["b"] = Boost;
                
                return node;
            }

            public bool Equals(ColorBoostEvent other) => Boost == other.Boost;

            public override bool Equals(object obj) => obj is ColorBoostEvent other && Equals(other);

            public override int GetHashCode() => Boost;
        }

        public struct NJSEvent : IEquatable<NJSEvent>
        {
            public int UsePrevious { get; set; }
            public int Easing { get; set; }
            public float RelativeNJS { get; set; }

            public static NJSEvent GetFromJson(JSONNode node) => new()
            {
                UsePrevious = node["p"].AsInt,
                Easing = node["e"].AsInt,
                RelativeNJS = node["d"].AsFloat
            };

            public static NJSEvent FromBaseNJSEvent(BaseNJSEvent baseNJSEvent) => new()
            {
                UsePrevious = baseNJSEvent.UsePrevious, Easing = baseNJSEvent.Easing, RelativeNJS = baseNJSEvent.RelativeNJS
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["p"] = UsePrevious;
                node["e"] = Easing;
                node["d"] = RelativeNJS;
                
                return node;
            }

            public bool Equals(NJSEvent other) => UsePrevious == other.UsePrevious 
                                                  && Easing == other.Easing 
                                                  && RelativeNJS.Equals(other.RelativeNJS);

            public override bool Equals(object obj) => obj is NJSEvent other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = UsePrevious;
                    hashCode = (hashCode * 397) ^ Easing;
                    hashCode = (hashCode * 397) ^ RelativeNJS.GetHashCode();
                    return hashCode;
                }
            }
        }
        
        public struct Waypoint : IEquatable<Waypoint>
        {
            public int PosX { get; set; }
            public int PosY { get; set; }
            public int OffsetDirection { get; set; }

            public static Waypoint GetFromJson(JSONNode node) => new()
            {
                PosX = node["x"].AsInt,
                PosY = node["y"].AsInt,
                OffsetDirection = node["d"].AsInt
            };

            public static Waypoint FromBaseWayPoint(BaseWaypoint baseWayPoint) => new()
            {
                PosX = baseWayPoint.PosX, PosY = baseWayPoint.PosY, OffsetDirection = baseWayPoint.OffsetDirection
            };

            public JSONNode ToJson()
            {
                JSONNode node = new JSONObject();

                node["x"] = PosX;
                node["y"] = PosY;
                node["d"] = OffsetDirection;
                
                return node;
            }

            public bool Equals(Waypoint other) => PosX == other.PosX 
                                                  && PosY == other.PosY 
                                                  && OffsetDirection == other.OffsetDirection;

            public override bool Equals(object obj) => obj is Waypoint other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = PosX;
                    hashCode = (hashCode * 397) ^ PosY;
                    hashCode = (hashCode * 397) ^ OffsetDirection;
                    return hashCode;
                }
            }
        }

        public struct IndexFilter : IEquatable<IndexFilter>
        {
            public int Type { get; set; }
            public int Param0 { get; set; }
            public int Param1 { get; set; }
            public int Reverse { get; set; }
            public int Chunks { get; set; }
            public int Random { get; set; }
            public int Seed { get; set; }
            public float Limit { get; set; }
            public int LimitAffectsType { get; set; }

            public static IndexFilter FromBaseIndexFilter(BaseIndexFilter baseIndexFilter) => new()
            {
                Type = baseIndexFilter.Type,
                Param0 = baseIndexFilter.Param0,
                Param1 = baseIndexFilter.Param1,
                Reverse = baseIndexFilter.Reverse,
                Chunks = baseIndexFilter.Chunks,
                Random = baseIndexFilter.Random,
                Seed = baseIndexFilter.Seed,
                Limit = baseIndexFilter.Limit,
                LimitAffectsType = baseIndexFilter.LimitAffectsType
            };
            
            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["f"] = Type;
                node["p"] = Param0;
                node["t"] = Param1;
                node["r"] = Reverse;
                node["c"] = Chunks;
                node["n"] = Random;
                node["s"] = Seed;
                node["l"] = Limit;
                node["d"] = LimitAffectsType;

                return node;
            }

            public bool Equals(IndexFilter other) => Type == other.Type 
                                                     && Param0 == other.Param0 
                                                     && Param1 == other.Param1 
                                                     && Reverse == other.Reverse 
                                                     && Chunks == other.Chunks 
                                                     && Random == other.Random 
                                                     && Seed == other.Seed 
                                                     && Limit.Equals(other.Limit) 
                                                     && LimitAffectsType == other.LimitAffectsType;

            public override bool Equals(object obj) => obj is IndexFilter other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Type;
                    hashCode = (hashCode * 397) ^ Param0;
                    hashCode = (hashCode * 397) ^ Param1;
                    hashCode = (hashCode * 397) ^ Reverse;
                    hashCode = (hashCode * 397) ^ Chunks;
                    hashCode = (hashCode * 397) ^ Random;
                    hashCode = (hashCode * 397) ^ Seed;
                    hashCode = (hashCode * 397) ^ Limit.GetHashCode();
                    hashCode = (hashCode * 397) ^ LimitAffectsType;
                    return hashCode;
                }
            }
        }
        
        public struct LightColorEventBox : IEquatable<LightColorEventBox>
        {
            public float BeatDistribution { get; set; }
            public int BeatDistributionType { get; set; }
            public int Easing { get; set; }
            public float BrightnessDistribution { get; set; }
            public int BrightnessDistributionType { get; set; }
            public int BrightnessAffectFirst { get; set; }
            
            public static LightColorEventBox GetFromJson(JSONNode node) => new()
            {
                BeatDistribution = node["w"].AsFloat,
                BeatDistributionType = node["d"].AsInt,
                BrightnessDistribution = node["s"].AsFloat,
                BrightnessDistributionType = node["t"].AsInt,
                BrightnessAffectFirst = node["b"].AsInt,
                Easing = node["e"].AsInt
            };

            public static LightColorEventBox FromBaseLightColorEventBox(BaseLightColorEventBox baseLightColorEventBox) => new()
            {
                BeatDistribution = baseLightColorEventBox.BeatDistribution,
                BeatDistributionType = baseLightColorEventBox.BeatDistributionType,
                Easing = baseLightColorEventBox.Easing,
                BrightnessDistribution = baseLightColorEventBox.BrightnessDistribution,
                BrightnessDistributionType = baseLightColorEventBox.BrightnessDistributionType,
                BrightnessAffectFirst = baseLightColorEventBox.BrightnessAffectFirst,
            };
            
            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["w"] = BeatDistribution;
                node["d"] = BeatDistributionType;
                node["s"] = BrightnessDistribution;
                node["t"] = BrightnessDistributionType;
                node["b"] = BrightnessAffectFirst;
                node["e"] = Easing;

                return node;
            }

            public bool Equals(LightColorEventBox other) => BeatDistribution.Equals(other.BeatDistribution)
                                                            && BeatDistributionType == other.BeatDistributionType 
                                                            && Easing == other.Easing 
                                                            && BrightnessDistribution.Equals(other.BrightnessDistribution)
                                                            && BrightnessDistributionType == other.BrightnessDistributionType
                                                            && BrightnessAffectFirst == other.BrightnessAffectFirst;

            public override bool Equals(object obj) => obj is LightColorEventBox other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = BeatDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ BeatDistributionType;
                    hashCode = (hashCode * 397) ^ Easing;
                    hashCode = (hashCode * 397) ^ BrightnessDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ BrightnessDistributionType;
                    hashCode = (hashCode * 397) ^ BrightnessAffectFirst;
                    return hashCode;
                }
            }
        }
        
        public struct LightColorEvent : IEquatable<LightColorEvent>
        {
            public int Easing { get; set; }
            public int Color { get; set; }
            public float Brightness { get; set; }
            public int TransitionType { get; set; }
            public int Frequency { get; set; }
            public float StrobeBrightness { get; set; }
            public int StrobeFade { get; set; }
            
            public static LightColorEvent GetFromJson(JSONNode node) => new()
            {
                TransitionType = node["p"].AsInt,
                Easing = node["e"].AsInt, 
                Color = node["c"].AsInt, 
                Brightness = node["b"].AsInt, 
                Frequency = node["f"].AsInt, 
                StrobeBrightness = node["sb"].AsInt, 
                StrobeFade = node["sf"].AsInt,
            };

            public static LightColorEvent FromBaseLightColorEvent(BaseLightColorBase baseLightColorEvent) => new()
            {
                TransitionType = baseLightColorEvent.TransitionType,
                Easing = baseLightColorEvent.Easing,
                Color = baseLightColorEvent.Color,
                Brightness = baseLightColorEvent.Brightness,
                Frequency = baseLightColorEvent.Frequency,
                StrobeBrightness = baseLightColorEvent.StrobeBrightness,
                StrobeFade = baseLightColorEvent.StrobeFade
            };

            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["p"] = TransitionType;
                node["e"] = Easing;
                node["c"] = Color;
                node["b"] = Brightness;
                node["f"] = Frequency;
                node["sb"] = StrobeBrightness;
                node["sf"] = StrobeFade;

                return node;
            }

            public bool Equals(LightColorEvent other) => Easing == other.Easing 
                                                         && Color == other.Color 
                                                         && Brightness.Equals(other.Brightness)
                                                         && TransitionType == other.TransitionType
                                                         && Frequency == other.Frequency 
                                                         && StrobeBrightness.Equals(other.StrobeBrightness)
                                                         && StrobeFade == other.StrobeFade;

            public override bool Equals(object obj) => obj is LightColorEvent other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Easing;
                    hashCode = (hashCode * 397) ^ Color;
                    hashCode = (hashCode * 397) ^ Brightness.GetHashCode();
                    hashCode = (hashCode * 397) ^ TransitionType;
                    hashCode = (hashCode * 397) ^ Frequency;
                    hashCode = (hashCode * 397) ^ StrobeBrightness.GetHashCode();
                    hashCode = (hashCode * 397) ^ StrobeFade;
                    return hashCode;
                }
            }
        }
        
        public struct LightRotationEventBox : IEquatable<LightRotationEventBox>
        {
            public float BeatDistribution { get; set; }
            public int BeatDistributionType { get; set; }
            public int Easing { get; set; }
            public float RotationDistribution { get; set; }
            public int RotationDistributionType { get; set; }
            public int RotationAffectFirst { get; set; }
            public int Axis { get; set; }
            public int Flip { get; set; }
            
            public static LightRotationEventBox GetFromJson(JSONNode node) => new()
            {
                BeatDistribution = node["w"].AsFloat,
                BeatDistributionType = node["d"].AsInt,
                RotationDistribution = node["s"].AsFloat,
                RotationDistributionType = node["t"].AsInt,
                RotationAffectFirst = node["b"].AsInt,
                Easing = node["e"].AsInt,
                Axis = node["a"].AsInt,
                Flip = node["f"].AsInt
            };
            
            public static LightRotationEventBox FromBaseLightRotationEventBox(BaseLightRotationEventBox baseLightRotationEventBox) => new()
            {
                BeatDistribution = baseLightRotationEventBox.BeatDistribution,
                BeatDistributionType = baseLightRotationEventBox.BeatDistributionType,
                Easing = baseLightRotationEventBox.Easing,
                RotationDistribution = baseLightRotationEventBox.RotationDistribution,
                RotationDistributionType = baseLightRotationEventBox.RotationDistributionType,
                RotationAffectFirst = baseLightRotationEventBox.RotationAffectFirst,
                Axis = baseLightRotationEventBox.Axis,
                Flip = baseLightRotationEventBox.Flip
            };
            
            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["w"] = BeatDistribution;
                node["d"] = BeatDistributionType;
                node["s"] = RotationDistribution;
                node["t"] = RotationDistributionType;
                node["b"] = RotationAffectFirst;
                node["e"] = Easing;
                node["a"] = Axis;
                node["f"] = Flip;

                return node;
            }

            public bool Equals(LightRotationEventBox other) => BeatDistribution.Equals(other.BeatDistribution) 
                                                               && BeatDistributionType == other.BeatDistributionType
                                                               && Easing == other.Easing
                                                               && RotationDistribution.Equals(other.RotationDistribution)
                                                               && RotationDistributionType == other.RotationDistributionType 
                                                               && RotationAffectFirst == other.RotationAffectFirst 
                                                               && Axis == other.Axis 
                                                               && Flip == other.Flip;

            public override bool Equals(object obj) => obj is LightRotationEventBox other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = BeatDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ BeatDistributionType;
                    hashCode = (hashCode * 397) ^ Easing;
                    hashCode = (hashCode * 397) ^ RotationDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ RotationDistributionType;
                    hashCode = (hashCode * 397) ^ RotationAffectFirst;
                    hashCode = (hashCode * 397) ^ Axis;
                    hashCode = (hashCode * 397) ^ Flip;
                    return hashCode;
                }
            }
        }
        
        public struct LightRotationEvent : IEquatable<LightRotationEvent>
        {
            public int Easing { get; set; }
            public float Rotation { get; set; }
            public int TransitionType { get; set; }
            public int Direction { get; set; }
            public int Loop { get; set; }
            
            public static LightRotationEvent GetFromJson(JSONNode node) => new()
            {
                TransitionType = node["p"].AsInt,
                Easing = node["e"].AsInt, 
                Rotation = node["r"].AsInt, 
                Direction = node["d"].AsInt, 
                Loop = node["l"].AsInt,
            };

            public static LightRotationEvent FromBaseLightRotationEvent(BaseLightRotationBase baseLightRotationEvent) => new()
            {
                TransitionType = baseLightRotationEvent.UsePrevious,
                Easing = baseLightRotationEvent.EaseType,
                Rotation = baseLightRotationEvent.Rotation,
                Direction = baseLightRotationEvent.Direction,
                Loop = baseLightRotationEvent.Loop
            };

            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["p"] = TransitionType;
                node["e"] = Easing;
                node["r"] = Rotation;
                node["d"] = Direction;
                node["l"] = Loop;

                return node;
            }

            public bool Equals(LightRotationEvent other) => Easing == other.Easing
                                                            && Rotation.Equals(other.Rotation) 
                                                            && TransitionType == other.TransitionType 
                                                            && Direction == other.Direction 
                                                            && Loop == other.Loop;

            public override bool Equals(object obj) => obj is LightRotationEvent other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Easing;
                    hashCode = (hashCode * 397) ^ Rotation.GetHashCode();
                    hashCode = (hashCode * 397) ^ TransitionType;
                    hashCode = (hashCode * 397) ^ Direction;
                    hashCode = (hashCode * 397) ^ Loop;
                    return hashCode;
                }
            }
        }
        
        public struct LightTranslationEventBox : IEquatable<LightTranslationEventBox>
        {
            public float BeatDistribution { get; set; }
            public int BeatDistributionType { get; set; }
            public int Easing { get; set; }
            public float TranslationDistribution { get; set; }
            public int TranslationDistributionType { get; set; }
            public int TranslationAffectFirst { get; set; }
            public int Axis { get; set; }
            public int Flip { get; set; }
            
            public static LightTranslationEventBox GetFromJson(JSONNode node) => new()
            {
                BeatDistribution = node["w"].AsFloat,
                BeatDistributionType = node["d"].AsInt,
                TranslationDistribution = node["s"].AsFloat,
                TranslationDistributionType = node["t"].AsInt,
                TranslationAffectFirst = node["b"].AsInt,
                Easing = node["e"].AsInt,
                Axis = node["a"].AsInt,
                Flip = node["f"].AsInt
            };
            
            public static LightTranslationEventBox FromBaseLightTranslationEventBox(BaseLightTranslationEventBox baseLightTranslationEventBox) => new()
            {
                BeatDistribution = baseLightTranslationEventBox.BeatDistribution,
                BeatDistributionType = baseLightTranslationEventBox.BeatDistributionType,
                Easing = baseLightTranslationEventBox.Easing,
                TranslationDistribution = baseLightTranslationEventBox.TranslationDistribution,
                TranslationDistributionType = baseLightTranslationEventBox.TranslationDistributionType,
                TranslationAffectFirst = baseLightTranslationEventBox.TranslationAffectFirst,
                Axis = baseLightTranslationEventBox.Axis,
                Flip = baseLightTranslationEventBox.Flip
            };
            
            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["w"] = BeatDistribution;
                node["d"] = BeatDistributionType;
                node["s"] = TranslationDistribution;
                node["t"] = TranslationDistributionType;
                node["b"] = TranslationAffectFirst;
                node["e"] = Easing;
                node["a"] = Axis;
                node["f"] = Flip;

                return node;
            }

            public bool Equals(LightTranslationEventBox other) => BeatDistribution.Equals(other.BeatDistribution) 
                                                                  && BeatDistributionType == other.BeatDistributionType 
                                                                  && Easing == other.Easing 
                                                                  && TranslationDistribution.Equals(other.TranslationDistribution) 
                                                                  && TranslationDistributionType == other.TranslationDistributionType 
                                                                  && TranslationAffectFirst == other.TranslationAffectFirst 
                                                                  && Axis == other.Axis 
                                                                  && Flip == other.Flip;

            public override bool Equals(object obj) => obj is LightTranslationEventBox other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = BeatDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ BeatDistributionType;
                    hashCode = (hashCode * 397) ^ Easing;
                    hashCode = (hashCode * 397) ^ TranslationDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ TranslationDistributionType;
                    hashCode = (hashCode * 397) ^ TranslationAffectFirst;
                    hashCode = (hashCode * 397) ^ Axis;
                    hashCode = (hashCode * 397) ^ Flip;
                    return hashCode;
                }
            }
        }
        
        public struct LightTranslationEvent : IEquatable<LightTranslationEvent>
        {
            public int Easing { get; set; }
            public float Translation { get; set; }
            public int TransitionType { get; set; }
            
            public static LightTranslationEvent GetFromJson(JSONNode node) => new()
            {
                TransitionType = node["p"].AsInt,
                Easing = node["e"].AsInt, 
                Translation = node["t"].AsInt, 
            };

            public static LightTranslationEvent FromBaseLightTranslationEvent(BaseLightTranslationBase baseLightTranslationEvent) => new()
            {
                TransitionType = baseLightTranslationEvent.UsePrevious,
                Easing = baseLightTranslationEvent.EaseType,
                Translation = baseLightTranslationEvent.Translation,
            };

            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["p"] = TransitionType;
                node["e"] = Easing;
                node["t"] = Translation;

                return node;
            }

            public bool Equals(LightTranslationEvent other) => Easing == other.Easing
                                                               && Translation.Equals(other.Translation)
                                                               && TransitionType == other.TransitionType;

            public override bool Equals(object obj) => obj is LightTranslationEvent other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Easing;
                    hashCode = (hashCode * 397) ^ Translation.GetHashCode();
                    hashCode = (hashCode * 397) ^ TransitionType;
                    return hashCode;
                }
            }
        }
        
        public struct FxEventBox : IEquatable<FxEventBox>
        {
            public float BeatDistribution { get; set; }
            public int BeatDistributionType { get; set; }
            public int Easing { get; set; }
            public float FxDistribution { get; set; }
            public int FxDistributionType { get; set; }
            public int FxAffectFirst { get; set; }

            public static FxEventBox GetFromJson(JSONNode node) => new()
            {
                BeatDistribution = node["w"].AsFloat,
                BeatDistributionType = node["d"].AsInt,
                FxDistribution = node["s"].AsFloat,
                FxDistributionType = node["t"].AsInt,
                FxAffectFirst = node["b"].AsInt,
                Easing = node["e"].AsInt,
            };

            public static FxEventBox FromBaseFxEventBox(BaseVfxEventEventBox baseVfxEventEventBox) => new()
            {
                BeatDistribution = baseVfxEventEventBox.BeatDistribution,
                BeatDistributionType = baseVfxEventEventBox.BeatDistributionType,
                FxDistribution = baseVfxEventEventBox.VfxDistribution,
                FxDistributionType = baseVfxEventEventBox.VfxDistributionType,
                FxAffectFirst = baseVfxEventEventBox.VfxAffectFirst,
                Easing = baseVfxEventEventBox.Easing,
            };

            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["w"] = BeatDistribution;
                node["d"] = BeatDistributionType;
                node["s"] = FxDistribution;
                node["t"] = FxDistributionType;
                node["b"] = FxAffectFirst;
                node["e"] = Easing;

                return node;
            }

            public bool Equals(FxEventBox other) => BeatDistribution.Equals(other.BeatDistribution) 
                                                    && BeatDistributionType == other.BeatDistributionType 
                                                    && Easing == other.Easing 
                                                    && FxDistribution.Equals(other.FxDistribution)
                                                    && FxDistributionType == other.FxDistributionType
                                                    && FxAffectFirst == other.FxAffectFirst;

            public override bool Equals(object obj) => obj is FxEventBox other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = BeatDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ BeatDistributionType;
                    hashCode = (hashCode * 397) ^ Easing;
                    hashCode = (hashCode * 397) ^ FxDistribution.GetHashCode();
                    hashCode = (hashCode * 397) ^ FxDistributionType;
                    hashCode = (hashCode * 397) ^ FxAffectFirst;
                    return hashCode;
                }
            }
        }

        public struct FloatFxEvent : IEquatable<FloatFxEvent>
        {
            public int TransitionType { get; set; }
            public float Value { get; set; }
            public int Easing { get; set; }

            public static FloatFxEvent GetFromJson(JSONNode node) => new()
            {
                TransitionType = node["p"].AsInt, Easing = node["e"].AsInt, Value = node["v"].AsInt,
            };

            public static FloatFxEvent FromFloatFxEventBase(FloatFxEventBase floatFxEvent) => new()
            {
                TransitionType = floatFxEvent.UsePreviousEventValue,
                Value = floatFxEvent.Value,
                Easing = floatFxEvent.Easing
            };

            public JSONNode ToJson()
            {
                var node = new JSONObject();

                node["p"]= TransitionType; 
                node["e"]= Easing; 
                node["v"]= Value; 

                return node;
            }

            public bool Equals(FloatFxEvent other) => TransitionType == other.TransitionType
                                                      && Value.Equals(other.Value)
                                                      && Easing == other.Easing;

            public override bool Equals(object obj) => obj is FloatFxEvent other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = TransitionType;
                    hashCode = (hashCode * 397) ^ Value.GetHashCode();
                    hashCode = (hashCode * 397) ^ Easing;
                    return hashCode;
                }
            }
        }
    }
}
