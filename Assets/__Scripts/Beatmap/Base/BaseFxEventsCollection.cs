using System;
using System.Linq;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseFxEventsCollection : BaseItem
    {
        public IntFxEventBase[] IntFxEvents = { };
        public FloatFxEventBase[] FloatFxEvents = { };

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3FxEventsCollection.ToJson(this)
        };


        public override BaseItem Clone()
        {
            var eventsCollection = new BaseFxEventsCollection();
            eventsCollection.IntFxEvents = IntFxEvents.Select(evt => evt.Clone() as IntFxEventBase).ToArray();
            eventsCollection.FloatFxEvents = FloatFxEvents.Select(evt => evt.Clone() as FloatFxEventBase).ToArray();
            return eventsCollection;
        }
    }

    // Well... Turns out IntFxEvents was never used in v3 format and then got removed in v4
    // TODO: Would be worth cleaning up this later
    public class IntFxEventBase : FxEventBase<int>
    {
        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3IntFxEvent.ToJson(this)
        };

        public override BaseItem Clone()
        {
            var floatFxEvents = new IntFxEventBase();
            floatFxEvents.JsonTime = JsonTime;
            floatFxEvents.UsePreviousEventValue = UsePreviousEventValue;
            floatFxEvents.Value = Value;
            return floatFxEvents;
        }
    }

    public class FloatFxEventBase : FxEventBase<float>, IEquatable<FloatFxEventBase>
    {
        public int Easing;

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3FloatFxEvent.ToJson(this)
        };

        public override BaseItem Clone()
        {
            var floatFxEvents = new FloatFxEventBase();
            floatFxEvents.JsonTime = JsonTime;
            floatFxEvents.UsePreviousEventValue = UsePreviousEventValue;
            floatFxEvents.Value = Value;
            floatFxEvents.Easing = Easing;
            return floatFxEvents;
        }

        public bool Equals(FloatFxEventBase other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Easing == other.Easing 
                   && Mathf.Approximately(JsonTime, other.JsonTime) 
                   && UsePreviousEventValue == other.UsePreviousEventValue 
                   && Mathf.Approximately(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FloatFxEventBase)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Easing;
                hashCode = (hashCode * 397) ^ JsonTime.GetHashCode();
                hashCode = (hashCode * 397) ^ UsePreviousEventValue;
                hashCode = (hashCode * 397) ^ Value.GetHashCode();

                return hashCode;
            }
        }
    }

    public abstract class FxEventBase<T> : BaseItem where T : struct
    {
        public float JsonTime;
        public int UsePreviousEventValue;
        public T Value;
    }
}
