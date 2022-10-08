using System;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseItem
    {
        protected static int DecimalPrecision =>
#if UNITY_EDITOR
            6;
#else
            Settings.Instance.TimeValueDecimalPrecision;
#endif
        protected static float DecimalTolerance => 0.001f;

        public abstract JSONNode ToJson();

        public override string ToString() => ToJson().ToString();

        public abstract BaseItem Clone();

        protected JSONNode RetrieveRequiredNode(JSONNode node, string key)
        {
            if (!node.HasKey(key)) throw new ArgumentException($"{GetType().Name} missing required node \"{key}\".");
            return node[key];
        }
    }
}
