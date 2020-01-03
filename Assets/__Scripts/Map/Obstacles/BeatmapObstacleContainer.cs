using UnityEngine;

public class BeatmapObstacleContainer : BeatmapObjectContainer {

    private static readonly float MAPPINGEXTENSIONS_START_HEIGHT_MULTIPLIER = 1.35f;


    public override BeatmapObject objectData { get => obstacleData; set => obstacleData = (BeatmapObstacle)value; }

    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private AudioTimeSyncController atsc;

    public BeatmapObstacle obstacleData;

    public static BeatmapObstacleContainer SpawnObstacle(BeatmapObstacle data, AudioTimeSyncController atsc, ref GameObject prefab, ref ObstacleAppearanceSO appearanceSO)
    {
        BeatmapObstacleContainer container = Instantiate(prefab).GetComponent<BeatmapObstacleContainer>();
        container.obstacleData = data;
        container.obstacleAppearance = appearanceSO;
        container.atsc = atsc;
        appearanceSO.SetObstacleAppearance(container);
        return container;
    }

    public override void UpdateGridPosition()
    {
        float position = obstacleData._lineIndex - 2f; //ME 6+ lane
        if (obstacleData._lineIndex >= 1000)
            position = (((float)obstacleData._lineIndex - 1000) / 1000f) - 2f;
        else if (obstacleData._lineIndex <= -1000)
            position = (((float)obstacleData._lineIndex - 1000) / 1000f);

        //Kyle can go hyuck himself for this weird ME custom walls format (It aint even accurate on GitHub LULW)
        float startHeight = obstacleData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 0 : 1.5f;
        float height = obstacleData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 3.5f : 2;
        float width = obstacleData._width;

        if (obstacleData._width >= 1000) width = ((float)obstacleData._width - 1000) / 1000;
        if (obstacleData._type > 1 && obstacleData._type < 1000)
        {
            startHeight = obstacleData._type / (750 / 3.5f); //start height 750 == standard wall height
            height = 3.5f;
        }
        else if (obstacleData._type >= 1000 && obstacleData._type <= 4000)
        {
            startHeight = 0; //start height = floor
            height = ((float)obstacleData._type - 1000) / (1000 / 3.5f); //1000 = no height, 2000 = full height
        }else if (obstacleData._type > 4000)
        {
            float modifiedType = obstacleData._type - 4001;
            startHeight = ((modifiedType % 1000) / (1000 / 3.5f)) * MAPPINGEXTENSIONS_START_HEIGHT_MULTIPLIER;
            height = (modifiedType / 1000) / (1000 / 3.5f);
        }

        transform.localPosition = new Vector3(
            position,
            startHeight,
            obstacleData._time * EditorScaleController.EditorScale
            );
        transform.localScale = new Vector3(
            width,
            height,
            obstacleData._duration * EditorScaleController.EditorScale
            );
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
