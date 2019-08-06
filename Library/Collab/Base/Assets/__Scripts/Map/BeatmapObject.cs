using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BeatmapObject {
    public abstract float _time { get; }
    public abstract Type beatmapType { get; }
    public enum Type { Note, Event, Obstacle, Bomb, Custom }
}
