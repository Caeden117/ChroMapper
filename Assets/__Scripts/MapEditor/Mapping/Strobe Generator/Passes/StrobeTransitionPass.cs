using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;

public class StrobeTransitionPass : StrobeGeneratorPass
{
    private const string DefaultEasing = "easeLinear";
    private const string DefaultLerpType = "RGB";

    private readonly string easing;
    private readonly string lerpType;

    public StrobeTransitionPass(string easing, string lerpType)
    {
        this.easing = (easing != DefaultEasing) ? easing : null;
        this.lerpType = (lerpType != DefaultLerpType) ? lerpType : null;
    }

    public override bool IsEventValidForPass(BaseEvent @event) => @event.IsLightEvent();

    public override IEnumerable<BaseEvent> StrobePassForLane(IEnumerable<BaseEvent> original, int type,
        EventGridContainer.PropMode propMode, int[] propID)
    {
        var generatedObjects = original.Select(BeatmapFactory.Clone).ToList();

        for (var i = 1; i < generatedObjects.Count; i++)
        {
            var previousEvent = generatedObjects[i - 1];
            previousEvent.CustomEasing = easing;
            previousEvent.CustomLerpType = lerpType;
            
            var currentEvent = generatedObjects[i];
            if (currentEvent.IsBlue)
            {
                currentEvent.Value = (int)LightValue.BlueTransition;
            }
            else if (currentEvent.IsRed)
            {
                currentEvent.Value = (int)LightValue.RedTransition;
            }
            else if (currentEvent.IsWhite)
            {
                currentEvent.Value = (int)LightValue.WhiteTransition;
            }
        }

        return generatedObjects;
    }
}
