using System;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseObject : BaseItem, ICustomData, IHeckObject, IChromaObject, INetSerializable, IComparable<BaseObject>
    {
        public virtual void Serialize(NetDataWriter writer)
        {
            writer.Put(jsonTime);
            writer.Put(songBpmTime);
            writer.Put(CustomData?.ToString());
        }

        public virtual void Deserialize(NetDataReader reader)
        {
            jsonTime = reader.GetFloat();
            songBpmTime = reader.GetFloat();
            CustomData = JSON.Parse(reader.GetString());
        }

        protected BaseObject()
        {
        }

        protected BaseObject(float time, JSONNode customData = null)
        {
            JsonTime = time;
            CustomData = customData;
        }

        protected BaseObject(float jsonTime, float songBpmTime, JSONNode customData = null)
        {
            this.jsonTime = jsonTime;
            this.songBpmTime = songBpmTime;
            CustomData = customData;
        }

        public abstract ObjectType ObjectType { get; set; }
        public bool HasAttachedContainer { get; set; } = false;

        private float jsonTime;
        public float JsonTime
        {
            get => jsonTime;
            set
            {
                var bpmChangeGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);
                songBpmTime = bpmChangeGridContainer?.JsonTimeToSongBpmTime(value) ?? value;
                jsonTime = value;
            }
        }

        private float songBpmTime;
        public float SongBpmTime
        {
            get => songBpmTime;
            set
            {
                var bpmChangeGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);
                jsonTime = bpmChangeGridContainer?.SongBpmTimeToJsonTime(value) ?? value;
                songBpmTime = value;
            }
        }

        public void SetTimes(float jsonTime, float songBpmTime)
        {
            this.jsonTime = jsonTime;
            this.songBpmTime = songBpmTime;
            RecomputeSongBpmTime();
        }

        public virtual Color? CustomColor { get; set; }
        public abstract string CustomKeyColor { get; }

        private JSONNode customData = new JSONObject();

        public JSONNode CustomData
        {
            get => customData;
            set
            {
                customData = value ?? new JSONObject();
                ParseCustom();
            }
        }
        
        public void SetCustomData(JSONNode node) => customData = node ?? new JSONObject();

        public virtual bool IsChroma() => false;

        public virtual bool IsNoodleExtensions() => false;

        public virtual bool IsMappingExtensions() => false;

        public JSONNode CustomTrack { get; set; }

        public abstract string CustomKeyTrack { get; }

        public virtual void RecomputeSongBpmTime() => JsonTime = JsonTime;

        public virtual bool IsConflictingWith(BaseObject other, bool deletion = false) =>
            Mathf.Abs(JsonTime - other.JsonTime) < BeatmapObjectContainerCollection.Epsilon &&
            IsConflictingWithObjectAtSameTime(other, deletion);

        protected abstract bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false);

        public virtual void Apply(BaseObject originalData)
        {
            JsonTime = originalData.JsonTime;
            CustomData = originalData.CustomData?.Clone();
        }

        protected virtual void ParseCustom()
        {
            CustomTrack = (CustomData?.HasKey(CustomKeyTrack) ?? false) ? CustomData?[CustomKeyTrack] : null;
            CustomColor = (CustomData?.HasKey(CustomKeyColor) ?? false) ? CustomData?[CustomKeyColor].ReadColor() : null;
        }

        public void RefreshCustom() => ParseCustom();

        protected internal virtual JSONNode SaveCustom()
        {
            var node = CustomData is JSONObject ? CustomData : new JSONObject();
            if (CustomTrack != null) node[CustomKeyTrack] = CustomTrack; else node.Remove(CustomKeyTrack);
            if (CustomColor != null) node[CustomKeyColor] = CustomColor; else node.Remove(CustomKeyColor);
            
            SetCustomData(node);
            return node;
        }

        public void WriteCustom() => SaveCustom();

        public JSONNode GetOrCreateCustom()
        {
            if (CustomData == null)
                CustomData = new JSONObject();

            return CustomData;
        }

        // Generic comparison function that only cares about time
        public virtual int CompareTo(BaseObject other) => JsonTime.CompareTo(other.JsonTime);
    }
}
