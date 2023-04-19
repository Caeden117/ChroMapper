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

        protected BaseSlider(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
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

        public JSONNode CustomTailCoordinate { get; set; }

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

        public Vector2 GetTailPosition() => DerivePositionFromTailData();

        private Vector2 DerivePositionFromTailData()
        {
            var position = TailPosX - 1.5f;
            float layer = TailPosY;

            if (CustomTailCoordinate != null && CustomTailCoordinate.IsArray)
            {
                if (CustomTailCoordinate[0].IsNumber) position = CustomTailCoordinate[0] + 0.5f;
                if (CustomTailCoordinate[1].IsNumber) layer = CustomTailCoordinate[1];
                return new Vector2(position, layer);
            }

            if (TailPosX >= 1000)
                position = (TailPosX / 1000f) - 2.5f;
            else if (TailPosX <= -1000)
                position = (TailPosX / 1000f) - 0.5f;

            if (TailPosY >= 1000 || TailPosY <= -1000) layer = (TailPosY / 1000f) - 1f;

            return new Vector2(position, layer);
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();

            CustomTailCoordinate = (CustomData?.HasKey(CustomKeyTailCoordinate) ?? false) ? CustomData?[CustomKeyTailCoordinate] : null;
        }

        protected internal override JSONNode SaveCustom()
        {
            CustomData = base.SaveCustom();
            if (CustomTailCoordinate != null) CustomData[CustomKeyTailCoordinate] = CustomTailCoordinate; else CustomData.Remove(CustomKeyTailCoordinate);
            return CustomData;
        }
    }
}
