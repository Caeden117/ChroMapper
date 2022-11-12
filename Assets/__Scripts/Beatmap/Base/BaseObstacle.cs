using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Shared;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseObstacle : BaseGrid, ICustomDataObstacle
    {
        private const float mappingExtensionsStartHeightMultiplier = 1.35f;
        private const float mappingExtensionsUnitsToFullHeightWall = 1000 / 3.5f;

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
            CustomData = other.SaveCustom().Clone();
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
        public virtual int Type { get; set; }
        public float Duration { get; set; }
        public virtual int Width { get; set; }
        public virtual int Height { get; set; }

        public virtual Vector3? CustomSize { get; set; }

        public abstract string CustomKeySize { get; }

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
            var startHeight = Type == (int)ObstacleType.Crouch ? 1.5f : -0.5f;
            var height = Type == (int)ObstacleType.Crouch ? 3f : 5f;
            float width = Width;
            GetHeights(ref height, ref startHeight);

            // ME

            if (Width >= 1000) width = ((float)Width - 1000) / 1000;
            if (PosX >= 1000)
                position = (((float)PosX - 1000) / 1000f) - 2f;
            else if (PosX <= -1000)
                position = ((float)PosX - 1000) / 1000f;

            if (Type > 2 && Type < 1000)
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

        protected void InferType() =>
            Type = PosY switch
            {
                (int)GridY.Base when Height is (int)ObstacleHeight.Full => (int)ObstacleType.Full,
                (int)GridY.Top when Height is (int)ObstacleHeight.Crouch => (int)ObstacleType.Crouch,
                _ => (int)ObstacleType.Freeform
            };

        protected void InferPosYHeight()
        {
            switch (Type)
            {
                case (int)ObstacleType.Full:
                    PosY = (int)GridY.Base;
                    Height = (int)ObstacleHeight.Full;
                    break;
                case (int)ObstacleType.Crouch:
                    PosY = (int)GridY.Top;
                    Height = (int)ObstacleHeight.Crouch;
                    break;
            }
        }

        private void GetHeights(ref float height, ref float startHeight)
        {
            if (Type != (int)ObstacleType.Freeform) return;
            var clampedY = Mathf.Clamp(PosY, 0, 2);
            startHeight = -0.5f + clampedY;
            height = Mathf.Min(Height, 5 - clampedY);
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;

            if (CustomData.HasKey(CustomKeySize) && CustomData[CustomKeySize].IsArray)
            {
                var temp = CustomData[CustomKeySize].AsArray;
                if (temp.Count < 2) temp.Add(0);
                CustomSize = temp;
            }
        }

        protected internal override JSONNode SaveCustom()
        {
            CustomData = base.SaveCustom();
            if (CustomSize != null) CustomData[CustomKeySize] = CustomSize;
            return CustomData;
        }
    }
}
