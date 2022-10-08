using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class IChain : IBaseSlider, ICustomDataChain
    {
        public const float
            PosOffsetFactor = 0.17333f; // Hardcorded because haven't found exact relationship between ChainScale yet

        public static readonly Vector3 ChainScale = new Vector3(1.5f, 0.8f, 1.5f);

        protected IChain()
        {
        }

        protected IChain(IChain other)
        {
            Time = other.Time;
            Color = other.Color;
            PosX = other.PosX;
            PosY = other.PosY;
            CutDirection = other.CutDirection;
            TailTime = other.TailTime;
            TailPosX = other.TailPosX;
            TailPosY = other.TailPosY;
            SliceCount = other.SliceCount;
            Squish = other.Squish;
            CustomData = other.CustomData?.Clone();
        }

        protected IChain(INote start, INote end)
        {
            Time = start.Time;
            Color = start.Color;
            PosX = start.PosX;
            PosY = start.PosY;
            CutDirection = start.CutDirection;
            TailTime = end.Time;
            TailPosX = end.PosX;
            TailPosY = end.PosY;
            SliceCount = 5;
            Squish = 1;
            CustomData = start.CustomData?.Clone();
        }

        protected IChain(float time, int color, int posX, int posY, int cutDirection, int angleOffset,
            float tailTime, int tailPosX, int tailPosY, int sliceCount, float squish, JSONNode customData = null) :
            base(time, color, posX, posY, cutDirection, angleOffset, tailTime, tailPosX, tailPosY, customData)
        {
            SliceCount = sliceCount;
            Squish = squish;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Chain;
        public int SliceCount { get; set; }
        public float Squish { get; set; }

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is IChain chain)
            {
                SliceCount = chain.SliceCount;
                Squish = chain.Squish;
            }
        }
    }
}
