using System;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using LiteNetLib.Utils;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseArc : BaseSlider, ICustomDataArc
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(HeadControlPointLengthMultiplier);
            writer.Put(TailCutDirection);
            writer.Put(TailControlPointLengthMultiplier);
            writer.Put(MidAnchorMode);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            HeadControlPointLengthMultiplier = reader.GetFloat();
            TailCutDirection = reader.GetInt();
            TailControlPointLengthMultiplier = reader.GetFloat();
            MidAnchorMode = reader.GetInt();
            base.Deserialize(reader);
        }

        public BaseArc()
        {
        }

        public BaseArc(BaseArc other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Color = other.Color;
            PosX = other.PosX;
            PosY = other.PosY;
            CutDirection = other.CutDirection;
            HeadControlPointLengthMultiplier = other.HeadControlPointLengthMultiplier;
            SetTailTimes(other.TailJsonTime, other.TailSongBpmTime);
            TailPosX = other.TailPosX;
            TailPosY = other.TailPosY;
            TailCutDirection = other.TailCutDirection;
            TailControlPointLengthMultiplier = other.TailControlPointLengthMultiplier;
            MidAnchorMode = other.MidAnchorMode;
            CustomData = other.CustomData.Clone();
        }

        public BaseArc(BaseNote start, BaseNote end)
        {
            SetTimes(start.JsonTime, start.SongBpmTime);
            Color = start.Color;
            PosX = start.PosX;
            PosY = start.PosY;
            CutDirection = start.CutDirection;
            HeadControlPointLengthMultiplier = 1f;
            SetTailTimes(end.JsonTime, end.SongBpmTime);
            TailPosX = end.PosX;
            TailPosY = end.PosY;
            TailCutDirection = end.CutDirection;
            TailControlPointLengthMultiplier = 1f;
            MidAnchorMode = 0;
            CustomData = SaveCustomFromNotes(start, end);
        }

        // Used for Node Editor
        public BaseArc(JSONNode node) : this(BeatmapFactory.Arc(node)) {}

        public override ObjectType ObjectType { get; set; } = ObjectType.Arc;
        public float HeadControlPointLengthMultiplier { get; set; }
        public int TailCutDirection { get; set; }
        public float TailControlPointLengthMultiplier { get; set; }
        public int MidAnchorMode { get; set; }

        public override string CustomKeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyColor,
            3 => V3Arc.CustomKeyColor,
            _ => UndefinedKey
        };

        public override string CustomKeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyTrack,
            3 => V3Arc.CustomKeyTrack,
            _ => UndefinedKey
        };
        

        public override string CustomKeyTailCoordinate => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyTailCoordinate,
            3 => V3Arc.CustomKeyTailCoordinate,
            _ => UndefinedKey
        };
        

        public override string CustomKeyAnimation => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyAnimation,
            3 => V3Arc.CustomKeyAnimation,
            _ => UndefinedKey
        };
        
        public override string CustomKeyCoordinate => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyCoordinate,
            3 => V3Arc.CustomKeyCoordinate,
            _ => UndefinedKey
        };
        
        public override string CustomKeyWorldRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyWorldRotation,
            3 => V3Arc.CustomKeyWorldRotation,
            _ => UndefinedKey
        };
        
        public override string CustomKeyLocalRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyLocalRotation,
            3 => V3Arc.CustomKeyLocalRotation,
            _ => UndefinedKey
        };
        
        public override string CustomKeySpawnEffect => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeySpawnEffect,
            3 => V3Arc.CustomKeySpawnEffect,
            _ => UndefinedKey
        };
        
        public override string CustomKeyNoteJumpMovementSpeed => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyNoteJumpMovementSpeed,
            3 => V3Arc.CustomKeyNoteJumpMovementSpeed,
            _ => UndefinedKey
        };
        
        public override string CustomKeyNoteJumpStartBeatOffset => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.CustomKeyNoteJumpStartBeatOffset,
            3 => V3Arc.CustomKeyNoteJumpStartBeatOffset,
            _ => UndefinedKey
        };
        
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
             (CustomData.HasKey(CustomKeyNoteJumpStartBeatOffset) && CustomData[CustomKeyNoteJumpStartBeatOffset].IsNumber) ||
             (CustomData.HasKey(CustomKeyCoordinate) && CustomData[CustomKeyCoordinate].IsArray) ||
             (CustomData.HasKey(CustomKeyTailCoordinate) && CustomData[CustomKeyTailCoordinate].IsArray) ||
             (CustomData.HasKey(CustomKeyWorldRotation) &&
              (CustomData[CustomKeyWorldRotation].IsArray || CustomData[CustomKeyWorldRotation].IsNumber)));

        public override bool IsMappingExtensions() =>
            (PosX <= -1000 || PosX >= 1000 || PosY < 0 || PosY > 2 ||
             TailPosX <= -1000 || TailPosX >= 1000 || TailPosY < 0 || TailPosY > 2 ||
             (CutDirection >= 1000 && CutDirection <= 1360) ||
             (CutDirection >= 2000 && CutDirection <= 2360) ||
             (TailCutDirection >= 1000 && TailCutDirection <= 1360)) &&
            !IsNoodleExtensions();

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseArc arc)
            {
                return base.IsConflictingWithObjectAtSameTime(other)
                    && HeadControlPointLengthMultiplier == arc.HeadControlPointLengthMultiplier
                    && TailCutDirection == arc.TailCutDirection
                    && TailControlPointLengthMultiplier == arc.TailControlPointLengthMultiplier
                    && MidAnchorMode == arc.MidAnchorMode;
            }

            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseArc arc)
            {
                HeadControlPointLengthMultiplier = arc.HeadControlPointLengthMultiplier;
                TailCutDirection = arc.TailCutDirection;
                TailControlPointLengthMultiplier = arc.TailControlPointLengthMultiplier;
                MidAnchorMode = arc.MidAnchorMode;
            }
        }

        public override void SwapHeadAndTail()
        {
            base.SwapHeadAndTail();
            (CutDirection, TailCutDirection) = (TailCutDirection, CutDirection);
            (HeadControlPointLengthMultiplier, TailControlPointLengthMultiplier) = (TailControlPointLengthMultiplier, HeadControlPointLengthMultiplier);
        }
        
        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseArc arc) return comparison;

            // Compare by mu if previous slider comparisons match
            if (comparison == 0) comparison = HeadControlPointLengthMultiplier.CompareTo(arc.HeadControlPointLengthMultiplier);

            // Compare by tmu if mu matches
            if (comparison == 0) comparison = TailControlPointLengthMultiplier.CompareTo(arc.TailControlPointLengthMultiplier);
            
            // Compare by tail cut direction if tmu matches
            if (comparison == 0) comparison = TailCutDirection.CompareTo(arc.TailCutDirection);

            // Compare by mid anchor if tail cut match
            if (comparison == 0) comparison = MidAnchorMode.CompareTo(arc.MidAnchorMode);

            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0) comparison = string.Compare(CustomData?.ToString(), arc.CustomData?.ToString(), StringComparison.Ordinal);

            return comparison;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2Arc.ToJson(this),
            3 => V3Arc.ToJson(this)
        };

        public override BaseItem Clone()
        {
            var arc = new BaseArc(this);
            arc.ParseCustom();
            return arc;
        }
    }
}
