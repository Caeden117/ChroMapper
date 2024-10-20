﻿using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class StrobeGenerator : MonoBehaviour
{
    [FormerlySerializedAs("eventsContainer")][SerializeField] private EventGridContainer eventGridContainer;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private StrobeGeneratorUIDropdown ui;

    public void ToggleUI() => ui.ToggleDropdown(!StrobeGeneratorUIDropdown.IsActive);

    // TODO(Caeden): holy ***SHIT*** what was my ass COOKING!?!?!?!?
    //   this is NOT very cash money.
    public void GenerateStrobe(IEnumerable<StrobeGeneratorPass> passes)
    {
        var generatedObjects = new List<BaseObject>();
        var containers =
            SelectionController.SelectedObjects.Where(x => x is BaseEvent).Cast<BaseEvent>(); //Grab selected objects
        var oldEvents = new List<BaseObject>(); //For the Action
        //Order by type, then by descending time
        var groupings = containers.GroupBy(x => x.Type);
        foreach (var group in groupings)
        {
            var type = group.Key;

            // Try and do this in one pass so we don't tank performance
            var partitioned = group.GroupBy(y =>
                    y.CustomLightID != null ? EventGridContainer.PropMode.Light : EventGridContainer.PropMode.Off)
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
                    var lightIds = propGroup.FirstOrDefault()?.CustomLightID;
                    if (propGroup.Count() >= 2)
                    {
                        IEnumerable<BaseEvent> ordered = propGroup.OrderByDescending(x => x.JsonTime);
                        var end = ordered.First();
                        var start = ordered.Last();

                        var windowSpan = eventGridContainer.GetBetween(start.JsonTime, end.JsonTime);
                        var containersBetween = new List<BaseEvent>(windowSpan.Length);

                        for (var i = 0; i < windowSpan.Length; i++)
                        {
                            var oldEvent = windowSpan[i];

                            var isValid = oldEvent.Type == start.Type &&
                                ((start.CustomLightID is null && oldEvent.CustomLightID is null)
                                || (start.CustomLightID != null && oldEvent.CustomLightID != null && oldEvent.CustomLightID.Contains(start.CustomLightID[0])));

                            if (isValid)
                            {
                                containersBetween.Add(oldEvent);
                                oldEvents.Add(oldEvent);
                            }
                        }

                        containersBetween.TrimExcess();

                        foreach (var pass in passes)
                        {
                            var validEvents = containersBetween.FindAll(it => pass.IsEventValidForPass(it));

                            if (validEvents.Count() >= 2)
                            {
                                var strobePassGenerated = pass.StrobePassForLane(validEvents.OrderBy(x => x.JsonTime),
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

        generatedObjects.OrderBy(x => x.JsonTime);
        if (generatedObjects.Count > 0)
        {
            //Delete conflicting vanilla events
            foreach (BaseEvent e in oldEvents) eventGridContainer.DeleteObject(e, false, false, inCollectionOfDeletes: true);
            //Spawn objects that were generated
            foreach (BaseEvent data in generatedObjects)
            {
                data.WriteCustom();
                eventGridContainer.SpawnObject(data, false, false, true);
            };
            eventGridContainer.RefreshPool(true);
            eventGridContainer.LinkAllLightEvents();
            eventGridContainer.RefreshEventsAppearance(generatedObjects.Cast<BaseEvent>());
            //yield return PersistentUI.Instance.FadeOutLoadingScreen();
            SelectionController.DeselectAll();
            SelectionController.SelectedObjects = new HashSet<BaseObject>(generatedObjects);
            SelectionController.SelectionChangedEvent?.Invoke();
            SelectionController.RefreshSelectionMaterial(false);
            BeatmapActionContainer.AddAction(
                new StrobeGeneratorGenerationAction(generatedObjects.ToArray(), oldEvents.ToArray()));
        }
    }
}
