using System;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public static class BeatmapAssert
    {
        private const float epsilon = 0.001f;

        public static void NotePropertiesAreEqual(BaseNote note, float jsonTime, int x, int y, int type, int cutDirection, int angleOffset)
        {
            Assert.AreEqual(jsonTime, note.JsonTime, epsilon);
            Assert.AreEqual(x, note.PosX);
            Assert.AreEqual(y, note.PosY);
            Assert.AreEqual(type, note.Type);
            Assert.AreEqual(cutDirection, note.CutDirection);
            Assert.AreEqual(angleOffset, note.AngleOffset);
        }

        public static void ObstaclePropertiesAreEqual(BaseObstacle obstacle, float jsonTime, int x, int y, int type, int width,
            int height, float duration)
        {
            Assert.AreEqual(jsonTime, obstacle.JsonTime, epsilon);
            Assert.AreEqual(x, obstacle.PosX);
            Assert.AreEqual(y, obstacle.PosY);
            Assert.AreEqual(type, obstacle.Type);
            Assert.AreEqual(width, obstacle.Width);
            Assert.AreEqual(height, obstacle.Height);
            Assert.AreEqual(duration, obstacle.Duration, epsilon);
            
        }

        public static void BpmEventPropertiesAreEqual(BaseBpmEvent bpmEvent, float jsonTime, float floatValue)
        {
            Assert.AreEqual(jsonTime, bpmEvent.JsonTime, epsilon);
            Assert.AreEqual((int)EventTypeValue.BpmChange, bpmEvent.Type);
            Assert.AreEqual(floatValue, bpmEvent.FloatValue, epsilon);
        }
        
        public static void EventPropertiesAreEqual(BaseEvent evt, float jsonTime, int type, int value, float floatValue,
            float? rotation = null)
        {
            Assert.AreEqual(jsonTime, evt.JsonTime, epsilon);
            Assert.AreEqual(type, evt.Type);
            Assert.AreEqual(value, evt.Value);
            Assert.AreEqual(floatValue, evt.FloatValue);
            
            // TODO: Rotation needs to be part of BaseEvent
            if (rotation != null)
            {
                Assert.AreEqual(rotation, ((BaseRotationEvent)evt).Rotation);
            }
        }

        public static void ArcPropertiesAreEqual(BaseArc arc, float jsonTime, int x, int y, int color, int cutDirection,
            float headMult, float tailJsonTime, int tailX, int tailY, int tailCutDirection, float tailMult,
            int midPointAnchor)
        {
            Assert.AreEqual(jsonTime, arc.JsonTime, epsilon);
            Assert.AreEqual(x, arc.PosX);
            Assert.AreEqual(y, arc.PosY);
            Assert.AreEqual(color, arc.Color);
            Assert.AreEqual(cutDirection, arc.CutDirection);
            Assert.AreEqual(headMult, arc.HeadControlPointLengthMultiplier, epsilon);
            Assert.AreEqual(tailJsonTime, arc.TailJsonTime, epsilon);
            Assert.AreEqual(tailX, arc.TailPosX);
            Assert.AreEqual(tailY, arc.TailPosY);
            Assert.AreEqual(tailCutDirection, arc.TailCutDirection);
            Assert.AreEqual(tailMult, arc.TailControlPointLengthMultiplier, epsilon);
            Assert.AreEqual(midPointAnchor, arc.MidAnchorMode);
        }
        
        public static void ChainPropertiesAreEqual(BaseChain chain, float jsonTime, int x, int y, int color,
            int cutDirection, float tailJsonTime, int tailX, int tailY, int sliceCount, float squish)
        {
            Assert.AreEqual(jsonTime, chain.JsonTime, epsilon);
            Assert.AreEqual(x, chain.PosX);
            Assert.AreEqual(y, chain.PosY);
            Assert.AreEqual(color, chain.Color);
            Assert.AreEqual(cutDirection, chain.CutDirection);
            Assert.AreEqual(tailJsonTime, chain.TailJsonTime, epsilon);
            Assert.AreEqual(tailX, chain.TailPosX);
            Assert.AreEqual(tailY, chain.TailPosY);
            Assert.AreEqual(sliceCount, chain.SliceCount, epsilon);
            Assert.AreEqual(squish, chain.Squish);
        }
    }
}