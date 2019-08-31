using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private List<BeatmapObjectContainer> generatedObjects = new List<BeatmapObjectContainer>();
    private List<BeatmapObject> generatedData = new List<BeatmapObject>();

    public StrobeGeneratorGenerationAction(List<BeatmapObjectContainer> generated, List<BeatmapObjectContainer> notGenerated) : base(null)
    {
        generatedObjects = new List<BeatmapObjectContainer>(generated);
        generatedObjects.RemoveAll(x => notGenerated.Contains(x)); //Remove all objects that were here from the start.
        foreach (BeatmapObjectContainer obj in generated)
            generatedData.Add(obj.objectData);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in generatedObjects)
            param.events.DeleteObject(obj);
        generatedObjects.Clear();
        SelectionController.SelectedObjects.Clear();
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        generatedObjects.Clear();
        foreach (BeatmapObject obj in generatedData)
            generatedObjects.Add(param.events.SpawnObject(obj));
        SelectionController.SelectedObjects.Clear();
        SelectionController.SelectedObjects.AddRange(generatedObjects);
        SelectionController.RefreshSelectionMaterial(false);
    }
}
