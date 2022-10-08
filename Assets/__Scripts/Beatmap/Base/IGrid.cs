using Beatmap.Base.Customs;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class IGrid : IObject, IObjectBounds, IHeckGrid, INoodleExtensionsGrid
    {
        protected Vector2? _customCoordinate;
        protected Vector3? _customLocalRotation;
        protected string _customTrack;
        protected Vector3? _customWorldRotation;

        protected IGrid()
        {
        }

        protected IGrid(float time, int posX, int posY, JSONNode customData = null) : base(time, customData)
        {
            PosX = posX;
            PosY = posY;
        }

        public int PosX { get; set; }
        public int PosY { get; set; }

        public Vector2 GetCenter() => GetPosition() + new Vector2(0f, 0.5f);

        public virtual Vector2 GetPosition() => DerivePositionFromData();

        public virtual Vector3 GetScale() => Vector3.one;

        public override void Apply(IObject originalData)
        {
            base.Apply(originalData);

            if (originalData is IGrid note)
            {
                PosX = note.PosX;
                PosY = note.PosY;
            }
        }

        public string CustomTrack
        {
            get => _customTrack;
            set
            {
                GetOrCreateCustom()[CustomKeyTrack] = value;
                _customTrack = value;
            }
        }

        public virtual Vector2? CustomCoordinate
        {
            get => _customCoordinate;
            set
            {
                GetOrCreateCustom()[CustomKeyCoordinate] = value;
                _customCoordinate = value;
            }
        }

        public virtual Vector3? CustomWorldRotation
        {
            get => _customWorldRotation;
            set
            {
                GetOrCreateCustom()[CustomKeyWorldRotation] = value;
                _customWorldRotation = value;
            }
        }

        public virtual Vector3? CustomLocalRotation
        {
            get => _customLocalRotation;
            set
            {
                GetOrCreateCustom()[CustomKeyLocalRotation] = value;
                _customLocalRotation = value;
            }
        }

        public abstract string CustomKeyTrack { get; }
        public abstract string CustomKeyCoordinate { get; }
        public abstract string CustomKeyWorldRotation { get; }
        public abstract string CustomKeyLocalRotation { get; }

        public override void ParseCustom()
        {
            base.ParseCustom();
            if (CustomData == null) return;
            if (CustomData[CustomKeyTrack] != null) CustomTrack = CustomData[CustomKeyTrack].Value;
            if (CustomData[CustomKeyCoordinate] != null) CustomCoordinate = CustomData[CustomKeyCoordinate].ReadVector2();
            if (CustomData[CustomKeyWorldRotation] != null) CustomWorldRotation = CustomData[CustomKeyWorldRotation].ReadVector3();
            if (CustomData[CustomKeyLocalRotation] != null) CustomLocalRotation = CustomData[CustomKeyLocalRotation].ReadVector3();
        }

        protected virtual Vector2 DerivePositionFromData()
        {
            var position = PosX - 1.5f;
            float layer = PosY;

            if (PosX >= 1000)
                position = PosX / 1000f - 2.5f;
            else if (PosX <= -1000)
                position = PosX / 1000f - 0.5f;

            if (PosY >= 1000 || PosY <= -1000) layer = PosY / 1000f - 1f;

            return new Vector2(position, layer);
        }
    }
}
