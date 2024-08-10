using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public static class V3EnvironmentEnhancement
    {
        public static string KeyID = "id";

        public static string KeyLookupMethod = "lookupMethod";

        public static string KeyGeometry = "geometry";

        public static string KeyTrack = "track";

        public static string KeyDuplicate = "duplicate";

        public static string KeyActive = "active";

        public static string KeyScale = "scale";

        public static string KeyPosition = "position";

        public static string KeyRotation = "rotation";

        public static string KeyLocalPosition = "localPosition";

        public static string KeyLocalRotation = "localRotation";

        public static string KeyComponents = "components";

        public static string KeyLightID = "lightID";

        public static string KeyLightType = "type";

        public static string GeometryKeyType = "type";

        public static string GeometryKeyMaterial = "material";

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
            if (environment.Components != null) node[KeyComponents] = environment.Components;

            return node;
        }
    }
}
