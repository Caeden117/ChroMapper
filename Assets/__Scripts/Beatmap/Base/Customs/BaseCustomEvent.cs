using System;
using Beatmap.Enums;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using LiteNetLib.Utils;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public class BaseCustomEvent : BaseObject
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Type);
            writer.Put(Data.ToString());
            base.Serialize(writer);
        }
        public override void Deserialize(NetDataReader reader)
        {
            Type = reader.GetString();
            Data = JSON.Parse(reader.GetString());
            base.Deserialize(reader);
        }

        public BaseCustomEvent()
        {
        }

        protected BaseCustomEvent(BaseCustomEvent other)
        {
            JsonTime = other.JsonTime;
            Type = other.Type;
            Data = other.SaveCustom().Clone();
        }

        public BaseCustomEvent(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, KeyTime).AsFloat;
            Type = RetrieveRequiredNode(node, KeyType).Value;
            Data = RetrieveRequiredNode(node, KeyData);
        }

        protected BaseCustomEvent(float time, string type, JSONNode node = null) : base(time)
        {
            Type = type;
            Data = node is JSONObject ? node : new JSONObject();
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.CustomEvent;

        public string Type { get; set; }

        private JSONNode data;

        public JSONNode Data
        {
            get => data;
            set
            {
                data = value;
                ParseCustom();
            }
        }

        public float? DataDuration { get; set; }
        public string? DataEasing { get; set; }
        public int? DataRepeat { get; set; }
        public JSONNode DataChildrenTracks { get; set; }
        public JSONNode DataParentTrack { get; set; }
        public bool? DataWorldPositionStays { get; set; }

        public string KeyTime => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.KeyTime,
            3 => V3CustomEvent.KeyTime
        };

        public string KeyType => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.KeyType,
            3 => V3CustomEvent.KeyType
        };

        public string KeyData => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.KeyData,
            3 => V3CustomEvent.KeyData
        };

        public string DataKeyDuration => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.DataKeyDuration,
            3 => V3CustomEvent.DataKeyDuration
        };

        public string DataKeyEasing => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.DataKeyEasing,
            3 => V3CustomEvent.DataKeyEasing
        };

        public string DataKeyRepeat => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.DataKeyRepeat,
            3 => V3CustomEvent.DataKeyRepeat
        };

        public string DataKeyChildrenTracks  => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.DataKeyChildrenTracks,
            3 => V3CustomEvent.DataKeyChildrenTracks
        };

        public string DataKeyParentTrack  => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.DataKeyParentTrack,
            3 => V3CustomEvent.DataKeyParentTrack
        };

        public string DataKeyWorldPositionStays  => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.DataKeyWorldPositionStays,
            3 => V3CustomEvent.DataKeyWorldPositionStays
        };


        public override string CustomKeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.CustomKeyColor,
            3 => V3CustomEvent.CustomKeyColor
        };

        public override string CustomKeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.CustomKeyTrack,
            3 => V3CustomEvent.CustomKeyTrack
        };

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) => false;

        protected override void ParseCustom()
        {
            CustomTrack = Data.HasKey(CustomKeyTrack) ? Data[CustomKeyTrack] : null;
            
            // I don't know why but tenary operator causes these properties to be set to default on node editor or save
            // when they shouldn't be. I guess some sort of implicit cast was happening but I couldn't see why?
            if (Data.HasKey(DataKeyDuration))
                DataDuration = Data[DataKeyDuration].AsFloat;
            else
                DataDuration = null;
            
            if (Data.HasKey(DataKeyRepeat))
                DataRepeat = Data[DataKeyRepeat].AsInt;
            else
                DataRepeat = null;
            
            if (Data.HasKey(DataKeyWorldPositionStays))
                DataWorldPositionStays = Data[DataKeyWorldPositionStays].AsBool;
            else
                DataWorldPositionStays = null;
            
            DataEasing = Data.HasKey(DataKeyEasing) ? Data[DataKeyEasing] : null;
            DataChildrenTracks = Data.HasKey(DataKeyChildrenTracks) ? Data[DataKeyChildrenTracks] : null;
            DataParentTrack = Data.HasKey(DataKeyParentTrack) ? Data[DataKeyParentTrack] : null;
        }

        protected internal override JSONNode SaveCustom()
        {
            if (CustomTrack != null) Data[CustomKeyTrack] = CustomTrack; else Data.Remove(CustomKeyTrack);
            if (DataDuration != null) Data[DataKeyDuration] = DataDuration; else Data.Remove(DataKeyDuration);
            if (DataEasing != null) Data[DataKeyEasing] = DataEasing; else Data.Remove(DataKeyEasing);
            if (DataRepeat != null) Data[DataKeyRepeat] = DataRepeat; else Data.Remove(DataKeyRepeat);
            if (DataChildrenTracks != null) Data[DataKeyChildrenTracks] = DataChildrenTracks; else Data.Remove(DataKeyChildrenTracks);
            if (DataParentTrack != null) Data[DataKeyParentTrack] = DataParentTrack; else Data.Remove(DataKeyParentTrack);
            if (DataWorldPositionStays != null) Data[DataKeyWorldPositionStays] = DataWorldPositionStays; else Data.Remove(DataWorldPositionStays);
            return Data;
        }

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            if (other is not BaseCustomEvent customEvent) return comparison; ;

            // Order by custom event type if times match up
            return comparison == 0
                ? string.Compare(Type, customEvent.Type, StringComparison.Ordinal)
                : comparison;
        }
        
        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2CustomEvent.ToJson(this),
            3 or 4 => V3CustomEvent.ToJson(this)
        };
        
        public override BaseItem Clone()
        {
            var customEvent = new BaseCustomEvent(this);
            customEvent.ParseCustom();
            return customEvent;
        }
    }
}
