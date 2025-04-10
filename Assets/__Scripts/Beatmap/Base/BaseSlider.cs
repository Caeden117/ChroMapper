using Beatmap.Base.Customs;
using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseSlider : BaseGrid, ICustomDataSlider
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Color); ;
            writer.Put(CutDirection);
            writer.Put(AngleOffset);
            writer.Put(TailJsonTime);
            writer.Put(TailSongBpmTime);
            writer.Put(TailPosX);
            writer.Put(TailPosY);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Color = reader.GetInt();
            CutDirection = reader.GetInt();
            AngleOffset = reader.GetInt();
            tailJsonTime = reader.GetFloat();
            tailSongBpmTime = reader.GetFloat();
            TailPosX = reader.GetInt();
            TailPosY = reader.GetInt();
            base.Deserialize(reader);
        }

        protected BaseSlider()
        {
            TailJsonTime = 0; // needed to set tailSongBpmTime
        }

        protected BaseSlider(float time, int posX, int posY, int color, int cutDirection, int angleOffset,
            float tailTime, int tailPosX, int tailPosY, JSONNode customData = null) : base(time, posX, posY, customData)
        {
            Color = color;
            CutDirection = cutDirection;
            AngleOffset = angleOffset;
            TailJsonTime = tailTime;
            TailPosX = tailPosX;
            TailPosY = tailPosY;
        }

        protected BaseSlider(float jsonTime, float songBpmTime, int posX, int posY, int color, int cutDirection, int angleOffset,
            float tailJsonTime, float tailSongBpmTime, int tailPosX, int tailPosY, JSONNode customData = null)
            : base(jsonTime, songBpmTime, posX, posY, customData)
        {
            Color = color;
            CutDirection = cutDirection;
            AngleOffset = angleOffset;
            this.tailJsonTime = tailJsonTime;
            this.tailSongBpmTime = tailSongBpmTime;
            TailPosX = tailPosX;
            TailPosY = tailPosY;
        }


        public int Color { get; set; }
        public int CutDirection { get; set; }
        public int AngleOffset { get; set; }

        private float tailJsonTime;
        public float TailJsonTime
        {
            get => tailJsonTime;
            set
            {
                tailJsonTime = value;
                RecomputeTailSongBpmTime();
            }
        }
        private float? tailSongBpmTime;
        public float TailSongBpmTime => (float)tailSongBpmTime;

        public void SetTailTimes(float jsonTime)
        {
            TailJsonTime = jsonTime;
        }

        public int TailPosX { get; set; }
        public int TailPosY { get; set; }

        public int TailRotation { get; set; }

        public JSONNode CustomTailCoordinate { get; set; }

        public abstract string CustomKeyTailCoordinate { get; }

        public override void RecomputeSongBpmTime()
        {
            base.RecomputeSongBpmTime();
            RecomputeTailSongBpmTime();
        }

        private void RecomputeTailSongBpmTime()
        {
            var map = BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Map : null;
            tailSongBpmTime = map?.JsonTimeToSongBpmTime(TailJsonTime);
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseSlider slider)
            {
                return Mathf.Abs(TailJsonTime - slider.TailJsonTime) < BeatmapObjectContainerCollection.Epsilon
                    && Vector2.Distance(this.GetPosition(), slider.GetPosition()) < 0.1
                    && Vector2.Distance(this.GetTailPosition(), slider.GetTailPosition()) < 0.1
                    && CutDirection == slider.CutDirection;
            }

            return false;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseSlider baseSlider)
            {
                Color = baseSlider.Color;
                CutDirection = baseSlider.CutDirection;
                AngleOffset = baseSlider.AngleOffset;
                TailJsonTime = baseSlider.TailJsonTime;
                TailPosX = baseSlider.TailPosX;
                TailPosY = baseSlider.TailPosY;
            }
        }

        public virtual void SwapHeadAndTail()
        {
            var tempJsonTime = JsonTime;
            SetTimes(tailJsonTime);
            SetTailTimes(tempJsonTime);
            (PosX, TailPosX) = (TailPosX, PosX);
            (PosY, TailPosY) = (TailPosY, PosY);
        }

        public Vector2 GetTailPosition() => DerivePositionFromTailData();

        private Vector2 DerivePositionFromTailData()
        {
            var position = TailPosX - 1.5f;
            float layer = TailPosY;

            if (CustomTailCoordinate != null && CustomTailCoordinate.IsArray)
            {
                if (CustomTailCoordinate[0].IsNumber) position = CustomTailCoordinate[0] + 0.5f;
                if (CustomTailCoordinate[1].IsNumber) layer = CustomTailCoordinate[1];
                return new Vector2(position, layer);
            }

            if (TailPosX >= 1000)
                position = (TailPosX / 1000f) - 2.5f;
            else if (TailPosX <= -1000)
                position = (TailPosX / 1000f) - 0.5f;

            if (TailPosY >= 1000 || TailPosY <= -1000) layer = (TailPosY / 1000f) - 1f;

            return new Vector2(position, layer);
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();

            CustomTailCoordinate = (CustomData?.HasKey(CustomKeyTailCoordinate) ?? false) ? CustomData?[CustomKeyTailCoordinate] : null;
        }

        protected JSONNode SaveCustomFromNotes(BaseNote head, BaseNote tail)
        {
            var customData = head.SaveCustom();
            tail.SaveCustom();
            if (tail.CustomData?.HasKey(CustomKeyCoordinate) ?? false)
            {
                CustomTailCoordinate = tail.CustomData[CustomKeyCoordinate];
                customData[CustomKeyTailCoordinate] = CustomTailCoordinate;
            }

            return customData;
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            if (CustomTailCoordinate != null) node[CustomKeyTailCoordinate] = CustomTailCoordinate; else node.Remove(CustomKeyTailCoordinate);
            
            SetCustomData(node);
            return node;
        }
        
        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseSlider slider) return comparison;

            // Compare by X pos if times match
            if (comparison == 0) comparison = PosX.CompareTo(slider.PosX);

            // Compare by Y pos if X pos match
            if (comparison == 0) comparison = PosY.CompareTo(slider.PosY);
            
            // Compare by cut direction if Y pos match
            if (comparison == 0) comparison = CutDirection.CompareTo(slider.CutDirection);
            
            // Compare by cut direction if Y pos match
            if (comparison == 0) comparison = AngleOffset.CompareTo(slider.AngleOffset);
            
            // Start comparing tails of head is identical
            if (comparison == 0) comparison = TailJsonTime.CompareTo(slider.TailJsonTime);
            
            // Compare by X pos if times match
            if (comparison == 0) comparison = TailPosX.CompareTo(slider.TailPosX);

            // Compare by Y pos if X pos match
            if (comparison == 0) comparison = TailPosY.CompareTo(slider.TailPosY);

            // ...i give up.
            return comparison;
        }
    }
}
