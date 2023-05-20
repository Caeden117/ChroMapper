using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseWaypoint : BaseGrid
    {
        protected BaseWaypoint()
        {
        }

        protected BaseWaypoint(BaseWaypoint other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            PosX = other.PosX;
            PosY = other.PosY;
            OffsetDirection = other.OffsetDirection;
            CustomData = other.SaveCustom().Clone();
        }

        protected BaseWaypoint(float time, int posX, int posY, int offsetDirection, JSONNode customData = null) :
            base(time, posX, posY, customData) =>
            OffsetDirection = offsetDirection;

        public override ObjectType ObjectType { get; set; } = ObjectType.Note;
        public int OffsetDirection { get; set; }

        public override string CustomKeyAnimation { get; } = "animation";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            // Only down to 1/4 spacing
            if (other is BaseWaypoint waypoint) return Vector2.Distance(waypoint.GetPosition(), GetPosition()) < 0.1;
            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseWaypoint waypoint) OffsetDirection = waypoint.OffsetDirection;
        }
    }
}
