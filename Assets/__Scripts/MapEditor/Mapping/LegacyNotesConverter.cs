using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

//TODO rename to LegacyEventsConverter
public class LegacyNotesConverter : MonoBehaviour
{
    public void ConvertFrom() => StartCoroutine(ConvertFromLegacy());

    public void ConvertTo() => StartCoroutine(ConvertToLegacy());

    private IEnumerator ConvertFromLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();

        var events = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
        var chromaColorsByEventType = new Dictionary<int, Color?>();
        foreach (var obj in events.UnsortedObjects.ToArray())
        {
            var e = obj as BaseEvent;
            if (chromaColorsByEventType.TryGetValue(e.Type, out var chroma))
            {
                if (e.Value >= ColourManager.RgbintOffset)
                {
                    chromaColorsByEventType[e.Type] = ColourManager.ColourFromInt(e.Value);
                    events.DeleteObject(e, false, false);
                    continue;
                }

                if (e.Value == ColourManager.RGBReset)
                {
                    chromaColorsByEventType[e.Type] = null;
                    events.DeleteObject(e, false, false);
                    continue;
                }

                if (chroma != null && e.Value != (int)LightValue.Off)
                    e.CustomColor = chroma;
            }
            else
            {
                chromaColorsByEventType.Add(e.Type, null);
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
