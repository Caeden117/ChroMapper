using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallPreview : MonoBehaviour {

    [SerializeField] GameObject WallPrefab;
    [SerializeField] AudioTimeSyncController atsc;
    [SerializeField] Transform obstaclesGrid;
    [SerializeField] ObstaclesContainer obstaclesContainer;

    public static bool IsActive = false;

    private GameObject hoverWall;
    private static BeatmapObstacleContainer container;
    private BeatSaberMap map;

    private bool IsExtending = false;
    private BeatmapObstacleContainer beatmapObstacle;
    private GameObject ExtendingGO;
    private int ExtendedWidth = 1;
    private float ExtendedLength = 0;
    private float OriginTime;
    private int OriginIndex;

	void Start () {
        map = BeatSaberSongContainer.Instance.map;
	}

    void Update()
    {
        if (PauseManager.IsPaused) return;
        if (hoverWall == null) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (atsc.IsPlaying) return;
            if (!IsExtending && hoverWall.activeInHierarchy)
            {
                Debug.Log("Placing wall...");
                ApplyToMap();
                hoverWall.SetActive(false);
                IsExtending = true;
            }
            else if (IsExtending)
            {
                Debug.Log("Finishing wall placement!");
                IsExtending = false;
                RefreshHovers();
                UndoRedoController.CreateSnapshot();
            }
        }
        if (Input.GetMouseButtonDown(1) && IsExtending) //Cancels wall placement.
        {
            Debug.Log("Cancelling wall placement!");
            IsExtending = false;
            obstaclesContainer.loadedObstacles.Remove(beatmapObstacle);
            Destroy(beatmapObstacle.gameObject);
            obstaclesContainer.SortObstacles();
            RefreshHovers();
        }
        if (IsExtending && beatmapObstacle != null && ExtendingGO != null)
        {
            beatmapObstacle.obstacleData._duration = atsc.CurrentBeat - OriginTime;
            ExtendingGO.transform.localScale = new Vector3(ExtendingGO.transform.localScale.x, ExtendingGO.transform.localScale.y,
                    (atsc.CurrentBeat - OriginTime));
        }
    }

    void OnMouseOver()
    {
        if (PauseManager.IsPaused) return;
        if (!NotePreviewController.Instance.PlacingWall)
        {
            if (hoverWall == null) return;
            if (hoverWall.activeInHierarchy) hoverWall.SetActive(false);
            IsActive = false;
            return;
        }
        if (hoverWall == null) RefreshHovers();
        if (atsc.IsPlaying) return;
        IsActive = true;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (IsExtending && beatmapObstacle != null && ExtendingGO != null)
            {
                beatmapObstacle.obstacleData._width = Mathf.CeilToInt(Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                                    Mathf.Ceil(GetComponent<MeshCollider>().bounds.min.x),
                                    Mathf.Floor(GetComponent<MeshCollider>().bounds.max.x)
                                ) + 2) - (OriginIndex);
                ExtendingGO.transform.localScale = new Vector3(beatmapObstacle.obstacleData._width, ExtendingGO.transform.localScale.y,
                    ExtendingGO.transform.localScale.z);
            }
            else if (!IsExtending)
            {
                hoverWall.SetActive(true);
                hoverWall.transform.position = new Vector3(
                    Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                        Mathf.Ceil(GetComponent<MeshCollider>().bounds.min.x),
                        Mathf.Floor(GetComponent<MeshCollider>().bounds.max.x)
                    ) - 1f,
                    hit.point.y <= 1.5f ? 0 : 1.5f,
                    0);
                hoverWall.transform.localScale = new Vector3(
                    hoverWall.transform.localScale.x,
                    hoverWall.transform.position.y == 0 ? 3.5f : 2, 0);
                container.obstacleData._lineIndex = Mathf.RoundToInt(hoverWall.transform.position.x + 2);
                container.obstacleData._type = Mathf.FloorToInt(hoverWall.transform.position.y);
            }
        }
        if (Input.GetKeyDown(KeyCode.Delete)) DeleteHoveringObstacle();
    }

    void OnMouseExit()
    {
        IsActive = false;
        if (hoverWall != null) hoverWall.SetActive(false);
    }

    void DeleteHoveringObstacle()
    {
        BeatmapObjectContainer conflicting = obstaclesContainer.loadedObstacles.Where(
            (BeatmapObjectContainer x) => 
            (x.objectData as BeatmapObstacle)._lineIndex <= container.obstacleData._lineIndex && //If it's between the left side
            (x.objectData as BeatmapObstacle)._lineIndex + ((x.objectData as BeatmapObstacle)._width - 1) >= container.obstacleData._lineIndex && //...and the right
            (x.objectData as BeatmapObstacle)._duration + x.objectData._time >= atsc.CurrentBeat && //If it's between the end point in time
            x.objectData._time <= atsc.CurrentBeat && //...and the beginning point in time
            (x.objectData as BeatmapObstacle)._type == container.obstacleData._type //And, for good measure, if they match types.
            ).FirstOrDefault();
        if (conflicting == null) return;
        UndoRedoController.CreateSnapshot();
        obstaclesContainer.loadedObstacles.Remove(conflicting);
        obstaclesContainer.SortObstacles();
        UndoRedoController.AddToRecover(conflicting);
        List<BeatmapObstacle> newObstacles = new List<BeatmapObstacle>();
        foreach (BeatmapObstacleContainer con in obstaclesContainer.loadedObstacles) newObstacles.Add(con.obstacleData);
        BeatSaberSongContainer.Instance.map._obstacles = newObstacles;
    }

    void RefreshHovers()
    {
        if (hoverWall != null) Destroy(hoverWall);
        hoverWall = Instantiate(WallPrefab);
        hoverWall.name = "Hover Wall";
        container = hoverWall.GetComponent<BeatmapObstacleContainer>();
    }

    void ApplyToMap()
    {
        OriginTime = atsc.CurrentBeat;
        OriginIndex = container.obstacleData._lineIndex;
        container.obstacleData._time = OriginTime;
        container.obstacleData._width = 1;
        container.obstacleData._duration = 1;
        beatmapObstacle = BeatmapObstacleContainer.SpawnObstacle(container.obstacleData, ref WallPrefab);
        ExtendingGO = beatmapObstacle.gameObject;
        beatmapObstacle.transform.SetParent(obstaclesGrid);
        beatmapObstacle.UpdateGridPosition();
        obstaclesContainer.loadedObstacles.Add(beatmapObstacle);
        obstaclesContainer.SortObstacles();
        List<BeatmapObstacle> newObstacles = new List<BeatmapObstacle>();
        foreach (BeatmapObstacleContainer con in obstaclesContainer.loadedObstacles) newObstacles.Add(con.obstacleData);
        BeatSaberSongContainer.Instance.map._obstacles = newObstacles;
    }
}
