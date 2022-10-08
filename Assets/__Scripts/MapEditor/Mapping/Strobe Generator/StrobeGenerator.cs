using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class StrobeGenerator : MonoBehaviour
{
    [FormerlySerializedAs("eventsContainer")] [SerializeField] private EventGridContainer eventGridContainer;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private StrobeGeneratorUIDropdown ui;

    public void ToggleUI() => ui.ToggleDropdown(!StrobeGeneratorUIDropdown.IsActive);

    public void GenerateStrobe(IEnumerable<StrobeGeneratorPass> passes)
    {
        var generatedObjects = new List<IObject>();
        var containers =
            SelectionController.SelectedObjects.Where(x => x is IEvent).Cast<IEvent>(); //Grab selected objects
        var oldEvents = new List<IObject>(); //For the Action
        //Order by type, then by descending time
        var groupings = containers.GroupBy(x => x.Type);
        foreach (var group in groupings)
        {
            var type = group.Key;

            // Try and do this in one pass so we don't tank performance
            var partitioned = group.GroupBy(y =>
                    y.IsLightID ? EventGridContainer.PropMode.Light : EventGridContainer.PropMode.Off)
                .ToDictionary(z => z.Key, modeGroup =>
                    modeGroup.Key == EventGridContainer.PropMode.Off
                        ? modeGroup.GroupBy(y => 1)
                        : modeGroup.GroupBy(y => y.CustomLightID[0]));

            foreach (var mode in partitioned)
            {
                var propMode = mode.Key;
                var propGroups = mode.Value;

                foreach (var propGroup in propGroups)
                {
                    var customData = propGroup.FirstOrDefault()?.CustomData;
                    var lightIds = customData?["_lightID"] ?? customData?["lightID"];
                    if (propGroup.Count() >= 2)
                    {
                        IEnumerable<IEvent> ordered = propGroup.OrderByDescending(x => x.Time);
                        var end = ordered.First();
                        var start = ordered.Last();

                        var containersBetween = eventGridContainer.LoadedObjects.GetViewBetween(start, end)
                            .Cast<IEvent>().Where(x =>
                                x.Type == start.Type && //Grab all events between start and end point.
                                ((!start.IsLightID && !x.IsLightID) || (start.IsLightID &&
                                                                        x.IsLightID && x.CustomLightID.Contains(start.CustomLightID[0])))
                            );
                        oldEvents.AddRange(containersBetween);

                        foreach (var pass in passes)
                        {
                            var validEvents = containersBetween.Where(x => pass.IsEventValidForPass(x));
                            if (validEvents.Count() >= 2)
                            {
                                var strobePassGenerated = pass.StrobePassForLane(validEvents.OrderBy(x => x.Time),
                                    type, propMode, lightIds).ToList();
                                // REVIEW: Perhaps implement a "smart merge" to conflicting events, rather than outright removing those from previous passes
                                // Now, what would a "smart merge" entail? I have no clue.
                                generatedObjects.RemoveAll(x => strobePassGenerated.Any(y => y.IsConflictingWith(x)));
                                generatedObjects.AddRange(strobePassGenerated);
                            }
                        }
                    }
                }
            }
        }

        generatedObjects.OrderBy(x => x.Time);
        if (generatedObjects.Count > 0)
        {
            //Delete conflicting vanilla events
            foreach (IEvent e in oldEvents) eventGridContainer.DeleteObject(e, false, false);
            //Spawn objects that were generated
            foreach (IEvent data in generatedObjects) eventGridContainer.SpawnObject(data, false, false);
            eventGridContainer.RefreshPool(true);
            //yield return PersistentUI.Instance.FadeOutLoadingScreen();
            SelectionController.DeselectAll();
            SelectionController.SelectedObjects = new HashSet<IObject>(generatedObjects);
            SelectionController.SelectionChangedEvent?.Invoke();
            SelectionController.RefreshSelectionMaterial(false);
            BeatmapActionContainer.AddAction(
                new StrobeGeneratorGenerationAction(generatedObjects.ToArray(), oldEvents.ToArray()));
        }
    }
}
