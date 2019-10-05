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
        generatedObjects.RemoveAll(x => notGenerated.Contains(x) || x is null);
        foreach (BeatmapObjectContainer obj in generated)
            generatedData.Add(BeatmapObject.GenerateCopy(obj.objectData));
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in generatedObjects) param.events.DeleteObject(obj);
        generatedObjects.Clear();
        SelectionController.SelectedObjects.Clear();
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        generatedObjects.Clear();
        foreach (BeatmapObject obj in generatedData)
        {
            if (obj == null) continue;
            generatedObjects.Add(param.events.SpawnObject(data, out _));
        }
        SelectionController.SelectedObjects.Clear();
        SelectionController.SelectedObjects.AddRange(generatedObjects);
        SelectionController.RefreshSelectionMaterial(false);
    }
}
