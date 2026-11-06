using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Beatmap.Containers;
using ZLinq;

public class BeatmapActionContainer : MonoBehaviour, CMInput.IActionsActions
{
    public static event Action<BeatmapAction> OnActionCreated;
    public static event Action<BeatmapAction> OnActionUndo;
    public static event Action<BeatmapAction> OnActionRedo;

    // Action-heavy tests can disable undo/redo logging to avoid formatting thousands of log messages.
    public static bool ActionDiagnosticsEnabled { get; set; } = true;

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
        BeatmapAction mergedAction = null;
        if (!action.Networked)
        {
            // Clear all local, inactive actions from the queue
            // This essentially removes all actions that were undone prior to this action being added
            instance.beatmapActions.RemoveAll(x => !x.Networked && !x.Active);

            // Merge local actions
            // Networked actions shouldn't get merged since they're received in already-merged form
            var previousAction = instance.beatmapActions.LastOrDefault(x => !x.Networked);
            if (action is IMergeableAction mergeableAction
                && previousAction is IMergeableAction previousMergeableAction)
            {
                var merged = (BeatmapAction)mergeableAction.TryMerge(previousMergeableAction);
                if (merged is not null)
                {
                    instance.beatmapActions.Remove(previousAction);
                    mergedAction = merged;
                }
            }
        }

        instance.beatmapActions.Add(mergedAction ?? action);

        // Perform the original original action - not the merged action
        if (perform) instance.DoRedo(action);

        // Debug.Log($"Action of type {action.GetType().Name} added. ({action.Comment})");

        OnActionCreated?.Invoke(action);
    }

    public static void RemoveAllActionsOfType<T>() where T : BeatmapAction =>
        instance.beatmapActions.RemoveAll(x => x is T);

    public static void Undo(Guid actionGuid)
    {
        var action = instance.beatmapActions.Find(x => x.Guid == actionGuid);
        if (action == null) return;
        if (ActionDiagnosticsEnabled)
        {
            Debug.Log($"Undid a {action.GetType().Name}. ({action.Comment})");
        }
        instance.DoUndo(action);

        OnActionUndo?.Invoke(action);
    }

    public static void Redo(Guid actionGuid)
    {
        var action = instance.beatmapActions.Find(x => x.Guid == actionGuid);
        if (action == null) return;
        if (ActionDiagnosticsEnabled)
        {
            Debug.Log($"Redid a {action.GetType().Name}. ({action.Comment})");
        }
        instance.DoRedo(action);

        OnActionRedo?.Invoke(action);
    }

    public BeatmapAction Undo()
    {
        var lastActive = beatmapActions.AsValueEnumerable().LastOrDefault(x => !x.Networked && x.Active);
        if (lastActive == null) return null;
        if (ActionDiagnosticsEnabled)
        {
            Debug.Log($"Undid a {lastActive.GetType().Name}. ({lastActive.Comment})");
        }
        DoUndo(lastActive);
        OnActionUndo?.Invoke(lastActive);
        return lastActive;
    }

    public BeatmapAction Redo()
    {
        var firstNotActive = beatmapActions.AsValueEnumerable().FirstOrDefault(x => !x.Networked && !x.Active);
        if (firstNotActive == null) return null;
        if (ActionDiagnosticsEnabled)
        {
            Debug.Log($"Redid a {firstNotActive.GetType().Name}. ({firstNotActive.Comment})");
        }
        DoRedo(firstNotActive);
        OnActionRedo?.Invoke(firstNotActive);
        return firstNotActive;
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

                var lastLocatedActionIndex =
                    beatmapActions.FindLastIndex(it => it.Active && it.DoesInvolveObject(objectData) != null);

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
                    var dialogBox = PersistentUI
                        .Instance.CreateNewDialogBox()
                        .WithTitle("MultiMapping", "multi.trace");

                    dialogBox
                        .AddComponent<TextComponent>()
                        .WithInitialValue("MultiMapping", "multi.trace.first", firstLocatedAction.Identity.Name);

                    if (lastLocatedAction != firstLocatedAction && lastLocatedAction.Identity != null)
                    {
                        dialogBox
                            .AddComponent<TextComponent>()
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
