using System;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using Beatmap.V2;
using Beatmap.V3;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseObstacle : BaseGrid, ICustomDataObstacle
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Type);
            writer.Put(Duration);
            writer.Put(Width);
            writer.Put(Height);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Type = reader.GetInt();
            Duration = reader.GetFloat();
            Width = reader.GetInt();
            Height = reader.GetInt();
            base.Deserialize(reader);
        }

        private const float mappingExtensionsStartHeightMultiplier = 1.35f;
        private const float mappingExtensionsUnitsToFullHeightWall = 1000 / 3.5f;

        public BaseObstacle()
        {
        }

        private BaseObstacle(BaseObstacle other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            PosX = other.PosX;
            InternalPosY = other.PosY;
            InternalType = other.Type;
            Duration = other.Duration;
            Width = other.Width;
            Height = other.Height;
            CustomData = other.SaveCustom().Clone();
            CustomFake = other.CustomFake;
        }
        
        // Used for node editor
        public BaseObstacle(JSONNode node): this(BeatmapFactory.Obstacle(node)) {}

        public override ObjectType ObjectType { get; set; } = ObjectType.Obstacle;
        
        private int InternalType;
        private int InternalHeight;
        private int InternalPosY;

        public override int PosY
        {
            get => InternalPosY;
            set 
            {
                InternalPosY = value;
                
                // set the v2 type to the closest matching base.
                InternalType = value >= 2 ? (int)ObstacleType.Crouch : (int)ObstacleType.Full;
            }
        }

        // v2 property which sets the v3 properties to match
        public int Type
        {
            get => InternalType;
            set
            {
                InternalType = value;
                switch (value)
                {
                    case (int)ObstacleType.Crouch:
                        InternalPosY = (int)GridY.Top;
                        InternalHeight = (int)ObstacleHeight.Crouch;
                        break;
                    default:
                        InternalPosY = (int)GridY.Base;
                        InternalHeight = (int)ObstacleHeight.Full;
                        break;
                }
            }
        }

        public int Height
        {
            get => InternalHeight;
            set => InternalHeight = value;
        }

        public float Duration { get; set; }
        public float DurationSongBpm { get; set; }
        public int Width { get; set; }
        
        public override float DespawnSongBpmTime { get { return SongBpmTime + DurationSongBpm + Hjd; } }

        public virtual JSONNode CustomSize { get; set; }

        public string CustomKeySize => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeySize,
            3 => V3Obstacle.CustomKeySize,
            _ => UndefinedKey
        };

        public override string CustomKeyColor => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyColor,
            3 => V3Obstacle.CustomKeyColor,
            _ => UndefinedKey
        };

        public override string CustomKeyTrack => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyTrack,
            3 => V3Obstacle.CustomKeyTrack,
            _ => UndefinedKey
        };

        public override string CustomKeyAnimation => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyAnimation,
            3 => V3Obstacle.CustomKeyAnimation,
            _ => UndefinedKey
        };

        public override string CustomKeyCoordinate => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyCoordinate,
            3 => V3Obstacle.CustomKeyCoordinate,
            _ => UndefinedKey
        };

        public override string CustomKeyWorldRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyWorldRotation,
            3 => V3Obstacle.CustomKeyWorldRotation,
            _ => UndefinedKey
        };

        public override string CustomKeyLocalRotation => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyLocalRotation,
            3 => V3Obstacle.CustomKeyLocalRotation,
            _ => UndefinedKey
        };

        public override string CustomKeySpawnEffect => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeySpawnEffect,
            3 => V3Obstacle.CustomKeySpawnEffect,
            _ => UndefinedKey
        };

        public override string CustomKeyNoteJumpMovementSpeed => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyNoteJumpMovementSpeed,
            3 => V3Obstacle.CustomKeyNoteJumpMovementSpeed,
            _ => UndefinedKey
        };

        public override string CustomKeyNoteJumpStartBeatOffset => Settings.Instance.MapVersion switch
        {
            2 => V2Obstacle.CustomKeyNoteJumpStartBeatOffset,
            3 => V3Obstacle.CustomKeyNoteJumpStartBeatOffset,
            _ => UndefinedKey
        };

        
        public override bool IsChroma() =>
            CustomData != null && CustomData.HasKey(CustomKeyColor) && CustomData[CustomKeyColor].IsArray;

        public override bool IsNoodleExtensions() =>
            CustomData != null &&
            ((CustomData.HasKey("uninteractable") && CustomData["uninteractable"].IsBoolean) ||
             (CustomData.HasKey(CustomKeyLocalRotation) && CustomData[CustomKeyLocalRotation].IsArray) ||
             (CustomData.HasKey(CustomKeyNoteJumpMovementSpeed) && CustomData[CustomKeyNoteJumpMovementSpeed].IsNumber) ||
             (CustomData.HasKey(CustomKeyNoteJumpStartBeatOffset) &&
              CustomData[CustomKeyNoteJumpStartBeatOffset].IsNumber) ||
             (CustomData.HasKey(CustomKeyCoordinate) && CustomData[CustomKeyCoordinate].IsArray) ||
             (CustomData.HasKey(CustomKeyWorldRotation) &&
              (CustomData[CustomKeyWorldRotation].IsArray || CustomData[CustomKeyWorldRotation].IsNumber)) ||
             (CustomData.HasKey(CustomKeySize) && CustomData[CustomKeySize].IsArray));

        public override bool IsMappingExtensions() =>
            PosX <= -1000 || PosX >= 1000 ||
            PosY < 0 || PosY > 2 ||
            Width <= -1000 || Width >= 1000 ||
            Height <= -1000 || Height > 5 || 
            (Settings.Instance.MapVersion == 2 && (PosX < 0 || PosX > 3));

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseObstacle obstacle)
            {
                SaveCustom();
                obstacle.SaveCustom();
                if (IsNoodleExtensions() || obstacle.IsNoodleExtensions())
                    return ToJson().ToString() == other.ToJson().ToString();
                return PosX == obstacle.PosX && PosY == obstacle.PosY && Width == obstacle.Width &&
                       Height == obstacle.Height;
            }

            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseObstacle obstacle)
            {
                Duration = obstacle.Duration;
                Width = obstacle.Width;
                InternalPosY = obstacle.PosY;
                InternalHeight = obstacle.Height;
                InternalType = obstacle.Type;
            }
        }

        public ObstacleBounds GetShape()
        {
            var position = PosX - 2f; //Line index
            var clampedY = Mathf.Clamp(PosY, 0, 2);
            float startHeight = clampedY;
            float height = Mathf.Min(Height, 5 - clampedY);
            float width = Width;

            // ME

            if (Width >= 1000) width = ((float)Width - 1000) / 1000;
            if (PosX >= 1000)
                position = (((float)PosX - 1000) / 1000f) - 2f;
            else if (PosX <= -1000)
                position = ((float)PosX - 1000) / 1000f;

            if (Type > 1 && Type < 1000)
            {
                startHeight = Type / (750 / 3.5f); //start height 750 == standard wall height
                height = 3.5f;
            }
            else if (Type >= 1000 && Type <= 4000)
            {
                startHeight = 0; //start height = floor
                height = ((float)Type - 1000) /
                         mappingExtensionsUnitsToFullHeightWall; //1000 = no height, 2000 = full height
            }
            else if (Type > 4000)
            {
                float modifiedType = Type - 4001;
                startHeight = modifiedType % 1000 / mappingExtensionsUnitsToFullHeightWall *
                              mappingExtensionsStartHeightMultiplier;
                height = modifiedType / 1000 / mappingExtensionsUnitsToFullHeightWall;
            }

            // NE

            //Just look at the difference in code complexity for Mapping Extensions support and Noodle Extensions support.
            //Hot damn.
            if (CustomData == null) return new ObstacleBounds(width, height, position, startHeight);
            if (CustomCoordinate != null && CustomCoordinate.IsArray)
            {
                if (CustomCoordinate[0].IsNumber) position = CustomCoordinate[0];
                if (CustomCoordinate[1].IsNumber) startHeight = CustomCoordinate[1];
            }

            if (CustomSize != null && CustomSize.IsArray)
            {
                if (CustomSize[0].IsNumber) width = CustomSize[0];
                if (CustomSize[1].IsNumber) height = CustomSize[1];
            }

            return new ObstacleBounds(width, height, position, startHeight);
        }

        public override void RecomputeSongBpmTime()
        {
            base.RecomputeSongBpmTime();
            DurationSongBpm = (BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange)
                ?.JsonTimeToSongBpmTime(JsonTime + Duration) ?? (JsonTime + Duration)) - SongBpmTime;
        }

        protected void InferType() =>
            InternalType = PosY switch
            {
                (int)GridY.Base when Height is (int)ObstacleHeight.Full => (int)ObstacleType.Full,
                (int)GridY.Top when Height is (int)ObstacleHeight.Crouch => (int)ObstacleType.Crouch,
                _ => Type
            };

        protected override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;

            if (CustomData.HasKey(CustomKeySize))
            {
                CustomSize = CustomData[CustomKeySize];
            }
            else
            {
                CustomSize = null;
            }
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            if (CustomSize != null)
            {
                node[CustomKeySize] = CustomSize;
            }
            else
            {
                node.Remove(CustomKeySize);
            }
            
            SetCustomData(node);
            return node;
        }
        
        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseObstacle obstacle) return comparison;

            // Compare by X pos if times match
            if (comparison == 0) comparison = PosX.CompareTo(obstacle.PosX);

            // Compare by Y pos if X pos match
            if (comparison == 0) comparison = PosY.CompareTo(obstacle.PosY);
            
            // Compare by type if Y pos match
            if (comparison == 0) comparison = Type.CompareTo(obstacle.Type);
            
            // Compare by duration if type match
            if (comparison == 0) comparison = Duration.CompareTo(obstacle.Duration);
            
            // Compare by width if duration match
            if (comparison == 0) comparison = Width.CompareTo(obstacle.Width);
            
            // Compare by height if duration match
            if (comparison == 0) comparison = Height.CompareTo(obstacle.Height);

            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0) comparison = string.Compare(CustomData?.ToString(), obstacle.CustomData?.ToString(), StringComparison.Ordinal);

            return comparison;
        }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 or 4 => V3Obstacle.ToJson(this),
            2 => V2Obstacle.ToJson(this)
        };

        public override BaseItem Clone()
        {
            var obstacle = new BaseObstacle(this);
            obstacle.ParseCustom();
            return obstacle;
        }
    }
}
