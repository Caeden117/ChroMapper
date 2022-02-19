using System;
using SimpleJSON;
using UnityEngine;

public class EnvEnhancement
{
    private bool? active;
    private int duplicate;
    public string ID;
    private Vector3? localPosition;
    private Vector3? localRotation;
    private ELookupMethod lookupMethod;
    private Vector3? position;
    private Vector3? rotation;
    private int? lightID;

    private Vector3? scale;

    private string track;

    public EnvEnhancement(JSONNode node)
    {
        ID = node["_id"].Value;

        lookupMethod = ELookupMethod.Contains;
        Enum.TryParse(node["_lookupMethod"].Value, out lookupMethod);

        duplicate = node["_duplicate"].AsInt;
        active = !node.HasKey("_active") || node["_active"].IsNull ? (bool?)null : node["_active"].AsBool;
        scale = ReadVector3OrNull(node, "_scale");
        position = ReadVector3OrNull(node, "_position");
        localPosition = ReadVector3OrNull(node, "_localPosition");
        rotation = ReadVector3OrNull(node, "_rotation");
        localRotation = ReadVector3OrNull(node, "_localRotation");
        lightID = !node.HasKey("_lightID") || node["_lightID"].IsNull ? (int?)null : node["_lightID"].AsInt;
        track = node["_track"].Value;
    }

    public EnvEnhancement(string toRemove)
    {
        ID = toRemove;
        active = false;
        lookupMethod = ELookupMethod.Contains;
    }

    private static Vector3? ReadVector3OrNull(JSONNode node, string key) =>
        !node.HasKey(key) || node[key].IsNull ? (Vector3?)null : node[key].ReadVector3();

    private static void WriteVector3(JSONNode node, string key, Vector3? v)
    {
        if (!v.HasValue) return;

        node[key] = new JSONArray();
        node[key].WriteVector3(v.Value);
    }

    public JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();

        node["_id"] = ID;
        node["_lookupMethod"] = lookupMethod.ToString();
        if (duplicate > 0) node["_duplicate"] = duplicate;
        if (active.HasValue) node["_active"] = active.Value;
        WriteVector3(node, "_scale", scale);
        WriteVector3(node, "_position", position);
        WriteVector3(node, "_localPosition", localPosition);
        WriteVector3(node, "_rotation", rotation);
        WriteVector3(node, "_localRotation", localRotation);
        if (lightID.HasValue) node["_lightID"] = lightID.Value;
        if (!string.IsNullOrEmpty(track)) node["_track"] = track;

        return node;
    }

    public EnvEnhancement Clone() =>
        new EnvEnhancement(ID)
        {
            lookupMethod = lookupMethod,
            duplicate = duplicate,
            active = active,
            scale = scale,
            position = position,
            localPosition = localPosition,
            rotation = rotation,
            localRotation = localRotation,
            lightID = lightID,
            track = track
        };

    protected bool Equals(EnvEnhancement other) =>
        ID == other.ID && lookupMethod == other.lookupMethod && duplicate == other.duplicate &&
        active == other.active && Nullable.Equals(scale, other.scale) &&
        Nullable.Equals(position, other.position) && Nullable.Equals(localPosition, other.localPosition) &&
        Nullable.Equals(rotation, other.rotation) &&
        Nullable.Equals(localRotation, other.localRotation) && 
        Nullable.Equals(lightID, other.lightID) && track == other.track;

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((EnvEnhancement)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ID != null ? ID.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (int)lookupMethod;
            hashCode = (hashCode * 397) ^ duplicate;
            hashCode = (hashCode * 397) ^ active.GetHashCode();
            hashCode = (hashCode * 397) ^ scale.GetHashCode();
            hashCode = (hashCode * 397) ^ position.GetHashCode();
            hashCode = (hashCode * 397) ^ localPosition.GetHashCode();
            hashCode = (hashCode * 397) ^ rotation.GetHashCode();
            hashCode = (hashCode * 397) ^ localRotation.GetHashCode();
            hashCode = (hashCode * 397) ^ lightID.GetHashCode();
            hashCode = (hashCode * 397) ^ (track != null ? track.GetHashCode() : 0);
            return hashCode;
        }
    }

    private enum ELookupMethod
    {
        Contains, Exact, Regex
    }
}
