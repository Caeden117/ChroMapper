using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Shared;
using Beatmap.V2;
using Beatmap.V3;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] private TracksManager manager;
    [SerializeField] private NoteLanesController noteLanesController;

    [Space] [SerializeField] private Transform containerCollectionsContainer;

    private BaseDifficulty map;
    private int noteLaneSize = 2;
    private int noteLayerSize = 3;

    public void UpdateMapData(BaseDifficulty map)
    {
        if (map is V3Difficulty)
        {
            var copy = new V3Difficulty
            {
                CustomData = map.CustomData?.Clone(),
                Notes = new List<BaseNote>(map.Notes),
                Obstacles = new List<BaseObstacle>(map.Obstacles),
                Arcs = new List<BaseArc>(map.Arcs),
                Chains = new List<BaseChain>(map.Chains),
                Events = new List<BaseEvent>(map.Events),
                BpmChanges = new List<BaseBpmEvent>(map.BpmChanges),
                ColorBoostEvents = new List<BaseColorBoostEvent>(map.ColorBoostEvents),
                CustomEvents = new List<BaseCustomEvent>(map.CustomEvents)
            };
            this.map = copy;
        }
        else
        {
            var copy = new V2Difficulty
            {
                CustomData = map.CustomData?.Clone(),
                Notes = new List<BaseNote>(map.Notes),
                Obstacles = new List<BaseObstacle>(map.Obstacles),
                Events = new List<BaseEvent>(map.Events),
                BpmChanges = new List<BaseBpmEvent>(map.BpmChanges),
                CustomEvents = new List<BaseCustomEvent>(map.CustomEvents)
            };
            this.map = copy;
        }

    }

    public IEnumerator HardRefresh()
    {
        if (Settings.Instance.Load_Notes) yield return StartCoroutine(LoadObjects(map.Notes));
        if (Settings.Instance.Load_Obstacles) yield return StartCoroutine(LoadObjects(map.Obstacles));
        if (Settings.Instance.Load_Events) yield return StartCoroutine(LoadObjects(map.Events));
        if (Settings.Instance.Load_Others)
        {
            yield return StartCoroutine(LoadObjects(map.BpmChanges));
            yield return StartCoroutine(LoadObjects(map.CustomEvents));
        }
        if (Settings.Instance.Load_MapV3)
        {
            yield return StartCoroutine(LoadObjects(map.Arcs));
            yield return StartCoroutine(LoadObjects(map.Chains));
        }

        PersistentUI.Instance.LevelLoadSliderLabel.text = "Finishing up...";
        manager.RefreshTracks();
        SelectionController.RefreshMap();
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
    }

    public IEnumerator LoadObjects<T>(IEnumerable<T> objects) where T : BaseObject
    {
        if (!objects.Any()) yield break;
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(objects.First().ObjectType);
        if (collection == null) yield break;
        foreach (var obj in collection.LoadedObjects.ToArray()) collection.DeleteObject(obj, false, false);
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        collection.LoadedObjects = new SortedSet<BaseObject>(objects, new ObjectComparer());
        collection.UnsortedObjects = collection.LoadedObjects.ToList();
        UpdateSlider<T>();
        if (typeof(T) == typeof(BaseNote) || typeof(T) == typeof(BaseObstacle))
        {
            for (var i = 0; i < objects.Count(); i++)
            {
                BaseObject data = objects.ElementAt(i);
                if (data is BaseNote noteData)
                {
                    if (noteData.PosX >= 1000 || noteData.PosX <= -1000 || noteData.PosY >= 1000 ||
                        noteData.PosY <= -1000)
                    {
                        continue;
                    }

                    if (2 - noteData.PosX > noteLaneSize) noteLaneSize = 2 - noteData.PosX;
                    if (noteData.PosX - 1 > noteLaneSize) noteLaneSize = noteData.PosX - 1;
                    if (noteData.PosY + 1 > noteLayerSize) noteLayerSize = noteData.PosY + 1;
                }
                else if (data is BaseObstacle obstacleData)
                {
                    if (data is V3Obstacle) continue;
                    if (obstacleData.PosX >= 1000 || obstacleData.PosX <= -1000) continue;
                    if (2 - obstacleData.PosX > noteLaneSize) noteLaneSize = 2 - obstacleData.PosX;
                    if (obstacleData.PosX - 1 > noteLaneSize) noteLaneSize = obstacleData.PosX - 1;
                }
            }

            if (Settings.NonPersistentSettings.ContainsKey("NoteLanes"))
                Settings.NonPersistentSettings["NoteLanes"] = (noteLaneSize * 2).ToString();
            else
                Settings.NonPersistentSettings.Add("NoteLanes", (noteLaneSize * 2).ToString());
            noteLanesController.UpdateNoteLanes((noteLaneSize * 2).ToString());
        }

        if (typeof(T) == typeof(BaseEvent))
        {
            manager.RefreshTracks();
            var events = collection as EventGridContainer;
            events.AllRotationEvents = objects.Cast<BaseEvent>().Where(x => x.IsLaneRotationEvent()).ToList();
            events.AllBoostEvents = objects.Cast<BaseEvent>().Where(x => x.IsColorBoostEvent())
                .ToList();

            if (Settings.Instance.Load_MapV3)
            {
                events.LinkAllLightEvents();
            }
        }

        collection.RefreshPool(true);
    }

    private void UpdateSlider<T>() where T : BaseObject
    {
        PersistentUI.Instance.LevelLoadSliderLabel.text = $"Loading {typeof(T).Name}s... ";
        PersistentUI.Instance.LevelLoadSlider.value = 1;
    }
}
