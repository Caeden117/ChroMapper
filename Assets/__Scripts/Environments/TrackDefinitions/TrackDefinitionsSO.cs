using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackDefinitionsSO : ScriptableObject
{
    [SerializeField] private List<TrackDefinitionBasic> basicEntries = new();
    [SerializeField] private List<TrackDefinitionGLS> glsEntries = new();

    public Dictionary<int, TrackDefinitionBasic> Basic;
    public Dictionary<int, TrackDefinitionGLS> Gls;

    public static readonly TrackDefinitionBasic DefaultBasic = new();
    public static readonly TrackDefinitionGLS DefaultGls = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    public void Initialize()
    {
        Basic = basicEntries.ToDictionary(x => x.Type, x => x);
        Gls = glsEntries.Select((x, i) => (i, x)).ToDictionary(x => x.x.ID, x => x.x);
    }

    public TrackDefinitionBasic GetBasicOrDefault(int type) => Basic.GetValueOrDefault(type, DefaultBasic);
    public TrackDefinitionGLS GetGlsOrDefault(int id) => Gls.GetValueOrDefault(id, DefaultGls);

    public TrackDefinitionsSO Copy(TrackDefinitionsSO other)
    {
        basicEntries = other.basicEntries;
        glsEntries = other.glsEntries;
        Initialize();
        return this;
    }

    public void Register(TrackDefinitionBasic basic)
    {
        basicEntries.Add(basic);
        Initialize();
    }

    public void Register(TrackDefinitionGLS gls)
    {
        glsEntries.Add(gls);
        Initialize();
    }

    public void UnregisterAll()
    {
        basicEntries.Clear();
        glsEntries.Clear();
        Initialize();
    }
}

[Serializable]
public class TrackDefinitionBasic
{
    public string Name = "Default";
    public int Type = -1;
    public BasicEventKind Kind = BasicEventKind.Generic;

    // Track component metadata stays independent from toolbar kind so mixed light/ring tracks are represented accurately.
    public BasicEventComponent Components = BasicEventComponent.None;
}

[Serializable]
public class TrackDefinitionGLS
{
    public string Group;
    public string Name;
    public int ID;

    public bool ColorTrack;

    public bool[] RotationTracks = new bool[3];
    public string OverrideDefaultRotationAxis;

    public bool[] TranslationTracks = new bool[3];
    public string OverrideDefaultTranslationAxis;

    public bool FloatFXTrack;

    public bool Duplicate;
}

public enum BasicEventKind : byte
{
    Generic,
    None,
    Lights,
    Toggle,
    FloatValue,
    IntValue,
    BtsCharacter,
    Car
}

// Basic events can drive multiple environment component types at once.
[Flags]
public enum BasicEventComponent : byte
{
    None = 0,
    RingRotation = 1 << 0,
    RingZoom = 1 << 1,
    // Laser-speed tracks are identified by the light-rotation effect components that consume their events.
    LightRotation = 1 << 2,
    LightRotationLeft = 1 << 3,
    LightRotationRight = 1 << 4,
    // The Second's legacy smooth-step ring has distinct i/customData.step semantics.
    SmoothStepRingZoom = 1 << 5
}
