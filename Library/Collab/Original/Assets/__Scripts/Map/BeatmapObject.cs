using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BeatmapObject {
    public abstract float _time { get; set; }
    public abstract Type beatmapType { get; set; }
    public enum Type { Note, Event, Obstacle, Bomb, Custom }
}
