using UnityEngine;

/// <summary>
/// A BeatmapAction contains a BeatmapObjectContainer as well as a methods to Undo and Redo the action.
/// 
/// An action can consist of placing and deleting, as well as more advanced options, like modifying via Node Editor,
/// or using Strobe Generator
/// </summary>
/// <typeparam name="T">Class the Action is made for. It must inherit "BeatmapObjectContainer"</typeparam>
public abstract class BeatmapAction
{
    public bool Active = true;

    public BeatmapObject data;
    public BeatmapObjectContainer container;

    public BeatmapAction (BeatmapObjectContainer obj){
        data = obj.objectData;
        container = obj;
    }

    /// <summary>
    /// Steps that should be taken to Undo an Action.
    /// </summary>
    /// <param name="objectContainerPrefab">Prefab of a Beatmap Container for undoing deletion actions.</param>
    /// <param name="others">Array of other stuff to be used in more advanced actions.</param>
    public abstract void Undo(BeatmapActionContainer.BeatmapActionParams param);

    /// <summary>
    /// Steps that should be taken to Redo an Action.
    /// </summary>
    /// <param name="objectContainerPrefab">Prefab of a Beatmap Container for redoing placement actions.</param>
    /// <param name="others">Array of other stuff to be used in more advanced actions.</param>
    public abstract void Redo(BeatmapActionContainer.BeatmapActionParams param);
}