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
    [Space]
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private EventAppearanceSO eventAppearance;

    private void Start()
    {
        BeatmapNoteContainer con = new BeatmapNoteContainer();
        BeatmapNotePlacementAction test = new BeatmapNotePlacementAction(con);
        AddAction(test);
    }

    /// <summary>
    /// Adds a BeatmapAction to the stack.
    /// </summary>
    /// <param name="action">BeatmapAction to add.</param>
    public void AddAction(BeatmapAction action)
    {
        beatmapActions.RemoveAll(x => !x.Active);
        beatmapActions.Add(action);
    }

    public void Undo()
    {
        BeatmapAction lastActive = beatmapActions.Last(x => x.Active);
    }

    public void Redo()
    {
        BeatmapAction firstNotActive = beatmapActions.First(x => !x.Active);
    }
}
