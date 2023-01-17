using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseLightTranslationBase : BaseObject
    {
        protected BaseLightTranslationBase()
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

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightTranslationBase lrb)
                return Math.Abs(Translation - lrb.Translation) < DecimalTolerance ||
                       EaseType == lrb.EaseType || UsePrevious == lrb.UsePrevious;
            return false;
        }
    }
}
