using SimpleJSON;

public interface IJsonSetting
{
    void FromJson(JSONNode obj);

    JSONObject ToJson();
}
