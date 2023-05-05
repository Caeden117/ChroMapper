using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public abstract class BaseCustomEvent : BaseObject
    {

        protected BaseCustomEvent()
        {
        }

        protected BaseCustomEvent(BaseCustomEvent other)
        {
            JsonTime = other.JsonTime;
            Type = other.Type;
            Data = other.Data.Clone();
        }

        protected BaseCustomEvent(JSONNode node) => InstantiateHelper(ref node);

        protected BaseCustomEvent(float time, string type, JSONNode node = null) : base(time)
        {
            Type = type;
            Data = node is JSONObject ? node : new JSONObject();
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.CustomEvent;

        public string Type { get; set; }
        public JSONNode Data { get; set; }
        public abstract string KeyTime { get; }
        public abstract string KeyType { get; }
        public abstract string KeyData { get; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) => false;

        public override JSONNode ToJson() =>
            new JSONObject { [KeyTime] = JsonTime, [KeyType] = Type, [KeyData] = Data };

        private void InstantiateHelper(ref JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, KeyTime).AsFloat;
            Type = RetrieveRequiredNode(node, KeyType).Value;
            Data = RetrieveRequiredNode(node, KeyData);
        }
    }
}
