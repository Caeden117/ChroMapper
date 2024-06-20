using System;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseChain : BaseSlider, ICustomDataChain
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SliceCount);
            writer.Put(Squish);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SliceCount = reader.GetInt();
            Squish = reader.GetFloat();
            base.Deserialize(reader);
        }

        public static readonly Vector3 ChainScale = new Vector3(1.5f, 0.8f, 1.5f);

        protected BaseChain()
        {
        }

        protected BaseChain(BaseChain other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Color = other.Color;
            PosX = other.PosX;
            PosY = other.PosY;
            CutDirection = other.CutDirection;
            SetTailTimes(other.TailJsonTime, other.TailSongBpmTime);
            TailPosX = other.TailPosX;
            TailPosY = other.TailPosY;
            SliceCount = other.SliceCount;
            Squish = other.Squish;
            CustomData = other.SaveCustom().Clone();
        }

        protected BaseChain(BaseNote start, BaseNote end)
        {
            SetTimes(start.JsonTime, start.SongBpmTime);
            Color = start.Color;
            PosX = start.PosX;
            PosY = start.PosY;
            CutDirection = start.CutDirection;
            SetTailTimes(end.JsonTime, end.SongBpmTime);
            TailPosX = end.PosX;
            TailPosY = end.PosY;
            SliceCount = 5;
            Squish = 1;
            CustomData = SaveCustomFromNotes(start, end);
        }

        protected BaseChain(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            float tailTime, int tailPosX, int tailPosY, int sliceCount, float squish, JSONNode customData = null) :
            base(time, posX, posY, color, cutDirection, angleOffset, tailTime, tailPosX, tailPosY, customData)
        {
            SliceCount = sliceCount;
            Squish = squish;
        }

        protected BaseChain(float jsonTime, float songBpmTime, int posX, int posY, int color, int cutDirection, int angleOffset,
            float tailJsonTime, float tailSongBpmTime, int tailPosX, int tailPosY, int sliceCount, float squish, JSONNode customData = null) :
            base(jsonTime, songBpmTime, posX, posY, color, cutDirection, angleOffset, tailJsonTime, tailSongBpmTime, tailPosX, tailPosY, customData)
        {
            SliceCount = sliceCount;
            Squish = squish;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Chain;
        public int SliceCount { get; set; }
        public float Squish { get; set; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseChain chain)
            {
                return base.IsConflictingWithObjectAtSameTime(other)
                    && SliceCount == chain.SliceCount
                    && Squish == chain.Squish;
            }

            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseChain chain)
            {
                SliceCount = chain.SliceCount;
                Squish = chain.Squish;
            }
        }
        
        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseChain chain) return comparison;

            // Compare by slice count if previous slider comparisons match
            if (comparison == 0) comparison = SliceCount.CompareTo(chain.SliceCount);

            // Compare by squish if slice counts match
            if (comparison == 0) comparison = Squish.CompareTo(chain.Squish);
            
            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0) comparison = string.Compare(CustomData?.ToString(), chain.CustomData?.ToString(), StringComparison.Ordinal);

            return comparison;
        }
    }
}
