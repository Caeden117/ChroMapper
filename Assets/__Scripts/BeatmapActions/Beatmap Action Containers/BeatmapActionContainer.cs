﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapActionContainer : MonoBehaviour, CMInput.IActionsActions
{
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
        instance.beatmapActions.RemoveAll(x => !x.Active);
        instance.beatmapActions.Add(action);
        if (perform) instance.DoRedo(action);
        Debug.Log($"Action of type {action.GetType().Name} added. ({action.Comment})");
    }

    public static void RemoveAllActionsOfType<T>() where T : BeatmapAction =>
        instance.beatmapActions.RemoveAll(x => x is T);

    //Idk what these do but I started getting warnings about them since updating to Visual Studio 2019 v16.6
    public static BeatmapAction GetLastAction() =>
        instance.beatmapActions.Any() ? instance.beatmapActions.LastOrDefault(x => x.Active) : null;

    public void Undo()
    {
        if (!beatmapActions.Any(x => x.Active)) return;
        var lastActive = beatmapActions.LastOrDefault(x => x.Active);
        if (lastActive == null) return;
        Debug.Log($"Undid a {lastActive?.GetType()?.Name ?? "UNKNOWN"}. ({lastActive?.Comment ?? "Unknown comment."})");
        var param = new BeatmapActionParams(this);
        lastActive.Undo(param);
        lastActive.Active = false;
        nodeEditor.ObjectWasSelected();
    }

    public void Redo()
    {
        if (!beatmapActions.Any(x => !x.Active)) return;
        var firstNotActive = beatmapActions.Find(x => !x.Active);
        if (firstNotActive == null) return;
        Debug.Log(
            $"Redid a {firstNotActive?.GetType()?.Name ?? "UNKNOWN"}. ({firstNotActive?.Comment ?? "Unknown comment."})");
        DoRedo(firstNotActive);
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
