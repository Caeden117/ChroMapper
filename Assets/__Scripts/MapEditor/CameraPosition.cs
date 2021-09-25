using SimpleJSON;
using UnityEngine;

public class CameraPosition : IJsonSetting
{
    public CameraPosition(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    public CameraPosition()
    {
        // Used by settings loader before FromJSON
    }

    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }

    public void FromJson(JSONNode obj)
    {
        if (obj == null) return;
        Position = new Vector3(obj["position"][0], obj["position"][1], obj["position"][2]);
        Rotation = new Quaternion(obj["rotation"][1], obj["rotation"][2], obj["rotation"][3], obj["rotation"][0]);
    }

    public JSONObject ToJson()
    {
        var obj = new JSONObject();

        obj["position"].Add(Position.x);
        obj["position"].Add(Position.y);
        obj["position"].Add(Position.z);

        obj["rotation"].Add(Rotation.w);
        obj["rotation"].Add(Rotation.x);
        obj["rotation"].Add(Rotation.y);
        obj["rotation"].Add(Rotation.z);

        return obj;
    }
}
