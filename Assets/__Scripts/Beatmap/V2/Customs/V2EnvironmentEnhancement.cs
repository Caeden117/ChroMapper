using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2EnvironmentEnhancement : BaseEnvironmentEnhancement
    {
        public V2EnvironmentEnhancement()
        {
        }

        public V2EnvironmentEnhancement(JSONNode node) : base(node)
        {
        }

        public override string KeyID { get; } = "_id";

        public override string KeyLookupMethod { get; } = "_lookupMethod";

        public override string KeyGeometry { get; } = "_geometry";

        public override string KeyTrack { get; } = "_track";

        public override string KeyDuplicate { get; } = "_duplicate";

        public override string KeyActive { get; } = "_active";

        public override string KeyScale { get; } = "_scale";

        public override string KeyPosition { get; } = "_position";

        public override string KeyRotation { get; } = "_rotation";

        public override string KeyLocalPosition { get; } = "_localPosition";

        public override string KeyLocalRotation { get; } = "_localRotation";

        public override string KeyComponents { get; } = "_components";

        public override string KeyLightID { get; } = "_lightID";

        public override JSONNode ToJson()
        {
            var node = new JSONObject();
            if (Geometry != null)
            {
                node[KeyGeometry] = Geometry;
            }
            else
            {
                node[KeyID] = ID;
                node[KeyLookupMethod] = LookupMethod.ToString();
            }

            if (node[KeyTrack] != null) node[KeyTrack] = Track;
            if (node[KeyDuplicate] != null) node[KeyDuplicate] = Duplicate;
            if (node[KeyActive] != null) node[KeyActive] = Active;
            if (node[KeyScale] != null) node[KeyScale] = Scale;
            if (node[KeyPosition] != null) node[KeyPosition] = Position;
            if (node[KeyRotation] != null) node[KeyRotation] = Rotation;
            if (node[KeyLocalPosition] != null) node[KeyLocalPosition] = LocalPosition;
            if (node[KeyLightID] != null) node[KeyLightID] = KeyLightID;

            return node;
        }

        public override BaseItem Clone() => new V2EnvironmentEnhancement(ToJson().Clone());
    }
}
