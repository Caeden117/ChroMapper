using System;
using System.Collections;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using Beatmap.V2;
using Beatmap.V3;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseEvent : BaseObject, ICustomDataEvent
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Type);
            writer.Put(Value);
            writer.Put(FloatValue);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Type = reader.GetInt();
            Value = reader.GetInt();
            FloatValue = reader.GetFloat();
            base.Deserialize(reader);
        }

        public BaseEvent()
        {
        }

        public BaseEvent(BaseEvent other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Type = other.Type;
            Value = other.Value;
            FloatValue = other.FloatValue;
            CustomData = other.SaveCustom().Clone();
        }
        
        // Used for Node Editor
        public BaseEvent(JSONNode node) : this(BeatmapFactory.Event(node)) {}

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public virtual int Type { get; set; }

        private int value;
        public int Value
        {
            get => value;
            set
            {
                if (IsLaneRotationEvent())
                {
                    if (0 <= value && value < LightValueToRotationDegrees.Length)
                    {
                        floatValue = LightValueToRotationDegrees[value];
                    }
                    else
                    {
                        floatValue = value;
                    }
                }
                this.value = value;
            } 
        }

        private float floatValue = 1f;

        public float FloatValue
        {
            get => floatValue;
            set
            {
                if (IsLaneRotationEvent())
                {
                    if (value < (LightValueToRotationDegrees[0] + LightValueToRotationDegrees[1]) / 2f)
                        this.value = 0;
                    else if (value < (LightValueToRotationDegrees[1] + LightValueToRotationDegrees[2]) / 2f)
                        this.value = 1;
                    else if (value < (LightValueToRotationDegrees[2] + LightValueToRotationDegrees[3]) / 2f)
                        this.value = 2;
                    else if (value < (LightValueToRotationDegrees[3] + LightValueToRotationDegrees[4]) / 2f)
                        this.value = 3;
                    else if (value < (LightValueToRotationDegrees[4] + LightValueToRotationDegrees[5]) / 2f)
                        this.value = 4;
                    else if (value < (LightValueToRotationDegrees[5] + LightValueToRotationDegrees[6]) / 2f)
                        this.value = 5;
                    else if (value < (LightValueToRotationDegrees[6] + LightValueToRotationDegrees[7]) / 2f)
                        this.value = 6;
                    else 
                        this.value = 7;
                }
                floatValue = value;
            }
        }

        public BaseEvent Prev { get; set; }
        public BaseEvent Next { get; set; }
        
        public static readonly int[] LightValueToRotationDegrees = { -60, -45, -30, -15, 15, 30, 45, 60 };
        private int[] customLightID;
        protected float? customSpeed;
        

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
        public bool IsPropagation => CustomPropID >= -1;

        public virtual int CustomPropID { get; set; } = -1;

        public virtual int[] CustomLightID
        {
            get => customLightID;
            set
            {
                if (value == null || value.Length == 0)
                {
                    customLightID = null;
                    return;
                }

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

        public virtual float? CustomRingRotation { get; set; }

        public virtual float? CustomStepMult { get; set; }

        public virtual float? CustomPropMult { get; set; }

        public virtual float? CustomSpeedMult { get; set; }

        public virtual float? CustomPreciseSpeed { get; set; }

        public virtual int? CustomDirection { get; set; }

        public virtual bool? CustomLockRotation { get; set; }

        public virtual string CustomNameFilter { get; set; }

        public virtual float? CustomLaneRotation { get; set; }

        public override string CustomKeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyColor,
            3 => V3BasicEvent.CustomKeyColor
        };

        public override string CustomKeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyTrack,
            3 => V3BasicEvent.CustomKeyTrack
        };

        public string CustomKeyPropID => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyPropID,
            3 => V3BasicEvent.CustomKeyPropID
        };

        public string CustomKeyLightID => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyLightID,
            3 => V3BasicEvent.CustomKeyLightID
        };

        public string CustomKeyLerpType => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyLerpType,
            3 => V3BasicEvent.CustomKeyLerpType
        };

        public string CustomKeyEasing => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyEasing,
            3 => V3BasicEvent.CustomKeyEasing
        };

        public string CustomKeyLightGradient => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyLightGradient,
            3 => V3BasicEvent.CustomKeyLightGradient
        };

        public string CustomKeyStep => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyStep,
            3 => V3BasicEvent.CustomKeyStep
        };

        public string CustomKeyProp => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyProp,
            3 => V3BasicEvent.CustomKeyProp
        };

        public string CustomKeySpeed => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeySpeed,
            3 => V3BasicEvent.CustomKeySpeed
        };

        public string CustomKeyRingRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyRingRotation,
            3 => V3BasicEvent.CustomKeyRingRotation
        };

        public string CustomKeyDirection => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyDirection,
            3 => V3BasicEvent.CustomKeyDirection
        };

        public string CustomKeyLockRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyLockRotation,
            3 => V3BasicEvent.CustomKeyLockRotation
        };

        public string CustomKeyLaneRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyLaneRotation,
            3 => V3BasicEvent.CustomKeyLaneRotation
        };

        public string CustomKeyNameFilter => Settings.Instance.MapVersion switch
        {
            2 => V2Event.CustomKeyNameFilter,
            3 => V3BasicEvent.CustomKeyNameFilter
        };

        public string CustomKeyStepMult => V2Event.CustomKeyStepMult;
        public string CustomKeyPropMult => V2Event.CustomKeyPropMult;
        public string CustomKeySpeedMult => V2Event.CustomKeySpeedMult;
        public string CustomKeyPreciseSpeed => V2Event.CustomKeyPreciseSpeed;
        

        public override bool IsChroma() =>
            CustomData != null &&
            ((CustomData.HasKey(CustomKeyColor) && CustomData[CustomKeyColor].IsArray) ||
             (CustomData.HasKey(CustomKeyLightGradient) && CustomData[CustomKeyLightGradient].IsArray) ||
             (CustomData.HasKey(CustomKeyLightID) &&
              (CustomData[CustomKeyLightID].IsArray || CustomData[CustomKeyLightID].IsNumber)) ||
             (CustomData.HasKey(CustomKeyPropID) &&
             (CustomData[CustomKeyPropID].IsArray || CustomData[CustomKeyPropID].IsNumber)) ||
             (CustomData.HasKey(CustomKeyEasing) && CustomData[CustomKeyEasing].IsString) ||
             (CustomData.HasKey(CustomKeyLerpType) && CustomData[CustomKeyLerpType].IsString) ||
             (CustomData.HasKey(CustomKeyNameFilter) && CustomData[CustomKeyNameFilter].IsString) ||
             (CustomData.HasKey("_reset") && CustomData["_reset"].IsBoolean) ||
             (CustomData.HasKey("_counterSpin") && CustomData["_counterSpin"].IsBoolean) ||
             (CustomData.HasKey(CustomKeyPropMult) && CustomData[CustomKeyPropMult].IsNumber) ||
             (CustomData.HasKey(CustomKeyStepMult) && CustomData[CustomKeyStepMult].IsNumber) ||
             (CustomData.HasKey(CustomKeySpeedMult) && CustomData[CustomKeySpeedMult].IsNumber) ||
             (!IsLaneRotationEvent() && CustomData.HasKey(CustomKeyRingRotation) && CustomData[CustomKeyRingRotation].IsNumber) ||
             (CustomData.HasKey(CustomKeyStep) && CustomData[CustomKeyStep].IsNumber) ||
             (CustomData.HasKey(CustomKeyProp) && CustomData[CustomKeyProp].IsNumber) ||
             (CustomData.HasKey(CustomKeySpeed) && CustomData[CustomKeySpeed].IsNumber) ||
             (CustomData.HasKey(CustomKeyDirection) && CustomData[CustomKeyDirection].IsNumber) ||
             (CustomData.HasKey(CustomKeyLockRotation) && CustomData[CustomKeyLockRotation].IsBoolean));
        
        public override bool IsNoodleExtensions() =>
            IsLaneRotationEvent() && CustomData != null &&
            CustomData.HasKey("_rotation") && CustomData["_rotation"].IsNumber;

        public override bool IsMappingExtensions() =>
            IsLaneRotationEvent() && Value >= 1000 && Value <= 1720;

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

        public virtual bool IsBpmEvent() => Type is (int)EventTypeValue.BpmChange;

        public Vector2? GetPosition(CreateEventTypeLabels labels, EventGridContainer.PropMode mode, int prop)
        {   
            if (mode == EventGridContainer.PropMode.Off)
            {
                return new Vector2(
                    labels.EventTypeToLaneId(Type) + 0.5f,
                    0.5f
                );
            }

            if (Type != prop) return null;

            if (CustomLightID is null)
            {
                return new Vector2(
                    0.5f,
                    0.5f
                );
            }

            CustomPropID = labels.LightIdsToPropId(Type, CustomLightID) ?? -1;

            var x = mode == EventGridContainer.PropMode.Prop ? CustomPropID : -1;

            if (x < 0) x = CustomLightID.Length > 0 ? labels.LightIDToEditor(Type, CustomLightID[0]) : -1;

            return new Vector2(
                x + 1.5f,
                0.5f);
        }

        public virtual float? GetRotationDegreeFromValue()
        {
            var queued = (CustomData?.HasKey("_queuedRotation") ?? false) ? CustomData["_queuedRotation"].AsInt : Value;
            if (queued >= 0 && queued < LightValueToRotationDegrees.Length)
                return LightValueToRotationDegrees[queued];
            //Mapping Extensions precision rotation from 1000 to 1720: 1000 = -360 degrees, 1360 = 0 degrees, 1720 = 360 degrees
            if (queued >= 1000 && queued <= 1720)
                return queued - 1360;
            return null;
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseEvent @event)
            {
                var lightId = CustomLightID;
                var otherLightId = @event.CustomLightID;
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

            if (CustomData?.HasKey(CustomKeyLightID) ?? false)
            {
                var temp = CustomData[CustomKeyLightID];
                CustomLightID = temp.IsNumber
                    ? new[] { temp.AsInt }
                    : temp.AsArray.Linq.Where(x => x.Value.IsNumber).Select(x => x.Value.AsInt).ToArray();
            }
            else
            {
                CustomLightID = null;
            }

            if (CustomData?.HasKey(CustomKeyLightGradient) ?? false)
            {
                var gradient = CustomData[CustomKeyLightGradient];
                CustomLightGradient = new ChromaLightGradient(gradient["_startColor"], gradient["_endColor"],
                    gradient["_duration"], gradient["_easing"]);
            }
            else
            {
                CustomLightGradient = null;
            }

            CustomLerpType = (CustomData?.HasKey(CustomKeyLerpType) ?? false) ? CustomData?[CustomKeyLerpType].Value : null;
            CustomEasing = (CustomData?.HasKey(CustomKeyEasing) ?? false) ? CustomData?[CustomKeyEasing].Value : null;
            CustomStep = (CustomData?.HasKey(CustomKeyStep) ?? false) ? CustomData?[CustomKeyStep].AsFloat : null;
            CustomProp = (CustomData?.HasKey(CustomKeyProp) ?? false) ? CustomData?[CustomKeyProp].AsFloat : null;
            CustomSpeed = (CustomData?.HasKey(CustomKeySpeed) ?? false) ? CustomData?[CustomKeySpeed].AsFloat : null;
            CustomRingRotation = (CustomData?.HasKey(CustomKeyRingRotation) ?? false) ? CustomData?[CustomKeyRingRotation].AsFloat : null;
            CustomDirection = (CustomData?.HasKey(CustomKeyDirection) ?? false) ? CustomData?[CustomKeyDirection].AsInt : null;
            CustomLockRotation = (CustomData?.HasKey(CustomKeyLockRotation) ?? false) ? CustomData?[CustomKeyLockRotation].AsBool : null;
        }

        protected internal override JSONNode SaveCustom()
        {
            CustomData = base.SaveCustom();
            if (CustomLightID != null)
            {
                CustomData[CustomKeyLightID] = new JSONArray();
                foreach (var i in CustomLightID) CustomData[CustomKeyLightID].Add(i);
            }
            else
            {
                CustomData.Remove(CustomKeyLightID);
            }

            if (CustomLightGradient != null)
            {
                CustomData[CustomKeyLightGradient] = CustomLightGradient.ToJson();
            }
            else
            {
                CustomData.Remove(CustomKeyLightGradient);
            }

            if (CustomLerpType != null) CustomData[CustomKeyLerpType] = CustomLerpType; else CustomData.Remove(CustomKeyLerpType);
            if (CustomEasing != null) CustomData[CustomKeyEasing] = CustomEasing; else CustomData.Remove(CustomKeyEasing);
            if (CustomStep != null) CustomData[CustomKeyStep] = CustomStep; else CustomData.Remove(CustomKeyStep);
            if (CustomProp != null) CustomData[CustomKeyProp] = CustomProp; else CustomData.Remove(CustomKeyProp);
            if (CustomSpeed != null) CustomData[CustomKeySpeed] = CustomSpeed; else CustomData.Remove(CustomKeySpeed);
            if (CustomRingRotation != null) CustomData[CustomKeyRingRotation] = CustomRingRotation; else CustomData.Remove(CustomKeyRingRotation);
            if (CustomDirection != null) CustomData[CustomKeyDirection] = CustomDirection; else CustomData.Remove(CustomKeyDirection);
            if (CustomLockRotation != null) CustomData[CustomKeyLockRotation] = CustomLockRotation; else CustomData.Remove(CustomKeyLockRotation);
            return CustomData;
        }
        
        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseEvent @event) return comparison;

            // Compare by type if times match
            if (comparison == 0) comparison = Type.CompareTo(@event.Type);

            // Compare by value if type matches
            if (comparison == 0) comparison = Value.CompareTo(@event.Value);

            // Compare by float value if value matches
            if (comparison == 0) comparison = FloatValue.CompareTo(@event.FloatValue);

            // Compare by lightID if float value matches
            // (we need to implement this ourselves because StructuralComparisons.StructuralComparer.Compare fails at differing length arrays
            if (comparison == 0)
            {
                switch ((customLightID, @event.customLightID))
                {
                    case (null, not null): return -1;
                    case (not null, null): return 1;
                    case (not null, not null):
                        var length = Mathf.Min(customLightID.Length, @event.customLightID.Length);

                        for (var i = 0; i < length; i++)
                        {
                            comparison = customLightID[i].CompareTo(@event.customLightID[i]);
                            
                            if (comparison != 0) return comparison;
                        }

                        return customLightID.Length.CompareTo(@event.customLightID.Length);
                }
            }
            //if (comparison == 0) comparison = StructuralComparisons.StructuralComparer.Compare(CustomLightID, @event.CustomLightID);

            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0) comparison = string.Compare(CustomData?.ToString(), @event.CustomData?.ToString(), StringComparison.Ordinal);
            
            return comparison;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2Event.ToJson(this),
            3 => Type switch
            {
                (int)EventTypeValue.EarlyLaneRotation => V3RotationEvent.ToJson(this),
                (int)EventTypeValue.LateLaneRotation => V3RotationEvent.ToJson(this),
                (int)EventTypeValue.ColorBoost => V3ColorBoostEvent.ToJson(this),
                _ => V3BasicEvent.ToJson(this)
            }
        };

        public override BaseItem Clone()
        {
            var evt = new BaseEvent(this);
            evt.RefreshCustom();
            
            // This depends on environment and is calculated by grid position after creation
            // so we need to set this here to clone correctly  
            evt.CustomPropID = CustomPropID;
            
            return evt;
        }
    }
}
