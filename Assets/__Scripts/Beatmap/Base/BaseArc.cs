using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseArc : BaseSlider, ICustomDataArc
    {
        protected BaseArc()
        {
        }

        protected BaseArc(BaseArc other)
        {
            Time = other.Time;
            Color = other.Color;
            PosX = other.PosX;
            PosY = other.PosY;
            CutDirection = other.CutDirection;
            TailTime = other.TailTime;
            TailPosX = other.TailPosX;
            TailPosY = other.TailPosY;
            ControlPointLengthMultiplier = other.ControlPointLengthMultiplier;
            TailControlPointLengthMultiplier = other.TailControlPointLengthMultiplier;
            TailCutDirection = other.TailCutDirection;
            MidAnchorMode = other.MidAnchorMode;
            CustomData = other.CustomData?.Clone();
        }

        protected BaseArc(BaseNote start, BaseNote end)
        {
            Time = start.Time;
            Color = start.Color;
            PosX = start.PosX;
            PosY = start.PosY;
            CutDirection = start.CutDirection;
            TailTime = end.Time;
            TailPosX = end.PosX;
            TailPosY = end.PosY;
            ControlPointLengthMultiplier = 1f;
            TailControlPointLengthMultiplier = 1f;
            TailCutDirection = end.CutDirection;
            MidAnchorMode = 0;
            CustomData = start.CustomData?.Clone();
        }

        protected BaseArc(float time, int color, int posX, int posY, int cutDirection, int angleOffset,
            float mult, float tailTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult,
            int midAnchorMode, JSONNode customData = null) : base(time, color, posX, posY, cutDirection, angleOffset,
            tailTime, tailPosX, tailPosY, customData)
        {
            ControlPointLengthMultiplier = mult;
            TailCutDirection = tailCutDirection;
            TailControlPointLengthMultiplier = tailMult;
            MidAnchorMode = midAnchorMode;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Arc;
        public float ControlPointLengthMultiplier { get; set; }
        public int TailCutDirection { get; set; }
        public float TailControlPointLengthMultiplier { get; set; }
        public int MidAnchorMode { get; set; }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseArc arc)
            {
                ControlPointLengthMultiplier = arc.ControlPointLengthMultiplier;
                TailCutDirection = arc.TailCutDirection;
                TailControlPointLengthMultiplier = arc.TailControlPointLengthMultiplier;
                MidAnchorMode = arc.MidAnchorMode;
            }
        }
    }
}
