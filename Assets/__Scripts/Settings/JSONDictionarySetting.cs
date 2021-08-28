using System.Collections.Generic;
using SimpleJSON;

public abstract class JsonDictionarySetting : Dictionary<string, JSONNode>, IJsonSetting
{
    public void FromJson(JSONNode obj)
    {
        var keys = new string[Keys.Count];
        Keys.CopyTo(keys, 0);
        foreach (var key in keys)
        {
            if (obj[key] != null)
                this[key] = obj[key];
        }
    }

    public JSONObject ToJson()
    {
        var obj = new JSONObject();
        foreach (var pair in this) obj[pair.Key] = pair.Value;

        return obj;
    }
}
