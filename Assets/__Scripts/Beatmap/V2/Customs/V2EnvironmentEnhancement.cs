using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2EnvironmentEnhancement : BaseEnvironmentEnhancement, V2Object
    {
        public V2EnvironmentEnhancement()
        {
        }

        public V2EnvironmentEnhancement(BaseEnvironmentEnhancement other) : base(other)
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

        public override string GeometryKeyType { get; } = "_type";

        public override string GeometryKeyMaterial { get; } = "_material";

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

            if (!string.IsNullOrEmpty(Track)) node[KeyTrack] = Track;
            if (Duplicate > 0) node[KeyDuplicate] = Duplicate;
            if (Active != null) node[KeyActive] = Active;
            if (Scale != null) WriteVector3(node, KeyScale, Scale);
            if (Position != null) WriteVector3(node, KeyPosition, Position);
            if (Rotation != null) WriteVector3(node, KeyRotation, Rotation);
            if (LocalPosition != null) WriteVector3(node, KeyLocalPosition, LocalPosition);
            if (LocalRotation != null) WriteVector3(node, KeyLocalRotation, LocalRotation);
            if (LightID > 0) node[KeyLightID] = LightID;

            return node;
        }

        public override BaseItem Clone() => new V2EnvironmentEnhancement(this);
    }
}
