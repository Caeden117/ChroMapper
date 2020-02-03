using SimpleJSON;

public class MapContributor
{
    public string Name { get; private set; }
    public string Role { get; private set; }
    public string LocalImageLocation { get; private set; }

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

    public JSONNode ToJSONNode()
    {
        JSONObject obj = new JSONObject();
        obj["_name"] = Name;
        obj["_role"] = Role;
        obj["_iconPath"] = LocalImageLocation;
        return obj;
    }
}