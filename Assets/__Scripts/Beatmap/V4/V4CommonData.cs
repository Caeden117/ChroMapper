using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4CommonData
    {
        public struct Note
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
        }

        public struct Bomb
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
        }

        public struct Obstacle
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
        }

        public struct Arc
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
        }

        public struct Chain
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
        }

        public struct RotationEvent
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
        }

        public struct BasicEvent
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
        }

        public struct ColorBoostEvent
        {
            public int Boost { get; set; }

            public static ColorBoostEvent GetFromJson(JSONNode node) => new() { Boost = node["b"].AsInt };

            public static ColorBoostEvent FromBaseEvent(BaseEvent baseEvent) => new()
            {
                Boost = baseEvent.Value == 1 ? 1 : 0
            };
        }

        public struct Waypoint
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
        }
        
        
        public struct LightColorEventBox
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

            // public static BasicEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }
        
        public struct LightColorEvent
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
                StrobeBrightness = node["b"].AsInt, 
                StrobeFade = node["sf"].AsInt,
            };

            // public static LightColorEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }
        
        public struct LightRotationEventBox
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

            // public static BasicEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }
        
        public struct LightRotationEvent
        {
            public int Easing { get; set; }
            public int Color { get; set; }
            public float Rotation { get; set; }
            public int TransitionType { get; set; }
            public int Direction { get; set; }
            public int Loop { get; set; }
            
            public static LightRotationEvent GetFromJson(JSONNode node) => new()
            {
                TransitionType = node["p"].AsInt,
                Easing = node["e"].AsInt, 
                Color = node["c"].AsInt, 
                Rotation = node["r"].AsInt, 
                Direction = node["d"].AsInt, 
                Loop = node["l"].AsInt,
            };

            // public static LightColorEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }
        
        public struct LightTranslationEventBox
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

            // public static BasicEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }
        
        public struct LightTranslationEvent
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

            // public static LightColorEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }
        
        public struct FxEventBox
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

            // public static LightColorEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }

        public struct FloatFxEvent
        {
            public int TransitionType { get; set; }
            public float Value { get; set; }
            public int Easing { get; set; }

            public static FloatFxEvent GetFromJson(JSONNode node) => new()
            {
                TransitionType = node["p"].AsInt, Easing = node["e"].AsInt, Value = node["v"].AsInt,
            };

            // public static LightColorEvent FromBaseEvent(BaseEvent baseEvent) => new()
            // {
            //     Type = baseEvent.Type, Value = baseEvent.Value, FloatValue = baseEvent.FloatValue
            // };
        }
    }
}
