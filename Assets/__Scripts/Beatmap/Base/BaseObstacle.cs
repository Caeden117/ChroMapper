using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Shared;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseObstacle : BaseGrid, ICustomDataObstacle
    {
        public const float MappingExtensionsStartHeightMultiplier = 1.35f;
        public const float MappingExtensionsUnitsToFullHeightWall = 1000 / 3.5f;

        // Editor walls heights are squished compared to in game as 1.5f is accurate-ish but y=0 wall and y=0 note base is different.
        private const float heightStep = 0.75f;
        protected Vector3? _customSize;

        protected BaseObstacle()
        {
        }

        protected BaseObstacle(BaseObstacle other)
        {
            Time = other.Time;
            PosX = other.PosX;
            PosY = other.PosY;
            Type = other.Type;
            Duration = other.Duration;
            Width = other.Width;
            Height = other.Height;
            CustomData = other.CustomData?.Clone();
        }

        protected BaseObstacle(float time, int posX, int type, float duration, int width,
            JSONNode customData = null) : base(time, posX, 0, customData)
        {
            Type = type;
            Duration = duration;
            Width = width;
            InferPosYHeight();
        }

        protected BaseObstacle(float time, int posX, int posY, float duration, int width, int height,
            JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Duration = duration;
            Width = width;
            Height = height;
            InferType();
        }

        protected BaseObstacle(float time, int posX, int posY, int type, float duration, int width, int height,
            JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Type = type;
            Duration = duration;
            Width = width;
            Height = height;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Obstacle;
        public int Type { get; set; }
        public float Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseObstacle obstacle)
            {
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
                Height = obstacle.Height;
            }
        }

        public ObstacleBounds GetShape()
        {
            var position = PosX - 2f; //Line index
            var startHeight = Type == (int)ObstacleType.Crouch ? 1.5f : 0;
            var height = Type == (int)ObstacleType.Crouch ? 2.25f : 3.75f;
            float width = Width;
            GetHeights(ref height, ref startHeight);

            // ME

            if (Width >= 1000) width = ((float)Width - 1000) / 1000;
            if (PosX >= 1000)
                position = ((float)PosX - 1000) / 1000f - 2f;
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
                         MappingExtensionsUnitsToFullHeightWall; //1000 = no height, 2000 = full height
            }
            else if (Type > 4000)
            {
                float modifiedType = Type - 4001;
                startHeight = modifiedType % 1000 / MappingExtensionsUnitsToFullHeightWall *
                              MappingExtensionsStartHeightMultiplier;
                height = modifiedType / 1000 / MappingExtensionsUnitsToFullHeightWall;
            }

            // NE

            //Just look at the difference in code complexity for Mapping Extensions support and Noodle Extensions support.
            //Hot damn.
            if (CustomData == null) return new ObstacleBounds(width, height, position, startHeight);
            if (CustomCoordinate != null)
            {
                var wallPos = CustomCoordinate;
                position = wallPos.Value.x;
                startHeight = wallPos.Value.y;
            }

            if (CustomSize == null) return new ObstacleBounds(width, height, position, startHeight);
            var wallSize = CustomSize;
            width = wallSize.Value.x;
            height = wallSize.Value.y;

            return new ObstacleBounds(width, height, position, startHeight);
        }

        public void InferType()
        {
            switch (PosY)
            {
                case (int)GridY.Base when Height == (int)ObstacleHeight.Full:
                    Type = (int)ObstacleType.Full;
                    break;
                case (int)GridY.Top when Height == (int)ObstacleHeight.Crouch:
                    Type = (int)ObstacleType.Crouch;
                    break;
                default:
                    Type = (int)ObstacleType.Freeform;
                    break;
            }
        }

        public void InferPosYHeight()
        {
            switch (Type)
            {
                case (int)ObstacleType.Crouch:
                    PosY = (int)GridY.Base;
                    Height = (int)ObstacleHeight.Full;
                    break;
                case (int)ObstacleType.Full:
                    PosY = (int)GridY.Top;
                    Height = (int)ObstacleHeight.Crouch;
                    break;
            }
        }

        public virtual Vector3? CustomSize
        {
            get => _customSize;
            set
            {
                if (value == null && CustomData?[CustomKeySize] != null)
                {
                    CustomData.Remove(CustomKeySize);
                }
                else
                {
                    GetOrCreateCustom()[CustomKeySize] = value;
                }
                _customSize = value;
            }
        }

        public abstract string CustomKeySize { get; }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData?[CustomKeySize] != null)
            {
                var temp = CustomData[CustomKeySize].AsArray;
                if (temp.Count < 2) temp.Add(0);
                CustomSize = temp;
            }
        }

        private void GetHeights(ref float height, ref float startHeight)
        {
            if (Type < 0 || Type > 2) return;
            startHeight = heightStep * Mathf.Clamp(PosY, 0, 2);
            height = Mathf.Min(Height * heightStep, 5 * heightStep - PosY * heightStep);
        }
    }
}
