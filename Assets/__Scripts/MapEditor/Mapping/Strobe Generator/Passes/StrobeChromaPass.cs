using System.Collections.Generic;
using System.Linq;
using Beatmap.Helper;
using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;
using UnityEngine;

public class StrobeChromaPass : StrobeGeneratorPass
{
    private readonly string easing;

    public StrobeChromaPass(string easing) => this.easing = easing;

    public override bool IsEventValidForPass(BaseEvent baseEvent) => baseEvent.CustomColor != null && !baseEvent.IsLightID;

    public override IEnumerable<BaseEvent> StrobePassForLane(IEnumerable<BaseEvent> original, int type,
        EventGridContainer.PropMode propMode, JSONNode propID)
    {
        var generatedObjects = new List<BaseEvent>();

        var nonGradients = original.Where(x => x.CustomLightGradient == null);
        for (var i = 0; i < nonGradients.Count() - 1; i++)
        {
            var currentChroma = nonGradients.ElementAt(i);
            var nextChroma = nonGradients.ElementAt(i + 1);

            var generated = BeatmapFactory.Clone(currentChroma);
            generated.CustomLightGradient = new ChromaLightGradient(
                (Color)currentChroma.CustomColor, //Start color
                (Color)nextChroma.CustomColor, //End color
                nextChroma.Time - currentChroma.Time, //Duration
                easing); //Duration

            // Don't forget to replace our Chroma color with a Light Gradient in _customData
            generated.CustomData.Add("_lightGradient", generated.CustomLightGradient.ToJson());
            generated.CustomData.Remove("_color");
            generated.CustomData.Remove("color");

            generatedObjects.Add(generated);
        }

        generatedObjects.Add(BeatmapFactory.Clone(original.Last()));

        return generatedObjects;
    }
}
