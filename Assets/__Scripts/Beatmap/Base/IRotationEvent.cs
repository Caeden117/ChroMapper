using System;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class IRotationEvent : IObject
    {
        protected IRotationEvent()
        {
        }

        protected IRotationEvent(IRotationEvent other)
        {
            Time = other.Time;
            ExecutionTime = other.ExecutionTime;
            Rotation = other.Rotation;
            CustomData = other.CustomData?.Clone();
        }

        protected IRotationEvent(IEvent evt)
        {
            Time = evt.Time;
            Rotation = CustomData != null && evt.CustomLaneRotation != null
                ? (int)evt.CustomLaneRotation
                : evt.Value;
            if (Rotation >= 0 && Rotation < IEvent.LightValueToRotationDegrees.Length)
                Rotation = IEvent.LightValueToRotationDegrees[(int)Rotation];
            if (Rotation >= 1000 && Rotation <= 1720)
                Rotation -= 1360;
            ExecutionTime = evt.Type == (int)EventTypeValue.EarlyLaneRotation ? 0 : 1;
            CustomData = evt.CustomData?.Clone();
        }

        protected IRotationEvent(float time, int executionTime, float rotation, JSONNode customData = null) :
            base(time, customData)
        {
            ExecutionTime = executionTime;
            Rotation = rotation;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public int ExecutionTime { get; set; }
        public float Rotation { get; set; }

        protected override bool IsConflictingWithObjectAtSameTime(IObject other, bool deletion = false)
        {
            if (other is IRotationEvent re)
                return ExecutionTime == re.ExecutionTime || Math.Abs(Rotation - re.Rotation) < DecimalTolerance;
            return false;
        }

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is IRotationEvent re)
            {
                ExecutionTime = re.ExecutionTime;
                Rotation = re.Rotation;
            }
        }
    }
}
