using System.Linq;
using Beatmap.Base;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;

namespace Tests.Util
{
    public class CheckUtils
    {
        public static void CheckNote(string msg, BeatmapObjectContainerCollection container, int idx, float time,
            int posX, int posY, int type, int cutDirection, int angleOffset, JSONNode customData = null)
        {
            BaseObject obj = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseNote>(obj);
            if (obj is BaseNote note)
            {
                Assert.AreEqual(time, note.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(type, note.Type, $"{msg}: Mismatched type");
                Assert.AreEqual(posX, note.PosX, $"{msg}: Mismatched position X");
                Assert.AreEqual(posY, note.PosY, $"{msg}: Mismatched position Y");
                Assert.AreEqual(cutDirection, note.CutDirection, $"{msg}: Mismatched cut direction");
                Assert.AreEqual(angleOffset, note.AngleOffset, $"{msg}: Mismatched angle offset");

                if (customData != null)
                {
                    note.SaveCustom();
                    Assert.AreEqual(customData.ToString(), note.CustomData.ToString(), $"{msg}: Mismatched custom data");
                }
            }
        }
        
        public static void CheckWall(string msg, BeatmapObjectContainerCollection container, int idx, float time,
            int posX, int posY, int? type, float duration, int width, int height, JSONNode customData = null)
        {
            BaseObject obj = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseObstacle>(obj);
            if (obj is BaseObstacle wall)
            {
                Assert.AreEqual(time, wall.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(posX, wall.PosX, $"{msg}: Mismatched position X");
                Assert.AreEqual(posY, wall.PosY, $"{msg}: Mismatched position Y");
                Assert.AreEqual(duration, wall.Duration, 0.001f, $"{msg}: Mismatched duration");
                Assert.AreEqual(width, wall.Width, $"{msg}: Mismatched width");
                Assert.AreEqual(height, wall.Height, $"{msg}: Mismatched height");
                
                if (type != null)
                {
                    Assert.AreEqual(type, wall.Type, $"{msg}: Mismatched type");
                }

                if (customData != null)
                {
                    wall.SaveCustom();
                    Assert.AreEqual(customData.ToString(), wall.CustomData.ToString(), $"{msg}: Mismatched custom data");
                }
            }
        }
        
        public static void CheckEvent(string msg, BeatmapObjectContainerCollection container, int idx, float time, int type, int value, float floatValue = 1f, JSONNode customData = null)
        {
            BaseObject obj = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseEvent>(obj);
            if (obj is BaseEvent evt)
            {
                Assert.AreEqual(time, evt.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(type, evt.Type, $"{msg}: Mismatched type");
                Assert.AreEqual(value, evt.Value, $"{msg}: Mismatched value");
                Assert.AreEqual(floatValue, evt.FloatValue, 0.001f, $"{msg}: Mismatched float value");

                // ConvertToJSON causes gradient to get updated
                if (customData != null)
                {
                    // Custom data needed to be saved before compare
                    evt.SaveCustom();
                    Assert.AreEqual(customData.ToString(), evt.CustomData?.ToString(), $"{msg}: Mismatched custom data");
                }
            }
        }
        
        public static void CheckArc(string msg, BeatmapObjectContainerCollection container, int idx, float time,
            int posX, int posY, int color, int cutDirection, int angleOffset, float mult, float tailTime, int tailPosX,
            int tailPosY,
            int tailCutDirection, float tailMult, int midAnchorMode, JSONNode customData = null)
        {
            BaseObject obj = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseArc>(obj);
            if (obj is BaseArc arc)
            {
                Assert.AreEqual(time, arc.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(color, arc.Color, $"{msg}: Mismatched color");
                Assert.AreEqual(posX, arc.PosX, $"{msg}: Mismatched position X");
                Assert.AreEqual(posY, arc.PosY, $"{msg}: Mismatched position Y");
                Assert.AreEqual(cutDirection, arc.CutDirection, $"{msg}: Mismatched cut direction");
                Assert.AreEqual(angleOffset, arc.AngleOffset, $"{msg}: Mismatched angle offset");
                Assert.AreEqual(mult, arc.HeadControlPointLengthMultiplier, $"{msg}: Mismatched head control point length multiplier");
                Assert.AreEqual(tailTime, arc.TailTime, 0.001f, $"{msg}: Mismatched tail time");
                Assert.AreEqual(tailPosX, arc.TailPosX, $"{msg}: Mismatched tail position X");
                Assert.AreEqual(tailPosY, arc.TailPosY, $"{msg}: Mismatched tail position Y");
                Assert.AreEqual(tailCutDirection, arc.TailCutDirection, $"{msg}: Mismatched tail cut direction");
                Assert.AreEqual(tailMult, arc.TailControlPointLengthMultiplier, $"{msg}: Mismatched tail control point length multiplier");
                Assert.AreEqual(midAnchorMode, arc.MidAnchorMode, $"{msg}: Mismatched mid anchor mode");

                if (customData != null)
                {
                    arc.SaveCustom();
                    Assert.AreEqual(customData.ToString(), arc.CustomData.ToString(), $"{msg}: Mismatched custom data");
                }
            }
        }
        
        public static void CheckChain(string msg, BeatmapObjectContainerCollection container, int idx, float time,
            int posX, int posY, int color, int cutDirection, int angleOffset, float tailTime, int tailPosX,
            int tailPosY, int sliceCount,
            float squish, JSONNode customData = null)
        {
            BaseObject obj = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseChain>(obj);
            if (obj is BaseChain chain)
            {
                Assert.AreEqual(time, chain.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(color, chain.Color, $"{msg}: Mismatched color");
                Assert.AreEqual(posX, chain.PosX, $"{msg}: Mismatched position X");
                Assert.AreEqual(posY, chain.PosY, $"{msg}: Mismatched position Y");
                Assert.AreEqual(cutDirection, chain.CutDirection, $"{msg}: Mismatched cut direction");
                Assert.AreEqual(tailTime, chain.TailTime, 0.001f, $"{msg}: Mismatched tail time");
                Assert.AreEqual(tailPosX, chain.TailPosX, $"{msg}: Mismatched tail position X");
                Assert.AreEqual(tailPosY, chain.TailPosY, $"{msg}: Mismatched tail position Y");
                Assert.AreEqual(sliceCount, chain.SliceCount, $"{msg}: Mismatched slice count");
                Assert.AreEqual(squish, chain.Squish, $"{msg}: Mismatched squish");

                if (customData != null)
                {
                    chain.SaveCustom();
                    Assert.AreEqual(customData.ToString(), chain.CustomData.ToString(), $"{msg}: Mismatched custom data");
                }
            }
        }
        
        public static void CheckV3Object(string msg, BeatmapObjectContainerCollection container, int idx)
        {
            BaseObject obj = container.LoadedObjects.Skip(idx).First();
            
            if (!(obj is V3Object))
            {
                Assert.Fail($"{msg}: Object is not beatmap v3 object");
            }
        }
        
        public static void CheckV2Object(string msg, BeatmapObjectContainerCollection container, int idx)
        {
            BaseObject obj = container.LoadedObjects.Skip(idx).First();
            
            if (!(obj is V2Object))
            {
                Assert.Fail($"{msg}: Object is not beatmap v2 object");
            }
        }
    }
}