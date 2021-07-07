using System;
using SimpleJSON;
using UnityEngine;

public class EnvEnhancement
{
    public string ID;
    private ELookupMethod LookupMethod;
    private int Duplicate;
    private bool? Active;

    private Vector3? Scale;
    private Vector3? Position;
    private Vector3? LocalPosition;
    private Vector3? Rotation;
    private Vector3? LocalRotation;

    private string Track;

    private enum ELookupMethod
    {
        Contains, Exact, Regex
    }
    
    public EnvEnhancement(JSONNode node)
    {
        ID = node["_id"].Value;

        LookupMethod = ELookupMethod.Contains;
        Enum.TryParse(node["_lookupMethod"].Value, out LookupMethod);

        Duplicate = node["_duplicate"].AsInt;
        Active = !node.HasKey("_active") || node["_active"].IsNull ? (bool?) null : node["_active"].AsBool;
        Scale = ReadVector3OrNull(node, "_scale");
        Position = ReadVector3OrNull(node, "_position");
        LocalPosition = ReadVector3OrNull(node, "_localPosition");
        Rotation = ReadVector3OrNull(node, "_rotation");
        LocalRotation = ReadVector3OrNull(node, "_localRotation");
        Track = node["_track"].Value;
    }

    private static Vector3? ReadVector3OrNull(JSONNode node, string key) => (!node.HasKey(key) || node[key].IsNull) ? (Vector3?) null : node[key].ReadVector3();

    private static void WriteVector3(JSONNode node, string key, Vector3? v)
    {
        if (!v.HasValue) return;
        
        node[key] = new JSONArray();
        node[key].WriteVector3(v.Value);
    }

    public EnvEnhancement(string toRemove)
    {
        ID = toRemove;
        Active = false;
        LookupMethod = ELookupMethod.Contains;
    }

    public JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();

        node["_id"] = ID;
        node["_lookupMethod"] = LookupMethod.ToString();
        if (Duplicate > 0) node["_duplicate"] = Duplicate;
        if (Active.HasValue) node["_active"] = Active.Value;
        WriteVector3(node, "_scale", Scale);
        WriteVector3(node, "_position", Position);
        WriteVector3(node, "_localPosition", LocalPosition);
        WriteVector3(node, "_rotation", Rotation);
        WriteVector3(node, "_localRotation", LocalRotation);
        if (!string.IsNullOrEmpty(Track)) node["_track"] = Track;

        return node;
    }

    public EnvEnhancement Clone() =>
        new EnvEnhancement(ID)
        {
            LookupMethod = LookupMethod,
            Duplicate = Duplicate,
            Active = Active,
            Scale = Scale,
            Position = Position,
            LocalPosition = LocalPosition,
            Rotation = Rotation,
            LocalRotation = LocalRotation,
            Track = Track
        };

    protected bool Equals(EnvEnhancement other)
    {
        return ID == other.ID && LookupMethod == other.LookupMethod && Duplicate == other.Duplicate && Active == other.Active && Nullable.Equals(Scale, other.Scale) &&
               Nullable.Equals(Position, other.Position) && Nullable.Equals(LocalPosition, other.LocalPosition) && Nullable.Equals(Rotation, other.Rotation) &&
               Nullable.Equals(LocalRotation, other.LocalRotation) && Track == other.Track;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EnvEnhancement) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (ID != null ? ID.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int) LookupMethod;
            hashCode = (hashCode * 397) ^ Duplicate;
            hashCode = (hashCode * 397) ^ Active.GetHashCode();
            hashCode = (hashCode * 397) ^ Scale.GetHashCode();
            hashCode = (hashCode * 397) ^ Position.GetHashCode();
            hashCode = (hashCode * 397) ^ LocalPosition.GetHashCode();
            hashCode = (hashCode * 397) ^ Rotation.GetHashCode();
            hashCode = (hashCode * 397) ^ LocalRotation.GetHashCode();
            hashCode = (hashCode * 397) ^ (Track != null ? Track.GetHashCode() : 0);
            return hashCode;
        }
    }
}