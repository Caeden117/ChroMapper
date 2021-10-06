﻿using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapObstacleContainer : BeatmapObjectContainer
{
    private static readonly int colorTint = Shader.PropertyToID("_ColorTint");

    [SerializeField] private TracksManager manager;
    [SerializeField] private GameObject outlineGameObject;

    [FormerlySerializedAs("obstacleData")] public BeatmapObstacle ObstacleData;

    public override BeatmapObject ObjectData { get => ObstacleData; set => ObstacleData = (BeatmapObstacle)value; }

    public int ChunkEnd => (int)((ObstacleData.Time + ObstacleData.Duration) / Intersections.ChunkSize);

    public bool IsRotatedByNoodleExtensions =>
        ObstacleData.CustomData != null && (ObstacleData.CustomData?.HasKey("_rotation") ?? false);

    public static BeatmapObstacleContainer SpawnObstacle(BeatmapObstacle data, TracksManager manager,
        ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapObstacleContainer>();
        container.ObstacleData = data;
        container.manager = manager;
        return container;
    }

    public override void Setup()
    {
        base.Setup();
        MaterialPropertyBlock.SetFloat(handleScale, 1);
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(colorTint, color);
        UpdateMaterials();
    }

    public void SetObstacleOutlineVisibility(bool visible) => outlineGameObject.SetActive(visible);

    public override void UpdateGridPosition()
    {
        var duration = ObstacleData.Duration;
        var localRotation = Vector3.zero;

        //Take half jump duration into account if the setting is enabled.
        if (ObstacleData.Duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls)
        {
            var num = 60f / BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            float halfJumpDuration = 4;
            var songNoteJumpSpeed = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;
            var songStartBeatOffset = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpStartBeatOffset;

            while (songNoteJumpSpeed * num * halfJumpDuration > 18)
                halfJumpDuration /= 2;

            halfJumpDuration += songStartBeatOffset;

            if (halfJumpDuration < 0.25f) halfJumpDuration = 0.25f;

            duration -= duration * Mathf.Abs(duration / halfJumpDuration);
        }

        duration *= EditorScaleController
            .EditorScale; // Apply Editor Scale here since it can be overwritten by NE _scale Z

        if (ObstacleData.CustomData != null)
        {
            if (ObstacleData.CustomData.HasKey("_scale"))
            {
                if (ObstacleData.CustomData["_scale"].Count > 2) //Apparently scale supports Z now, ok
                    duration = ObstacleData.CustomData["_scale"]?.ReadVector3().z ?? duration;
            }

            if (ObstacleData.CustomData.HasKey("_localRotation"))
                localRotation = ObstacleData.CustomData["_localRotation"]?.ReadVector3() ?? Vector3.zero;
            if (ObstacleData.CustomData.HasKey("_rotation"))
            {
                Track track = null;
                if (ObstacleData.CustomData["_rotation"].IsNumber)
                {
                    float rotation = ObstacleData.CustomData["_rotation"];
                    track = manager.CreateTrack(rotation);
                }
                else if (ObstacleData.CustomData["_rotation"].IsArray)
                {
                    track = manager.CreateTrack(ObstacleData.CustomData["_rotation"].ReadVector3());
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
        transform.localScale = new Vector3(
            Mathf.Abs(bounds.Width),
            Mathf.Abs(bounds.Height),
            Mathf.Abs(duration)
        );

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
