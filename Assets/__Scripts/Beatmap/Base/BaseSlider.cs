using Beatmap.Base.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseSlider : BaseGrid, ICustomDataSlider
    {
        protected BaseSlider()
        {
        }

        protected BaseSlider(float time, int color, int posX, int posY, int cutDirection, int angleOffset,
            float tailTime, int tailPosX, int tailPosY, JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Color = color;
            CutDirection = cutDirection;
            AngleOffset = angleOffset;
            TailTime = tailTime;
            TailPosX = tailPosX;
            TailPosY = tailPosY;
        }

        public int Color { get; set; }
        public int CutDirection { get; set; }
        public int AngleOffset { get; set; }
        public float TailTime { get; set; }
        public int TailPosX { get; set; }
        public int TailPosY { get; set; }

        public Vector2? CustomTailCoordinate { get; set; }

        public abstract string CustomKeyTailCoordinate { get; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) => false;

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseSlider baseSlider)
            {
                Color = baseSlider.Color;
                CutDirection = baseSlider.CutDirection;
                AngleOffset = baseSlider.AngleOffset;
                TailTime = baseSlider.TailTime;
                TailPosX = baseSlider.TailPosX;
                TailPosY = baseSlider.TailPosY;
            }
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;
            if (CustomData[CustomKeyTailCoordinate] != null)
                CustomTailCoordinate = CustomData[CustomKeyTailCoordinate].ReadVector2();
        }

        protected override JSONNode SaveCustom()
        {
            base.SaveCustom();
            if (CustomTailCoordinate != null) CustomData[CustomKeyTailCoordinate] = CustomTailCoordinate;
            return CustomData;
        }
    }
}
