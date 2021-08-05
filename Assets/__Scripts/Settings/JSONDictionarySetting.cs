using System.Collections.Generic;
using SimpleJSON;

public abstract class JSONDictionarySetting : Dictionary<string, JSONNode>, IJSONSetting
{
    public void FromJSON(JSONNode obj)
    {
        string[] keys = new string[Keys.Count];
        Keys.CopyTo(keys, 0);
        foreach (string key in keys)
        {
            if (obj[key] != null) this[key] = obj[key];
        }
    }

    public JSONObject ToJSON()
    {
        JSONObject obj = new JSONObject();
        foreach (KeyValuePair<string, JSONNode> pair in this)
        {
            obj[pair.Key] = pair.Value;
        }

        return obj;
    }
}