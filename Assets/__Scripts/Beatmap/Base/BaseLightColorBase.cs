using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseLightColorBase : BaseObject
    {
        protected BaseLightColorBase()
        {
        }

        protected BaseLightColorBase(float time, int color, float brightness, int transitionType, int frequency,
            float strobeBrightness, int strobeFade, JSONNode customData = null) : base(time, customData)
        {
            Color = color;
            Brightness = brightness;
            TransitionType = transitionType;
            Frequency = frequency;
            StrobeBrightness = strobeBrightness;
            StrobeFade = strobeFade;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public int Color { get; set; }
        public float Brightness { get; set; }
        public int TransitionType { get; set; }
        public int Frequency { get; set; }
        public float StrobeBrightness { get; set; }
        public int StrobeFade { get; set; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightColorBase lcb)
                return Color == lcb.Color || Math.Abs(Brightness - lcb.Brightness) < DecimalTolerance ||
                       TransitionType == lcb.TransitionType || Frequency == lcb.Frequency;
            return false;
        }
    }
}
