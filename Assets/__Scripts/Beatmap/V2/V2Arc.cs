using System;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Arc : BaseArc
    {
        public V2Arc()
        {
        }

        public V2Arc(BaseArc other) : base(other) => ParseCustom();

        public V2Arc(JSONNode node)
        {
            Color = RetrieveRequiredNode(node, "_colorType").AsInt;
            Time = RetrieveRequiredNode(node, "_headTime").AsFloat;
            PosX = RetrieveRequiredNode(node, "_headLineIndex").AsInt;
            PosY = RetrieveRequiredNode(node, "_headLineLayer").AsInt;
            CutDirection = RetrieveRequiredNode(node, "_headCutDirection").AsInt;
            TailTime = RetrieveRequiredNode(node, "_tailTime").AsFloat;
            TailPosX = RetrieveRequiredNode(node, "_tailLineIndex").AsInt;
            TailPosY = RetrieveRequiredNode(node, "_tailLineLayer").AsInt;
            ControlPointLengthMultiplier = RetrieveRequiredNode(node, "_headControlPointLengthMultiplier").AsFloat;
            TailControlPointLengthMultiplier = RetrieveRequiredNode(node, "_tailControlPointLengthMultiplier").AsFloat;
            TailCutDirection = RetrieveRequiredNode(node, "_tailCutDirection").AsInt;
            MidAnchorMode = RetrieveRequiredNode(node, "_sliderMidAnchorMode").AsInt;
            CustomData = node["_customData"];
            ParseCustom();
        }

        public V2Arc(float time, int color, int posX, int posY, int cutDirection, int angleOffset, float mult,
            float tailTime, int tailPosX, int tailPosY, int tailCutDirection, float tailMult, int midAnchorMode,
            JSONNode customData = null) : base(time, color, posX, posY, cutDirection, angleOffset, mult, tailTime,
            tailPosX, tailPosY, tailCutDirection, tailMult, midAnchorMode, customData) =>
            ParseCustom();

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override string CustomKeyTailCoordinate { get; } = "_tailPosition";

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_colorType"] = Color;
            node["_headTime"] = Math.Round(Time, DecimalPrecision);
            node["_headLineIndex"] = PosX;
            node["_headLineLayer"] = PosY;
            node["_headCutDirection"] = CutDirection;
            node["_tailTime"] = TailTime;
            node["_tailLineIndex"] = TailPosX;
            node["_tailLineLayer"] = TailPosY;
            node["_headControlPointLengthMultiplier"] = ControlPointLengthMultiplier;
            node["_tailControlPointLengthMultiplier"] = TailControlPointLengthMultiplier;
            node["_tailCutDirection"] = TailCutDirection;
            node["_sliderMidAnchorMode"] = MidAnchorMode;
            CustomData = SaveCustom();
            if (CustomData.Count == 0) return node;
            node["_customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V2Arc(Time, Color, PosX, PosY, CutDirection, AngleOffset, ControlPointLengthMultiplier,
                TailTime, TailPosX, TailPosY, TailCutDirection, TailControlPointLengthMultiplier, MidAnchorMode,
                CustomData?.Clone());
    }
}
