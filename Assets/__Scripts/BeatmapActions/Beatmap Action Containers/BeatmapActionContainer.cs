using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatmapActionContainer : MonoBehaviour
{
    private List<BeatmapAction> beatmapActions = new List<BeatmapAction>();
    private static BeatmapActionContainer instance;
    [SerializeField] private GameObject moveableGridTransform;
    [SerializeField] private SelectionController selection;
    [SerializeField] private NodeEditorController nodeEditor;
    [SerializeField] private TracksManager tracksManager;

    private void Start()
    {
        instance = this;
    }

    /// <summary>
    /// Adds a BeatmapAction to the stack.
    /// </summary>
    /// <param name="action">BeatmapAction to add.</param>
    public static void AddAction(BeatmapAction action)
    {
        instance.beatmapActions.RemoveAll(x => !x.Active);
        instance.beatmapActions.Add(action);
        instance.beatmapActions = instance.beatmapActions.Distinct().ToList();
        Debug.Log($"Action of type {action.GetType().Name} added. ({action.Comment})");
    }

    public void Undo()
    {
        if (!beatmapActions.Any()) return;
        BeatmapAction lastActive = beatmapActions.LastOrDefault(x => x.Active);
        if (lastActive == null) return;
        Debug.Log($"Undid a {lastActive.GetType().Name}. ({lastActive.Comment})");
        BeatmapActionParams param = new BeatmapActionParams(this);
        lastActive.Undo(param);
        lastActive.Active = false;
    }

    public void Redo()
    {
        if (!beatmapActions.Any()) return;
        BeatmapAction firstNotActive = beatmapActions.FirstOrDefault(x => !x.Active);
        if (firstNotActive == null) return;
        Debug.Log($"Redid a {firstNotActive?.GetType()?.Name ?? "Unknown"}. ({firstNotActive.Comment ?? "No comment."})");
        BeatmapActionParams param = new BeatmapActionParams(this);
        firstNotActive?.Redo(param);
        firstNotActive.Active = true;
    }

    public class BeatmapActionParams
    {
        public SelectionController selection;
        public NodeEditorController nodeEditor;
        public TracksManager tracksManager;
        public BeatmapActionParams(BeatmapActionContainer container)
        {
            selection = container.selection;
            nodeEditor = container.nodeEditor;
            tracksManager = container.tracksManager;
        }
    }
}
