using System.Collections.Generic;
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
        public static void CheckNote(string msg, NoteGridContainer container, int idx, float time,
            int posX, int posY, int type, int cutDirection, int angleOffset, JSONNode customData = null)
        {
            var note = container.MapObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseNote>(note);
          
            Assert.AreEqual(time, note.JsonTime, 0.001f, $"{msg}: Mismatched time");
            Assert.AreEqual(type, note.Type, $"{msg}: Mismatched type");
            Assert.AreEqual(posX, note.PosX, $"{msg}: Mismatched position X");
            Assert.AreEqual(posY, note.PosY, $"{msg}: Mismatched position Y");
            Assert.AreEqual(cutDirection, note.CutDirection, $"{msg}: Mismatched cut direction");
            Assert.AreEqual(angleOffset, note.AngleOffset, $"{msg}: Mismatched angle offset");

            if (customData != null)
            {
                Assert.AreEqual(customData.ToString(), note.CustomData.ToString(),
                    $"{msg}: Mismatched custom data");
            }
        }

        public static void CheckWall(string msg, ObstacleGridContainer container, int idx, float time,
            int posX, int posY, int? type, float duration, int width, int height, JSONNode customData = null)
        {
            var wall = container.MapObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseObstacle>(wall);
            
            Assert.AreEqual(time, wall.JsonTime, 0.001f, $"{msg}: Mismatched time");
            Assert.AreEqual(posX, wall.PosX, $"{msg}: Mismatched position X");
            Assert.AreEqual(posY, wall.PosY, $"{msg}: Mismatched position Y");
            Assert.AreEqual(duration, wall.Duration, 0.001f, $"{msg}: Mismatched duration");
            Assert.AreEqual(width, wall.Width, $"{msg}: Mismatched width");
            Assert.AreEqual(height, wall.Height, $"{msg}: Mismatched height");

            if (type != null) Assert.AreEqual(type, wall.Type, $"{msg}: Mismatched type");

            if (customData != null)
            {
                Assert.AreEqual(customData.ToString(), wall.CustomData.ToString(),
                    $"{msg}: Mismatched custom data");
            }
        }

        public static void CheckEvent(string msg, EventGridContainer container, int idx, float time,
            int type, int value, float floatValue = 1f, JSONNode customData = null)
        {
            var evt = container.MapObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseEvent>(evt);
        
            Assert.AreEqual(time, evt.JsonTime, 0.001f, $"{msg}: Mismatched time");
            Assert.AreEqual(type, evt.Type, $"{msg}: Mismatched type");
            Assert.AreEqual(value, evt.Value, $"{msg}: Mismatched value");
            Assert.AreEqual(floatValue, evt.FloatValue, 0.001f, $"{msg}: Mismatched float value");

            // ConvertToJSON causes gradient to get updated
            if (customData != null)
            {
                // Custom data needed to be saved before compare
                Assert.AreEqual(customData.ToString(), evt.CustomData?.ToString(),
                    $"{msg}: Mismatched custom data");
            }
        }

        public static void CheckEventPrevAndNext(string msg, BaseEvent evt, BaseEvent prevEvt, BaseEvent nextEvt)
        {
            if (prevEvt != null)
                Assert.AreEqual(evt, prevEvt.Next, $"{msg} Mismatched Prev.Next");

            Assert.AreEqual(prevEvt, evt.Prev, $"{msg} Mismatched Event.Prev");
            Assert.AreEqual(nextEvt, evt.Next, $"{msg} Mismatched Event.Next");

            if (nextEvt != null)
                Assert.AreEqual(evt, nextEvt.Prev, $"{msg} Mismatched Next.Prev");
        }

        public static void CheckRotationEvent(string msg, EventGridContainer container, int idx, float time,
            int executionTime, float rotation, JSONNode customData = null)
        {
            var evt = container.MapObjects.Skip(idx).First();

            Assert.AreEqual(time, evt.JsonTime, 0.001f, $"{msg}: Mismatched time");
            Assert.AreEqual(executionTime == 0 ? 14 : 15, evt.Type, $"{msg}: Mismatched type");
            Assert.AreEqual(executionTime, evt.Type == 14 ? 0 : 1, $"{msg}: Mismatched execution time");
            Assert.AreEqual(rotation, evt.Rotation, 0.001f, $"{msg}: Mismatched rotation");
        
            // ConvertToJSON causes gradient to get updated
            if (customData != null)
            {
                // Custom data needed to be saved before compare
                Assert.AreEqual(customData.ToString(), evt.CustomData?.ToString(),
                    $"{msg}: Mismatched custom data");
            }
        }

        public static void CheckArc(string msg, ArcGridContainer container, int idx, float time,
            int posX, int posY, int color, int cutDirection, int angleOffset, float mult, float tailTime, int tailPosX,
            int tailPosY,
            int tailCutDirection, float tailMult, int midAnchorMode, JSONNode customData = null)
        {
            var arc = container.MapObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseArc>(arc);
            
            Assert.AreEqual(time, arc.JsonTime, 0.001f, $"{msg}: Mismatched time");
            Assert.AreEqual(color, arc.Color, $"{msg}: Mismatched color");
            Assert.AreEqual(posX, arc.PosX, $"{msg}: Mismatched position X");
            Assert.AreEqual(posY, arc.PosY, $"{msg}: Mismatched position Y");
            Assert.AreEqual(cutDirection, arc.CutDirection, $"{msg}: Mismatched cut direction");
            Assert.AreEqual(angleOffset, arc.AngleOffset, $"{msg}: Mismatched angle offset");
            Assert.AreEqual(mult, arc.HeadControlPointLengthMultiplier,
                $"{msg}: Mismatched head control point length multiplier");
            Assert.AreEqual(tailTime, arc.TailJsonTime, 0.001f, $"{msg}: Mismatched tail time");
            Assert.AreEqual(tailPosX, arc.TailPosX, $"{msg}: Mismatched tail position X");
            Assert.AreEqual(tailPosY, arc.TailPosY, $"{msg}: Mismatched tail position Y");
            Assert.AreEqual(tailCutDirection, arc.TailCutDirection, $"{msg}: Mismatched tail cut direction");
            Assert.AreEqual(tailMult, arc.TailControlPointLengthMultiplier,
                $"{msg}: Mismatched tail control point length multiplier");
            Assert.AreEqual(midAnchorMode, arc.MidAnchorMode, $"{msg}: Mismatched mid anchor mode");

            if (customData != null)
            {
                arc.WriteCustom();
                Assert.AreEqual(customData.ToString(), arc.CustomData.ToString(), $"{msg}: Mismatched custom data");
            }
        }

        public static void CheckChain(string msg, ChainGridContainer container, int idx, float time,
            int posX, int posY, int color, int cutDirection, int angleOffset, float tailTime, int tailPosX,
            int tailPosY, int sliceCount,
            float squish, JSONNode customData = null)
        {
            var chain = container.MapObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseChain>(chain);
   
            Assert.AreEqual(time, chain.JsonTime, 0.001f, $"{msg}: Mismatched time");
            Assert.AreEqual(color, chain.Color, $"{msg}: Mismatched color");
            Assert.AreEqual(posX, chain.PosX, $"{msg}: Mismatched position X");
            Assert.AreEqual(posY, chain.PosY, $"{msg}: Mismatched position Y");
            Assert.AreEqual(cutDirection, chain.CutDirection, $"{msg}: Mismatched cut direction");
            Assert.AreEqual(angleOffset, chain.AngleOffset, $"{msg}: Mismatched angle offset");
            Assert.AreEqual(tailTime, chain.TailJsonTime, 0.001f, $"{msg}: Mismatched tail time");
            Assert.AreEqual(tailPosX, chain.TailPosX, $"{msg}: Mismatched tail position X");
            Assert.AreEqual(tailPosY, chain.TailPosY, $"{msg}: Mismatched tail position Y");
            Assert.AreEqual(sliceCount, chain.SliceCount, $"{msg}: Mismatched slice count");
            Assert.AreEqual(squish, chain.Squish, $"{msg}: Mismatched squish");

            if (customData != null)
            {
                chain.WriteCustom();
                Assert.AreEqual(customData.ToString(), chain.CustomData.ToString(),
                    $"{msg}: Mismatched custom data");
            }
        }

        public static void CheckV3Object(string msg, BeatmapObjectContainerCollection container, int idx)
        {
            var obj = container.LoadedObjects.Skip(idx).First();

            if (!(obj is V3Object)) Assert.Fail($"{msg}: Object is not beatmap v3 object");
        }

        public static void CheckV2Object(string msg, BeatmapObjectContainerCollection container, int idx)
        {
            var obj = container.LoadedObjects.Skip(idx).First();

            if (!(obj is V2Object)) Assert.Fail($"{msg}: Object is not beatmap v2 object");
        }
        
        public static void CheckNotesAreSorted(IReadOnlyList<BaseNote> noteMapObjects)
        {
            for (var i = 1; i < noteMapObjects.Count; i++)
            {
                if (noteMapObjects[i - 1].CompareTo(noteMapObjects[i]) == 1)
                {
                    Assert.Fail($"Notes {noteMapObjects[i - 1]} and {noteMapObjects[i]} are out of order | i = {i}");
                }
            }
        }

        public static void CheckEventsAreSorted(IReadOnlyList<BaseEvent> eventMapObjects)
        {
            for (var i = 1; i < eventMapObjects.Count; i++)
            {
                if (eventMapObjects[i - 1].CompareTo(eventMapObjects[i]) == 1)
                {
                    Assert.Fail($"Events {eventMapObjects[i - 1]} and {eventMapObjects[i]} are out of order | i = {i}");
                }
            }
        }

        public static void CheckEventsLinksAreCorrectAndSorted(IReadOnlyList<BaseEvent> eventMapObjects)
        {
            if (eventMapObjects.Count == 1)
            {
                CheckEventPrevAndNext($"0", eventMapObjects[0], null, null);
                return;
            }

            for (var i = 0; i < eventMapObjects.Count; i++)
            {
                if (i == 0)
                    CheckEventPrevAndNext($"{i}", eventMapObjects[i], null, eventMapObjects[i + 1]);
                else if (i == eventMapObjects.Count - 1)
                    CheckEventPrevAndNext($"{i}", eventMapObjects[i], eventMapObjects[i - 1], null);
                else
                    CheckEventPrevAndNext($"{i}", eventMapObjects[i], eventMapObjects[i - 1], eventMapObjects[i + 1]);
            }
            
            for (var i = 1; i < eventMapObjects.Count; i++)
            {
                if (eventMapObjects[i - 1].CompareTo(eventMapObjects[i]) == 1)
                {
                    Assert.Fail($"Events {eventMapObjects[i - 1]} and {eventMapObjects[i]} are out of order | i = {i}");
                }
            }
        }
    }
}