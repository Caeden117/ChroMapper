using SimpleJSON;
using UnityEngine;

public class MultiSettings : IJsonSetting
{
    public MapperIdentityPacket LocalIdentity => new MapperIdentityPacket(DisplayName, 0, GridColor);

    public string DisplayName = "Mapper";
    public Color GridColor = Random.ColorHSV(0, 1, 1, 1, 1, 1);

    public string ChroMapTogetherServerUrl = "http://chromapper.caeden.dev";

    public string LastHostedPort = "6969";
    public string LastJoinedIP = "127.0.0.1";
    public string LastJoinedPort = "6969";

    public void FromJson(JSONNode obj)
    {
        DisplayName = obj[nameof(DisplayName)];
        GridColor = obj[nameof(GridColor)];
        ChroMapTogetherServerUrl = obj[nameof(ChroMapTogetherServerUrl)];
        LastHostedPort = obj[nameof(LastHostedPort)];
        LastJoinedIP = obj[nameof(LastJoinedIP)];
        LastJoinedPort = obj[nameof(LastJoinedPort)];
    }
    
    public JSONObject ToJson()
    {
        var obj = new JSONObject();

        obj[nameof(DisplayName)] = DisplayName;
        obj[nameof(GridColor)] = GridColor;
        obj[nameof(ChroMapTogetherServerUrl)] = ChroMapTogetherServerUrl;
        obj[nameof(LastHostedPort)] = LastHostedPort;
        obj[nameof(LastJoinedIP)] = LastJoinedIP;
        obj[nameof(LastJoinedPort)] = LastJoinedPort;

        return obj;
    }
}
