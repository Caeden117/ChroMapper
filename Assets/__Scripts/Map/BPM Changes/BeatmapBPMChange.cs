﻿using SimpleJSON;
using System;

public class BeatmapBPMChange : BeatmapObject
{

    public BeatmapBPMChange(JSONNode node)
    {
        _time = RetrieveRequiredNode(node, "_time").AsFloat;
        _BPM = RetrieveRequiredNode(node, "_BPM").AsFloat;
        _beatsPerBar = 4;
        _metronomeOffset = 4;
    }

    public BeatmapBPMChange(float BPM, float time)
    {
        _BPM = BPM;
        _time = time;
        _beatsPerBar = 4;
        _metronomeOffset = 4;
    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_BPM"] = _BPM;
        node["_beatsPerBar"] = _beatsPerBar;
        node["_metronomeOffset"] = _metronomeOffset;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other) => true;

    public override Type beatmapType { get; set; } = Type.BPM_CHANGE;
    public float _BPM;
    public float _beatsPerBar;
    public float _metronomeOffset;
    /// <summary>
    /// Correctly rounded, modified BPM beat for this event. Internal use only.
    /// </summary>
    public int _Beat = 0;
}
