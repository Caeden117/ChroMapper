using Beatmap.Base.Customs;
using Beatmap.Animations;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseGrid : BaseObject, IObjectBounds, INoodleExtensionsGrid
    {
        internal ObjectPathAnimationPreparation PreparedPathAnimation;

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PosX);
            writer.Put(PosY);
            base.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            PosX = reader.GetInt();
            PosY = reader.GetInt();
            base.Deserialize(reader);
        }

        protected BaseGrid()
        {
        }

        protected BaseGrid(float time, int posX, int posY, int rotation, JSONNode customData = null) : base(
            time,
            customData)
        {
            PosX = posX;
            PosY = posY;
            Rotation = rotation;
            RecomputeSpawnParameters();
        }

        protected BaseGrid(
            float jsonTime,
            float songBpmTime,
            int posX,
            int posY,
            int rotation,
            JSONNode customData = null) :
            base(jsonTime, songBpmTime, customData)
        {
            PosX = posX;
            PosY = posY;
            Rotation = rotation;
            RecomputeSpawnParameters();
        }

        public int PosX { get; set; }
        public virtual int PosY { get; set; }

        public int Rotation { get; set; }

        // Half Jump Duration (SongBpmTime)
        public float HalfJumpDuration { get; private set; }
        internal event System.Action<BaseGrid, float, float> HalfJumpDurationChanged;

        // Half Jump Distance
        public float HalfJumpDistance { get; private set; }

        public virtual float SpawnSongBpmTime => SongBpmTime - HalfJumpDuration;
        public virtual float DespawnSongBpmTime => SongBpmTime + HalfJumpDuration;

        public virtual JSONNode CustomAnimation { get; set; }

        public virtual JSONNode CustomCoordinate { get; set; }

        public virtual JSONNode CustomWorldRotation { get; set; }

        public virtual JSONNode CustomLocalRotation { get; set; }

        // Enable on V3, disable on V2
        public virtual JSONNode CustomSpawnEffect { get; set; }

        public virtual JSONNode CustomNoteJumpMovementSpeed { get; set; }

        public virtual JSONNode CustomNoteJumpStartBeatOffset { get; set; }

        public virtual bool CustomFake { get; set; }

        public abstract string CustomKeyAnimation { get; }
        public abstract string CustomKeyCoordinate { get; }
        public abstract string CustomKeyWorldRotation { get; }
        public abstract string CustomKeyLocalRotation { get; }
        public abstract string CustomKeySpawnEffect { get; }
        public abstract string CustomKeyNoteJumpMovementSpeed { get; }
        public abstract string CustomKeyNoteJumpStartBeatOffset { get; }

        public Vector2 GetCenter() => GetPosition() + new Vector2(0f, BeatmapConstant.LaneSize / 2f);

        public Vector2 GetPosition() => DerivePositionFromData();

        public Vector3 GetScale() => Vector3.one;

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseGrid note)
            {
                PosX = note.PosX;
                PosY = note.PosY;
            }
        }

        public void RecomputeSpawnParameters()
        {
            // Unity singletons need explicit null checks before reading beatmap difficulty defaults.
            var songContainer = BeatSaberSongContainer.Instance;
            var mapDifficultyInfo = songContainer != null ? songContainer.MapDifficultyInfo : null;
            var info = songContainer != null ? songContainer.Info : null;
            var njs = CustomNoteJumpMovementSpeed?.AsFloat
                ?? mapDifficultyInfo?.NoteJumpSpeed ?? 0f;
            var offset = CustomNoteJumpStartBeatOffset?.AsFloat
                ?? mapDifficultyInfo?.NoteStartBeatOffset ?? 0f;
            var bpm = info?.BeatsPerMinute ?? 0f;

            var hjd = SpawnParameterHelper.CalculateHalfJumpDuration(njs, offset, bpm);
            var jd = SpawnParameterHelper.CalculateJumpDistance(njs, offset, bpm);
            SetSpawnParameters(hjd, jd / 2f);
        }

        public void SetSpawnParameters(float hjd, float jd)
        {
            var previous = HalfJumpDuration;
            HalfJumpDuration = hjd;
            HalfJumpDistance = jd;
            if (previous != hjd)
                HalfJumpDurationChanged?.Invoke(this, previous, hjd);
        }

        private Vector2 DerivePositionFromData()
        {
            var position = PosX - 1.5f;
            float layer = PosY;

            if (CustomCoordinate != null && CustomCoordinate.IsArray)
            {
                if (CustomCoordinate[0].IsNumber) position = CustomCoordinate[0] + 0.5f;
                if (CustomCoordinate[1].IsNumber) layer = CustomCoordinate[1];
                return new Vector2(position, layer) * BeatmapConstant.LaneSize;
            }

            if (PosX >= 1000)
                position = (PosX / 1000f) - 2.5f;
            else if (PosX <= -1000) position = (PosX / 1000f) - 0.5f;

            if (PosY >= 1000 || PosY <= -1000) layer = (PosY / 1000f) - 1f;

            return new Vector2(position, layer) * BeatmapConstant.LaneSize;
        }

        protected override void ParseCustom()
        {
            PreparedPathAnimation = null;
            base.ParseCustom();

            CustomAnimation = (CustomData?.HasKey(CustomKeyAnimation) ?? false)
                ? CustomData?[CustomKeyAnimation]
                : null;
            CustomCoordinate = (CustomData?.HasKey(CustomKeyCoordinate) ?? false)
                ? CustomData?[CustomKeyCoordinate]
                : null;
            CustomWorldRotation = (CustomData?.HasKey(CustomKeyWorldRotation) ?? false)
                ? CustomData?[CustomKeyWorldRotation]
                : null;
            CustomLocalRotation = (CustomData?.HasKey(CustomKeyLocalRotation) ?? false)
                ? CustomData?[CustomKeyLocalRotation]
                : null;
            CustomSpawnEffect = (CustomData?.HasKey(CustomKeySpawnEffect) ?? false)
                ? CustomData[CustomKeySpawnEffect]
                : null;
            CustomNoteJumpMovementSpeed = (CustomData?.HasKey(CustomKeyNoteJumpMovementSpeed) ?? false)
                ? CustomData?[CustomKeyNoteJumpMovementSpeed]
                : null;
            CustomNoteJumpStartBeatOffset = (CustomData?.HasKey(CustomKeyNoteJumpStartBeatOffset) ?? false)
                ? CustomData?[CustomKeyNoteJumpStartBeatOffset]
                : null;

            RecomputeSpawnParameters();
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            if (CustomAnimation != null)
                node[CustomKeyAnimation] = CustomAnimation;
            else
                node.Remove(CustomKeyAnimation);
            if (CustomCoordinate != null)
                node[CustomKeyCoordinate] = CustomCoordinate;
            else
                node.Remove(CustomKeyCoordinate);
            if (CustomWorldRotation != null)
                node[CustomKeyWorldRotation] = CustomWorldRotation;
            else
                node.Remove(CustomKeyWorldRotation);
            if (CustomLocalRotation != null)
                node[CustomKeyLocalRotation] = CustomLocalRotation;
            else
                node.Remove(CustomKeyLocalRotation);
            if (CustomSpawnEffect != null)
                node[CustomKeySpawnEffect] = CustomSpawnEffect;
            else
                node.Remove(CustomKeySpawnEffect);
            if (CustomNoteJumpMovementSpeed != null)
                node[CustomKeyNoteJumpMovementSpeed] = CustomNoteJumpMovementSpeed;
            else
                node.Remove(CustomKeyNoteJumpMovementSpeed);
            if (CustomNoteJumpStartBeatOffset != null)
                node[CustomKeyNoteJumpStartBeatOffset] = CustomNoteJumpStartBeatOffset;
            else
                node.Remove(CustomKeyNoteJumpStartBeatOffset);

            SetCustomData(node);
            return node;
        }
    }
}
