using SimpleJSON;

public interface IJSONSetting
{
    void FromJSON(JSONNode obj);

    JSONObject ToJSON();
}
