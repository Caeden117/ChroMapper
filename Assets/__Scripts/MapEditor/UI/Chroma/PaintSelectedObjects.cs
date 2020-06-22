using SimpleJSON;
using System.Linq;
using UnityEngine;

public class PaintSelectedObjects : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;

    public void Paint()
    {
        foreach (BeatmapObject obj in SelectionController.SelectedObjects)
        {
            if (obj is BeatmapBPMChange || obj is BeatmapCustomEvent) continue; //These should probably not be colored.
            if (obj._customData.Children.Count() == 0) //TODO: Look into making BeatmapObject._customData nullable
            {
                obj._customData = new JSONObject();
            }
            obj._customData.Add("_color", picker.CurrentColor);
        }
        foreach (BeatmapObject unique in SelectionController.SelectedObjects.DistinctBy(x => x.beatmapType))
        {
            BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType).RefreshPool(true);
        }
    }
}
