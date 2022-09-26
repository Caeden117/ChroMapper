using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapObstacleContainer : BeatmapObjectContainer
{
    private static readonly int colorTint = Shader.PropertyToID("_ColorTint");
    private static readonly int shaderScale = Shader.PropertyToID("_WorldScale");

    [SerializeField] private TracksManager manager;

    [FormerlySerializedAs("obstacleData")] public BeatmapObstacle ObstacleData;

    public override BeatmapObject ObjectData { get => ObstacleData; set => ObstacleData = (BeatmapObstacle)value; }

    public int ChunkEnd => (int)((ObstacleData.Time + ObstacleData.Duration) / Intersections.ChunkSize);

    public bool IsRotatedByNoodleExtensions =>
        ObstacleData.CustomData != null && (ObstacleData.CustomRotation != null);

    public static BeatmapObstacleContainer SpawnObstacle(BeatmapObstacle data, TracksManager manager,
        ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapObstacleContainer>();
        container.ObstacleData = data;
        container.manager = manager;
        return container;
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(colorTint, color);
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

            var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(songNoteJumpSpeed, songStartBeatOffset, bpm);

            duration -= duration * Mathf.Abs(duration / halfJumpDuration);
        }

        duration *= EditorScaleController
            .EditorScale; // Apply Editor Scale here since it can be overwritten by NE _scale Z

        if (ObstacleData.CustomData != null)
        {
            if (ObstacleData.CustomSize)
            {
                if (ObstacleData.CustomSize.Count > 2) //Apparently scale supports Z now, ok
                    duration = ObstacleData.CustomSize?.ReadVector3().z ?? duration;
            }

            if (ObstacleData.CustomLocalRotation)
                localRotation = ObstacleData.CustomLocalRotation?.ReadVector3() ?? Vector3.zero;
            if (ObstacleData.CustomRotation)
            {
                Track track = null;
                if (ObstacleData.CustomRotation.IsNumber)
                {
                    float rotation = ObstacleData.CustomRotation;
                    track = manager.CreateTrack(rotation);
                }
                else if (ObstacleData.CustomRotation.IsArray)
                {
                    track = manager.CreateTrack(ObstacleData.CustomRotation.ReadVector3());
                }

                if (track != null)
                {
                    track.AttachContainer(this);
                }
            }
        }

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
