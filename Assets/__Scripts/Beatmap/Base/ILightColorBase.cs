using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class ILightColorBase : IObject
    {
        protected ILightColorBase()
        {
        }

        protected ILightColorBase(float time, int color, float brightness, int transitionType, int frequency,
            JSONNode customData = null) : base(time, customData)
        {
            Color = color;
            Brightness = brightness;
            TransitionType = transitionType;
            Frequency = frequency;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public int Color { get; set; }
        public float Brightness { get; set; }
        public int TransitionType { get; set; }
        public int Frequency { get; set; }

        public override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            if (other is ILightColorBase lcb)
                return Color == lcb.Color || Math.Abs(Brightness - lcb.Brightness) < DecimalTolerance ||
                       TransitionType == lcb.TransitionType || Frequency == lcb.Frequency;
            return false;
        }
    }
}
