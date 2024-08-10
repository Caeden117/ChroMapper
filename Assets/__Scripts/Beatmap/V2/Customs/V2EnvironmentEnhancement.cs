using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public static class V2EnvironmentEnhancement
    {
        public static string KeyID = "_id";

        public static string KeyLookupMethod = "_lookupMethod";

        public static string KeyGeometry = "_geometry";

        public static string KeyTrack = "_track";

        public static string KeyDuplicate = "_duplicate";

        public static string KeyActive = "_active";

        public static string KeyScale = "_scale";

        public static string KeyPosition = "_position";

        public static string KeyRotation = "_rotation";

        public static string KeyLocalPosition = "_localPosition";

        public static string KeyLocalRotation = "_localRotation";

        public static string KeyComponents = "_components";

        public static string KeyLightID = "_lightID";

        public static string KeyLightType = "_type";

        public static string GeometryKeyType = "_type";

        public static string GeometryKeyMaterial = "_material";

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
