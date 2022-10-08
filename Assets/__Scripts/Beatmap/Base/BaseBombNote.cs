using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseBombNote : BaseGrid, ICustomDataBomb
    {
        protected BaseBombNote()
        {
        }

        protected BaseBombNote(BaseGrid other)
        {
            Time = other.Time;
            PosX = other.PosX;
            PosY = other.PosY;
            CustomData = other.CustomData?.Clone();
        }

        protected BaseBombNote(float time, int posX, int posY, JSONNode customData = null) : base(time, posX, posY,
            customData)
        {
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Bomb;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            // Only down to 1/4 spacing
            if (other is BaseBombNote || other is BaseNote)
                return Vector2.Distance(((BaseGrid)other).GetPosition(), GetPosition()) < 0.1;
            return false;
        }
    }
}
