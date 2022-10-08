using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class IWaypoint : IGrid
    {
        protected IWaypoint()
        {
        }

        protected IWaypoint(IWaypoint other)
        {
            Time = other.Time;
            PosX = other.PosX;
            PosY = other.PosY;
            OffsetDirection = other.OffsetDirection;
            CustomData = other.CustomData?.Clone();
        }

        protected IWaypoint(float time, int posX, int posY, int offsetDirection, JSONNode customData = null) :
            base(time, posX, posY, customData) =>
            OffsetDirection = offsetDirection;

        public override ObjectType ObjectType { get; set; } = ObjectType.Waypoint;
        public int OffsetDirection { get; set; }

        public override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            // Only down to 1/4 spacing
            if (other is IWaypoint waypoint) return Vector2.Distance(waypoint.GetPosition(), GetPosition()) < 0.1;
            return false;
        }

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is IWaypoint waypoint) OffsetDirection = waypoint.OffsetDirection;
        }
    }
}
