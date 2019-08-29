using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatmapActionContainer : MonoBehaviour
{
    private List<BeatmapAction> beatmapActions = new List<BeatmapAction>();

    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;

    /// <summary>
    /// Adds a BeatmapAction to the stack.
    /// </summary>
    /// <param name="action">BeatmapAction to add.</param>
    public void AddAction(BeatmapAction action)
    {
        beatmapActions.RemoveAll(x => !x.Active);
        beatmapActions.Add(action);
        Debug.Log($"Action of type {action.GetType().Name} added.");
    }

    public void Undo()
    {
        if (!beatmapActions.Any()) return;
        BeatmapAction lastActive = beatmapActions.Last(x => x.Active);
        Debug.Log($"Undid a {lastActive.GetType().Name}.");
        BeatmapActionParams param = new BeatmapActionParams(this);
        lastActive.Undo(param);
        lastActive.Active = false;
    }

    public void Redo()
    {
        if (!beatmapActions.Any()) return;
        BeatmapAction firstNotActive = beatmapActions.First(x => !x.Active);
        Debug.Log($"Redid a {firstNotActive.GetType().Name}.");
        BeatmapActionParams param = new BeatmapActionParams(this);
        firstNotActive.Redo(param);
        firstNotActive.Active = true;
    }

    public class BeatmapActionParams
    {
        public NotesContainer notes;
        public ObstaclesContainer obstacles;
        public EventsContainer events;
        public BPMChangesContainer bpm;

        public BeatmapActionParams(BeatmapActionContainer container)
        {
            notes = container.notes;
            obstacles = container.obstacles;
            events = container.events;
            bpm = container.bpm;
        }
    }
}
