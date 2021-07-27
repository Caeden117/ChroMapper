using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformDescriptor : MonoBehaviour {

    [Header("Rings")]
    [Tooltip("Leave null if you do not want small rings.")]
    public TrackLaneRingsManager SmallRingManager;
    [Tooltip("Leave null if you do not want big rings.")]
    public TrackLaneRingsManagerBase BigRingManager;
    [Header("Lighting Groups")]
    [Tooltip("Manually map an Event ID (Index) to a group of lights (LightingManagers)")]
    public LightsManager[] LightingManagers = { };
    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;
    [HideInInspector] public PlatformColors colors;
    public PlatformColors defaultColors = new PlatformColors();
    [Tooltip("-1 = No Sorting | 0 = Default Sorting | 1 = Collider Platform Special | 2 = New lanes 6/7 + 16/17")]
    public int SortMode;
    [Tooltip("Objects to disable through the L keybind, like lights and static objects in 360 environments.")]
    public GameObject[] DisablableObjects;
    [Tooltip("Change scale of normal map for shiny objects.")]
    public float NormalMapScale = 2f;

    public bool SoloAnEventType { get; private set; } = false;
    public int SoloEventType { get; private set; } = 0;

    public bool ColorBoost { get; private set; } = false;

    private BeatmapObjectCallbackController callbackController;
    private RotationCallbackController rotationCallback;
    private AudioTimeSyncController atsc;

    private Dictionary<int, List<PlatformEventHandler>> platformEventHandlers = new Dictionary<int, List<PlatformEventHandler>>();
    private Dictionary<LightsManager, Color> ChromaCustomColors = new Dictionary<LightsManager, Color>();
    private Dictionary<LightsManager, Gradient> ChromaGradients = new Dictionary<LightsManager, Gradient>();

    private void Start()
    {
        var eventHandlers = GetComponentsInChildren<PlatformEventHandler>();

        foreach (var handler in eventHandlers)
        {
            foreach (var type in handler.ListeningEventTypes)
            {
                if (!platformEventHandlers.TryGetValue(type, out var list))
                {
                    list = new List<PlatformEventHandler>();
                    platformEventHandlers.Add(type, list);
                }

                list.Add(handler);
            }
        }

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
            if (renderer.sharedMaterial.name.Contains("Shiny Ass Black"))
            {
                Vector3 scale = renderer.gameObject.transform.lossyScale;
                Vector2 normalScale = new Vector2(scale.x, scale.z) / NormalMapScale;
                renderer.material.SetTextureScale(Shader.PropertyToID("_BaseMap"), normalScale);
                renderer.material.SetTextureOffset(Shader.PropertyToID("_BaseMap"), Vector2.zero);
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
            manager.ChangeColor(colors.BlueColor, 0, lights);
            manager.ChangeColor(colors.RedColor, 0, invertedLights);
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

    public void KillChromaLights()
    {
        ChromaCustomColors.Clear();
        foreach (var kvp in ChromaGradients)
        {
            StopCoroutine(kvp.Value.Routine);
            kvp.Key.ChangeMultiplierAlpha(1, kvp.Key.ControllingLights);
        }
        ChromaGradients.Clear();
    }

    public void EventPassed(bool initial, int index, BeatmapObject obj)
    {
        MapEvent e = obj as MapEvent;

        // Two events at the same time should yield same results
        UnityEngine.Random.InitState(Mathf.RoundToInt(obj._time * 100));

        // FUN PART BOIS
        switch (e._type)
        {
            case 8:
                if (obj._customData?.HasKey("_nameFilter") ?? false)
                {
                    string filter = obj._customData["_nameFilter"];
                    if (filter.Contains("Big") || filter.Contains("Large"))
                    {
                        BigRingManager?.HandleRotationEvent(obj._customData);
                    }
                    else if (filter.Contains("Small")|| filter.Contains("Panels") || filter.Contains("Triangle"))
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
                foreach (RotatingLightsBase l in LightingManagers[MapEvent.EVENT_TYPE_LEFT_LASERS].RotatingLights)
                {
                    l.UpdateOffset(true, e._value, UnityEngine.Random.Range(0, 180), UnityEngine.Random.Range(0, 1) == 1, obj._customData);
                }

                if (LightingManagers.Length < MapEvent.EVENT_TYPE_CUSTOM_LIGHT_2) break;

                foreach (RotatingLightsBase l in LightingManagers[MapEvent.EVENT_TYPE_CUSTOM_LIGHT_2].RotatingLights)
                {
                    l.UpdateOffset(true, e._value, UnityEngine.Random.Range(0, 180), UnityEngine.Random.Range(0, 1) == 1, obj._customData);
                }
                break;
            case 13:
                foreach (RotatingLightsBase r in LightingManagers[MapEvent.EVENT_TYPE_RIGHT_LASERS].RotatingLights)
                {
                    r.UpdateOffset(false, e._value, UnityEngine.Random.Range(0, 180), UnityEngine.Random.Range(0, 1) == 1, obj._customData);
                }

                if (LightingManagers.Length < MapEvent.EVENT_TYPE_CUSTOM_LIGHT_3) break;

                foreach (RotatingLightsBase r in LightingManagers[MapEvent.EVENT_TYPE_CUSTOM_LIGHT_3].RotatingLights)
                {
                    r.UpdateOffset(false, e._value, UnityEngine.Random.Range(0, 180), UnityEngine.Random.Range(0, 1) == 1, obj._customData);
                }
                break;
            case 5:
                ColorBoost = e._value == 1;
                foreach (var manager in LightingManagers)
                {
                    manager?.Boost(ColorBoost ? colors.RedBoostColor : colors.RedColor,
                        ColorBoost ? colors.BlueBoostColor : colors.BlueColor);
                }
                break;
            default:
                if (e._type < LightingManagers.Length && LightingManagers[e._type] != null)
                    HandleLights(LightingManagers[e._type], e._value, e);
                break;
        }

        if (atsc != null && atsc.IsPlaying && platformEventHandlers.TryGetValue(e._type, out var eventHandlers))
        {
            foreach (var handler in eventHandlers)
            {
                handler.OnEventTrigger(e._type, e);
            }
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

            var gradient = new Gradient
            {
                GradientEvent = e,
                Routine = StartCoroutine(GradientRoutine(e, group))
            };

            // If the gradient is over already then null is returned due to coroutine never yielding
            if (gradient.Routine != null)
            {
                ChromaGradients.Add(group, gradient);
            }
        }

        //Set initial light values
        if (value <= 3)
        {
            mainColor = ColorBoost ? colors.BlueBoostColor : colors.BlueColor;
            invertedColor = colors.RedColor;
        }
        else if (value <= 7)
        {
            mainColor = ColorBoost ? colors.RedBoostColor : colors.RedColor;
            invertedColor = colors.BlueColor;
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
            group.ChangeMultiplierAlpha(mainColor.a, group.ControllingLights);
        }
        
        //Check to see if we're soloing any particular event
        if (SoloAnEventType && e._type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        IEnumerable<LightingEvent> allLights = group.ControllingLights;

        if (e.IsLightIdEvent && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = e.LightId;
            allLights = group.ControllingLights.FindAll(x => lightIDArr.Contains(x.lightID));

            // Temporarily(?) commented as Debug.LogWarning is expensive
            //if (allLights.Count() < lightIDArr.Length)
            //{
            //    Debug.LogWarning($"Missing lights for {lightIDArr} in event type {e._type}!");
            //}
        }

        foreach (var light in allLights)
        {
            var color = light.UseInvertedPlatformColors ? invertedColor : mainColor;

            switch (value)
            {
                case MapEvent.LIGHT_VALUE_OFF:
                    light.UpdateTargetAlpha(0, 0);
                    light.UpdateMultiplyAlpha(1);
                    break;
                case MapEvent.LIGHT_VALUE_BLUE_ON:
                case MapEvent.LIGHT_VALUE_RED_ON:
                    light.UpdateMultiplyAlpha(color.a);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDR_Intensity), 0);
                    light.UpdateTargetAlpha(1, 0);
                    break;
                case MapEvent.LIGHT_VALUE_BLUE_FLASH:
                case MapEvent.LIGHT_VALUE_RED_FLASH:
                    light.UpdateTargetAlpha(1, 0);
                    light.UpdateMultiplyAlpha(color.a);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDR_Flash_Intensity), 0);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDR_Intensity), LightsManager.FadeTime);
                    break;
                case MapEvent.LIGHT_VALUE_BLUE_FADE:
                case MapEvent.LIGHT_VALUE_RED_FADE:
                    light.UpdateTargetAlpha(1, 0);
                    light.UpdateMultiplyAlpha(color.a);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDR_Flash_Intensity), 0);
                    if (light.CanBeTurnedOff)
                    {
                        light.UpdateTargetAlpha(0, LightsManager.FadeTime);
                        light.UpdateTargetColor(Color.black, LightsManager.FadeTime);
                    }
                    else
                    {
                        light.UpdateTargetColor(color.Multiply(LightsManager.HDR_Intensity), LightsManager.FadeTime);
                    }
                    break;
            }
        }

        group.SetValue(value);
    }

    private IEnumerator GradientRoutine(MapEvent gradientEvent, LightsManager group)
    {
        MapEvent.ChromaGradient gradient = gradientEvent._lightGradient;
        Func<float, float> easingFunc = Easing.byName[gradient.EasingType];
        float progress = 0;
        while ((progress = (atsc.CurrentBeat - gradientEvent._time) / gradient.Duration) < 1)
        {
            Color lerped = Color.LerpUnclamped(gradient.StartColor, gradient.EndColor, easingFunc(progress));
            if (!SoloAnEventType || gradientEvent._type == SoloEventType)
            {
                ChromaCustomColors[group] = lerped;
                group.ChangeColor(lerped.WithAlpha(1), 0, group.ControllingLights);
                group.ChangeMultiplierAlpha(lerped.a, group.ControllingLights);
            }
            yield return new WaitForEndOfFrame();
        }
        ChromaCustomColors[group] = gradient.EndColor;
        group.ChangeColor(ChromaCustomColors[group].WithAlpha(1), 0, group.ControllingLights);
        group.ChangeMultiplierAlpha(ChromaCustomColors[group].a, group.ControllingLights);
    }

    private class Gradient
    {
        public Coroutine Routine;
        public MapEvent GradientEvent;
    }
}
