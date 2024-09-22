using System.Linq;
using Beatmap.V3;
using SimpleJSON;

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

    public class FloatFxEventBase : FxEventBase<float>
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
    }

    public abstract class FxEventBase<T> : BaseItem where T : struct
    {
        public float JsonTime;
        public int UsePreviousEventValue;
        public T Value;
    }
}
