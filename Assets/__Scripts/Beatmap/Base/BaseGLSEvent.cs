using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseGLSEvent : BaseObject
    {
        protected BaseGLSEvent()
        {
        }

        protected BaseGLSEvent(float relativeTime, float time, JSONNode customData = null) : base(time, customData) =>
            RelativeJsonTime = relativeTime;

        protected BaseGLSEvent(BaseGLSEvent other) : base(other.JsonTime, other.CustomData?.Clone())
        {
            RelativeJsonTime = other.RelativeJsonTime;
            BoxIndex = other.BoxIndex;
            EventBoxData = other.EventBoxData;
            EventBoxGroupData = other.EventBoxGroupData;
            Easing = other.Easing;
            UsePrevious = other.UsePrevious;
            RefreshCustom();
        }

        public float RelativeJsonTime { get; set; }
        public int Easing { get; set; }
        public int UsePrevious { get; set; }
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;

        public override void RecomputeSongBpmTime()
        {
            if (EventBoxGroupData != null) jsonTime = EventBoxGroupData.JsonTime + RelativeJsonTime;
            base.RecomputeSongBpmTime();
        }

        public override void Apply(BaseObject originalData)
        {
            if (originalData is BaseGLSEvent gls)
            {
                RelativeJsonTime = gls.RelativeJsonTime;
                Easing = gls.Easing;
                UsePrevious = gls.UsePrevious;
            }

            base.Apply(originalData);
        }

        public override string CustomKeyColor => "unusedColor";
        public override string CustomKeyTrack => "unusedKeyTrack";

        public BaseEventBox EventBoxData;
        public BaseEventBoxGroup EventBoxGroupData;
        public int BoxIndex = -1;

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseGLSEvent evt) return comparison;

            // Is not the same group type
            if (other.GetType() != GetType()) return comparison;

            // Compare by ID if type match
            if (comparison == 0) comparison = BoxIndex.CompareTo(evt.BoxIndex);

            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0)
                comparison = string.Compare(
                    CustomData?.ToString(),
                    evt.CustomData?.ToString(),
                    StringComparison.Ordinal);

            return comparison;
        }
    }
}
