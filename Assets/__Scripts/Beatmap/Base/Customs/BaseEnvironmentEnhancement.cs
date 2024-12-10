using System;
using Beatmap.Enums;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base.Customs
{
    public class BaseEnvironmentEnhancement : BaseItem
    {
        public BaseEnvironmentEnhancement()
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

        public BaseEnvironmentEnhancement(JSONNode node) => InstantiateHelper(ref node);

        public BaseEnvironmentEnhancement(string toRemove)
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

        public int? LightID
        {
            get
            {
                if (Components != null && Components["ILightWithId"] != null &&
                    Components["ILightWithId"]["lightID"] != null)
                    return Components["ILightWithId"]["lightID"].AsInt;

                return null;
            }
            set
            {
                if (Components != null)
                {
                    if (Components["ILightWithId"] != null)
                        Components["ILightWithId"]["lightID"] = value;
                    else
                        Components["ILightWithId"] = new JSONObject { ["lightID"] = value };
                }
                else
                {
                    var iLightWithID = new JSONObject { ["lightID"] = value };
                    Components = new JSONObject { ["ILightWithId"] = iLightWithID };
                }
            }
        }

        public int? LightType
        {
            get
            {
                if (Components != null && Components["ILightWithId"] != null &&
                    Components["ILightWithId"]["type"] != null)
                    return Components["ILightWithId"]["type"].AsInt;

                return null;
            }
            set
            {
                if (Components != null)
                {
                    if (Components["ILightWithId"] != null)
                        Components["ILightWithId"]["type"] = value;
                    else
                        Components["ILightWithId"] = new JSONObject { ["type"] = value };
                }
                else
                {
                    var iLightWithID = new JSONObject { ["type"] = value };
                    Components = new JSONObject { ["ILightWithId"] = iLightWithID };
                }
            }
        }

        #region CustomData Keys

        public string KeyID => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyID,
            3 => V3EnvironmentEnhancement.KeyID
        };

        public string KeyLookupMethod => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyLookupMethod,
            3 => V3EnvironmentEnhancement.KeyLookupMethod
        };

        public string KeyGeometry => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyGeometry,
            3 => V3EnvironmentEnhancement.KeyGeometry
        };

        public string KeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyTrack,
            3 => V3EnvironmentEnhancement.KeyTrack
        };

        public string KeyDuplicate => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyDuplicate,
            3 => V3EnvironmentEnhancement.KeyDuplicate
        };

        public string KeyActive => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyActive,
            3 or 4 => V3EnvironmentEnhancement.KeyActive
        };

        public string KeyScale => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyScale,
            3 or 4 => V3EnvironmentEnhancement.KeyScale
        };

        public string KeyPosition => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyPosition,
            3 or 4 => V3EnvironmentEnhancement.KeyPosition
        };

        public string KeyRotation => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyRotation,
            3 or 4 => V3EnvironmentEnhancement.KeyRotation
        };

        public string KeyLocalPosition => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyLocalPosition,
            3 or 4 => V3EnvironmentEnhancement.KeyLocalPosition
        };

        public string KeyLocalRotation => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyLocalRotation,
            3 or 4 => V3EnvironmentEnhancement.KeyLocalRotation
        };

        public string KeyComponents => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyComponents,
            3 or 4 => V3EnvironmentEnhancement.KeyComponents
        };

        public string KeyLightID => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyLightID,
            3 or 4 => V3EnvironmentEnhancement.KeyLightID
        };

        public string KeyLightType => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.KeyLightType,
            3 or 4 => V3EnvironmentEnhancement.KeyLightType
        };


        public string GeometryKeyType => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.GeometryKeyType,
            3 or 4 => V3EnvironmentEnhancement.GeometryKeyType
        };

        public string GeometryKeyMaterial => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.GeometryKeyMaterial,
            3 or 4 => V3EnvironmentEnhancement.GeometryKeyMaterial
        };
        
        #endregion


        private static Vector3? ReadVector3OrNull(JSONNode node, string key) =>
            !node.HasKey(key) || node[key].IsNull ? (Vector3?)null : node[key].ReadVector3();

        public static void WriteVector3(JSONNode node, string key, Vector3? v)
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

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2EnvironmentEnhancement.ToJson(this),
            3 or 4 => V3EnvironmentEnhancement.ToJson(this)
        };

        public override BaseItem Clone() => new BaseEnvironmentEnhancement(this);
    }
}
