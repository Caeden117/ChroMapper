using System;
using SimpleJSON;

public class BeatmapBPMChange : BeatmapObject
{
    /// <summary>
    ///     Correctly rounded, modified BPM beat for this event. Internal use only.
    /// </summary>
    public int Beat = 0;

    public float BeatsPerBar;
    public float Bpm;
    public float MetronomeOffset;

    public BeatmapBPMChange(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "_time").AsFloat;
        Bpm = RetrieveRequiredNode(node, "_BPM").AsFloat;
        BeatsPerBar = RetrieveRequiredNode(node, "_beatsPerBar").AsFloat;
        MetronomeOffset = RetrieveRequiredNode(node, "_metronomeOffset").AsFloat;
    }

    public BeatmapBPMChange(float bpm, float time)
    {
        Bpm = bpm;
        Time = time;
        BeatsPerBar = 4;
        MetronomeOffset = 4;
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.BpmChange;

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(Time, DecimalPrecision);
        node["_BPM"] = Bpm;
        node["_beatsPerBar"] = BeatsPerBar;
        node["_metronomeOffset"] = MetronomeOffset;
        if (CustomData != null) node["_customData"] = CustomData;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion) => true;

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapBPMChange bpm)
        {
            Bpm = bpm.Bpm;
            BeatsPerBar = bpm.BeatsPerBar;
            MetronomeOffset = bpm.MetronomeOffset;
        }
    }
}
