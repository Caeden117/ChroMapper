using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StrobeGenerator : MonoBehaviour {

    private static readonly float MaxDistanceBetweenEventsInBeats = 10;

    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private StrobeGeneratorUIDropdown ui;

    public void ToggleUI()
    {
        ui.ToggleDropdown(!StrobeGeneratorUIDropdown.IsActive);
    }

    public void GenerateStrobe(IEnumerable<StrobeGeneratorPass> passes)
    {
        List<BeatmapObject> generatedObjects = new List<BeatmapObject>();
        IEnumerable<MapEvent> containers = SelectionController.SelectedObjects.Where(x => x is MapEvent).Cast<MapEvent>(); //Grab selected objects
        List<BeatmapObject> oldEvents = new List<BeatmapObject>(); //For the Action
        //Order by type, then by descending time
        var groupings = containers.GroupBy(x => x._type);
        foreach (var group in groupings)
        {
            int type = group.Key;
            if (group.Count() >= 2)
            {
                IEnumerable<MapEvent> ordered = group.OrderByDescending(x => x._time);
                MapEvent end = ordered.First();
                MapEvent start = ordered.Last();

                IEnumerable<MapEvent> containersBetween = eventsContainer.UnsortedObjects.Where(x =>
                (x as MapEvent)._type == type && //Grab all events between start and end point.
                x._time >= start._time && x._time <= end._time
                ).Cast<MapEvent>();
                oldEvents.AddRange(containersBetween);

                foreach (StrobeGeneratorPass pass in passes)
                {
                    IEnumerable<MapEvent> validEvents = containersBetween.Where(x => pass.IsEventValidForPass(x));
                    if (validEvents.Count() >= 2)
                    {
                        List<MapEvent> strobePassGenerated = pass.StrobePassForLane(validEvents.OrderBy(x => x._time), type).ToList();
                        // REVIEW: Perhaps implement a "smart merge" to conflicting events, rather than outright removing those from previous passes
                        // Now, what would a "smart merge" entail? I have no clue.
                        generatedObjects.RemoveAll(x => strobePassGenerated.Any(y => y.IsConflictingWith(x)));
                        generatedObjects.AddRange(strobePassGenerated);
                    }
                }
            }
        }
        generatedObjects.OrderBy(x => x._time);
        //Delete conflicting vanilla events
        foreach (MapEvent e in oldEvents)
        {
            eventsContainer.DeleteObject(e, false, false);
        }
        //Spawn objects that were generated
        foreach (MapEvent data in generatedObjects)
        {
            eventsContainer.SpawnObject(data, false, false);
        }
        eventsContainer.RefreshPool(true);
        //yield return PersistentUI.Instance.FadeOutLoadingScreen();
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new HashSet<BeatmapObject>(generatedObjects);
        SelectionController.RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new StrobeGeneratorGenerationAction(generatedObjects.ToArray(), oldEvents.ToArray()));
    }
}
