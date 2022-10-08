using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class ILightRotationBase : IObject
    {
        protected ILightRotationBase()
        {
        }

        protected ILightRotationBase(float time, float rotation, int direction, int easeType, int loop,
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

        protected override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            if (other is ILightRotationBase lrb)
                return Math.Abs(Rotation - lrb.Rotation) < DecimalTolerance || Direction == lrb.Direction ||
                       EaseType == lrb.EaseType || Loop == lrb.Loop || UsePrevious == lrb.UsePrevious;
            return false;
        }
    }
}
