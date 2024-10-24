using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public abstract class BaseCustomEvent : BaseObject
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

        protected BaseCustomEvent()
        {
        }

        protected BaseCustomEvent(BaseCustomEvent other)
        {
            JsonTime = other.JsonTime;
            Type = other.Type;
            Data = other.SaveCustom().Clone();
        }

        protected BaseCustomEvent(JSONNode node) => InstantiateHelper(ref node);

        protected BaseCustomEvent(float time, string type, JSONNode node = null) : base(time)
        {
            Type = type;
            Data = node is JSONObject ? node : new JSONObject();
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.CustomEvent;

        public string Type { get; set; }
        public JSONNode Data { get; set; }
        public float? DataDuration { get; set; }
        public string? DataEasing { get; set; }
        public int? DataRepeat { get; set; }
        public JSONNode DataChildrenTracks { get; set; }
        public JSONNode DataParentTrack { get; set; }
        public bool? DataWorldPositionStays { get; set; }

        public abstract string KeyTime { get; }
        public abstract string KeyType { get; }
        public abstract string KeyData { get; }
        public abstract string DataKeyDuration { get; }
        public abstract string DataKeyEasing { get; }
        public abstract string DataKeyRepeat { get; }
        public abstract string DataKeyChildrenTracks { get; }
        public abstract string DataKeyParentTrack { get; }
        public abstract string DataKeyWorldPositionStays { get; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) => false;

        public override JSONNode ToJson() =>
            new JSONObject { [KeyTime] = JsonTime, [KeyType] = Type, [KeyData] = Data };

        private void InstantiateHelper(ref JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, KeyTime).AsFloat;
            Type = RetrieveRequiredNode(node, KeyType).Value;
            Data = RetrieveRequiredNode(node, KeyData);
            ParseCustom();
        }

        protected override void ParseCustom()
        {
            CustomTrack = Data.HasKey(CustomKeyTrack) ? Data[CustomKeyTrack] : null;
            DataDuration = Data.HasKey(DataKeyDuration) ? Data[DataKeyDuration] : null;
            DataEasing = Data.HasKey(DataKeyEasing) ? Data[DataKeyEasing] : null;
            DataRepeat = Data.HasKey(DataKeyRepeat) ? Data[DataKeyRepeat] : null;
            DataChildrenTracks = Data.HasKey(DataKeyChildrenTracks) ? Data[DataKeyChildrenTracks] : null;
            DataParentTrack = Data.HasKey(DataKeyParentTrack) ? Data[DataKeyParentTrack] : null;
            DataWorldPositionStays = Data.HasKey(DataKeyWorldPositionStays) ? Data[DataKeyWorldPositionStays] : null;
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
                ? Type.CompareTo(customEvent.Type)
                : comparison;
        }
    }
}
