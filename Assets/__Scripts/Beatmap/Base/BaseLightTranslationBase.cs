using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationBase : BaseObject
    {
        public BaseLightTranslationBase()
        {
        }

        protected BaseLightTranslationBase(float time, float translation, int easeType,
            int usePrevious, JSONNode customData = null) : base(time, customData)
        {
            Translation = translation;
            EaseType = easeType;
            UsePrevious = usePrevious;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public float Translation { get; set; }
        public int EaseType { get; set; }
        public int UsePrevious { get; set; }

        public override string CustomKeyColor { get; } = "unusedColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightTranslationBase lrb)
                return Math.Abs(Translation - lrb.Translation) < DecimalTolerance ||
                       EaseType == lrb.EaseType || UsePrevious == lrb.UsePrevious;
            return false;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightTranslationBase.ToJson(this),
        };

        public override BaseItem Clone() => throw new NotImplementedException();
    }
}
