using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

namespace Tests.Util
{
    public class PlaceUtils
    {
        public static void PlaceNote(
            NotePlacement obstaclePlacement, BaseNote note)
        {
            obstaclePlacement.queuedData = note;
            obstaclePlacement.RoundedJsonTime = obstaclePlacement.queuedData.JsonTime;
            obstaclePlacement.ApplyToMap();
        }

        public static void PlaceWall(
            ObstaclePlacement obstaclePlacement, BaseObstacle obstacle)
        {
            obstaclePlacement.queuedData = obstacle;
            obstaclePlacement.RoundedJsonTime = obstaclePlacement.queuedData.JsonTime;
            obstaclePlacement.instantiatedContainer.SetScale(new Vector3(0, 0,
                obstaclePlacement.queuedData.Duration * EditorScaleController.EditorScale));
            obstaclePlacement.ApplyToMap(); // Starts placement
            obstaclePlacement.ApplyToMap(); // Completes placement
        }

        public static void PlaceEvents(EventPlacement eventPlacement, IEnumerable<BaseEvent> events, bool precRotation = false)
        {
            foreach (var evt in events)
            {
                PlaceEvent(eventPlacement, evt, precRotation);
            }
        }

        public static void PlaceEvent(
            EventPlacement eventPlacement, BaseEvent evt, bool precRotation = false)
        {
            eventPlacement.queuedData = evt;
            eventPlacement.queuedValue = eventPlacement.queuedData.Value;
            eventPlacement.queuedFloatValue = eventPlacement.queuedData.FloatValue;
            eventPlacement.RoundedJsonTime = eventPlacement.queuedData.JsonTime;

            if (precRotation)
            {
                eventPlacement.PlacePrecisionRotation = true;
                eventPlacement.ApplyToMap();
                eventPlacement.PlacePrecisionRotation = false;
            }
            else
            {
                eventPlacement.ApplyToMap();
            }
        }

        public static void PlaceArc(
            ArcPlacement arcPlacement, BaseArc arc)
        {
            arcPlacement.queuedData = arc;
            arcPlacement.RoundedJsonTime = arcPlacement.queuedData.JsonTime;
            arcPlacement.ApplyToMap();
        }

        public static void PlaceChain(
            ChainPlacement chainPlacement, BaseChain chain)
        {
            chainPlacement.queuedData = chain;
            chainPlacement.RoundedJsonTime = chainPlacement.queuedData.JsonTime;
            chainPlacement.ApplyToMap();
        }
    }
}
