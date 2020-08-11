﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformDescriptor : MonoBehaviour {

    [Header("Rings")]
    [Tooltip("Leave null if you do not want small rings.")]
    public TrackLaneRingsManager SmallRingManager;
    [Tooltip("Leave null if you do not want big rings.")]
    public TrackLaneRingsManager BigRingManager;
    [Header("Lighting Groups")]
    [Tooltip("Manually map an Event ID (Index) to a group of lights (LightingManagers)")]
    public LightsManager[] LightingManagers = { };
    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;
    public Color RedColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
    public Color BlueColor = BeatSaberSong.DEFAULT_RIGHTCOLOR;
    public Color RedNoteColor = BeatSaberSong.DEFAULT_LEFTNOTE;
    public Color BlueNoteColor = BeatSaberSong.DEFAULT_RIGHTNOTE;
    public Color ObstacleColor = BeatSaberSong.DEFAULT_LEFTNOTE;
    [Tooltip("-1 = No Sorting | 0 = Default Sorting | 1 = Collider Platform Special")]
    public int SortMode;
    [Tooltip("Objects to disable through the L keybind, like lights and static objects in 360 environments.")]
    public GameObject[] DisablableObjects;
    [Tooltip("Change scale of normal map for shiny objects.")]
    public float NormalMapScale = 2f;

    public bool SoloAnEventType { get; private set; } = false;
    public int SoloEventType { get; private set; } = 0;

    private BeatmapObjectCallbackController callbackController;
    private RotationCallbackController rotationCallback;
    private AudioTimeSyncController atsc;
    private Dictionary<LightsManager, Color> ChromaCustomColors = new Dictionary<LightsManager, Color>();
    private Dictionary<LightsManager, Gradient> ChromaGradients = new Dictionary<LightsManager, Gradient>();

    void Start()
    {
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
        {
            LoadInitialMap.LevelLoadedEvent += LevelLoaded;
        }
        UpdateShinyMaterialSettings();
    }

    public void UpdateShinyMaterialSettings()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.material.name.Contains("Shiny Ass Black"))
            {
                Material mat = new Material(renderer.material);
                Vector3 scale = renderer.gameObject.transform.lossyScale;
                Vector2 normalScale = new Vector2(scale.x, scale.z) / NormalMapScale;
                mat.SetTextureScale(Shader.PropertyToID("_BaseMap"), normalScale);
                mat.SetTextureOffset(Shader.PropertyToID("_BaseMap"), Vector2.zero);
                renderer.material = mat;
            }
        }
    }

    void OnDestroy()
    {
        if (callbackController != null)
        {
            callbackController.EventPassedThreshold -= EventPassed;
        }
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
        {
            LoadInitialMap.LevelLoadedEvent -= LevelLoaded;
        }
    }

    private void LevelLoaded()
    {
        callbackController = GameObject.Find("Vertical Grid Callback").GetComponent<BeatmapObjectCallbackController>();
        rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        atsc = rotationCallback.atsc;
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }
        callbackController.EventPassedThreshold += EventPassed;
        RefreshLightingManagers();
    }

    public void RefreshLightingManagers()
    {
        foreach (LightsManager manager in LightingManagers)
        {
            if (manager is null) continue;
            IEnumerable<LightingEvent> allLights = manager.ControllingLights;
            IEnumerable<LightingEvent> lights = allLights.Where(x => !x.UseInvertedPlatformColors);
            IEnumerable<LightingEvent> invertedLights = allLights.Where(x => x.UseInvertedPlatformColors);
            manager.ChangeColor(BlueColor, 0, lights);
            manager.ChangeColor(RedColor, 0, invertedLights);
            manager.ChangeAlpha(0, 0, allLights);
        }
    }

    public void UpdateSoloEventType(bool solo, int soloTypeID)
    {
        SoloAnEventType = solo;
        SoloEventType = soloTypeID;
    }

    public void ToggleDisablableObjects()
    {
        foreach (GameObject go in DisablableObjects) go.SetActive(!go.activeInHierarchy);
    }

    public void KillLights()
    {
        foreach (LightsManager manager in LightingManagers) manager?.ChangeAlpha(0, 1, manager.ControllingLights);
    }

    public void KillChromaLights() => ChromaCustomColors.Clear();

    public void EventPassed(bool initial, int index, BeatmapObject obj)
    {
        MapEvent e = obj as MapEvent; //Two events at the same time should yield same results
        System.Random rng = new System.Random(Mathf.RoundToInt(obj._time * 100));
        switch (e._type) { //FUN PART BOIS
            case 8:
                if (obj._customData?.HasKey("_nameFilter") ?? false)
                {
                    string filter = obj._customData["_nameFilter"];
                    if (filter.Contains("Big") || filter.Contains("Large"))
                    {
                        BigRingManager?.HandleRotationEvent(obj._customData);
                    }
                    else if (filter.Contains("Small"))
                    {
                        SmallRingManager?.HandleRotationEvent(obj._customData);
                    }
                    else
                    {
                        BigRingManager?.HandleRotationEvent(obj._customData);
                        SmallRingManager?.HandleRotationEvent(obj._customData);
                    }
                }
                else
                {
                    BigRingManager?.HandleRotationEvent(obj._customData);
                    SmallRingManager?.HandleRotationEvent(obj._customData);
                }
                break;
            case 9:
                BigRingManager?.HandlePositionEvent();
                SmallRingManager?.HandlePositionEvent();
                break;
            case 12:
                foreach (RotatingLights l in LightingManagers[MapEvent.EVENT_TYPE_LEFT_LASERS].RotatingLights)
                    l.UpdateOffset(e._value, rng.Next(0, 180), rng.Next(0, 1) == 1, obj._customData);
                break;
            case 13:
                foreach (RotatingLights r in LightingManagers[MapEvent.EVENT_TYPE_RIGHT_LASERS].RotatingLights)
                    r.UpdateOffset(e._value, rng.Next(0, 180), rng.Next(0, 1) == 1, obj._customData);
                break;
            default:
                if (e._type < LightingManagers.Length && LightingManagers[e._type] != null)
                    HandleLights(LightingManagers[e._type], e._value, e);
                break;
        }
    }

    void HandleLights(LightsManager group, int value, MapEvent e)
    {
        Color mainColor = Color.white;
        Color invertedColor = Color.white;
        if (group is null) return; //Why go through extra processing for shit that dont exist
        //Check if its a legacy Chroma RGB event
        if (value >= ColourManager.RGB_INT_OFFSET && Settings.Instance.EmulateChromaLite)
        {
            if (ChromaCustomColors.ContainsKey(group)) ChromaCustomColors[group] = ColourManager.ColourFromInt(value);
            else ChromaCustomColors.Add(group, ColourManager.ColourFromInt(value));
            return;
        }
        else if (value == ColourManager.RGB_RESET && Settings.Instance.EmulateChromaLite)
        {
            if (ChromaCustomColors.ContainsKey(group)) ChromaCustomColors.Remove(group);
        }

        if (ChromaGradients.ContainsKey(group))
        {
            MapEvent gradientEvent = ChromaGradients[group].GradientEvent;
            if (atsc.CurrentBeat >= gradientEvent._lightGradient.Duration + gradientEvent._time || !Settings.Instance.EmulateChromaLite)
            {
                StopCoroutine(ChromaGradients[group].Routine);
                ChromaGradients.Remove(group);
                ChromaCustomColors.Remove(group);
            }
        }

        if (e._lightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            if (ChromaGradients.ContainsKey(group))
            {
                StopCoroutine(ChromaGradients[group].Routine);
                ChromaGradients.Remove(group);
            }
            Gradient gradient = new Gradient();
            gradient.GradientEvent = e;
            gradient.Routine = StartCoroutine(GradientRoutine(e, group));
            ChromaGradients.Add(group, gradient);
        }

        //Set initial light values
        if (value <= 3)
        {
            mainColor = BlueColor;
            invertedColor = RedColor;
        }
        else if (value <= 7)
        {
            mainColor = RedColor;
            invertedColor = BlueColor;
        }

        //Check if it is a PogU new Chroma event
        if (e._customData?.HasKey("_color") ?? false && Settings.Instance.EmulateChromaLite)
        {
            mainColor = invertedColor = e._customData["_color"];
            ChromaCustomColors.Remove(group);
            if (ChromaGradients.ContainsKey(group))
            {
                StopCoroutine(ChromaGradients[group].Routine);
                ChromaGradients.Remove(group);
            }
        }

        if (ChromaCustomColors.ContainsKey(group) && Settings.Instance.EmulateChromaLite)
        {
            mainColor = invertedColor = ChromaCustomColors[group];
        }
        
        //Check to see if we're soloing any particular event
        if (SoloAnEventType && e._type != SoloEventType) mainColor = invertedColor = Color.black;

        IEnumerable<LightingEvent> allLights = group.ControllingLights;
        if (e._customData?.HasKey("_propID") ?? false && Settings.Instance.EmulateChromaAdvanced)
        {
            int propID = e._customData["_propID"].AsInt;
            if (propID >= 0 && propID < group.LightsGroupedByZ.Length)
            {
                allLights = group.LightsGroupedByZ[propID];
            }
            else
            {
                Debug.LogWarning($"Light Prop ID {propID} does not exist for event type {e._type}!");
                allLights = new List<LightingEvent>() { };
            }
        }
        IEnumerable<LightingEvent> lights = allLights.Where(x => !x.UseInvertedPlatformColors);
        IEnumerable<LightingEvent> invertedLights = allLights.Where(x => x.UseInvertedPlatformColors);


        if (value == MapEvent.LIGHT_VALUE_OFF)
        {
            group.ChangeAlpha(0, 0, allLights);
        }
        else if (value == MapEvent.LIGHT_VALUE_BLUE_ON || value == MapEvent.LIGHT_VALUE_RED_ON)
        {
            group.ChangeColor(mainColor, 0, lights);
            group.ChangeColor(invertedColor, 0, invertedLights);
            group.ChangeAlpha(mainColor.a, 0, lights);
            group.ChangeAlpha(invertedColor.a, 0, invertedLights);
        }
        else if (value == MapEvent.LIGHT_VALUE_BLUE_FLASH || value == MapEvent.LIGHT_VALUE_RED_FLASH)
        {
            group.Flash(mainColor, lights);
            group.Flash(invertedColor, invertedLights);
        }
        else if (value == MapEvent.LIGHT_VALUE_BLUE_FADE || value == MapEvent.LIGHT_VALUE_RED_FADE)
        {
            group.Fade(mainColor, lights);
            group.Fade(invertedColor, invertedLights);
        }
    }

    private IEnumerator GradientRoutine(MapEvent gradientEvent, LightsManager group)
    {
        MapEvent.ChromaGradient gradient = gradientEvent._lightGradient;
        Func<float, float> easingFunc = Easing.byName[gradient.EasingType];
        float progress = 0;
        while (progress < 1)
        {
            progress = (atsc.CurrentBeat - gradientEvent._time) / gradientEvent._lightGradient.Duration;
            ChromaCustomColors[group] = Color.Lerp(gradient.StartColor, gradient.EndColor, easingFunc(progress));
            group.ChangeColor(ChromaCustomColors[group], 0, group.ControllingLights);
            yield return new WaitForEndOfFrame();
        }
        ChromaCustomColors[group] = gradient.EndColor;
    }

    private class Gradient
    {
        public Coroutine Routine;
        public MapEvent GradientEvent;
    }
}
