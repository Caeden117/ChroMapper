using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Info
{
    public class InfoContributor : BaseContributor
    {
        public InfoContributor(JSONNode node)
        {
            Name = node["_name"]?.Value;
            Role = node["_role"]?.Value;
            LocalImageLocation = node["_iconPath"]?.Value;
        }

        public InfoContributor(string name, string role, string iconPath)
        {
            Name = name;
            Role = role;
            LocalImageLocation = iconPath;
        }
        
        public override JSONNode ToJson() =>
            new JSONObject
            {
                ["_name"] = Name,
                ["_role"] = Role,
                ["_iconPath"] = LocalImageLocation
            };

        public override BaseItem Clone() => new InfoContributor(Name, Role, LocalImageLocation);
    }
}
