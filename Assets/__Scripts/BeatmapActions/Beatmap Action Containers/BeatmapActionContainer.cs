using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Beatmap.Containers;

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
        // Clear all local, inactive actions from the queue
        // This essentially removes all actions that were undone prior to this action being added
        if (!action.Networked)
        {
            instance.beatmapActions.RemoveAll(x => !x.Networked && !x.Active);
        }

        var previousAction = instance.beatmapActions.LastOrDefault(x => !x.Networked);
        if (action is IMergeableAction mergeableAction && previousAction is IMergeableAction previousMergeableAction and not null)
        {
            var merged = (BeatmapAction)mergeableAction.TryMerge(previousMergeableAction);
            if (merged is not null)
            {
                instance.beatmapActions.Remove(previousAction);
                action = merged;
            }
        }

        instance.beatmapActions.Add(action);
        if (perform) instance.DoRedo(action);
        Debug.Log($"Action of type {action.GetType().Name} added. ({action.Comment})");

        // Deferring ActionCreatedEvent until after execution brings AddAction in line with Undo/Redo
        // TODO: May make more sense to refactor ActionCreatedEvent to add a Networked boolean parameter and invoke this event unconditionally
        if (!action.Networked)
        {
            ActionCreatedEvent?.Invoke(action);
        }
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

    public void OnUserTrace(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        var ray = Camera.main.ScreenPointToRay(KeybindsController.MousePosition);

        if (Intersections.Raycast(ray, 9, out var hit))
        {
            var container = hit.GameObject.GetComponentInParent<ObjectContainer>();

            if (container != null)
            {
                var objectData = container.ObjectData;

                var lastLocatedActionIndex = beatmapActions.FindLastIndex(it => it.Active && it.DoesInvolveObject(objectData) != null);

                if (lastLocatedActionIndex == -1) return;

                var lastLocatedAction = beatmapActions[lastLocatedActionIndex];
                var firstLocatedAction = lastLocatedAction;

                for (var i = lastLocatedActionIndex; i >= 0; i--)
                {
                    var involvedObj = beatmapActions[i].DoesInvolveObject(objectData);

                    if (involvedObj != null)
                    {
                        objectData = involvedObj;
                        firstLocatedAction = beatmapActions[i];
                    }
                }

                if (firstLocatedAction != null && firstLocatedAction.Identity != null)
                {
                    var dialogBox = PersistentUI.Instance.CreateNewDialogBox()
                        .WithTitle("MultiMapping", "multi.trace");

                    dialogBox.AddComponent<TextComponent>()
                        .WithInitialValue("MultiMapping", "multi.trace.first", firstLocatedAction.Identity.Name);

                    if (lastLocatedAction != firstLocatedAction && lastLocatedAction.Identity != null)
                    {
                        dialogBox.AddComponent<TextComponent>()
                            .WithInitialValue("MultiMapping", "multi.trace.last", lastLocatedAction.Identity.Name);
                    }

                    dialogBox.AddFooterButton(null, "PersistentUI", "ok");
                    dialogBox.Open();
                }
            }
        }
    }

    private int activeActionsAfterSave;
    public void UpdateActiveActionsAfterSave() => activeActionsAfterSave = beatmapActions.Count(x => x.Active);
    public bool ContainsUnsavedActions => activeActionsAfterSave != beatmapActions.Count(x => x.Active);

    public void ClearBeatmapActions()
    {
        activeActionsAfterSave = 0;
        beatmapActions.Clear();
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
