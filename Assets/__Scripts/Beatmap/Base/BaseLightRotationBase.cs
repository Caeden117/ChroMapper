using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationBase : BaseObject
    {
        public BaseLightRotationBase()
        {
        }

        protected BaseLightRotationBase(float time, float rotation, int direction, int easeType, int loop,
            int usePrevious, JSONNode customData = null) : base(time, customData)
        {
            Rotation = rotation;
            Direction = direction;
            EaseType = easeType;
            Loop = loop;
            UsePrevious = usePrevious;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public float Rotation { get; set; }
        public int Direction { get; set; }
        public int EaseType { get; set; }
        public int Loop { get; set; }
        public int UsePrevious { get; set; }

        public override string CustomKeyColor { get; }

        public override string CustomKeyTrack { get; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightRotationBase lrb)
                return Math.Abs(Rotation - lrb.Rotation) < DecimalTolerance || Direction == lrb.Direction ||
                       EaseType == lrb.EaseType || Loop == lrb.Loop || UsePrevious == lrb.UsePrevious;
            return false;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightRotationBase.ToJson(this)
        };

        public override BaseItem Clone() => throw new NotImplementedException();
    }
}
