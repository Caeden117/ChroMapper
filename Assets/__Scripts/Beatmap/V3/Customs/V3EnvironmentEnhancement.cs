using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3EnvironmentEnhancement : BaseEnvironmentEnhancement, V3Object
    {
        public V3EnvironmentEnhancement()
        {
        }

        public V3EnvironmentEnhancement(BaseEnvironmentEnhancement other) : base(other)
        {
        }

        public V3EnvironmentEnhancement(JSONNode node) : base(node)
        {
        }

        public override string KeyID { get; } = "id";

        public override string KeyLookupMethod { get; } = "lookupMethod";

        public override string KeyGeometry { get; } = "geometry";

        public override string KeyTrack { get; } = "track";

        public override string KeyDuplicate { get; } = "duplicate";

        public override string KeyActive { get; } = "active";

        public override string KeyScale { get; } = "scale";

        public override string KeyPosition { get; } = "position";

        public override string KeyRotation { get; } = "rotation";

        public override string KeyLocalPosition { get; } = "localPosition";

        public override string KeyLocalRotation { get; } = "localRotation";

        public override string KeyComponents { get; } = "components";

        public override string KeyLightID { get; } = "lightID";

        public override string GeometryKeyType { get; } = "type";

        public override string GeometryKeyMaterial { get; } = "material";

        public override int? LightID
        {
            get
            {
                if (Components != null && Components["ILightWithId"] != null &&
                    Components["ILightWithId"]["lightID"] != null)
                    return Components["ILightWithId"]["lightID"].AsInt;

                return null;
            }
            set
            {
                if (Components != null)
                {
                    if (Components["ILightWithId"] != null)
                        Components["ILightWithId"]["lightID"] = value;
                    else
                        Components["ILightWithId"] = new JSONObject { ["lightID"] = value };
                }
                else
                {
                    var iLightWithID = new JSONObject { ["lightID"] = value };
                    Components = new JSONObject { ["ILightWithId"] = iLightWithID };
                }
            }
        }

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
            if (Components != null) node[KeyComponents] = Components;

            return node;
        }

        public override BaseItem Clone() => new V3EnvironmentEnhancement(this);
    }
}
