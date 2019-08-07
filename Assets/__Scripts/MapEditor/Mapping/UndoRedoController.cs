using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[Obsolete("Undo and Redo are being redone by Kiwi and I later. Do not modify this class.")]
public class UndoRedoController : MonoBehaviour {

    [SerializeField] internal NotesContainer notes;
    [SerializeField] internal ObstaclesContainer obstacles;
    [SerializeField] internal EventsContainer events;
    [SerializeField] internal BPMChangesContainer bpm;
    
    private Dictionary<BeatmapObject, GameObject> objectToGO = new Dictionary<BeatmapObject, GameObject>(); 

    private static UndoRedoController instance;
    private static List<Snapshot> snapshots = new List<Snapshot>();
    private static int snapshotIndex = 1;
    public static int SnapshotsLimit { get; private set; }

	// Use this for initialization
	void Start () {
        if (instance != null) Destroy(this);
        instance = this;
        SnapshotsLimit = 50; //Temp, until Options menu
        snapshots.Clear();
	}
	
	public static void CreateSnapshot()
    {
        if (instance == null) return;
        Snapshot snapshot = new Snapshot(instance);
        snapshots.Add(snapshot);
        if (snapshots.Count > SnapshotsLimit) snapshots.RemoveAt(0);
        while(snapshotIndex <= snapshots.Count - 1)
            snapshots.RemoveAt(snapshots.Count - 1);
        snapshotIndex = snapshots.Count;
        Debug.Log("Snapshot created!");
    }

    /// <summary>
    /// Allows a container to be recoverable via Redo. DO NOT DESTROY WITH Destroy();
    /// </summary>
    public static void AddToRecover(BeatmapObjectContainer container)
    {
        instance.objectToGO.Add(container.objectData, container.gameObject);
        container.gameObject.SetActive(false);
    }

    public void Undo()
    {
        if (snapshotIndex <= 0) return;
        Snapshot current = new Snapshot(this);
        --snapshotIndex;
        Snapshot previous = snapshots[snapshotIndex];
        DestroyAndRestore(current.notes, ref previous.notes, ref notes.loadedNotes);
        DestroyAndRestore(current.obstacles, ref previous.obstacles, ref obstacles.loadedObstacles);
        DestroyAndRestore(current.events, ref previous.events, ref events.loadedEvents);
        DestroyAndRestore(current.bpmChanges, ref previous.bpmChanges, ref bpm.loadedBPMChanges);
        RefreshMap();
        Debug.Log("Undo!");
    }

    public void Redo()
    {
        if (snapshots.Count == 0 || snapshotIndex >= snapshots.Count) return;
        Snapshot current = new Snapshot(this);
        if (snapshotIndex < snapshots.Count) ++snapshotIndex;
        Snapshot next = snapshots[snapshotIndex - 1];
        DestroyAndRestore(current.notes, ref next.notes, ref notes.loadedNotes);
        DestroyAndRestore(current.obstacles, ref next.obstacles, ref obstacles.loadedObstacles);
        DestroyAndRestore(current.events, ref next.events, ref events.loadedEvents);
        DestroyAndRestore(current.bpmChanges, ref next.bpmChanges, ref bpm.loadedBPMChanges);
        RefreshMap();
        Debug.Log("Redo!");
    }

    /// <summary>
    /// Destroys objects that exist in "from" but not "to", and restores objects that exist in "to" but not "from"
    /// </summary>
    private void DestroyAndRestore<T>(List<T> from, ref List<T> to, ref List<T> ToModify) where T : BeatmapObjectContainer
    {
        List<T> ToDestroy = from.Except(to).ToList(); //Get objects in "from" that do not exist in "to", needs destruction
        List<T> ToRestore = to.Except(from).ToList(); //Get objects in "to" that do not exist in "from", needs restoration
        ToModify.RemoveAll((x) => ToDestroy.Contains(x));
        foreach (T obj in ToDestroy)
        {
            if (!objectToGO.ContainsKey(obj.objectData)) objectToGO.Add(obj.objectData, obj.gameObject);
            obj.gameObject.SetActive(false);
            Debug.Log("Destroying " + obj.objectData.beatmapType);
        }
        foreach(T obj in ToRestore) //I hope it's this easy.
        {
            if (!objectToGO.ContainsKey(obj.objectData)) continue;
            T instantiate = Instantiate(objectToGO[obj.objectData], objectToGO[obj.objectData].transform.parent).GetComponent<T>();
            instantiate.gameObject.SetActive(true);
            instantiate.UpdateGridPosition();
            ToModify.Add(instantiate);
            objectToGO.Remove(obj.objectData);
            to.Remove(obj);
            to.Add(instantiate);
            Debug.Log("Restoring " + obj.objectData.beatmapType);
        }
    }

    private void RefreshMap()
    {
        notes.SortNotes();
        obstacles.SortObstacles();
        events.SortEvents();
        if (BeatSaberSongContainer.Instance.map != null)
        {
            List<BeatmapNote> newNotes = new List<BeatmapNote>();
            foreach (BeatmapNoteContainer n in notes.loadedNotes) newNotes.Add(n.mapNoteData);
            List<BeatmapObstacle> newObstacles = new List<BeatmapObstacle>();
            foreach (BeatmapObstacleContainer o in obstacles.loadedObstacles) newObstacles.Add(o.obstacleData);
            List<MapEvent> newEvents = new List<MapEvent>();
            foreach (BeatmapEventContainer e in events.loadedEvents) newEvents.Add(e.eventData);
            BeatSaberSongContainer.Instance.map._notes = newNotes;
            BeatSaberSongContainer.Instance.map._obstacles = newObstacles;
            BeatSaberSongContainer.Instance.map._events = newEvents;
        }
    }

    private class Snapshot
    {
        internal List<BeatmapObjectContainer> notes;
        internal List<BeatmapObjectContainer> events;
        internal List<BeatmapObjectContainer> obstacles;
        internal List<BeatmapBPMChangeContainer> bpmChanges;

        public Snapshot(UndoRedoController controller)
        {
            notes = new List<BeatmapObjectContainer>(controller.notes.loadedNotes);
            events = new List<BeatmapObjectContainer>(controller.events.loadedEvents);
            obstacles = new List<BeatmapObjectContainer>(controller.obstacles.loadedObstacles);
            bpmChanges = new List<BeatmapBPMChangeContainer>(controller.bpm.loadedBPMChanges);
        }
    }
}
