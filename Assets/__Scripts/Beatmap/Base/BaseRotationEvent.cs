using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseRotationEvent : BaseEvent
    {
        private int executionTime;
        protected BaseRotationEvent() => Type = 14;

        protected BaseRotationEvent(BaseRotationEvent other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            ExecutionTime = other.ExecutionTime;
            Rotation = other.Rotation;
            CustomData = other.SaveCustom().Clone();
        }

        protected BaseRotationEvent(BaseEvent evt)
        {
            SetTimes(evt.JsonTime, evt.SongBpmTime);
            ExecutionTime = evt.Type == (int)EventTypeValue.EarlyLaneRotation ? 0 : 1;
            Rotation = evt.CustomLaneRotation ?? evt.GetRotationDegreeFromValue() ?? 0f;
            CustomData = evt.SaveCustom().Clone();
        }

        protected BaseRotationEvent(float time, int executionTime, float rotation, JSONNode customData = null) :
            base()
        {
            JsonTime = time;
            ExecutionTime = executionTime;
            Rotation = rotation;
            CustomData = customData;
        }

        protected BaseRotationEvent(float jsonTime, float songBpmTime, int executionTime, float rotation,
           JSONNode customData = null) : base()
        {
            SetTimes(jsonTime, songBpmTime);
            ExecutionTime = executionTime;
            Rotation = rotation;
            CustomData = customData;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;

        public override int Type
        {
            get => base.Type;
            set
            {
                executionTime = value == 15 ? 1 : 0;
                base.Type = value == 15 ? 15 : 14;
            }
        }

        public int ExecutionTime
        {
            get => executionTime;
            set
            {
                executionTime = value;
                Type = value == 0 ? 14 : 15;
            }
        }

        public float Rotation { get; set; }

        public override float? GetRotationDegreeFromValue() => Rotation;

        public override bool IsLaneRotationEvent() => true;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseRotationEvent re)
                return ExecutionTime == re.ExecutionTime || Math.Abs(Rotation - re.Rotation) < DecimalTolerance;
            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseRotationEvent re)
            {
                ExecutionTime = re.ExecutionTime;
                Rotation = re.Rotation;
            }
        }
    }
}
