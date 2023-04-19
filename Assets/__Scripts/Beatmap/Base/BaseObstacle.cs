using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Shared;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseObstacle : BaseGrid, ICustomDataObstacle
    {
        protected int InternalType;
        protected int InternalHeight;
        protected int InternalPosY;

        private const float mappingExtensionsStartHeightMultiplier = 1.35f;
        private const float mappingExtensionsUnitsToFullHeightWall = 1000 / 3.5f;

        protected BaseObstacle()
        {
        }

        protected BaseObstacle(BaseObstacle other)
        {
            Time = other.Time;
            PosX = other.PosX;
            InternalPosY = other.PosY;
            InternalType = other.Type;
            Duration = other.Duration;
            Width = other.Width;
            InternalHeight = other.Height;
            CustomData = other.SaveCustom().Clone();
        }

        protected BaseObstacle(float time, int posX, int type, float duration, int width,
            JSONNode customData = null) : base(time, posX, 0, customData)
        {
            InternalType = type;
            Duration = duration;
            Width = width;
            InferPosYHeight();
        }

        protected BaseObstacle(float time, int posX, int posY, float duration, int width, int height,
            JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Duration = duration;
            Width = width;
            InternalHeight = height;
            InferType();
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Obstacle;

        public override int PosY
        {
            get => InternalPosY;
            set => InternalPosY = value;
        }

        // this is not making me happy since this is only v2 related stuff
        public virtual int Type
        {
            get => InternalType;
            set
            {
                InternalType = value;
                InferPosYHeight();
            }
        }

        public float Duration { get; set; }
        public int Width { get; set; }

        public virtual int Height
        {
            get => InternalHeight;
            set => InternalHeight = value;
        }

        public virtual Vector2Or3? CustomSize { get; set; }

        public abstract string CustomKeySize { get; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseObstacle obstacle)
            {
                SaveCustom();
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
            var startHeight = -0.5f + clampedY;
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
            InternalType = PosY switch
            {
                (int)GridY.Base when Height is (int)ObstacleHeight.Full => (int)ObstacleType.Full,
                (int)GridY.Top when Height is (int)ObstacleHeight.Crouch => (int)ObstacleType.Crouch,
                _ => Type
            };

        // type more than 1 will always default to full height wall
        protected void InferPosYHeight()
        {
            switch (Type)
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

        protected override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;

            if (CustomData.HasKey(CustomKeySize) && CustomData[CustomKeySize].IsArray)
            {
                var temp = CustomData[CustomKeySize].AsArray;
                while (temp.Count < 3) temp.Add(null);
                CustomSize = new Vector2Or3(temp[0], temp[1], temp[2].IsNull ? null : (float?)temp[2].AsFloat);
            }
            else
            {
                CustomSize = null;
            }
        }

        protected internal override JSONNode SaveCustom()
        {
            CustomData = base.SaveCustom();
            if (CustomSize != null)
            {
                var size = CustomSize.Value;
                if (size.z == null) // Tenary assignment would implicitly convert Vector2 to Vector3
                    CustomData[CustomKeySize] = new Vector2(size.x, size.y);
                else
                    CustomData[CustomKeySize] = new Vector3(size.x, size.y, size.z.Value);
            }
            else
            {
                CustomData.Remove(CustomKeySize);
            }
            return CustomData;
        }
    }
}
