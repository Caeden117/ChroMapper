using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3EnvironmentEnhancement : IEnvironmentEnhancement
    {
        public V3EnvironmentEnhancement()
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

        public override int? LightID
        {
            get => Components?["ILightWithId"]?["lightID"]?.AsInt;
            set
            {
                if (Components != null)
                {
                    if (Components["ILightWithId"] != null)
                        Components["ILightWithId"]["lightID"] = value;
                    else
                        Components["ILightWithId"] = new JSONObject
                        {
                            ["lightID"] = value
                        };
                }
                else
                {
                    var iLightWithID = new JSONObject
                    {
                        ["lightID"] = value
                    };
                    Components = new JSONObject
                    {
                        ["ILightWithId"] = iLightWithID
                    };
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

            if (node[KeyTrack] != null) node[KeyTrack] = Track;
            if (node[KeyDuplicate] != null) node[KeyDuplicate] = Duplicate;
            if (node[KeyActive] != null) node[KeyActive] = Active;
            if (node[KeyScale] != null) node[KeyScale] = Scale;
            if (node[KeyPosition] != null) node[KeyPosition] = Position;
            if (node[KeyRotation] != null) node[KeyRotation] = Rotation;
            if (node[KeyLocalPosition] != null) node[KeyLocalPosition] = LocalPosition;
            if (node[KeyComponents] != null) node[KeyComponents] = Components;

            return node;
        }

        public override IItem Clone() => new V3EnvironmentEnhancement(ToJson().Clone());
    }
}
