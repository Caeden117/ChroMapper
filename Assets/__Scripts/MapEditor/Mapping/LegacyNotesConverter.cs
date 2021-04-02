using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//TODO rename to LegacyEventsConverter
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

        var events = BeatmapObjectContainerCollection.GetCollectionForType<EventsContainer>(BeatmapObject.Type.EVENT);
        Dictionary<int, Color?> chromaColorsByEventType = new Dictionary<int, Color?>();
        foreach (var obj in events.UnsortedObjects.ToArray())
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
                    e.GetOrCreateCustomData()["_color"] = chroma;
                }
            }
            else
            {
                chromaColorsByEventType.Add(e._type, null);
            }
        }
        events.RefreshPool(true);

        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

    /*
     * I've ignored the ability to convert from Chroma 2.0 back to 1.0 since I do not see any use for doing that,
     * other than for perhaps Quest users stuck using the ChromaLite mod.
     * 
     * If given enough demand, or perhaps a PR, I'll add it.
     */
    private IEnumerator ConvertToLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
