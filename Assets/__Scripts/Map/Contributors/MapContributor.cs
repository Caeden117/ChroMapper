using System;
using SimpleJSON;

[Serializable]
public class MapContributor
{
    public string Name;
    public string Role;
    public string LocalImageLocation;

    public MapContributor(JSONNode node)
    {
        Name = node["_name"];
        Role = node["_role"];
        LocalImageLocation = node["_iconPath"];
    }

    public MapContributor(string name, string role, string iconPath)
    {
        Name = name;
        Role = role;
        LocalImageLocation = iconPath;
    }

    public JSONNode ToJsonNode()
    {
        var obj = new JSONObject();
        obj["_name"] = Name;
        obj["_role"] = Role;
        obj["_iconPath"] = LocalImageLocation;
        return obj;
    }
}
