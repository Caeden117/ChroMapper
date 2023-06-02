using Beatmap.Base;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ObstacleContainer : ObjectContainer
    {
        private static readonly int colorTint = Shader.PropertyToID("_ColorTint");
        private static readonly int shaderScale = Shader.PropertyToID("_WorldScale");

        [SerializeField] private TracksManager manager;

        [SerializeField] public BaseObstacle ObstacleData;

        private BPMChangeGridContainer bpmChangeGridContainer;

        public override BaseObject ObjectData
        {
            get => ObstacleData;
            set => ObstacleData = (BaseObstacle)value;
        }

        public int ChunkEnd => (int)((ObstacleData.JsonTime + ObstacleData.Duration) / Intersections.ChunkSize);

        public bool IsRotatedByNoodleExtensions => ObstacleData.CustomWorldRotation != null;

        public static ObstacleContainer SpawnObstacle(BaseObstacle data, TracksManager manager,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<ObstacleContainer>();
            container.bpmChangeGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(Enums.ObjectType.BpmChange);
            container.ObstacleData = data;
            container.manager = manager;
            return container;
        }

        public void SetColor(Color c)
        {
            MaterialPropertyBlock.SetColor(colorTint, c);
            UpdateMaterials();
        }

        public void SetScale(Vector3 scale)
        {
            Animator.LocalTarget.localScale = scale;

            MaterialPropertyBlock.SetVector(shaderScale, scale);
            UpdateMaterials();
        }

        public Vector3 GetScale()
        {
            return Animator.LocalTarget.localScale;
        }

        public float GetLength()
        {
            if (ObstacleData.CustomSize != null && ObstacleData.CustomSize.IsArray && ObstacleData.CustomSize[2].IsNumber)
                return ObstacleData.CustomSize[2];

            var obstacleStart = ObstacleData.SongBpmTime;
            var obstacleEnd = bpmChangeGridContainer?.JsonTimeToSongBpmTime(ObstacleData.JsonTime + ObstacleData.Duration) ?? 0;
            var length = obstacleEnd - obstacleStart;

            //Take half jump duration into account if the setting is enabled.
            if (ObstacleData.Duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls)
            {
                var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                var songNoteJumpSpeed = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
                var songStartBeatOffset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;

                var halfJumpDuration =
                    SpawnParameterHelper.CalculateHalfJumpDuration(songNoteJumpSpeed, songStartBeatOffset, bpm);

                length -= length * Mathf.Abs(length / halfJumpDuration);
            }

            length *= EditorScaleController
                .EditorScale; // Apply Editor Scale here since it can be overwritten by NE _scale Z

            return length;
        }

        public (Vector3, Vector3) ReadSizePosition()
        {
            var length = GetLength();

            var bounds = ObstacleData.GetShape();

            return (
                new Vector3(
                    Mathf.Abs(bounds.Width),
                    Mathf.Abs(bounds.Height),
                    Mathf.Abs(length)
                ),
                new Vector3(
                    bounds.Position + (bounds.Width / 2.0f),
                    bounds.StartHeight + (bounds.Height < 0 ? bounds.Height : 0) + 1f,
                    0
                )
            );
        }

        public override void UpdateGridPosition()
        {
            var localRotation = Vector3.zero;
            var length = GetLength();
            (var size, var position) = ReadSizePosition();

            if (ObstacleData.CustomLocalRotation != null)
                localRotation = ObstacleData.CustomLocalRotation.ReadVector3();
            if (ObstacleData.CustomWorldRotation != null && !Animator.AnimatedTrack)
            {
                if (ObstacleData.CustomWorldRotation.IsNumber)
                    manager.CreateTrack(new Vector3(0, ObstacleData.CustomWorldRotation, 0)).AttachContainer(this);
                else
                    manager.CreateTrack(ObstacleData.CustomWorldRotation.ReadVector3()).AttachContainer(this);
            }

            // Enforce positive scale, offset our obstacles to match.
            transform.localPosition = new Vector3(0, 0, (ObstacleData.SongBpmTime * EditorScaleController.EditorScale) + (length < 0 ? length : 0));
            Animator.LocalTarget.localPosition = position;

            SetScale(size);

            if (localRotation != Vector3.zero)
            {
                Animator.LocalTarget.localEulerAngles = localRotation;
            }

            UpdateCollisionGroups();
        }
    }
}
