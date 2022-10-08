using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using Random = System.Random;

public class StrobeLaserSpeedInterpolationPass : StrobeGeneratorPass
{
    private readonly int decimalPrecision; // Amount of decimals to round precise speed to.
    private readonly Func<float, float> easingFunc;
    private readonly float interval; // Density of laser speed events
    private readonly bool leftRotatesClockwise;
    private readonly bool lockLaserRotation; // Applies "_lockPosition" to events

    private readonly bool overrideDirection;

    private readonly Random random;
    private readonly bool rightRotatesClockwise;

    public StrobeLaserSpeedInterpolationPass(float interval, string easingID, int spinDirection,
        bool uniqueLaserDirection, bool lockRotation, int decimalPrecision)
    {
        this.interval = interval;
        lockLaserRotation = lockRotation;
        this.decimalPrecision = decimalPrecision;
        easingFunc = Easing.Named(easingID);

        random = new Random();
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

        if (uniqueLaserDirection) rightRotatesClockwise = !leftRotatesClockwise;
    }

    public override bool IsEventValidForPass(BaseEvent baseEvent) => baseEvent.IsLaserRotationEvent(EnvironmentInfoHelper.GetName());

    public override IEnumerable<BaseEvent> StrobePassForLane(IEnumerable<BaseEvent> original, int type,
        EventGridContainer.PropMode propMode, JSONNode propID)
    {
        var generatedObjects = new List<BaseEvent>();

        var startTime = original.First().Time;
        var endTime = original.Last().Time;

        var distanceInBeats = endTime - startTime;
        var originalDistance = distanceInBeats;
        var lastPassed = original.First();
        var nextEvent = original.ElementAt(1);

        var lastSpeed = GetLaserSpeedFromEvent(lastPassed);
        var nextSpeed = GetLaserSpeedFromEvent(nextEvent);

        while (distanceInBeats >= 0)
        {
            var any = original.Where(x => x.Time <= endTime - distanceInBeats).LastOrDefault();
            if (lastPassed != any)
            {
                lastPassed = any;
                nextEvent = original.Where(x => x.Time > lastPassed.Time).FirstOrDefault();
                lastSpeed = GetLaserSpeedFromEvent(lastPassed);
                if (nextEvent == null) nextEvent = lastPassed;
                nextSpeed = GetLaserSpeedFromEvent(nextEvent);
            }

            var newTime = originalDistance - distanceInBeats + startTime;
            var progress = Mathf.InverseLerp(lastPassed.Time, nextEvent.Time, newTime);

            var decimalPreciseSpeed =
                Math.Round(Mathf.Lerp(lastSpeed, nextSpeed, easingFunc(progress)), decimalPrecision);
            // This does not support negative numbers, however I do not believe there is a reason to support them in the first place
            var roundedPreciseSpeed = (int)Math.Max(1, Math.Round(decimalPreciseSpeed, MidpointRounding.AwayFromZero));

            var data = new V3BasicEvent(newTime, type, 1) { CustomData = new JSONObject(), Value = roundedPreciseSpeed };

            // Bit cheeky but hopefully a bit more readable
            if (Math.Abs(decimalPreciseSpeed - roundedPreciseSpeed) > 0.01f)
                data.CustomPreciseSpeed = decimalPreciseSpeed as float?;

            if (overrideDirection)
            {
                switch (type)
                {
                    case (int)EventTypeValue.LeftLaserRotation:
                        data.CustomDirection = Convert.ToInt32(leftRotatesClockwise);
                        break;
                    case (int)EventTypeValue.RightLaserRotation:
                        data.CustomDirection = Convert.ToInt32(rightRotatesClockwise);
                        break;
                }
            }

            if (lockLaserRotation) data.CustomLockRotation = true;

            generatedObjects.Add(data);
            distanceInBeats -= 1 / interval;
        }

        return generatedObjects;
    }

    private float GetLaserSpeedFromEvent(BaseEvent baseEvent)
    {
        if (baseEvent.CustomPreciseSpeed == null && baseEvent.CustomSpeed == null)
        {
            return baseEvent.Value;
        }

        return (float)(baseEvent.CustomPreciseSpeed != null
            ? baseEvent.CustomPreciseSpeed
            : baseEvent.CustomSpeed);
    }
}
