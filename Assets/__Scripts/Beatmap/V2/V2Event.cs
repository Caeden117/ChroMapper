using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;

using LiteNetLib.Utils;

namespace Beatmap.V2
{
    public class V2Event : BaseEvent, V2Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V2Event()
        {
        }

        public V2Event(BaseEvent other) : base(other) => ParseCustom();

        public V2Event(BaseBpmEvent baseBpm) : base(baseBpm)
        {
        }

        public V2Event(BaseColorBoostEvent cbe) : base(cbe)
        {
        }

        public V2Event(BaseRotationEvent re) : base(re) => ParseCustom();

        public V2Event(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "_time").AsFloat;
            Type = RetrieveRequiredNode(node, "_type").AsInt;
            Value = RetrieveRequiredNode(node, "_value").AsInt;
            FloatValue = node.HasKey("_floatValue") ? node["_floatValue"].AsFloat : 1f;
            CustomData = node["_customData"];
            ParseCustom();
        }

        public V2Event(float time, int type, int value, float floatValue = 1f, JSONNode customData = null) : base(time,
            type, value, floatValue, customData) =>
            ParseCustom();

        public override string CustomKeyTrack { get; } = "_track";

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

        public override string CustomKeyNameFilter { get; } = "_nameFilter";

        protected sealed override void ParseCustom()
        {
            base.ParseCustom();

            CustomLightGradient = (CustomData?.HasKey(CustomKeyLightGradient) ?? false)
                ? new ChromaLightGradient(CustomData[CustomKeyLightGradient])
                : null;
            CustomPropMult = (CustomData?.HasKey(CustomKeyPropMult) ?? false) ? CustomData?[CustomKeyPropMult].AsFloat : null;
            CustomStepMult = (CustomData?.HasKey(CustomKeyStepMult) ?? false) ? CustomData?[CustomKeyStepMult].AsFloat : null;
            CustomSpeedMult = (CustomData?.HasKey(CustomKeySpeedMult) ?? false) ? CustomData?[CustomKeySpeedMult].AsFloat : null;
            CustomPreciseSpeed = (CustomData?.HasKey(CustomKeyPreciseSpeed) ?? false) ? CustomData?[CustomKeyPreciseSpeed].AsFloat : null;
            CustomLaneRotation = (CustomData?.HasKey(CustomKeyLaneRotation) ?? false) ? CustomData?[CustomKeyLaneRotation].AsInt : null;
        }

        protected internal sealed override JSONNode SaveCustom()
        {
            CustomData = base.SaveCustom();
            if (CustomLightGradient != null) CustomData[CustomKeyLightGradient] = CustomLightGradient.ToJson();
            if (CustomPropMult != null) CustomData[CustomKeyPropMult] = CustomPropMult;
            if (CustomStepMult != null) CustomData[CustomKeyStepMult] = CustomStepMult;
            if (CustomPropMult != null) CustomData[CustomKeyPropMult] = CustomPropMult;
            if (CustomSpeedMult != null) CustomData[CustomKeySpeedMult] = CustomSpeedMult;
            if (CustomPreciseSpeed != null) CustomData[CustomKeyPreciseSpeed] = CustomPreciseSpeed;
            if (CustomLaneRotation != null) CustomData[CustomKeyLaneRotation] = CustomLaneRotation;
            return CustomData;
        }

        public override bool IsChroma() =>
            CustomData != null &&
            ((CustomData.HasKey("_color") && CustomData["_color"].IsArray) ||
             (CustomData.HasKey("_lightID") &&
              (CustomData["_lightID"].IsArray || CustomData["_lightID"].IsNumber)) ||
             (CustomData.HasKey("_propID") && CustomData["_propID"].IsNumber) ||
             CustomLightGradient != null ||
             (CustomData.HasKey("_easing") && CustomData["_easing"].IsString) ||
             (CustomData.HasKey("_lerpType") && CustomData["_lerpType"].IsString) ||
             (CustomData.HasKey("_nameFilter") && CustomData["_nameFilter"].IsString) ||
             (CustomData.HasKey("_reset") && CustomData["_reset"].IsBoolean) ||
             (CustomData.HasKey("_rotation") && CustomData["_rotation"].IsNumber) ||
             (CustomData.HasKey("_step") && CustomData["_step"].IsNumber) ||
             (CustomData.HasKey("_prop") && CustomData["_prop"].IsNumber) ||
             (CustomData.HasKey("_speed") && CustomData["_speed"].IsNumber) ||
             (CustomData.HasKey("_direction") && CustomData["_direction"].IsNumber) ||
             (CustomData.HasKey("_counterSpin") && CustomData["_counterSpin"].IsBoolean) ||
             (CustomData.HasKey("_stepMult") && CustomData["_stepMult"].IsNumber) ||
             (CustomData.HasKey("_propMult") && CustomData["_propMult"].IsNumber) ||
             (CustomData.HasKey("_speedMult") && CustomData["_speedMult"].IsNumber) ||
             (CustomData.HasKey("_lockPosition") && CustomData["_lockPosition"].IsBoolean) ||
             (CustomData.HasKey("_speed") && CustomData["_speed"].IsNumber) ||
             (CustomData.HasKey("_preciseSpeed") && CustomData["_preciseSpeed"].IsNumber) ||
             (CustomData.HasKey("_direction") && CustomData["_direction"].IsNumber));

        public override bool IsNoodleExtensions() =>
            IsLaneRotationEvent() && CustomData != null &&
            CustomData.HasKey("_rotation") && CustomData["_rotation"].IsNumber;

        public override bool IsMappingExtensions() =>
            IsLaneRotationEvent() && !IsNoodleExtensions() && Value >= 1000 && Value <= 1720;

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Math.Round(Time, DecimalPrecision);
            node["_type"] = Type;
            node["_value"] = Value;
            node["_floatValue"] = FloatValue;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["_customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V2Event(Time, Type, Value, FloatValue, SaveCustom().Clone());

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            CustomLightGradient = (ChromaLightGradient)CustomLightGradient?.Clone();
        }
    }
}
