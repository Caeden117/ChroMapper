using Beatmap.Base.Customs;
using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseNote : BaseGrid, ICustomDataNote
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

        private int color;
        private int type;

        protected BaseNote()
        {
        }

        protected BaseNote(BaseNote other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            PosX = other.PosX;
            PosY = other.PosY;
            Color = other.Color;
            Type = other.Type;
            CutDirection = other.CutDirection;
            AngleOffset = other.AngleOffset;
            CustomData = other.SaveCustom().Clone();
            CustomFake = other.CustomFake;
        }

        protected BaseNote(BaseBombNote baseBomb)
        {
            SetTimes(baseBomb.JsonTime, baseBomb.SongBpmTime);
            PosX = baseBomb.PosX;
            PosY = baseBomb.PosY;
            Color = (int)NoteType.Bomb;
            Type = (int)NoteType.Bomb;
            CutDirection = 0;
            AngleOffset = 0;
            CustomData = baseBomb.SaveCustom().Clone();
            CustomFake = baseBomb.CustomFake;
        }

        protected BaseNote(BaseSlider slider)
        {
            SetTimes(slider.JsonTime, slider.SongBpmTime);
            PosX = slider.PosX;
            PosY = slider.PosY;
            Color = slider.Color;
            Type = slider.Color;
            CutDirection = slider.CutDirection;
            AngleOffset = 0;
            CustomData = slider.SaveCustom().Clone();
            CustomFake = slider.CustomFake;
        }

        protected BaseNote(float time, int posX, int posY, int type, int cutDirection,
            JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Type = type;
            CutDirection = cutDirection;
            AngleOffset = 0;
            InferColor();
        }

        protected BaseNote(float jsonTime, float songBpmTime, int posX, int posY, int type, int cutDirection,
            JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, customData)
        {
            Type = type;
            CutDirection = cutDirection;
            AngleOffset = 0;
            InferColor();
        }

        protected BaseNote(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Color = color;
            CutDirection = cutDirection;
            AngleOffset = angleOffset;
            InferType();
        }

        protected BaseNote(float jsonTime, float songBpmTime, int posX, int posY, int color, int cutDirection, int angleOffset,
            JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, customData)
        {
            Color = color;
            CutDirection = cutDirection;
            AngleOffset = angleOffset;
            InferType();
        }


        public override ObjectType ObjectType { get; set; } = ObjectType.Note;

        public int Type
        {
            get => type;
            set
            {
                type = value;
                color = value;
            }
        }

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

        public bool IsMainDirection => CutDirection == (int)NoteCutDirection.Up ||
                                       CutDirection == (int)NoteCutDirection.Down ||
                                       CutDirection == (int)NoteCutDirection.Left ||
                                       CutDirection == (int)NoteCutDirection.Right;

        public virtual float? CustomDirection { get; set; }

        public abstract string CustomKeyDirection { get; }

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
        {
            // Only down to 1/4 spacing
            if (other is BaseBombNote || other is BaseNote)
                return Vector2.Distance(((BaseGrid)other).GetPosition(), GetPosition()) < 0.1;
            return false;
        }

        protected void InferType() => Type = Color;

        protected void InferColor() => Color = Type;
    }
}
