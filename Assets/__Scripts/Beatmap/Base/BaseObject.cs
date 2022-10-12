using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseObject : BaseItem, ICustomData, IChromaObject
    {
        private Color? customColor;

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

        public virtual Color? CustomColor
        {
            get => customColor;
            set
            {
                if (value == null && CustomData?[CustomKeyColor] != null)
                    CustomData.Remove(CustomKeyColor);
                else
                    GetOrCreateCustom()[CustomKeyColor] = value;
                customColor = value;
            }
        }

        public abstract string CustomKeyColor { get; }
        public JSONNode CustomData { get; set; }

        public JSONNode GetOrCreateCustom()
        {
            if (CustomData == null)
                CustomData = new JSONObject();

            return CustomData;
        }

        public virtual bool IsChroma() => false;

        public virtual bool IsNoodleExtensions() => false;

        public virtual bool IsMappingExtensions() => false;

        public virtual bool IsConflictingWith(BaseObject other, bool deletion = false) =>
            Mathf.Abs(Time - other.Time) < BeatmapObjectContainerCollection.Epsilon &&
            IsConflictingWithObjectAtSameTime(other, deletion);

        protected abstract bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false);

        public virtual void Apply(BaseObject originalData)
        {
            Time = originalData.Time;
            CustomData = originalData.CustomData?.Clone();
        }

        protected virtual void ParseCustom()
        {
            if (CustomData == null) return;
            if (CustomData[CustomKeyColor] != null) CustomColor = CustomData[CustomKeyColor].ReadColor();
        }
    }
}
