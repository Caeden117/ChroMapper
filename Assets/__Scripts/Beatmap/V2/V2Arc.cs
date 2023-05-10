using System;
using System.Linq;
using Beatmap.Base;
using LiteNetLib.Utils;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Arc : BaseArc, V2Object
    {
        public V2Arc()
        {
        }

        public V2Arc(BaseArc other) : base(other) => ParseCustom();

        public V2Arc(JSONNode node)
        {
            Color = RetrieveRequiredNode(node, "_colorType").AsInt;
            JsonTime = RetrieveRequiredNode(node, "_headTime").AsFloat;
            PosX = RetrieveRequiredNode(node, "_headLineIndex").AsInt;
            PosY = RetrieveRequiredNode(node, "_headLineLayer").AsInt;
            CutDirection = RetrieveRequiredNode(node, "_headCutDirection").AsInt;
            HeadControlPointLengthMultiplier = RetrieveRequiredNode(node, "_headControlPointLengthMultiplier").AsFloat;
            TailJsonTime = RetrieveRequiredNode(node, "_tailTime").AsFloat;
            TailPosX = RetrieveRequiredNode(node, "_tailLineIndex").AsInt;
            TailPosY = RetrieveRequiredNode(node, "_tailLineLayer").AsInt;
            TailCutDirection = RetrieveRequiredNode(node, "_tailCutDirection").AsInt;
            TailControlPointLengthMultiplier = RetrieveRequiredNode(node, "_tailControlPointLengthMultiplier").AsFloat;
            MidAnchorMode = RetrieveRequiredNode(node, "_sliderMidAnchorMode").AsInt;
            CustomData = node["_customData"];
            ParseCustom();
        }

        public V2Arc(float time, int posX, int posY, int color, int cutDirection, int angleOffset, float mult,
            float tailTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult, int midAnchorMode,
            JSONNode customData = null) : base(time, posX, posY, color, cutDirection, angleOffset, mult,
            tailTime, tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData) =>
            ParseCustom();

        public V2Arc(float jsonTime, float songBpmTime, int posX, int posY, int color, int cutDirection, int angleOffset, float mult,
            float tailJsonTime, float tailSongBpmTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult, int midAnchorMode,
            JSONNode customData = null) : base(jsonTime, songBpmTime, posX, posY, color, cutDirection, angleOffset, mult,
            tailJsonTime, tailSongBpmTime, tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData) =>
            ParseCustom();

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override string CustomKeyTailCoordinate { get; } = "_tailPosition";
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_colorType"] = Color;
            node["_headTime"] = Math.Round(JsonTime, DecimalPrecision);
            node["_headLineIndex"] = PosX;
            node["_headLineLayer"] = PosY;
            node["_headCutDirection"] = CutDirection;
            node["_headControlPointLengthMultiplier"] = HeadControlPointLengthMultiplier;
            node["_tailTime"] = TailJsonTime;
            node["_tailLineIndex"] = TailPosX;
            node["_tailLineLayer"] = TailPosY;
            node["_tailCutDirection"] = TailCutDirection;
            node["_tailControlPointLengthMultiplier"] = TailControlPointLengthMultiplier;
            node["_sliderMidAnchorMode"] = MidAnchorMode;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["_customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V2Arc(JsonTime, PosX, PosY, Color, CutDirection, AngleOffset,
                HeadControlPointLengthMultiplier, TailJsonTime, TailPosX, TailPosY, TailCutDirection,
                TailControlPointLengthMultiplier,
                MidAnchorMode, SaveCustom().Clone());
    }
}
