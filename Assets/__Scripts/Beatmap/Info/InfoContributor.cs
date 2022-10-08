using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.Info
{
    public class InfoContributor : IContributor
    {
        public InfoContributor(JSONNode node)
        {
            Name = node["_name"];
            Role = node["_role"];
            LocalImageLocation = node["_iconPath"];
        }

        public InfoContributor(string name, string role, string iconPath)
        {
            Name = name;
            Role = role;
            LocalImageLocation = iconPath;
        }

        public IDictionary<string, JSONNode> UnserializedData { get; }
        public string LocalImageLocation { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }

        public override JSONNode ToJson()
        {
            return new JSONObject
            {
                ["_name"] = Name,
                ["_role"] = Role,
                ["_iconPath"] = LocalImageLocation
            };
        }

        public override IItem Clone()
        {
            return new InfoContributor(Name, Role, LocalImageLocation);
        }
    }
}
