using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapActionContainer : MonoBehaviour, CMInput.IActionsActions
{
    public static event Action<BeatmapAction> ActionCreatedEvent;
    public static event Action<BeatmapAction> ActionUndoEvent;
    public static event Action<BeatmapAction> ActionRedoEvent;

    private static BeatmapActionContainer instance;
    [SerializeField] private GameObject moveableGridTransform;
    [SerializeField] private SelectionController selection;
    [SerializeField] private NodeEditorController nodeEditor;
    [SerializeField] private TracksManager tracksManager;

    private readonly List<BeatmapAction> beatmapActions = new List<BeatmapAction>();

    private void Start() => instance = this;

    public void OnUndoMethod1(InputAction.CallbackContext context) => OnUndo(context);
    public void OnUndoMethod2(InputAction.CallbackContext context) => OnUndo(context);
    public void OnRedoMethod1(InputAction.CallbackContext context) => OnRedo(context);
    public void OnRedoMethod2(InputAction.CallbackContext context) => OnRedo(context);

    /// <summary>
    ///     Adds a BeatmapAction to the stack.
    /// </summary>
    /// <param name="action">BeatmapAction to add.</param>
    /// <param name="perform">
    ///     If true Redo will be triggered immediately. This means you don't need separate logic to perform
    ///     the action the first time.
    /// </param>
    public static void AddAction(BeatmapAction action, bool perform = false)
    {
        if (!action.Networked)
        {
            instance.beatmapActions.RemoveAll(x => !x.Networked && !x.Active);
            ActionCreatedEvent?.Invoke(action);
        }
        instance.beatmapActions.Add(action);
        if (perform) instance.DoRedo(action);
        Debug.Log($"Action of type {action.GetType().Name} added. ({action.Comment})");
    }

    public static void RemoveAllActionsOfType<T>() where T : BeatmapAction =>
        instance.beatmapActions.RemoveAll(x => x is T);

    public static void Undo(Guid actionGuid)
    {
        var action = instance.beatmapActions.Find(x => x.Guid == actionGuid);
        if (action == null) return;
        Debug.Log($"Undid a {action.GetType().Name}. ({action.Comment})");
        instance.DoUndo(action);
    }

    public static void Redo(Guid actionGuid)
    {
        var action = instance.beatmapActions.Find(x => x.Guid == actionGuid);
        if (action == null) return;
        Debug.Log($"Redid a {action.GetType().Name}. ({action.Comment})");
        instance.DoRedo(action);
    }

    public void Undo()
    {
        var lastActive = beatmapActions.FindLast(x => !x.Networked && x.Active);
        if (lastActive == null) return;
        Debug.Log($"Undid a {lastActive.GetType().Name}. ({lastActive.Comment})");
        DoUndo(lastActive);
        ActionUndoEvent?.Invoke(lastActive);
    }

    public void Redo()
    {
        var firstNotActive = beatmapActions.Find(x => !x.Networked && !x.Active);
        if (firstNotActive == null) return;
        Debug.Log($"Redid a {firstNotActive.GetType().Name}. ({firstNotActive.Comment})");
        DoRedo(firstNotActive);
        ActionRedoEvent?.Invoke(firstNotActive);
    }

    private void DoUndo(BeatmapAction action)
    {
        var param = new BeatmapActionParams(this);
        action.Undo(param);
        action.Active = false;
        nodeEditor.ObjectWasSelected();
    }

    private void DoRedo(BeatmapAction action)
    {
        var param = new BeatmapActionParams(this);
        action.Redo(param);
        action.Active = true;
        nodeEditor.ObjectWasSelected();
    }

    public void OnUndo(InputAction.CallbackContext context)
    {
        if (context.performed) Undo();
    }

    public void OnRedo(InputAction.CallbackContext context)
    {
        if (context.performed) Redo();
    }

    public class BeatmapActionParams
    {
        public NodeEditorController NodeEditor;
        public SelectionController Selection;
        public TracksManager TracksManager;

        public BeatmapActionParams(BeatmapActionContainer container)
        {
            Selection = container.selection;
            NodeEditor = container.nodeEditor;
            TracksManager = container.tracksManager;
        }
    }
}
