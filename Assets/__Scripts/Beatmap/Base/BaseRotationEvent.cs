using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseRotationEvent : BaseEvent
    {
        protected BaseRotationEvent()
        {
        }

        protected BaseRotationEvent(BaseRotationEvent other)
        {
            Time = other.Time;
            ExecutionTime = other.ExecutionTime;
            Rotation = other.Rotation;
            CustomData = other.CustomData?.Clone();
        }

        protected BaseRotationEvent(BaseEvent evt)
        {
            Time = evt.Time;
            Rotation = CustomData != null && evt.CustomLaneRotation != null
                ? (int)evt.CustomLaneRotation
                : evt.Value;
            if (Rotation >= 0 && Rotation < LightValueToRotationDegrees.Length)
                Rotation = LightValueToRotationDegrees[(int)Rotation];
            if (Rotation >= 1000 && Rotation <= 1720)
                Rotation -= 1360;
            ExecutionTime = evt.Type == (int)EventTypeValue.EarlyLaneRotation ? 0 : 1;
            CustomData = evt.CustomData?.Clone();
        }

        protected BaseRotationEvent(float time, int executionTime, float rotation, JSONNode customData = null) :
            base(time, executionTime == 0 ? 14 : 15, customData)
        {
            ExecutionTime = executionTime;
            Rotation = rotation;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public int ExecutionTime { get; set; }
        public float Rotation { get; set; }

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
