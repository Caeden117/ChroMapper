using System;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseChain : BaseSlider, ICustomDataChain
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

        public BaseChain()
        {
        }

        public BaseChain(BaseChain other)
        {
            SetTimes(other.JsonTime);
            Color = other.Color;
            PosX = other.PosX;
            PosY = other.PosY;
            CutDirection = other.CutDirection;
            SetTailTimes(other.TailJsonTime);
            TailPosX = other.TailPosX;
            TailPosY = other.TailPosY;
            SliceCount = other.SliceCount;
            Squish = other.Squish;
            CustomData = other.CustomData.Clone();
        }

        public BaseChain(BaseNote start, BaseNote end)
        {
            SetTimes(start.JsonTime);
            Color = start.Color;
            PosX = start.PosX;
            PosY = start.PosY;
            CutDirection = start.CutDirection;
            SetTailTimes(end.JsonTime);
            TailPosX = end.PosX;
            TailPosY = end.PosY;
            SliceCount = 5;
            Squish = 1;
            CustomData = SaveCustomFromNotes(start, end);
        }

        // Used for Node Editor
        public BaseChain(JSONNode node) : this(BeatmapFactory.Chain(node)) {}

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

        public override string CustomKeyColor => V3Chain.CustomKeyColor;
        public override string CustomKeyTrack => V3Chain.CustomKeyTrack;
        public override string CustomKeyTailCoordinate => V3Chain.CustomKeyTailCoordinate;
        public override string CustomKeyAnimation => V3Chain.CustomKeyAnimation;
        public override string CustomKeyCoordinate => V3Chain.CustomKeyCoordinate;
        public override string CustomKeyWorldRotation => V3Chain.CustomKeyWorldRotation;
        public override string CustomKeyLocalRotation => V3Chain.CustomKeyLocalRotation;
        public override string CustomKeySpawnEffect => V3Chain.CustomKeySpawnEffect;
        public override string CustomKeyNoteJumpMovementSpeed => V3Chain.CustomKeyNoteJumpMovementSpeed;
        public override string CustomKeyNoteJumpStartBeatOffset => V3Chain.CustomKeyNoteJumpStartBeatOffset;
        
        public override bool IsChroma() =>
            CustomData != null &&
            ((CustomData.HasKey(CustomKeyColor) && CustomData[CustomKeyColor].IsArray) ||
             (CustomData.HasKey(CustomKeySpawnEffect) && CustomData[CustomKeySpawnEffect].IsBoolean) ||
             (CustomData.HasKey("disableDebris") && CustomData["disableDebris"].IsBoolean));

        public override bool IsNoodleExtensions() =>
            CustomData != null &&
            ((CustomData.HasKey("disableNoteGravity") && CustomData["disableNoteGravity"].IsBoolean) ||
             (CustomData.HasKey("disableNoteLook") && CustomData["disableNoteLook"].IsBoolean) ||
             (CustomData.HasKey("flip") && CustomData["flip"].IsArray) ||
             (CustomData.HasKey("uninteractable") && CustomData["uninteractable"].IsBoolean) ||
             (CustomData.HasKey(CustomKeyLocalRotation) && CustomData[CustomKeyLocalRotation].IsArray) ||
             (CustomData.HasKey(CustomKeyNoteJumpMovementSpeed) && CustomData[CustomKeyNoteJumpMovementSpeed].IsNumber) ||
             (CustomData.HasKey(CustomKeyNoteJumpStartBeatOffset) &&
              CustomData[CustomKeyNoteJumpStartBeatOffset].IsNumber) ||
             (CustomData.HasKey(CustomKeyCoordinate) && CustomData[CustomKeyCoordinate].IsArray) ||
             (CustomData.HasKey(CustomKeyWorldRotation) &&
              (CustomData[CustomKeyWorldRotation].IsArray || CustomData[CustomKeyWorldRotation].IsNumber)));

        public override bool IsMappingExtensions() =>
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 ||
             TailPosX <= -1000 || TailPosX >= 1000 || TailPosY < 0 || TailPosY > 2 ||
             (CutDirection >= 1000 && CutDirection <= 1360) ||
             (CutDirection >= 2000 && CutDirection <= 2360)) &&
            !IsNoodleExtensions();
        
        

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

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 or 4 => V3Chain.ToJson(this)
        };

        public override BaseItem Clone()
        {
            var chain = new BaseChain(this);
            chain.ParseCustom();
            return chain;
        }
    }
}
