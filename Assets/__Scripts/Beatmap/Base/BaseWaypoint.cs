using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseWaypoint : BaseObject
    {
        public BaseWaypoint()
        {
        }

        public BaseWaypoint(BaseWaypoint other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            PosX = other.PosX;
            PosY = other.PosY;
            OffsetDirection = other.OffsetDirection;
            CustomData = other.SaveCustom().Clone();
        }

        public int PosX { get; set; }
        public int PosY { get; set; }
        public int OffsetDirection { get; set; }
        
        public override ObjectType ObjectType { get; set; } = ObjectType.Waypoint;

        public override string CustomKeyColor { get; } = "unusedKeyColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            // Only down to 1/4 spacing
            if (other is BaseWaypoint waypoint)
            {
                return Vector2.Distance(
                    new Vector2(waypoint.PosX, waypoint.PosY),
                    new Vector2(PosX, PosY)) < 0.1;
            }
            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseWaypoint waypoint) OffsetDirection = waypoint.OffsetDirection;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2Waypoint.ToJson(this),
            3 => V3Waypoint.ToJson(this)
        };

        public override BaseItem Clone() => new BaseWaypoint(this);
    }
}
