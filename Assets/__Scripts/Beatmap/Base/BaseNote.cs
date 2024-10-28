using System;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseNote : BaseGrid, ICustomDataNote
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Color);
            writer.Put(Type);
            writer.Put(CutDirection);
            writer.Put(AngleOffset);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Color = reader.GetInt();
            Type = reader.GetInt();
            CutDirection = reader.GetInt();
            AngleOffset = reader.GetInt();
            base.Deserialize(reader);
        }

        public BaseNote()
        {
        }

        public BaseNote(BaseNote other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            PosX = other.PosX;
            PosY = other.PosY;
            Color = other.Color;
            Type = other.Type;
            CutDirection = other.CutDirection;
            AngleOffset = other.AngleOffset;
            CustomData = other.CustomData.Clone();
            CustomFake = other.CustomFake;
        }

        // Used for Node Editor
        public BaseNote(JSONNode node) : this(BeatmapFactory.Note(node)) {}
        
        protected override void ParseCustom()
        {
            base.ParseCustom();

            if (Settings.Instance.MapVersion == 2)
            {
                CustomDirection = (CustomData?.HasKey(CustomKeyDirection) ?? false) ? CustomData?[CustomKeyDirection].AsInt : null;
                CustomFake = (CustomData?.HasKey("_fake") ?? false) ? CustomData["_fake"].AsBool : false;
            }
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            
            if (Settings.Instance.MapVersion == 2)
            {
                if (CustomDirection != null) node[CustomKeyDirection] = CustomDirection;
                else node.Remove(CustomKeyDirection);
                if (CustomFake) node["_fake"] = true;
                else node.Remove("_fake");
            }

            SetCustomData(node);
            return node;
        }

        
        public override ObjectType ObjectType { get; set; } = ObjectType.Note;

        private int type;
        public int Type
        {
            get => type;
            set
            {
                type = value;
                color = value;
            }
        }

        private int color;
        public int Color
        {
            get => color;
            set
            {
                color = value;
                type = value;
            }
        }

        public int CutDirection { get; set; }
        public int AngleOffset { get; set; }

        public bool IsMainDirection => CutDirection is ((int)NoteCutDirection.Up) or
                                       ((int)NoteCutDirection.Down) or
                                       ((int)NoteCutDirection.Left) or
                                       ((int)NoteCutDirection.Right);

        public virtual float? CustomDirection { get; set; }

        public string CustomKeyDirection => V2Note.CustomKeyDirection;

        public override string CustomKeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyColor,
            3 => V3ColorNote.CustomKeyColor,
            _ => UndefinedKey
        };

        public override string CustomKeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyTrack,
            3 => V3ColorNote.CustomKeyTrack,
            _ => UndefinedKey
        };

        public override string CustomKeyAnimation => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyAnimation,
            3 => V3ColorNote.CustomKeyAnimation,
            _ => UndefinedKey
        };

        public override string CustomKeyCoordinate => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyCoordinate,
            3 => V3ColorNote.CustomKeyCoordinate,
            _ => UndefinedKey
        };

        public override string CustomKeyWorldRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyWorldRotation,
            3 => V3ColorNote.CustomKeyWorldRotation,
            _ => UndefinedKey
        };

        public override string CustomKeyLocalRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyLocalRotation,
            3 => V3ColorNote.CustomKeyLocalRotation,
            _ => UndefinedKey
        };

        public override string CustomKeySpawnEffect => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeySpawnEffect,
            3 => V3ColorNote.CustomKeySpawnEffect,
            _ => UndefinedKey
        };

        public override string CustomKeyNoteJumpMovementSpeed => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyNoteJumpMovementSpeed,
            3 => V3ColorNote.CustomKeyNoteJumpMovementSpeed,
            _ => UndefinedKey
        };

        public override string CustomKeyNoteJumpStartBeatOffset => Settings.Instance.MapVersion switch
        {
            2 => V2Note.CustomKeyNoteJumpStartBeatOffset,
            3 => V3ColorNote.CustomKeyNoteJumpStartBeatOffset,
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
             (CustomData.HasKey(CustomKeyNoteJumpStartBeatOffset) &&
              CustomData[CustomKeyNoteJumpStartBeatOffset].IsNumber) ||
             (CustomData.HasKey(CustomKeyCoordinate) && CustomData[CustomKeyCoordinate].IsArray) ||
             (CustomData.HasKey(CustomKeyWorldRotation) &&
              (CustomData[CustomKeyWorldRotation].IsArray || CustomData[CustomKeyWorldRotation].IsNumber)));

        public override bool IsMappingExtensions() =>
            PosX < 0 || PosX > 3 || PosY < 0 || PosY > 2 || (CutDirection >= 1000 && CutDirection <= 1360) ||
             (CutDirection >= 2000 && CutDirection <= 2360);
        

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseNote note)
            {
                Color = note.Color;
                CutDirection = note.CutDirection;
                AngleOffset = note.AngleOffset;
            }
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
           => other is BaseNote note && Vector2.Distance(note.GetPosition(), GetPosition()) < 0.1;

        // This should hopefully prevent flipped stack notes when playing in game.
        // (I'm done with note sorting; if you don't like it, go fix it yourself.)
        // TODO(Caeden): can this be done better
        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseNote note) return comparison;

            // Compare by X pos if times match
            if (comparison == 0) comparison = PosX.CompareTo(note.PosX);

            // Compare by Y pos if X pos match
            if (comparison == 0) comparison = PosY.CompareTo(note.PosY);
            
            // Compare by color if Y pos match
            if (comparison == 0) comparison = Color.CompareTo(note.Color);
            
            // Compare by cut direction if color matches
            if (comparison == 0) comparison = CutDirection.CompareTo(note.CutDirection);
            
            // Compare by angle offset if cut direction
            if (comparison == 0) comparison = AngleOffset.CompareTo(note.AngleOffset);

            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0) comparison = string.Compare(CustomData?.ToString(), note.CustomData?.ToString(), StringComparison.Ordinal);

            return comparison;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
            {
                2 => V2Note.ToJson(this),
                3 => Type == (int)NoteType.Bomb ? V3BombNote.ToJson(this) : V3ColorNote.ToJson(this)
            };

        public override BaseItem Clone()
        {
            var note = new BaseNote(this);
            note.ParseCustom();
            return note;
        }
    }
}
