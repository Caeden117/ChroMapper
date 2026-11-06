using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationBase : BaseGLSEvent
    {
        public BaseLightRotationBase()
        {
        }

        // Used for Node Editor
        public BaseLightRotationBase(JSONNode node) : this(V3LightRotationBase.GetFromJson(node))
        {
        }

        protected BaseLightRotationBase(
            float time,
            float rotation,
            int direction,
            int easeType,
            int loop,
            int usePrevious,
            JSONNode customData = null) : base(time, customData)
        {
            Rotation = rotation;
            Direction = direction;
            Easing = easeType;
            Loop = loop;
            UsePrevious = usePrevious;
        }

        protected BaseLightRotationBase(BaseLightRotationBase other) : base(other)
        {
            Rotation = other.Rotation;
            Direction = other.Direction;
            Loop = other.Loop;
        }

        public float Rotation { get; set; }
        public int Direction { get; set; }
        public int Loop { get; set; }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseLightRotationBase other)
                return;

            Rotation = other.Rotation;
            Direction = other.Direction;
            Loop = other.Loop;
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightRotationBase lrb) return BoxIndex == lrb.BoxIndex;
            return false;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightRotationBase.ToJson(this)
            };

        public override BaseItem Clone() => new BaseLightRotationBase(this);
    }
}
