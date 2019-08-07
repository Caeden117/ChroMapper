using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapBPMChange : BeatmapObject {

    public BeatmapBPMChange(JSONNode node)
    {
        _time = node["_time"].AsFloat;
        _BPM = node["_BPM"].AsFloat;
        _beatsPerBar = 4;
        _metronomeOffset = 4;
        beatmapType = Type.BPM_CHANGE;
    }

    public BeatmapBPMChange(float BPM, float time)
    {
        _BPM = BPM;
        _time = time;
        _beatsPerBar = 4;
        _metronomeOffset = 4;
        beatmapType = Type.BPM_CHANGE;
    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = _time;
        node["_BPM"] = _BPM;
        node["_beatsPerBar"] = _beatsPerBar;
        node["_metronomeOffset"] = _metronomeOffset;
        return node;
    }

    public override float _time { get; set; }
    public override Type beatmapType { get; set; }
    public override JSONNode _customData { get; set; }
    public float _BPM;
    public float _beatsPerBar;
    public float _metronomeOffset;
}
