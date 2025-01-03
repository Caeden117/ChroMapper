using Beatmap.Base.Customs;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseGrid : BaseObject, IObjectBounds, INoodleExtensionsGrid
    {
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

        protected BaseGrid(float time, int posX, int posY, JSONNode customData = null) : base(time, customData)
        {
            PosX = posX;
            PosY = posY;
            RecomputeSongBpmTime();
        }

        protected BaseGrid(float jsonTime, float songBpmTime, int posX, int posY, JSONNode customData = null) :
            base(jsonTime, songBpmTime, customData)
        {
            PosX = posX;
            PosY = posY;
            RecomputeSongBpmTime();
        }

        public int PosX { get; set; }
        public virtual int PosY { get; set; }

        // Half Jump Duration (SongBpmTime)
        public float Hjd { get; private set; }

        // Half Jump Distance
        public float Jd { get; private set; }

        public float EditorScale { get; private set; }

        public virtual float SpawnSongBpmTime { get { return SongBpmTime - Hjd; } }
        public virtual float DespawnSongBpmTime { get { return SongBpmTime + Hjd; } }

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

        public Vector2 GetCenter() => GetPosition() + new Vector2(0f, 0.5f);

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

        public override void RecomputeSongBpmTime()
        {
            var njs = CustomNoteJumpMovementSpeed?.AsFloat
                ?? BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var offset = CustomNoteJumpStartBeatOffset?.AsFloat
                ?? BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;
            var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            Hjd = SpawnParameterHelper.CalculateHalfJumpDuration(njs, offset, bpm);
            // (5 / 3) * njs * (60 / bpm) = 100
            EditorScale = 100f * njs / bpm;
            Jd = Hjd * EditorScale;
            base.RecomputeSongBpmTime();
        }

        private Vector2 DerivePositionFromData()
        {
            var position = PosX - 1.5f;
            float layer = PosY;

            if (CustomCoordinate != null && CustomCoordinate.IsArray)
            {
                if (CustomCoordinate[0].IsNumber) position = CustomCoordinate[0] + 0.5f;
                if (CustomCoordinate[1].IsNumber) layer = CustomCoordinate[1];
                return new Vector2(position, layer);
            }

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

            CustomAnimation = (CustomData?.HasKey(CustomKeyAnimation) ?? false) ? CustomData?[CustomKeyAnimation] : null;
            CustomCoordinate = (CustomData?.HasKey(CustomKeyCoordinate) ?? false) ? CustomData?[CustomKeyCoordinate] : null;
            CustomWorldRotation = (CustomData?.HasKey(CustomKeyWorldRotation) ?? false) ? CustomData?[CustomKeyWorldRotation] : null;
            CustomLocalRotation = (CustomData?.HasKey(CustomKeyLocalRotation) ?? false) ? CustomData?[CustomKeyLocalRotation] : null;
            CustomSpawnEffect = (CustomData?.HasKey(CustomKeySpawnEffect) ?? false) ? CustomData[CustomKeySpawnEffect] : null;
            CustomNoteJumpMovementSpeed = (CustomData?.HasKey(CustomKeyNoteJumpMovementSpeed) ?? false) ? CustomData?[CustomKeyNoteJumpMovementSpeed] : null;
            CustomNoteJumpStartBeatOffset = (CustomData?.HasKey(CustomKeyNoteJumpStartBeatOffset) ?? false) ? CustomData?[CustomKeyNoteJumpStartBeatOffset] : null;
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            if (CustomAnimation != null) node[CustomKeyAnimation] = CustomAnimation; else node.Remove(CustomKeyAnimation);
            if (CustomCoordinate != null) node[CustomKeyCoordinate] = CustomCoordinate; else node.Remove(CustomKeyCoordinate);
            if (CustomWorldRotation != null) node[CustomKeyWorldRotation] = CustomWorldRotation; else node.Remove(CustomKeyWorldRotation);
            if (CustomLocalRotation != null) node[CustomKeyLocalRotation] = CustomLocalRotation; else node.Remove(CustomKeyLocalRotation);
            if (CustomSpawnEffect != null) node[CustomKeySpawnEffect] = CustomSpawnEffect; else node.Remove(CustomKeySpawnEffect);
            if (CustomNoteJumpMovementSpeed != null) node[CustomKeyNoteJumpMovementSpeed] = CustomNoteJumpMovementSpeed; else node.Remove(CustomKeyNoteJumpMovementSpeed);
            if (CustomNoteJumpStartBeatOffset != null) node[CustomKeyNoteJumpStartBeatOffset] = CustomNoteJumpStartBeatOffset; else node.Remove(CustomKeyNoteJumpStartBeatOffset); 
            
            SetCustomData(node);
            return node;
        }
    }
}
