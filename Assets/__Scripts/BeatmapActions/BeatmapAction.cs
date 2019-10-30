using UnityEngine;

/// <summary>
/// A BeatmapAction contains a BeatmapObjectContainer as well as a methods to Undo and Redo the action.
/// 
/// An action can consist of placing and deleting, as well as more advanced options, like modifying via Node Editor,
/// or using Strobe Generator
/// </summary>
public abstract class BeatmapAction
{
    public bool Active = true;

    public BeatmapObject data;
    public BeatmapObjectContainer container;

    public BeatmapAction (BeatmapObjectContainer obj){
        if (obj == null) return;
        data = obj.objectData;
        container = obj;
    }

    /// <summary>
    /// Steps that should be taken to Undo an Action.
    /// </summary>
    /// <param name="param">Collection of useful stuff.</param>
    public abstract void Undo(BeatmapActionContainer.BeatmapActionParams param);

    /// <summary>
    /// Steps that should be taken to Redo an Action.
    /// </summary>
    /// <param name="param">Collection of useful stuff.</param>
    public abstract void Redo(BeatmapActionContainer.BeatmapActionParams param);
}