using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseBombNote : BaseNote, ICustomDataBomb
    {
        protected BaseBombNote() => Type = (int)NoteType.Bomb;

        protected BaseBombNote(BaseGrid other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            PosX = other.PosX;
            PosY = other.PosY;
            Type = (int)NoteType.Bomb;
            CustomData = other.SaveCustom().Clone();
        }

        protected BaseBombNote(float time, int posX, int posY, JSONNode customData = null) : base(time, posX, posY,
            (int)NoteType.Bomb, 0, customData) =>
            Type = (int)NoteType.Bomb;

        public override ObjectType ObjectType { get; set; } = ObjectType.Note;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            // Only down to 1/4 spacing
            if (other is BaseBombNote || other is BaseNote)
                return Vector2.Distance(((BaseGrid)other).GetPosition(), GetPosition()) < 0.1;
            return false;
        }
    }
}
