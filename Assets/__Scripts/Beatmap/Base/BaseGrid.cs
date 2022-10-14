using Beatmap.Base.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseGrid : BaseObject, IObjectBounds, INoodleExtensionsGrid
    {
        protected BaseGrid()
        {
        }

        protected BaseGrid(float time, int posX, int posY, JSONNode customData = null) : base(time, customData)
        {
            PosX = posX;
            PosY = posY;
        }

        public int PosX { get; set; }
        public int PosY { get; set; }
        
        public virtual Vector2? CustomCoordinate { get; set; }

        public virtual Vector3? CustomWorldRotation { get; set; }

        public virtual Vector3? CustomLocalRotation { get; set; }

        public abstract string CustomKeyCoordinate { get; }
        public abstract string CustomKeyWorldRotation { get; }
        public abstract string CustomKeyLocalRotation { get; }

        public Vector2 GetCenter() => GetPosition() + new Vector2(0f, 0.5f);

        public virtual Vector2 GetPosition() => DerivePositionFromData();

        public virtual Vector3 GetScale() => Vector3.one;

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseGrid note)
            {
                PosX = note.PosX;
                PosY = note.PosY;
            }
        }

        protected virtual Vector2 DerivePositionFromData()
        {
            var position = PosX - 1.5f;
            float layer = PosY;

            if (PosX >= 1000)
                position = (PosX / 1000f) - 2.5f;
            else if (PosX <= -1000)
                position = (PosX / 1000f) - 0.5f;

            if (PosY >= 1000 || PosY <= -1000) layer = (PosY / 1000f) - 1f;

            return new Vector2(position, layer);
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;
            if (CustomData[CustomKeyTrack] != null) CustomTrack = CustomData[CustomKeyTrack].Value;
            if (CustomData[CustomKeyCoordinate] != null)
                CustomCoordinate = CustomData[CustomKeyCoordinate].ReadVector2();
            if (CustomData[CustomKeyWorldRotation] != null)
                CustomWorldRotation = CustomData[CustomKeyWorldRotation].ReadVector3();
            if (CustomData[CustomKeyLocalRotation] != null)
                CustomLocalRotation = CustomData[CustomKeyLocalRotation].ReadVector3();
        }

        protected override JSONNode SaveCustom()
        {
            CustomData = base.SaveCustom();
            if (CustomCoordinate != null) CustomData[CustomKeyCoordinate] = CustomCoordinate;
            if (CustomWorldRotation != null) CustomData[CustomKeyWorldRotation] = CustomWorldRotation;
            if (CustomLocalRotation != null) CustomData[CustomKeyLocalRotation] = CustomLocalRotation;
            return CustomData;
        }
    }
}
