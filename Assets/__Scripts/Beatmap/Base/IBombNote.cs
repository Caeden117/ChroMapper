using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class IBombNote : IGrid, ICustomDataBomb
    {
        protected IBombNote()
        {
        }

        protected IBombNote(IGrid other)
        {
            Time = other.Time;
            PosX = other.PosX;
            PosY = other.PosY;
            CustomData = other.CustomData?.Clone();
        }

        protected IBombNote(float time, int posX, int posY, JSONNode customData = null) : base(time, posX, posY,
            customData)
        {
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Bomb;

        protected override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            // Only down to 1/4 spacing
            if (other is IBombNote || other is INote)
                return Vector2.Distance(((IGrid)other).GetPosition(), GetPosition()) < 0.1;
            return false;
        }
    }
}
