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
        public bool? Active { get; set; }
        public Vector3? Scale { get; set; }
        public Vector3? Position { get; set; }
        public Vector3? Rotation { get; set; }
        public Vector3? LocalPosition { get; set; }
        public Vector3? LocalRotation { get; set; }
        public JSONNode Components { get; set; }
        public virtual int? LightID { get; set; }

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
                hashCode = (hashCode * 397) ^ (int)LookupMethod;
                hashCode = (hashCode * 397) ^ (int)Duplicate;
                hashCode = (hashCode * 397) ^ Active.GetHashCode();
                hashCode = (hashCode * 397) ^ Scale.GetHashCode();
                hashCode = (hashCode * 397) ^ Position.GetHashCode();
                hashCode = (hashCode * 397) ^ LocalPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ Rotation.GetHashCode();
                hashCode = (hashCode * 397) ^ LocalRotation.GetHashCode();
                hashCode = (hashCode * 397) ^ LightID.GetHashCode();
                hashCode = (hashCode * 397) ^ (Track != null ? Track.GetHashCode() : 0);
                return hashCode;
            }
        }

        protected void InstantiateHelper(ref JSONNode node)
        {
            ID = node[KeyID]?.Value;
            Enum.TryParse(node[KeyLookupMethod]?.Value, out EnvironmentLookupMethod environmentLookup);
            LookupMethod = environmentLookup;

            Geometry = node[KeyGeometry];

            Track = node[KeyTrack]?.Value;
            Duplicate = node[KeyDuplicate]?.AsInt;
            Active = node[KeyActive]?.AsBool;
            Scale = ReadVector3OrNull(node, KeyScale);
            Position = ReadVector3OrNull(node, KeyPosition);
            Rotation = ReadVector3OrNull(node, KeyRotation);
            LocalPosition = ReadVector3OrNull(node, KeyLocalPosition);
            LocalRotation = ReadVector3OrNull(node, KeyLocalRotation);
            Components = node[KeyComponents];
            LightID = node[KeyLightID]?.AsInt;
        }
    }
}
