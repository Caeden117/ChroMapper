using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class INote : IGrid, ICustomDataNote
    {
        private int _color;
        protected int? _customDirection;
        private int _type;

        protected INote()
        {
        }

        protected INote(INote other)
        {
            Time = other.Time;
            PosX = other.PosX;
            PosY = other.PosY;
            Color = other.Color;
            Type = other.Type;
            CutDirection = other.CutDirection;
            AngleOffset = other.AngleOffset;
            CustomData = other.CustomData?.Clone();
        }

        protected INote(IBombNote bomb)
        {
            Time = bomb.Time;
            PosX = bomb.PosX;
            PosY = bomb.PosY;
            Color = (int)NoteType.Bomb;
            Type = (int)NoteType.Bomb;
            CutDirection = 0;
            AngleOffset = 0;
            CustomData = bomb.CustomData?.Clone();
        }

        protected INote(IBaseSlider slider)
        {
            Time = slider.Time;
            PosX = slider.PosX;
            PosY = slider.PosY;
            Color = slider.Color;
            Type = slider.Color;
            CutDirection = slider.CutDirection;
            AngleOffset = 0;
            CustomData = slider.CustomData?.Clone();
        }

        protected INote(float time, int posX, int posY, int type, int cutDirection,
            JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Type = type;
            CutDirection = cutDirection;
            AngleOffset = 0;
            InferColor();
        }

        protected INote(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Color = color;
            CutDirection = cutDirection;
            AngleOffset = angleOffset;
            InferType();
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Note;

        public int Type
        {
            get => _type;
            set
            {
                _type = value;
                _color = value;
            }
        }

        public int Color
        {
            get => _color;
            set
            {
                _color = value;
                _type = value;
            }
        }

        public int CutDirection { get; set; }
        public int AngleOffset { get; set; }

        public bool IsMainDirection => CutDirection == (int)NoteCutDirection.Up ||
                                       CutDirection == (int)NoteCutDirection.Down ||
                                       CutDirection == (int)NoteCutDirection.Left ||
                                       CutDirection == (int)NoteCutDirection.Right;

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is INote note)
            {
                Color = note.Color;
                CutDirection = note.CutDirection;
            }
        }

        public override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            // Only down to 1/4 spacing
            if (other is IBombNote || other is INote)
                return Vector2.Distance(((IGrid)other).GetPosition(), GetPosition()) < 0.1;
            return false;
        }

        protected void InferType() => Type = Color;

        protected void InferColor() => Color = Type;

        public virtual int? CustomDirection
        {
            get => _customDirection;
            set
            {
                GetOrCreateCustom()[CustomKeyDirection] = value;
                _customDirection = value;
            }
        }

        public abstract string CustomKeyDirection { get; }
    }
}
