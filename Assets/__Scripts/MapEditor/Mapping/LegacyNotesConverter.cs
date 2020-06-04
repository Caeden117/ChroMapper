using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LegacyNotesConverter : MonoBehaviour {

    public void ConvertFrom()
    {
       StartCoroutine(ConvertFromLegacy()); 
    }

    public void ConvertTo()
    {
        StartCoroutine(ConvertToLegacy());
    }

    private IEnumerator ConvertFromLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        //TODO convert legacy Chroma events to Chroma 2.0 events
        var events = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT) as EventsContainer;
        Dictionary<int, Color?> chromaColorsByEventType = new Dictionary<int, Color?>();
        foreach (var obj in events.UnsortedObjects)
        {
            MapEvent e = obj as MapEvent;
            if (chromaColorsByEventType.TryGetValue(e._type, out Color? chroma))
            {
                if (e._value >= ColourManager.RGB_INT_OFFSET)
                {
                    chromaColorsByEventType[e._type] = ColourManager.ColourFromInt(e._value);
                    events.DeleteObject(e, false, false);
                    continue;
                }
                else if (e._value == ColourManager.RGB_RESET)
                {
                    chromaColorsByEventType[e._type] = null;
                    events.DeleteObject(e, false, false);
                    continue;
                }
                if (chroma != null && e._value != MapEvent.LIGHT_VALUE_OFF)
                {
                    if (e._customData == null) e._customData = new JSONObject();
                    e._customData["_color"] = chroma;
                }
            }
            else
            {
                chromaColorsByEventType.Add(e._type, null);
            }
        }
        /*IEnumerable<IGrouping<int, MapEvent>> groupedByType = events.LoadedObjects.Cast<MapEvent>().GroupBy(x => x._type);
        foreach (var grouping in groupedByType)
        {
            Color? currentChromaColor = null;
            foreach (MapEvent e in grouping)
            {
                if (e._value >= ColourManager.RGB_INT_OFFSET)
                {
                    currentChromaColor = ColourManager.ColourFromInt(e._value);
                    events.DeleteObject(e, false);
                    continue;
                }
                else if (e._value == ColourManager.RGB_RESET)
                {
                    currentChromaColor = null;
                    events.DeleteObject(e, false);
                    continue;
                }
                if (currentChromaColor != null && e._value != MapEvent.LIGHT_VALUE_OFF)
                {
                    if (e._customData == null) e._customData = new JSONObject();
                    e._customData["_color"] = currentChromaColor;
                }
            }
        }*/
        events.RefreshPool(true);
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

    private IEnumerator ConvertToLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
