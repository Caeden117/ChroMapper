using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

public class StrobeChromaPass : StrobeGeneratorPass
{
    private readonly string easing;

    public StrobeChromaPass(string easing) => this.easing = easing;

    public override bool IsEventValidForPass(MapEvent @event) => @event.IsChromaEvent && !@event.IsLightIdEvent;

    public override IEnumerable<MapEvent> StrobePassForLane(IEnumerable<MapEvent> original, int type,
        EventsContainer.PropMode propMode, JSONNode propID)
    {
        var generatedObjects = new List<MapEvent>();

        var nonGradients = original.Where(x => x.LightGradient == null);
        for (var i = 0; i < nonGradients.Count() - 1; i++)
        {
            var currentChroma = nonGradients.ElementAt(i);
            var nextChroma = nonGradients.ElementAt(i + 1);

            var generated = BeatmapObject.GenerateCopy(currentChroma);
            generated.LightGradient = new MapEvent.ChromaGradient(
                currentChroma.CustomColor, //Start color
                nextChroma.CustomColor, //End color
                nextChroma.Time - currentChroma.Time, //Duration
                easing); //Duration

            // Don't forget to replace our Chroma color with a Light Gradient in _customData
            generated.CustomData.Add("_lightGradient", generated.LightGradient.ToJsonNode());
            generated.CustomData.Remove("_color");
            generated.CustomData.Remove("color");

            generatedObjects.Add(generated);
        }

        generatedObjects.Add(BeatmapObject.GenerateCopy(original.Last()));

        return generatedObjects;
    }
}
