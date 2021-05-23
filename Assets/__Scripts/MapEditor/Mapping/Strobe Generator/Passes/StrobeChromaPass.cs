using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

public class StrobeChromaPass : StrobeGeneratorPass
{
    private string easing;

    public StrobeChromaPass(string easing)
    {
        this.easing = easing;
    }

    public override bool IsEventValidForPass(MapEvent @event) => @event.IsChromaEvent && !@event.IsLightIdEvent;

    public override IEnumerable<MapEvent> StrobePassForLane(IEnumerable<MapEvent> original, int type, EventsContainer.PropMode propMode, JSONNode propID)
    {
        List<MapEvent> generatedObjects = new List<MapEvent>();

        IEnumerable<MapEvent> nonGradients = original.Where(x => x._lightGradient == null);
        for (int i = 0; i < nonGradients.Count() - 1; i++)
        {
            MapEvent currentChroma = nonGradients.ElementAt(i);
            MapEvent nextChroma = nonGradients.ElementAt(i + 1);

            MapEvent generated = BeatmapObject.GenerateCopy(currentChroma);
            generated._lightGradient = new MapEvent.ChromaGradient(
                currentChroma._customData["_color"], //Start color
                nextChroma._customData["_color"], //End color
                nextChroma._time - currentChroma._time, //Duration
                easing); //Duration

            // Don't forget to replace our Chroma color with a Light Gradient in _customData
            generated._customData.Add("_lightGradient", generated._lightGradient.ToJSONNode());
            generated._customData.Remove("_color");

            generatedObjects.Add(generated);
        }

        generatedObjects.Add(BeatmapObject.GenerateCopy(original.Last()));

        return generatedObjects;
    }
}
