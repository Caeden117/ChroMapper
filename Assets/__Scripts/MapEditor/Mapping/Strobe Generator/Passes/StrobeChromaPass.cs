using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;

// TODO rename this to StrobeTransitionPass
public class StrobeChromaPass : StrobeGeneratorPass
{
    private readonly string easing;

    public StrobeChromaPass(string easing) => this.easing = easing;

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

            generatedObjects.Add(generated);
        }

        return generatedObjects;
    }
}
