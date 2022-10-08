using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Event : IEvent
    {
        public V2Event()
        {
        }

        public V2Event(IEvent other) : base(other) => ParseCustom();

        public V2Event(IBpmEvent bpm) : base(bpm)
        {
        }

        public V2Event(IColorBoostEvent cbe) : base(cbe)
        {
        }

        public V2Event(IRotationEvent re) : base(re) => ParseCustom();

        public V2Event(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "_time").AsFloat;
            Type = RetrieveRequiredNode(node, "_type").AsInt;
            Value = RetrieveRequiredNode(node, "_value").AsInt;
            FloatValue = node["_floatValue"]?.AsFloat ?? 1f;
            CustomData = node["_customData"];
            ParseCustom();
        }

        public V2Event(float time, int type, int value, float floatValue = 1f, JSONNode customData = null) : base(time,
            type,
            value, floatValue, customData) =>
            ParseCustom();

        public sealed override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;

            if (CustomData[CustomKeyLightGradient] != null)
                CustomLightGradient = new ChromaLightGradient(CustomData[CustomKeyLightGradient]);
            if (CustomData[CustomKeyPropMult] != null) CustomPropMult = CustomData[CustomKeyPropMult].AsFloat;
            if (CustomData[CustomKeyStepMult] != null) CustomStepMult = CustomData[CustomKeyStepMult].AsFloat;
            if (CustomData[CustomKeyPropMult] != null) CustomPropMult = CustomData[CustomKeyPropMult].AsFloat;
            if (CustomData[CustomKeySpeedMult] != null) CustomSpeedMult = CustomData[CustomKeySpeedMult].AsFloat;
            if (CustomData[CustomKeyPreciseSpeed] != null) CustomPreciseSpeed = CustomData[CustomKeyPreciseSpeed].AsFloat;
            if (CustomData[CustomKeyLaneRotation] != null) CustomLaneRotation = CustomData[CustomKeyLaneRotation].AsInt;
        }

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyPropID { get; } = "_propID";

        public override string CustomKeyLightID { get; } = "_lightID";

        public override string CustomKeyLerpType { get; } = "_lerpType";

        public override string CustomKeyEasing { get; } = "_easing";

        public override string CustomKeyLightGradient { get; } = "_lightGradient";

        public override string CustomKeyStep { get; } = "_step";

        public override string CustomKeyProp { get; } = "_prop";

        public override string CustomKeySpeed { get; } = "_speed";

        public override string CustomKeyStepMult { get; } = "_stepMult";

        public override string CustomKeyPropMult { get; } = "_propMult";

        public override string CustomKeySpeedMult { get; } = "_speedMult";

        public override string CustomKeyPreciseSpeed { get; } = "_preciseSpeed";

        public override string CustomKeyDirection { get; } = "_direction";

        public override string CustomKeyLockRotation { get; } = "_lockPosition";

        public override string CustomKeyLaneRotation { get; } = "_rotation";

        public override bool IsChroma() =>
            (CustomData?["_color"] != null && CustomData["_color"].IsArray) ||
            (CustomData?["_lightID"] != null &&
             (CustomData["_lightID"].IsArray || CustomData["_lightID"].IsNumber)) ||
            (CustomData?["_propID"] != null && CustomData["_propID"].IsNumber) ||
            CustomLightGradient != null ||
            (CustomData?["_easing"] != null && CustomData["_easing"].IsString) ||
            (CustomData?["_lerpType"] != null && CustomData["_lerpType"].IsString) ||
            (CustomData?["_nameFilter"] != null && CustomData["_nameFilter"].IsString) ||
            (CustomData?["_reset"] != null && CustomData["_reset"].IsBoolean) ||
            (CustomData?["_rotation"] != null && CustomData["_rotation"].IsNumber) ||
            (CustomData?["_step"] != null && CustomData["_step"].IsNumber) ||
            (CustomData?["_prop"] != null && CustomData["_prop"].IsNumber) ||
            (CustomData?["_speed"] != null && CustomData["_speed"].IsNumber) ||
            (CustomData?["_direction"] != null && CustomData["_direction"].IsNumber) ||
            (CustomData?["_counterSpin"] != null && CustomData["_counterSpin"].IsBoolean) ||
            (CustomData?["_stepMult"] != null && CustomData["_stepMult"].IsNumber) ||
            (CustomData?["_propMult"] != null && CustomData["_propMult"].IsNumber) ||
            (CustomData?["_speedMult"] != null && CustomData["_speedMult"].IsNumber) ||
            (CustomData?["_lockPosition"] != null && CustomData["_lockPosition"].IsBoolean) ||
            (CustomData?["_speed"] != null && CustomData["_speed"].IsNumber) ||
            (CustomData?["_preciseSpeed"] != null && CustomData["_preciseSpeed"].IsNumber) ||
            (CustomData?["_direction"] != null && CustomData["_direction"].IsNumber);

        public override bool IsNoodleExtensions() =>
            IsLaneRotationEvent() &&
            CustomData?["_rotation"] != null && CustomData["_rotation"].IsNumber;

        public override bool IsMappingExtensions() => IsLaneRotationEvent() && !IsNoodleExtensions() && Value >= 1000 && Value <= 1720;

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Time;
            node["_type"] = Type;
            node["_value"] = Value;
            node["_floatValue"] = FloatValue;
            if (CustomData == null) return node;
            node["_customData"] = CustomData;
            if (CustomLightGradient != null) node["_customData"]["_lightGradient"] = CustomLightGradient.ToJson();
            return node;
        }

        public override IItem Clone() => new V2Event(Time, Type, Value, FloatValue, CustomData?.Clone());

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is V2Event)
                CustomLightGradient = (ChromaLightGradient)CustomLightGradient?.Clone();
        }
    }
}
