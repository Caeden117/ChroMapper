using Beatmap.Base.Customs;
using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseArc : BaseSlider, ICustomDataArc
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(HeadControlPointLengthMultiplier);
            writer.Put(TailCutDirection);
            writer.Put(TailControlPointLengthMultiplier);
            writer.Put(MidAnchorMode);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            HeadControlPointLengthMultiplier = reader.GetFloat();
            TailCutDirection = reader.GetInt();
            TailControlPointLengthMultiplier = reader.GetFloat();
            MidAnchorMode = reader.GetInt();
            base.Deserialize(reader);
        }

        protected BaseArc()
        {
        }

        protected BaseArc(BaseArc other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Color = other.Color;
            PosX = other.PosX;
            PosY = other.PosY;
            CutDirection = other.CutDirection;
            HeadControlPointLengthMultiplier = other.HeadControlPointLengthMultiplier;
            SetTailTimes(other.TailJsonTime, other.TailSongBpmTime);
            TailPosX = other.TailPosX;
            TailPosY = other.TailPosY;
            TailCutDirection = other.TailCutDirection;
            TailControlPointLengthMultiplier = other.TailControlPointLengthMultiplier;
            MidAnchorMode = other.MidAnchorMode;
            CustomData = other.SaveCustom().Clone();
        }

        protected BaseArc(BaseNote start, BaseNote end)
        {
            SetTimes(start.JsonTime, start.SongBpmTime);
            Color = start.Color;
            PosX = start.PosX;
            PosY = start.PosY;
            CutDirection = start.CutDirection;
            HeadControlPointLengthMultiplier = 1f;
            SetTailTimes(end.JsonTime, end.SongBpmTime);
            TailPosX = end.PosX;
            TailPosY = end.PosY;
            TailCutDirection = end.CutDirection;
            TailControlPointLengthMultiplier = 1f;
            MidAnchorMode = 0;
            CustomData = start.SaveCustom().Clone();
        }

        protected BaseArc(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            float mult, float tailTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult,
            int midAnchorMode, JSONNode customData = null) : base(time, posX, posY, color, cutDirection,
            angleOffset, tailTime, tailPosX, tailPosY, customData)
        {
            HeadControlPointLengthMultiplier = mult;
            TailCutDirection = tailCutDirection;
            TailControlPointLengthMultiplier = tailMult;
            MidAnchorMode = midAnchorMode;
        }

        protected BaseArc(float jsonTime, float songBpmTime, int posX, int posY, int color, int cutDirection, int angleOffset,
            float mult, float tailJsonTime, float tailSongBpmTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult,
            int midAnchorMode, JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, color, cutDirection,
            angleOffset, tailJsonTime, tailSongBpmTime, tailPosX, tailPosY, customData)
        {
            HeadControlPointLengthMultiplier = mult;
            TailCutDirection = tailCutDirection;
            TailControlPointLengthMultiplier = tailMult;
            MidAnchorMode = midAnchorMode;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Arc;
        public float HeadControlPointLengthMultiplier { get; set; }
        public int TailCutDirection { get; set; }
        public float TailControlPointLengthMultiplier { get; set; }
        public int MidAnchorMode { get; set; }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseArc arc)
            {
                HeadControlPointLengthMultiplier = arc.HeadControlPointLengthMultiplier;
                TailCutDirection = arc.TailCutDirection;
                TailControlPointLengthMultiplier = arc.TailControlPointLengthMultiplier;
                MidAnchorMode = arc.MidAnchorMode;
            }
        }
    }
}
