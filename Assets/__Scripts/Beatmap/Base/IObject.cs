using Beatmap.Base.Customs;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class IObject : IItem, ICustomData, IChromaObject
    {
        protected Color? _customColor;

        protected IObject()
        {
        }

        protected IObject(float time, JSONNode customData = null)
        {
            Time = time;
            CustomData = customData;
        }

        public abstract ObjectType ObjectType { get; set; }
        public bool HasAttachedContainer { get; set; } = false;
        public float Time { get; set; }
        public JSONNode CustomData { get; set; }

        public virtual bool IsConflictingWith(IObject other, bool deletion = false) =>
            Mathf.Abs(Time - other.Time) < BeatmapObjectContainerCollection.Epsilon &&
            IsConflictingWithObjectAtSameTime(other, deletion);

        public abstract bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false);

        public virtual void Apply(IObject originalData)
        {
            Time = originalData.Time;
            CustomData = originalData.CustomData?.Clone();
        }

        public JSONNode GetOrCreateCustom()
        {
            if (CustomData == null)
                CustomData = new JSONObject();

            return CustomData;
        }

        public virtual void ParseCustom()
        {
            if (CustomData == null) return;
            if (CustomData[CustomKeyColor] != null) CustomColor = CustomData[CustomKeyColor].ReadColor();
        }

        public virtual bool IsChroma() => false;

        public virtual bool IsNoodleExtensions() => false;

        public virtual bool IsMappingExtensions() => false;

        public virtual Color? CustomColor
        {
            get => _customColor;
            set
            {
                GetOrCreateCustom()[CustomKeyColor] = value;
                _customColor = value;
            }
        }

        public abstract string CustomKeyColor { get; }
    }
}
