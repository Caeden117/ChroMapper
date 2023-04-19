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

        public override BaseObject ObjectData
        {
            get => ObstacleData;
            set => ObstacleData = (BaseObstacle)value;
        }

        public int ChunkEnd => (int)((ObstacleData.Time + ObstacleData.Duration) / Intersections.ChunkSize);

        public bool IsRotatedByNoodleExtensions => ObstacleData.CustomWorldRotation != null;

        public static ObstacleContainer SpawnObstacle(BaseObstacle data, TracksManager manager,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<ObstacleContainer>();
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
            transform.localScale = scale;

            MaterialPropertyBlock.SetVector(shaderScale, scale);
            UpdateMaterials();
        }

        public override void UpdateGridPosition()
        {
            var duration = ObstacleData.Duration;
            var localRotation = Vector3.zero;

            //Take half jump duration into account if the setting is enabled.
            if (ObstacleData.Duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls)
            {
                var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                var songNoteJumpSpeed = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
                var songStartBeatOffset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;

                var halfJumpDuration =
                    SpawnParameterHelper.CalculateHalfJumpDuration(songNoteJumpSpeed, songStartBeatOffset, bpm);

                duration -= duration * Mathf.Abs(duration / halfJumpDuration);
            }

            duration *= EditorScaleController
                .EditorScale; // Apply Editor Scale here since it can be overwritten by NE _scale Z

            if (ObstacleData.CustomSize != null && ObstacleData.CustomSize.IsArray && ObstacleData.CustomSize[2].IsNumber)
                duration = ObstacleData.CustomSize[2];

            if (ObstacleData.CustomLocalRotation != null)
                localRotation = ObstacleData.CustomLocalRotation.ReadVector3();
            if (ObstacleData.CustomWorldRotation != null)
                manager.CreateTrack((Vector3)ObstacleData.CustomWorldRotation).AttachContainer(this);

            var bounds = ObstacleData.GetShape();

            // Enforce positive scale, offset our obstacles to match.
            transform.localPosition = new Vector3(
                bounds.Position + (bounds.Width < 0 ? bounds.Width : 0),
                bounds.StartHeight + (bounds.Height < 0 ? bounds.Height : 0),
                (ObstacleData.Time * EditorScaleController.EditorScale) + (duration < 0 ? duration : 0)
            );

            SetScale(new Vector3(
                Mathf.Abs(bounds.Width),
                Mathf.Abs(bounds.Height),
                Mathf.Abs(duration)
            ));

            if (localRotation != Vector3.zero)
            {
                transform.localEulerAngles = Vector3.zero;
                var side = transform.right.normalized * (bounds.Width / 2);
                var rectWorldPos = transform.position + side;

                transform.RotateAround(rectWorldPos, transform.right, localRotation.x);
                transform.RotateAround(rectWorldPos, transform.up, localRotation.y);
                transform.RotateAround(rectWorldPos, transform.forward, localRotation.z);
            }

            UpdateCollisionGroups();
        }
    }
}
