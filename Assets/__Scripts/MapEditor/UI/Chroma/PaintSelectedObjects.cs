using System.Collections.Generic;
using UnityEngine;

public class PaintSelectedObjects : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;

    public void Paint()
    {
        var allActions = new List<BeatmapAction>();
        foreach (var obj in SelectionController.SelectedObjects)
        {
            if (obj is BeatmapBPMChange || obj is BeatmapCustomEvent) continue; //These should probably not be colored.
            var beforePaint = BeatmapObject.GenerateCopy(obj);
            if (DoPaint(obj)) allActions.Add(new BeatmapObjectModifiedAction(obj, obj, beforePaint, "a", true));
        }

        if (allActions.Count == 0) return;

        foreach (var unique in SelectionController.SelectedObjects.DistinctBy(x => x.BeatmapType))
            BeatmapObjectContainerCollection.GetCollectionForType(unique.BeatmapType).RefreshPool(true);

        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true,
            "Painted a selection of objects."));
    }

    private bool DoPaint(BeatmapObject obj)
    {
        if (obj is MapEvent @event)
        {
            if (@event.Value == MapEvent.LightValueOff) return false; //Ignore painting Off events
            if (@event.LightGradient != null)
            {
                //Modify start color if we are painting a Chroma 2.0 gradient
                @event.LightGradient.StartColor = picker.CurrentColor;
                return true;
            }
        }

        obj.GetOrCreateCustomData()[MapLoader.heckUnderscore + "color"] = picker.CurrentColor;

        return true;
    }
}
