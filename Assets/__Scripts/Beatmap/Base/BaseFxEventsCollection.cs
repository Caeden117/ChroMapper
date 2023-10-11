namespace Beatmap.Base
{
    public abstract class BaseFxEventsCollection : BaseItem
    {
        public IntFxEventBase[] IntFxEvents = { };
        public FloatFxEventBase[] FloatFxEvents = { };
    }

    public abstract class IntFxEventBase : FxEventBase<int>
    {

    }

    public abstract class FloatFxEventBase : FxEventBase<float>
    {
        public int Easing;
    }

    public abstract class FxEventBase<T> : BaseItem where T : struct
    {
        public float JsonTime;
        public int UsePreviousEventValue;
        public T Value;
    }
}
