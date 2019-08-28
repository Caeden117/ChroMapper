using UnityEngine;

/// <summary>
/// A BeatmapAction contains a BeatmapObjectContainer as well as a methods to Undo and Redo the action.
/// 
/// An action can consist of placing and deleting, as well as more advanced options, like modifying via Node Editor,
/// or using Strobe Generator
/// </summary>
/// <typeparam name="T">Class the Action is made for. It must inherit "BeatmapObjectContainer"</typeparam>
public abstract class BeatmapAction<T> where T : BeatmapObjectContainer
{
    public bool Active;

    public BeatmapObject data;
    public T container;

    public BeatmapAction (T obj){
        data = obj.objectData;
        container = obj;
    }

    public abstract void Undo(ref GameObject objectContainerPrefab);

    public abstract void Redo(ref GameObject objectContainerPrefab);
}