using SimpleJSON;

namespace Beatmap.Info
{
    public static class V4Contributor
    {
        public static BaseContributor GetFromJson(JSONNode node)
        {
            var contributor = new BaseContributor();

            contributor.Name = node["name"]?.Value;
            contributor.Role = node["role"]?.Value;
            contributor.LocalImageLocation = node["iconPath"]?.Value;

            return contributor;
        }

        public static JSONObject ToJson(BaseContributor contributor) =>
            new JSONObject
            {
                ["name"] = contributor.Name,
                ["role"] = contributor.Role,
                ["iconPath"] = contributor.LocalImageLocation
            };
    }
}
