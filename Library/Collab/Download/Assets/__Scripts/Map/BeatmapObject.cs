using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BeatmapObject {

    public enum Type {
        NOTE,
        EVENT,
        OBSTACLE,
        BOMB,
        CUSTOM_NOTE,
        CUSTOM_EVENT
    }

    public abstract float _time { get; set; }
    public abstract Type beatmapType { get; set; }

}
