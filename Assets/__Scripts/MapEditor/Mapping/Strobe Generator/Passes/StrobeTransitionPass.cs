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
        var generatedObjects = new List<BaseEvent>();

        generatedObjects.Add(BeatmapFactory.Clone(original.First()));

        var nonGradients = original.Where(x => x.CustomLightGradient == null);
        for (var i = 1; i < nonGradients.Count(); i++)
        {
            var generated = BeatmapFactory.Clone(nonGradients.ElementAt(i));
            if (generated.IsBlue)
            {
                generated.Value = (int)LightValue.BlueTransition;
            }
            else if (generated.IsRed)
            {
                generated.Value = (int)LightValue.RedTransition;
            }
            else if (generated.IsWhite)
            {
                generated.Value = (int)LightValue.WhiteTransition;
            }
            generated.CustomEasing = easing;
            generated.CustomLerpType = lerpType;

            generatedObjects.Add(generated);
        }

        return generatedObjects;
    }
}
