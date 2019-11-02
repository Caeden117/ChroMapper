using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A BeatmapAction contains a BeatmapObjectContainer as well as a methods to Undo and Redo the action.
/// 
/// An action can consist of placing and deleting, as well as more advanced options, like modifying via Node Editor,
/// or using Strobe Generator
/// </summary>
public abstract class BeatmapAction
{
    public bool Active = true;

    public List<BeatmapObjectContainer> containers = new List<BeatmapObjectContainer>();
    public List<BeatmapObject> data = new List<BeatmapObject>();

    public BeatmapAction (IEnumerable<BeatmapObjectContainer> containers){
        if (containers is null) return;
        foreach(BeatmapObjectContainer con in containers)
        {
            if (con is null) continue;
            this.containers.Add(con);
            data.Add(con.objectData);
        }
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