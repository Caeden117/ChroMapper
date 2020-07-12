using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectObjectOnGrid : MonoBehaviour {

    void Start()
    {
        SelectionController.ObjectWasSelectedEvent += ObjectSelected;
    }

    void ObjectSelected(BeatmapObject container)
    {
        if (!KeybindsController.CtrlHeld || Settings.Instance.BoxSelect) return; //Only do mass selection on ctrl
        if (SelectionController.SelectedObjects.Count() < 2 && !KeybindsController.AltHeld) return;
        IEnumerable<MapEvent> events = SelectionController.SelectedObjects.Where(x => x is MapEvent).Cast<MapEvent>();
        IEnumerable<BeatmapNote> notes = SelectionController.SelectedObjects.Where(x => x is BeatmapNote).Cast<BeatmapNote>();
        if (events.Any())
        {
            events = events.OrderBy(x => x._type).ThenByDescending(x => x._time);
            for (var i = 0; i < 15; i++)
            {
                IEnumerable<MapEvent> eventsAtType = events.Where(x => x._type == i);
                if (eventsAtType.Count() >= 2)
                {
                    IEnumerable<BeatmapObject> inBetween = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT)
                        .LoadedObjects.Where(x =>
                        x._time >= eventsAtType.Last()._time &&
                        x._time <= eventsAtType.First()._time &&
                        (x as MapEvent)._type == i);
                    foreach (BeatmapObject con in inBetween) SelectionController.Select(con, true, false, false);
                }
            }
        }
        if (notes.Any())
        {
            notes = notes.OrderBy(x => x._lineIndex).ThenByDescending(x => x._time);
            for (var i = notes.First()._lineIndex; i <= notes.Last()._lineIndex; i++)
            {
                IEnumerable<BeatmapNote> notesAtIndex = notes.Where(x => x._lineIndex == i);
                if (notesAtIndex.Count() >= 2)
                {
                    IEnumerable<BeatmapObject> inBetween = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE)
                        .LoadedObjects.Where(x =>
                        x._time >= notesAtIndex.Last()._time &&
                        x._time <= notesAtIndex.First()._time &&
                        ((BeatmapNote)x)._lineIndex == i);
                    foreach (BeatmapObject con in inBetween) SelectionController.Select(con, true, false, false);
                }
            }
        }
        SelectionController.RefreshSelectionMaterial();
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= ObjectSelected;
    }
}
