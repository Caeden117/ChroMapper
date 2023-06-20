using System;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base.Customs
{
    public abstract class BaseEnvironmentEnhancement : BaseItem
    {
        protected BaseEnvironmentEnhancement()
        {
        }

        public BaseEnvironmentEnhancement(BaseEnvironmentEnhancement other)
        {
            ID = other.ID;
            LookupMethod = other.LookupMethod;
            Geometry = other.Geometry?.Clone();
            Track = other.Track;
            Duplicate = other.Duplicate;
            Active = other.Active;
            Scale = other.Scale;
            Position = other.Position;
            Rotation = other.Rotation;
            LocalPosition = other.LocalPosition;
            LocalRotation = other.LocalRotation;
            Components = other.Components?.Clone();
            LightID = other.LightID;
        }

        protected BaseEnvironmentEnhancement(JSONNode node) => InstantiateHelper(ref node);

        protected BaseEnvironmentEnhancement(string toRemove)
        {
            ID = toRemove;
            Active = false;
            LookupMethod = EnvironmentLookupMethod.Contains;
        }

        public string ID { get; set; }
        public EnvironmentLookupMethod LookupMethod { get; set; } = EnvironmentLookupMethod.Contains;
        public JSONNode Geometry { get; set; }
        public string Track { get; set; }
        public int? Duplicate { get; set; }
        public JSONNode Active { get; set; }
        public Vector3? Scale { get; set; }
        public Vector3? Position { get; set; }
        public Vector3? Rotation { get; set; }
        public Vector3? LocalPosition { get; set; }
        public Vector3? LocalRotation { get; set; }
        public JSONNode Components { get; set; }
        public virtual int? LightID { get; set; }
        public virtual int? LightType { get; set; }

        public abstract string KeyID { get; }
        public abstract string KeyLookupMethod { get; }
        public abstract string KeyGeometry { get; }
        public abstract string KeyTrack { get; }
        public abstract string KeyDuplicate { get; }
        public abstract string KeyActive { get; }
        public abstract string KeyScale { get; }
        public abstract string KeyPosition { get; }
        public abstract string KeyRotation { get; }
        public abstract string KeyLocalPosition { get; }
        public abstract string KeyLocalRotation { get; }
        public abstract string KeyComponents { get; }
        public abstract string KeyLightID { get; }
        public abstract string KeyLightType { get; }

        public abstract string GeometryKeyType { get; }
        public abstract string GeometryKeyMaterial { get; }

        private static Vector3? ReadVector3OrNull(JSONNode node, string key) =>
            !node.HasKey(key) || node[key].IsNull ? (Vector3?)null : node[key].ReadVector3();

        protected static void WriteVector3(JSONNode node, string key, Vector3? v)
        {
            if (!v.HasValue) return;

            node[key] = new JSONArray();
            node[key].WriteVector3(v.Value);
        }

        private bool Equals(BaseEnvironmentEnhancement other) =>
            ID == other.ID && LookupMethod == other.LookupMethod && Duplicate == other.Duplicate &&
            Active == other.Active && Nullable.Equals(Scale, other.Scale) &&
            Nullable.Equals(Position, other.Position) && Nullable.Equals(LocalPosition, other.LocalPosition) &&
            Nullable.Equals(Rotation, other.Rotation) &&
            Nullable.Equals(LocalRotation, other.LocalRotation) &&
            Nullable.Equals(LightID, other.LightID) && Track == other.Track;

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((BaseEnvironmentEnhancement)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ID != null ? ID.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ LookupMethod.GetHashCode();
                hashCode = (hashCode * 397) ^ (Geometry != null ? Geometry.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Duplicate != null ? Duplicate.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Active != null ? Active.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Scale != null ? Scale.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Position != null ? Position.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LocalPosition != null ? LocalPosition.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Rotation != null ? Rotation.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LocalRotation != null ? LocalRotation.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Components != null ? Components.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LightID != null ? LightID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Track != null ? Track.GetHashCode() : 0);
                return hashCode;
            }
        }

        private void InstantiateHelper(ref JSONNode node)
        {
            if (node[KeyGeometry] != null)
            {
                Geometry = node[KeyGeometry];
            }
            else
            {
                ID = node[KeyID]?.Value;
                Enum.TryParse(node[KeyLookupMethod]?.Value, out EnvironmentLookupMethod environmentLookup);
                LookupMethod = environmentLookup;
            }

            if (node[KeyTrack] != null) Track = node[KeyTrack].Value;
            if (node[KeyDuplicate] != null) Duplicate = node[KeyDuplicate].AsInt;
            if (node[KeyActive] != null) Active = node[KeyActive].AsBool;
            Scale = ReadVector3OrNull(node, KeyScale);
            Position = ReadVector3OrNull(node, KeyPosition);
            Rotation = ReadVector3OrNull(node, KeyRotation);
            LocalPosition = ReadVector3OrNull(node, KeyLocalPosition);
            LocalRotation = ReadVector3OrNull(node, KeyLocalRotation);
            Components = node[KeyComponents];
            if (node[KeyLightID] != null) LightID = node[KeyLightID].AsInt;
            if (node[KeyLightType] != null) LightType = node[KeyLightType].AsInt;
        }
    }
}
