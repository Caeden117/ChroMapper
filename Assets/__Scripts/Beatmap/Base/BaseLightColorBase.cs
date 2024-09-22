using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightColorBase : BaseObject
    {
        public BaseLightColorBase()
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

        public override string CustomKeyColor { get; } = "unusedColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightColorBase lcb)
                return Color == lcb.Color || Math.Abs(Brightness - lcb.Brightness) < DecimalTolerance ||
                       TransitionType == lcb.TransitionType || Frequency == lcb.Frequency;
            return false;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightColorBase.ToJson(this),
        };

        public override BaseItem Clone() => throw new NotImplementedException();
    }
}
