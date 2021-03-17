using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StrobeLaserSpeedInterpolationPass : StrobeGeneratorPass
{
    private float interval; // Density of laser speed events
    private string easingID; // ID for easing function
    private int spinDirection; // 0 : Counter-Clockwise, 1 : Clockwise, 2 : Ignore
    private bool uniqueLaserDirectionPerGroup = false; // Whether or not Left/Right laser speeds will have different direction
    private bool lockLaserRotation = false; // Applies "_lockPosition" to events
    private int decimalPrecision; // Amount of decimals to round precise speed to.

    private bool overrideDirection = false;
    private bool leftRotatesClockwise = false;
    private bool rightRotatesClockwise = false;

    private System.Random random;
    private Func<float, float> easingFunc;

    public StrobeLaserSpeedInterpolationPass(float interval, string easingID, int spinDirection, bool uniqueLaserDirection, bool lockRotation, int decimalPrecision)
    {
        this.interval = interval;
        this.easingID = easingID;
        this.spinDirection = spinDirection;
        uniqueLaserDirectionPerGroup = uniqueLaserDirection;
        lockLaserRotation = lockRotation;
        this.decimalPrecision = decimalPrecision;
        easingFunc = Easing.named(easingID);

        random = new System.Random();
        overrideDirection = lockLaserRotation;

        // Do basic direction calculation here to save a headache
        if (spinDirection != 2)
        {
            leftRotatesClockwise = spinDirection == 0;
            rightRotatesClockwise = spinDirection == 0;
        }
        else
        {
            leftRotatesClockwise = random.Next() == 1;
            rightRotatesClockwise = random.Next() == 1;
        }

        if (uniqueLaserDirection)
        {
            rightRotatesClockwise = !leftRotatesClockwise;
        }
    }

    public override bool IsEventValidForPass(MapEvent @event) => @event.IsLaserSpeedEvent;

    public override IEnumerable<MapEvent> StrobePassForLane(IEnumerable<MapEvent> original, int type, EventsContainer.PropMode propMode, JSONNode propID)
    {
        List<MapEvent> generatedObjects = new List<MapEvent>();

        float startTime = original.First()._time;
        float endTime = original.Last()._time;

        float distanceInBeats = endTime - startTime;
        float originalDistance = distanceInBeats;
        MapEvent lastPassed = original.First();
        MapEvent nextEvent = original.ElementAt(1);

        float lastSpeed = GetLaserSpeedFromEvent(lastPassed);
        float nextSpeed = GetLaserSpeedFromEvent(nextEvent);

        while (distanceInBeats >= 0)
        {
            MapEvent any = original.Where(x => x._time <= endTime - distanceInBeats).LastOrDefault();
            if (lastPassed != any)
            {
                lastPassed = any;
                nextEvent = original.Where(x => x._time > lastPassed._time).FirstOrDefault();
                lastSpeed = GetLaserSpeedFromEvent(lastPassed);
                if (nextEvent == null) nextEvent = lastPassed;
                nextSpeed = GetLaserSpeedFromEvent(nextEvent);
            }
            float newTime = originalDistance - distanceInBeats + startTime;
            float progress = Mathf.InverseLerp(lastPassed._time, nextEvent._time, newTime);

            var decimalPreciseSpeed = Math.Round(Mathf.Lerp(lastSpeed, nextSpeed, easingFunc(progress)), decimalPrecision);
            MapEvent data = new MapEvent(newTime, type, 1);
            data._customData = new JSONObject();
            data._customData["_preciseSpeed"] = decimalPreciseSpeed;
            if (overrideDirection)
            {
                switch (type)
                {
                    case MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED:
                        data._customData["_direction"] = Convert.ToInt32(leftRotatesClockwise);
                        break;
                    case MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED:
                        data._customData["_direction"] = Convert.ToInt32(rightRotatesClockwise);
                        break;
                }
            }
            if (lockLaserRotation)
            {
                data._customData["_lockPosition"] = true;
            }
            generatedObjects.Add(data);
            distanceInBeats -= 1 / interval;
        }

        return generatedObjects;
    }

    private float GetLaserSpeedFromEvent(MapEvent @event)
    {
        if (@event._customData == null || @event._customData.Children.Count() == 0 || !@event._customData.HasKey("_preciseSpeed"))
        {
            return @event._value;
        }
        else
        {
            return @event._customData["_preciseSpeed"].AsFloat;
        }
    }
}
