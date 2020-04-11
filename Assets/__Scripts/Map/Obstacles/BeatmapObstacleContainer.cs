using System;
using UnityEngine;

public class BeatmapObstacleContainer : BeatmapObjectContainer {

    private static readonly float MAPPINGEXTENSIONS_START_HEIGHT_MULTIPLIER = 1.35f;
    private static readonly float MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL = 1000 / 3.5f;

    public override BeatmapObject objectData { get => obstacleData; set => obstacleData = (BeatmapObstacle)value; }

    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TracksManager manager;

    public BeatmapObstacle obstacleData;

    public int ChunkEnd { get; private set; }

    public bool IsRotatedByNoodleExtensions => obstacleData._customData != null && obstacleData._customData?["_rotation"] != null;

    public static BeatmapObstacleContainer SpawnObstacle(BeatmapObstacle data, AudioTimeSyncController atsc, TracksManager manager, ref GameObject prefab, ref ObstacleAppearanceSO appearanceSO)
    {
        BeatmapObstacleContainer container = Instantiate(prefab).GetComponent<BeatmapObstacleContainer>();
        container.obstacleData = data;
        container.obstacleAppearance = appearanceSO;
        container.atsc = atsc;
        container.manager = manager;
        appearanceSO.SetObstacleAppearance(container);
        return container;
    }

    public override void UpdateGridPosition()
    {
        //Defining initial values here.
        float position = obstacleData._lineIndex - 2f; //Line index
        float startHeight = obstacleData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 0 : 1.5f;
        float height = obstacleData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 3.5f : 2;
        float width = obstacleData._width;
        float duration = obstacleData._duration;
        Vector3 localRotation = Vector3.zero;

        //Take half jump duration into account if the setting is enabled.
        if (obstacleData._duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls)
        {
            float num = 60f / BeatSaberSongContainer.Instance.song.beatsPerMinute;
            float halfJumpDuration = 4;
            float songNoteJumpSpeed = BeatSaberSongContainer.Instance.difficultyData.noteJumpMovementSpeed;
            float songStartBeatOffset = BeatSaberSongContainer.Instance.difficultyData.noteJumpStartBeatOffset;

            while (songNoteJumpSpeed * num * halfJumpDuration > 18)
                halfJumpDuration /= 2;

            halfJumpDuration += songStartBeatOffset;

            if (halfJumpDuration < 1) halfJumpDuration = 1;

            duration -= duration * Mathf.Abs(duration / halfJumpDuration);
        }

        //Just look at the difference in code complexity for Mapping Extensions support and Noodle Extensions support.
        //Hot damn.
        if (obstacleData._customData != null)
        {
            Vector2 wallPos = obstacleData._customData["_position"]?.ReadVector2() ?? Vector2.zero;
            Vector2 wallSize = obstacleData._customData["_scale"]?.ReadVector2() ?? Vector2.one;
            position = wallPos.x;
            startHeight = wallPos.y;
            width = wallSize.x;
            height = wallSize.y;
            if (obstacleData._customData["_localRotation"] != null)
            {
                localRotation = obstacleData._customData["_localRotation"]?.ReadVector3() ?? Vector3.zero;
            }
            if (obstacleData._customData["_rotation"] != null)
            {
                float? rotation = obstacleData._customData["_rotation"]?.AsInt;
                if (rotation is null)
                {
                    rotation = obstacleData._customData["_rotation"]?.AsFloat;
                }
                Track track = manager.CreateTrack(rotation ?? 0);
                track.AttachContainer(this);
            }
        }
        else
        {
            //Kyle can go hyuck himself for this weird ME custom walls format (It aint even accurate on GitHub LULW)
            if (obstacleData._lineIndex >= 1000)
                position = (((float)obstacleData._lineIndex - 1000) / 1000f) - 2f;
            else if (obstacleData._lineIndex <= -1000)
                position = ((float)obstacleData._lineIndex - 1000) / 1000f;

            if (obstacleData._width >= 1000) width = ((float)obstacleData._width - 1000) / 1000;
            if (obstacleData._type > 1 && obstacleData._type < 1000)
            {
                startHeight = obstacleData._type / (750 / 3.5f); //start height 750 == standard wall height
                height = 3.5f;
            }
            else if (obstacleData._type >= 1000 && obstacleData._type <= 4000)
            {
                startHeight = 0; //start height = floor
                height = ((float)obstacleData._type - 1000) / MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL; //1000 = no height, 2000 = full height
            }
            else if (obstacleData._type > 4000)
            {
                float modifiedType = obstacleData._type - 4001;
                startHeight = modifiedType % 1000 / MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL * MAPPINGEXTENSIONS_START_HEIGHT_MULTIPLIER;
                height = modifiedType / 1000 / MAPPINGEXTENSIONS_UNITS_TO_FULL_HEIGHT_WALL;
            }
        }

        transform.localPosition = new Vector3(
            position,
            startHeight,
            obstacleData._time * EditorScaleController.EditorScale
            );
        transform.localScale = new Vector3(
            width,
            height,
            duration * EditorScaleController.EditorScale
            );
        transform.localEulerAngles = localRotation;

        ChunkEnd = (int)Math.Round((objectData._time + obstacleData._duration) / (double)BeatmapObjectContainerCollection.ChunkSize,
                 MidpointRounding.AwayFromZero);
    }

    internal override void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(2) && !KeybindsController.ShiftHeld)
        {
            obstacleData._time += obstacleData._duration;
            obstacleData._duration *= -1;
            obstacleAppearance.SetObstacleAppearance(this);
            UpdateGridPosition();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            if (KeybindsController.AltHeld)
            {
                float measureSnapping = 1f / atsc.gridMeasureSnapping;
                obstacleData._duration += (Input.GetAxis("Mouse ScrollWheel") > 0 ? -measureSnapping : measureSnapping);
                UpdateGridPosition();
            }
        }
        else base.OnMouseOver();
    }
}
