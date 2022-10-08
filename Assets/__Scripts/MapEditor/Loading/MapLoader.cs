using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
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

    private IDifficulty map;
    private int noteLaneSize = 2;
    private int noteLayerSize = 3;

    public void UpdateMapData(IDifficulty map)
    {
        if (map is V3Difficulty)
        {
            var copy = new V3Difficulty
            {
                CustomData = map.CustomData?.Clone(),
                Notes = new List<INote>(map.Notes),
                Obstacles = new List<IObstacle>(map.Obstacles),
                Arcs = new List<IArc>(map.Arcs),
                Chains = new List<IChain>(map.Chains),
                Events = new List<IEvent>(map.Events),
                BpmChanges = new List<IBpmChange>(map.BpmChanges),
                ColorBoostEvents = new List<IColorBoostEvent>(map.ColorBoostEvents),
                CustomEvents = new List<ICustomEvent>(map.CustomEvents)
            };
            this.map = copy;
        }
        else
        {
            var copy = new V2Difficulty
            {
                CustomData = map.CustomData?.Clone(),
                Notes = new List<INote>(map.Notes),
                Obstacles = new List<IObstacle>(map.Obstacles),
                Events = new List<IEvent>(map.Events),
                BpmChanges = new List<IBpmChange>(map.BpmChanges),
                CustomEvents = new List<ICustomEvent>(map.CustomEvents)
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

    public IEnumerator LoadObjects<T>(IEnumerable<T> objects) where T : IObject
    {
        if (!objects.Any()) yield break;
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(objects.First().ObjectType);
        if (collection == null) yield break;
        foreach (var obj in collection.LoadedObjects.ToArray()) collection.DeleteObject(obj, false, false);
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        collection.LoadedObjects = new SortedSet<IObject>(objects, new ObjectComparer());
        collection.UnsortedObjects = collection.LoadedObjects.ToList();
        UpdateSlider<T>();
        if (typeof(T) == typeof(INote) || typeof(T) == typeof(IObstacle))
        {
            for (var i = 0; i < objects.Count(); i++)
            {
                IObject data = objects.ElementAt(i);
                if (data is INote noteData)
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
                else if (data is IObstacle obstacleData)
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

        if (typeof(T) == typeof(IEvent))
        {
            manager.RefreshTracks();
            var events = collection as EventGridContainer;
            events.AllRotationEvents = objects.Cast<IEvent>().Where(x => x.IsLaneRotationEvent()).ToList();
            events.AllBoostEvents = objects.Cast<IEvent>().Where(x => x.Type == (int)EventTypeValue.ColorBoost)
                .ToList();

            if (Settings.Instance.Load_MapV3)
            {
                events.LinkAllLightEvents();
            }
        }

        collection.RefreshPool(true);
    }

    private void UpdateSlider<T>() where T : IObject
    {
        PersistentUI.Instance.LevelLoadSliderLabel.text = $"Loading {typeof(T).Name}s... ";
        PersistentUI.Instance.LevelLoadSlider.value = 1;
    }
}
