﻿using Boo.Lang;
using SimpleJSON;
using System.Linq;
using UnityEngine;

public class PaintSelectedObjects : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;

    public void Paint()
    {
        List<BeatmapAction> allActions = new List<BeatmapAction>();
        foreach (BeatmapObject obj in SelectionController.SelectedObjects)
        {
            if (obj is BeatmapBPMChange || obj is BeatmapCustomEvent) continue; //These should probably not be colored.
            BeatmapObject beforePaint = BeatmapObject.GenerateCopy(obj);
            if (obj is MapEvent @event)
            {
                if (@event._value == MapEvent.LIGHT_VALUE_OFF) continue; //Ignore painting Off events
                if (@event._lightGradient != null)
                { 
                    //Modify start color if we are painting a Chroma 2.0 gradient
                    @event._lightGradient.StartColor = picker.CurrentColor;
                    continue;
                }
            }
            if (obj._customData == null || obj._customData.Count == 0 || obj._customData.Children.Count() == 0) //TODO: Look into making BeatmapObject._customData nullable
            {
                obj._customData = new JSONObject();
            }
            if (!obj._customData.HasKey("_color"))
            {
                obj._customData.Add("_color", picker.CurrentColor);
            }
            else
            {
                obj._customData["_color"] = picker.CurrentColor;
            }
            allActions.Add(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(obj), beforePaint));
        }
        foreach (BeatmapObject unique in SelectionController.SelectedObjects.DistinctBy(x => x.beatmapType))
        {
            BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType).RefreshPool(true);
        }
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, false, "Painted a selection of objects."));
    }
}
