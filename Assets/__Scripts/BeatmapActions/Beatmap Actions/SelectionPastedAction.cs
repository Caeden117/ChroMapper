using System.Collections.Generic;
using UnityEngine;

public class SelectionPastedAction : BeatmapAction
{
    private List<BeatmapObjectContainer> pastedObjects = new List<BeatmapObjectContainer>();
    private List<BeatmapObject> pastedObjectsData = new List<BeatmapObject>();
    private List<BeatmapObjectContainer> previouslySelected = new List<BeatmapObjectContainer>();

    public SelectionPastedAction(List<BeatmapObjectContainer> pasted, List<BeatmapObjectContainer> previouslySelected) : base(null)
    {
        pastedObjects = new List<BeatmapObjectContainer>(pasted);
        this.previouslySelected = previouslySelected;
        foreach (BeatmapObjectContainer container in pastedObjects)
            pastedObjectsData.Add(container.objectData);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        foreach (BeatmapObjectContainer obj in pastedObjects)
        {
            param.bpm.DeleteObject(obj);
            param.notes.DeleteObject(obj);
            param.events.DeleteObject(obj);
            param.obstacles.DeleteObject(obj);
        }
        SelectionController.SelectedObjects.Clear();
        SelectionController.SelectedObjects.AddRange(previouslySelected);
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        pastedObjects.Clear();
        SelectionController.DeselectAll();
        foreach (BeatmapObject obj in pastedObjectsData)
        {
            BeatmapObjectContainer recovered = null;
            switch (obj.beatmapType)
            {
                case BeatmapObject.Type.NOTE:
                    recovered = param.notes.SpawnObject(obj);
                    break;
                case BeatmapObject.Type.BOMB:
                    recovered = param.notes.SpawnObject(obj);
                    break;
                case BeatmapObject.Type.CUSTOM_NOTE:
                    recovered = param.notes.SpawnObject(obj);
                    break;
                case BeatmapObject.Type.OBSTACLE:
                    recovered = param.obstacles.SpawnObject(obj);
                    break;
                case BeatmapObject.Type.EVENT:
                    recovered = param.events.SpawnObject(obj);
                    break;
                case BeatmapObject.Type.CUSTOM_EVENT:
                    recovered = param.events.SpawnObject(obj);
                    break;
            }
            pastedObjects.Add(recovered);
        }
        SelectionController.SelectedObjects.Clear();
        SelectionController.SelectedObjects.AddRange(pastedObjects);
        SelectionController.RefreshSelectionMaterial(false);
    }
}
