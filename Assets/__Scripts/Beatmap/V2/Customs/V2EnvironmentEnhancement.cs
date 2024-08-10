using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public static class V2EnvironmentEnhancement
    {
        public const string KeyID = "_id";

        public const string KeyLookupMethod = "_lookupMethod";

        public const string KeyGeometry = "_geometry";

        public const string KeyTrack = "_track";

        public const string KeyDuplicate = "_duplicate";

        public const string KeyActive = "_active";

        public const string KeyScale = "_scale";

        public const string KeyPosition = "_position";

        public const string KeyRotation = "_rotation";

        public const string KeyLocalPosition = "_localPosition";

        public const string KeyLocalRotation = "_localRotation";

        public const string KeyComponents = "_components";

        public const string KeyLightID = "_lightID";

        public const string KeyLightType = "_type";

        public const string GeometryKeyType = "_type";

        public const string GeometryKeyMaterial = "_material";

        public static BaseEnvironmentEnhancement GetFromJson(JSONNode node) => new BaseEnvironmentEnhancement(node);

        public static JSONNode ToJson(BaseEnvironmentEnhancement environment)
        {
            var node = new JSONObject();
            if (environment.Geometry != null)
            {
                node[KeyGeometry] = environment.Geometry;
            }
            else
            {
                node[KeyID] = environment.ID;
                node[KeyLookupMethod] = environment.LookupMethod.ToString();
            }

            if (!string.IsNullOrEmpty(environment.Track)) node[KeyTrack] = environment.Track;
            if (environment.Duplicate > 0) node[KeyDuplicate] = environment.Duplicate;
            if (environment.Active != null) node[KeyActive] = environment.Active;
            if (environment.Scale != null) BaseEnvironmentEnhancement.WriteVector3(node, KeyScale, environment.Scale);
            if (environment.Position != null) BaseEnvironmentEnhancement.WriteVector3(node, KeyPosition, environment.Position);
            if (environment.Rotation != null) BaseEnvironmentEnhancement.WriteVector3(node, KeyRotation, environment.Rotation);
            if (environment.LocalPosition != null) BaseEnvironmentEnhancement.WriteVector3(node, KeyLocalPosition, environment.LocalPosition);
            if (environment.LocalRotation != null) BaseEnvironmentEnhancement.WriteVector3(node, KeyLocalRotation, environment.LocalRotation);
            if (environment.LightID > 0) node[KeyLightID] = environment.LightID;

            return node;
        }
    }
}
