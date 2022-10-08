using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class PaintSelectedObjects : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;

    public void Paint()
    {
        var allActions = new List<BeatmapAction>();
        foreach (var obj in SelectionController.SelectedObjects)
        {
            if (obj is BaseBpmChange || obj is BaseCustomEvent) continue; //These should probably not be colored.
            var beforePaint = BeatmapFactory.Clone(obj);
            if (DoPaint(obj)) allActions.Add(new BeatmapObjectModifiedAction(obj, obj, beforePaint, "a", true));
        }

        if (allActions.Count == 0) return;

        foreach (var unique in SelectionController.SelectedObjects.DistinctBy(x => x.ObjectType))
            BeatmapObjectContainerCollection.GetCollectionForType(unique.ObjectType).RefreshPool(true);

        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true,
            "Painted a selection of objects."));
    }

    private bool DoPaint(BaseObject obj)
    {
        if (obj is BaseEvent @event)
        {
            if (@event.Value == (int)LightValue.Off) return false; //Ignore painting Off events
            if (@event.CustomLightGradient != null)
            {
                //Modify start color if we are painting a Chroma 2.0 gradient
                @event.CustomLightGradient.StartColor = picker.CurrentColor;
                return true;
            }
        }

        obj.GetOrCreateCustom()["_color"] = picker.CurrentColor;

        return true;
    }
}
