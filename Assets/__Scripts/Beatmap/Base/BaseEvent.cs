using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Shared;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseEvent : BaseObject, ICustomDataEvent
    {
        public static readonly int[] LightValueToRotationDegrees = { -60, -45, -30, -15, 15, 30, 45, 60 };
        private int[] customLightID;
        protected float? customSpeed;

        protected BaseEvent()
        {
        }

        protected BaseEvent(BaseEvent other)
        {
            Time = other.Time;
            Type = other.Type;
            Value = other.Value;
            FloatValue = other.FloatValue;
            CustomData = other.CustomData?.Clone();
        }

        protected BaseEvent(BaseBpmEvent baseBpm)
        {
            Time = baseBpm.Time;
            Type = 100;
            Value = 0;
            FloatValue = baseBpm.Bpm;
            CustomData = baseBpm.CustomData?.Clone();
        }

        protected BaseEvent(BaseColorBoostEvent cbe)
        {
            Time = cbe.Time;
            Type = 5;
            Value = cbe.Toggle ? 1 : 0;
            FloatValue = 1;
            CustomData = cbe.CustomData?.Clone();
        }

        protected BaseEvent(BaseRotationEvent re)
        {
            Time = re.Time;
            Type = (int)(re.ExecutionTime == 0 ? EventTypeValue.EarlyLaneRotation : EventTypeValue.LateLaneRotation);
            Value = 0;
            FloatValue = 1;
            CustomData = re.CustomData?.Clone();
        }

        protected BaseEvent(float time, int type, int value, float floatValue = 1f, JSONNode customData = null) :
            base(time, customData)
        {
            Type = type;
            Value = value;
            FloatValue = floatValue;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public int Type { get; set; }
        public int Value { get; set; }
        public float FloatValue { get; set; }
        public BaseEvent Next { get; set; }

        public bool IsBlue => Value == (int)LightValue.BlueOn || Value == (int)LightValue.BlueFlash ||
                              Value == (int)LightValue.BlueFade || Value == (int)LightValue.BlueTransition;


        public bool IsRed => Value == (int)LightValue.RedOn || Value == (int)LightValue.RedFlash ||
                             Value == (int)LightValue.RedFade || Value == (int)LightValue.RedTransition;


        public bool IsWhite => Value == (int)LightValue.WhiteOn || Value == (int)LightValue.WhiteFlash ||
                               Value == (int)LightValue.WhiteFade || Value == (int)LightValue.WhiteTransition;


        public bool IsOff => Value is (int)LightValue.Off;

        public bool IsOn => Value == (int)LightValue.BlueOn || Value == (int)LightValue.RedOn ||
                            Value == (int)LightValue.WhiteOn;

        public bool IsFlash => Value == (int)LightValue.BlueFlash || Value == (int)LightValue.RedFlash ||
                               Value == (int)LightValue.WhiteFlash;


        public bool IsFade =>
            Value == (int)LightValue.BlueFade || Value == (int)LightValue.RedFade ||
            Value == (int)LightValue.WhiteFade;


        public bool IsTransition =>
            Value == (int)LightValue.BlueTransition || Value == (int)LightValue.RedTransition ||
            Value == (int)LightValue.WhiteTransition;

        public bool IsLegacyChroma => Value >= ColourManager.RgbintOffset;

        public virtual bool IsPropagation => CustomData?[CustomKeyPropID] != null;

        public virtual bool IsLightID => CustomData?[CustomKeyLightID] != null;

        public virtual int? CustomPropID { get; set; }

        public virtual int[] CustomLightID
        {
            get => customLightID;
            set
            {
                if (value == null)
                {
                    customLightID = null;
                    return;
                }

                if (value.Length == 0)
                {
                    customLightID = null;
                    return;
                }

                var ary = new JSONArray();
                foreach (var i in value) ary.Add(i);
                CustomData[CustomKeyLightID] = ary;
                customLightID = value;
            }
        }

        public virtual string CustomLerpType { get; set; }

        public virtual string CustomEasing { get; set; }

        public virtual ChromaLightGradient CustomLightGradient { get; set; }

        public virtual float? CustomStep { get; set; }

        public virtual float? CustomProp { get; set; }

        public virtual float? CustomSpeed
        {
            get => customSpeed;
            set => customSpeed = value;
        }

        public virtual float? CustomStepMult { get; set; }

        public virtual float? CustomPropMult { get; set; }

        public virtual float? CustomSpeedMult { get; set; }

        public virtual float? CustomPreciseSpeed { get; set; }

        public virtual int? CustomDirection { get; set; }

        public virtual bool? CustomLockRotation { get; set; }

        public virtual string CustomNameFilter { get; set; }

        public virtual int? CustomLaneRotation { get; set; }

        public abstract string CustomKeyPropID { get; }
        public abstract string CustomKeyLightID { get; }
        public abstract string CustomKeyLerpType { get; }
        public abstract string CustomKeyEasing { get; }
        public abstract string CustomKeyLightGradient { get; }
        public abstract string CustomKeyStep { get; }
        public abstract string CustomKeyProp { get; }
        public abstract string CustomKeySpeed { get; }
        public abstract string CustomKeyStepMult { get; }
        public abstract string CustomKeyPropMult { get; }
        public abstract string CustomKeySpeedMult { get; }
        public abstract string CustomKeyPreciseSpeed { get; }
        public abstract string CustomKeyDirection { get; }
        public abstract string CustomKeyLockRotation { get; }
        public abstract string CustomKeyLaneRotation { get; }
        public abstract string CustomKeyNameFilter { get; }

        public bool IsLightEvent(string environment = null) =>
            environment switch
            {
                _ => Type == (int)EventTypeValue.BackLasers || Type == (int)EventTypeValue.RingLights ||
                     Type == (int)EventTypeValue.LeftLasers || Type == (int)EventTypeValue.RightLasers ||
                     Type == (int)EventTypeValue.CenterLights || Type == (int)EventTypeValue.ExtraLeftLasers ||
                     Type == (int)EventTypeValue.ExtraRightLasers || Type == (int)EventTypeValue.ExtraLeftLights ||
                     Type == (int)EventTypeValue.ExtraRightLights
            };

        public bool IsColorBoostEvent() => Type is (int)EventTypeValue.ColorBoost;

        public bool IsRingEvent(string environment = null) =>
            environment switch
            {
                _ => Type == (int)EventTypeValue.RingRotation || Type == (int)EventTypeValue.RingZoom
            };

        public bool IsRingZoomEvent(string environment = null) =>
            environment switch
            {
                _ => Type is (int)EventTypeValue.RingZoom
            };

        public bool IsLaserRotationEvent(string environment = null) =>
            environment switch
            {
                _ => Type == (int)EventTypeValue.LeftLaserRotation || Type == (int)EventTypeValue.RightLaserRotation
            };

        public bool IsLaneRotationEvent() => Type == (int)EventTypeValue.EarlyLaneRotation ||
                                             Type == (int)EventTypeValue.LateLaneRotation;

        public bool IsExtraEvent(string environment = null) =>
            environment switch
            {
                _ => Type == (int)EventTypeValue.ExtraLeftLasers || Type == (int)EventTypeValue.ExtraLeftLights ||
                     Type == (int)EventTypeValue.ExtraRightLasers || Type == (int)EventTypeValue.ExtraRightLights
            };

        public bool IsUtilityEvent(string environment = null) =>
            environment switch
            {
                _ => Type == (int)EventTypeValue.UtilityEvent0 || Type == (int)EventTypeValue.UtilityEvent1 ||
                     Type == (int)EventTypeValue.UtilityEvent2 || Type == (int)EventTypeValue.UtilityEvent3
            };

        public bool IsSpecialEvent(string environment = null) =>
            environment switch
            {
                _ => Type == (int)EventTypeValue.SpecialEvent0 || Type == (int)EventTypeValue.SpecialEvent1 ||
                     Type == (int)EventTypeValue.SpecialEvent2 || Type == (int)EventTypeValue.SpecialEvent3
            };

        public bool IsBpmEvent() => Type is (int)EventTypeValue.BpmChange;

        public Vector2? GetPosition(CreateEventTypeLabels labels, EventGridContainer.PropMode mode, int prop)
        {
            if (IsLightID) CustomPropID = labels.LightIdsToPropId(Type, CustomLightID) ?? -1;

            if (mode == EventGridContainer.PropMode.Off)
                return new Vector2(
                    labels.EventTypeToLaneId(Type) + 0.5f,
                    0.5f
                );

            if (Type != prop) return null;

            if (!IsLightID)
                return new Vector2(
                    0.5f,
                    0.5f
                );
            var x = mode == EventGridContainer.PropMode.Prop ? CustomPropID : -1;

            if (x < 0) x = CustomLightID.Length > 0 ? labels.LightIDToEditor(Type, CustomLightID[0]) : -1;

            if (x != null)
                return new Vector2(
                    (float)x + 1.5f,
                    0.5f
                );

            return new Vector2(
                0.5f,
                0.5f
            );
        }

        public int? GetRotationDegreeFromValue()
        {
            //Mapping Extensions precision rotation from 1000 to 1720: 1000 = -360 degrees, 1360 = 0 degrees, 1720 = 360 degrees
            var val = CustomData != null && CustomLaneRotation != null
                ? (int)CustomLaneRotation
                : Value;
            if (val >= 0 && val < LightValueToRotationDegrees.Length)
                return LightValueToRotationDegrees[val];
            if (val >= 1000 && val <= 1720)
                return val - 1360;
            return null;
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseEvent @event)
            {
                var lightId = IsLightID ? CustomLightID : null;
                var otherLightId = @event.IsLightID ? @event.CustomLightID : null;
                var lightIdEquals = lightId?.Length == otherLightId?.Length &&
                                    (lightId == null || lightId.All(x => otherLightId.Contains(x)));

                return Type == @event.Type && lightIdEquals;
            }

            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseEvent obs)
            {
                Type = obs.Type;
                Value = obs.Value;
                FloatValue = obs.FloatValue;
            }
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;

            if (CustomData[CustomKeyLightID] != null)
            {
                var temp = CustomData[CustomKeyLightID];
                CustomLightID = temp.IsNumber
                    ? new[] { temp.AsInt }
                    : temp.AsArray.Linq.Where(x => x.Value.IsNumber).Select(x => x.Value.AsInt).ToArray();
            }

            if (CustomData[CustomKeyLerpType] != null) CustomLerpType = CustomData[CustomKeyLerpType].Value;
            if (CustomData[CustomKeyEasing] != null) CustomEasing = CustomData[CustomKeyEasing].Value;
            if (CustomData[CustomKeyStep] != null) CustomStep = CustomData[CustomKeyStep].AsFloat;
            if (CustomData[CustomKeyProp] != null) CustomProp = CustomData[CustomKeyProp].AsFloat;
            if (CustomData[CustomKeySpeed] != null) CustomSpeed = CustomData[CustomKeySpeed].AsFloat;
            if (CustomData[CustomKeyDirection] != null) CustomDirection = CustomData[CustomKeyDirection].AsInt;
            if (CustomData[CustomKeyLockRotation] != null)
                CustomLockRotation = CustomData[CustomKeyLockRotation].AsBool;
        }

        protected override JSONNode SaveCustom()
        {
            base.SaveCustom();
            if (CustomLightID != null)
            {
                CustomData[CustomKeyLightID] = new JSONArray();
                foreach (var i in CustomLightID) CustomData[CustomKeyLightID].Add(i);
            }

            if (CustomLerpType != null) CustomData[CustomKeyLerpType] = CustomLerpType;
            if (CustomEasing != null) CustomData[CustomKeyEasing] = CustomEasing;
            if (CustomStep != null) CustomData[CustomKeyStep] = CustomStep;
            if (CustomProp != null) CustomData[CustomKeyProp] = CustomProp;
            if (CustomSpeed != null) CustomData[CustomKeySpeed] = CustomSpeed;
            if (CustomDirection != null) CustomData[CustomKeyDirection] = CustomDirection;
            if (CustomLockRotation != null) CustomData[CustomKeyLockRotation] = CustomLockRotation;
            return CustomData;
        }
    }
}
