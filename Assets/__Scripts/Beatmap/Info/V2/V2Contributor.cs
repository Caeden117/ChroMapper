using SimpleJSON;

namespace Beatmap.Info
{
    public static class V2Contributor
    {
        public static BaseContributor GetFromJson(JSONNode node)
        {
            var contributor = new BaseContributor();

            contributor.Name = node["_name"]?.Value;
            contributor.Role = node["_role"]?.Value;
            contributor.LocalImageLocation = node["_iconPath"]?.Value;

            return contributor;
        }

        public static JSONObject ToJson(BaseContributor contributor) =>
            new JSONObject
            {
                ["_name"] = contributor.Name,
                ["_role"] = contributor.Role,
                ["_iconPath"] = contributor.LocalImageLocation
            };
    }
}
