using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.Base
{
    public abstract class BaseObject : BaseItem, ICustomData, IHeckObject, IChromaObject, INetSerializable
    {
        public abstract void Serialize(NetDataWriter writer);
        public abstract void Deserialize(NetDataReader reader);

        protected BaseObject()
        {
        }

        protected BaseObject(float time, JSONNode customData = null)
        {
            Time = time;
            CustomData = customData;
        }

        public abstract ObjectType ObjectType { get; set; }
        public bool HasAttachedContainer { get; set; } = false;
        public float Time { get; set; }
        public virtual Color? CustomColor { get; set; }
        public abstract string CustomKeyColor { get; }
        public JSONNode CustomData { get; set; } = new JSONObject();

        public virtual bool IsChroma() => false;

        public virtual bool IsNoodleExtensions() => false;

        public virtual bool IsMappingExtensions() => false;

        public string CustomTrack { get; set; }

        public abstract string CustomKeyTrack { get; }

        public virtual bool IsConflictingWith(BaseObject other, bool deletion = false) =>
            Mathf.Abs(Time - other.Time) < BeatmapObjectContainerCollection.Epsilon &&
            IsConflictingWithObjectAtSameTime(other, deletion);

        protected abstract bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false);

        public virtual void Apply(BaseObject originalData)
        {
            Time = originalData.Time;
            CustomData = originalData.CustomData?.Clone();
            RefreshCustom();
        }

        protected virtual void ParseCustom()
        {
            CustomTrack = (CustomData?.HasKey(CustomKeyTrack) ?? false) ? CustomData?[CustomKeyTrack].Value : null;
            CustomColor = (CustomData?.HasKey(CustomKeyColor) ?? false) ? CustomData?[CustomKeyColor].ReadColor() : null;
        }

        public void RefreshCustom() => ParseCustom();

        protected internal virtual JSONNode SaveCustom()
        {
            CustomData = CustomData is JSONObject ? CustomData : new JSONObject();
            if (!string.IsNullOrEmpty(CustomTrack)) CustomData[CustomKeyTrack] = CustomTrack;
            if (CustomColor != null) CustomData[CustomKeyColor] = CustomColor;
            return CustomData;
        }

        public JSONNode GetOrCreateCustom()
        {
            if (CustomData == null)
                CustomData = new JSONObject();

            return CustomData;
        }
    }
}
