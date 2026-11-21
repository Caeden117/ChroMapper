using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationBase : BaseGLSEvent
    {
        public BaseLightTranslationBase()
        {
        }

        // Used for Node Editor
        public BaseLightTranslationBase(JSONNode node) : this(V3LightTranslationBase.GetFromJson(node))
        {
        }

        protected BaseLightTranslationBase(
            float time,
            float translation,
            int easeType,
            int usePrevious,
            JSONNode customData = null) : base(time, customData)
        {
            Translation = translation;
            Easing = easeType;
            UsePrevious = usePrevious;
        }

        protected BaseLightTranslationBase(BaseLightTranslationBase other) : base(other)
        {
            Translation = other.Translation;
        }

        public float Translation { get; set; }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseLightTranslationBase other)
                return;

            Translation = other.Translation;
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightTranslationBase ltb) return BoxIndex == ltb.BoxIndex;
            return false;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightTranslationBase.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightTranslationBase(this);
    }
}
