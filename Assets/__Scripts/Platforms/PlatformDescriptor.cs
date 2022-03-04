using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlatformDescriptor : MonoBehaviour
{
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

    [FormerlySerializedAs("colors")] [HideInInspector] public PlatformColors Colors;
    [FormerlySerializedAs("defaultColors")] public PlatformColors DefaultColors = new PlatformColors();

    [Tooltip("-1 = No Sorting | 0 = Default Sorting | 1 = Collider Platform Special | 2 = New lanes 6/7 + 16/17")]
    public int SortMode;

    [Tooltip("Objects to disable through the L keybind, like lights and static objects in 360 environments.")]
    public GameObject[] DisablableObjects;

    [Tooltip("Change scale of normal map for shiny objects.")]
    public float NormalMapScale = 2f;

    private readonly Dictionary<LightsManager, Color> chromaCustomColors = new Dictionary<LightsManager, Color>();
    private readonly Dictionary<LightsManager, Gradient> chromaGradients = new Dictionary<LightsManager, Gradient>();

    private readonly Dictionary<int, List<PlatformEventHandler>> platformEventHandlers =
        new Dictionary<int, List<PlatformEventHandler>>();

    private AudioTimeSyncController atsc;

    private BeatmapObjectCallbackController callbackController;
    private RotationCallbackController rotationCallback;

    public bool SoloAnEventType { get; private set; }
    public int SoloEventType { get; private set; }

    public bool ColorBoost { get; private set; }

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

        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding") LoadInitialMap.LevelLoadedEvent += LevelLoaded;
        UpdateShinyMaterialSettings();
    }

    private void OnDestroy()
    {
        if (callbackController != null) callbackController.EventPassedThreshold -= EventPassed;
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding") LoadInitialMap.LevelLoadedEvent -= LevelLoaded;
    }

    public void UpdateShinyMaterialSettings()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.sharedMaterial.name.Contains("Shiny Ass Black"))
            {
                var scale = renderer.gameObject.transform.lossyScale;
                var normalScale = new Vector2(scale.x, scale.z) / NormalMapScale;
                renderer.material.SetTextureScale(Shader.PropertyToID("_BaseMap"), normalScale);
                renderer.material.SetTextureOffset(Shader.PropertyToID("_BaseMap"), Vector2.zero);
            }
        }
    }

    private void LevelLoaded()
    {
        callbackController = GameObject.Find("Vertical Grid Callback").GetComponent<BeatmapObjectCallbackController>();
        rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        atsc = rotationCallback.Atsc;
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
        foreach (var manager in LightingManagers)
        {
            if (manager is null) continue;
            IEnumerable<LightingEvent> allLights = manager.ControllingLights;
            var lights = allLights.Where(x => !x.UseInvertedPlatformColors);
            var invertedLights = allLights.Where(x => x.UseInvertedPlatformColors);
            manager.ChangeColor(Colors.BlueColor, 0, lights);
            manager.ChangeColor(Colors.RedColor, 0, invertedLights);
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
        foreach (var go in DisablableObjects) go.SetActive(!go.activeInHierarchy);
    }

    public void KillLights()
    {
        foreach (var manager in LightingManagers)
        {
            if (manager != null) manager.ChangeAlpha(0, 1, manager.ControllingLights);
        }
    }

    public void KillChromaLights()
    {
        chromaCustomColors.Clear();
        foreach (var kvp in chromaGradients)
        {
            StopCoroutine(kvp.Value.Routine);
            kvp.Key.ChangeMultiplierAlpha(1, kvp.Key.ControllingLights);
        }

        chromaGradients.Clear();
    }

    public void EventPassed(bool isPlaying, int index, BeatmapObject obj)
    {
        var e = obj as MapEvent;

        // Two events at the same time should yield same results
        Random.InitState(Mathf.RoundToInt(obj.Time * 100));

        // FUN PART BOIS
        switch (e.Type)
        {
            case 8:
                if (obj.CustomData?.HasKey("_nameFilter") ?? false)
                {
                    string filter = obj.CustomData["_nameFilter"];
                    if (filter.Contains("Big") || filter.Contains("Large"))
                    {
                        if (BigRingManager != null)
                            BigRingManager.HandleRotationEvent(obj.CustomData);
                    }
                    else if (filter.Contains("Small") || filter.Contains("Panels") || filter.Contains("Triangle"))
                    {
                        if (SmallRingManager != null)
                            SmallRingManager.HandleRotationEvent(obj.CustomData);
                    }
                    else
                    {
                        if (BigRingManager != null)
                            BigRingManager.HandleRotationEvent(obj.CustomData);
                        if (SmallRingManager != null)
                            SmallRingManager.HandleRotationEvent(obj.CustomData);
                    }
                }
                else
                {
                    if (BigRingManager != null)
                        BigRingManager.HandleRotationEvent(obj.CustomData);
                    if (SmallRingManager != null)
                        SmallRingManager.HandleRotationEvent(obj.CustomData);
                }

                break;
            case 9:
                if (BigRingManager != null)
                    BigRingManager.HandlePositionEvent(obj.CustomData);
                if (SmallRingManager != null)
                    SmallRingManager.HandlePositionEvent(obj.CustomData);
                break;
            case 12:
                var leftEventTypes = new List<int>() {MapEvent.EventTypeLeftLasers, MapEvent.EventTypeCustomLight2, MapEvent.EventTypeCustomLight4};

                foreach (var eventType in leftEventTypes.Where(eventType => LightingManagers.Length >= eventType))
                {
                    foreach (var l in LightingManagers[eventType].RotatingLights)
                    {
                        l.UpdateOffset(true, e.Value, Random.Range(0, 180), Random.Range(0, 1) == 1, obj.CustomData);
                        if (isPlaying) l.UpdateZPosition();
                    }
                }

                break;
            case 13:
                var rightEventTypes = new List<int>() {MapEvent.EventTypeRightLasers, MapEvent.EventTypeCustomLight3, MapEvent.EventTypeCustomLight5};

                foreach (var eventType in rightEventTypes.Where(eventType => LightingManagers.Length >= eventType))
                {
                    foreach (var l in LightingManagers[eventType].RotatingLights)
                    {
                        l.UpdateOffset(true, e.Value, Random.Range(0, 180), Random.Range(0, 1) == 1, obj.CustomData);
                        if (isPlaying) l.UpdateZPosition();
                    }
                }

                break;
            case 5:
                ColorBoost = e.Value == 1;
                foreach (var manager in LightingManagers)
                {
                    if (manager == null) continue;

                    manager.Boost(ColorBoost, ColorBoost ? Colors.RedBoostColor : Colors.RedColor,
                        ColorBoost ? Colors.BlueBoostColor : Colors.BlueColor);
                }

                break;
            default:
                if (e.Type < LightingManagers.Length && LightingManagers[e.Type] != null)
                    HandleLights(LightingManagers[e.Type], e.Value, e);
                break;
        }

        if (atsc != null && atsc.IsPlaying && platformEventHandlers.TryGetValue(e.Type, out var eventHandlers))
        {
            foreach (var handler in eventHandlers)
                handler.OnEventTrigger(e.Type, e);
        }
    }

    private void HandleLights(LightsManager group, int value, MapEvent e)
    {
        var mainColor = Color.white;
        var invertedColor = Color.white;
        if (group is null) return; //Why go through extra processing for shit that dont exist
        //Check if its a legacy Chroma RGB event
        if (value >= ColourManager.RgbintOffset && Settings.Instance.EmulateChromaLite)
        {
            if (chromaCustomColors.ContainsKey(group)) chromaCustomColors[group] = ColourManager.ColourFromInt(value);
            else chromaCustomColors.Add(group, ColourManager.ColourFromInt(value));
            return;
        }

        if (value == ColourManager.RGBReset && Settings.Instance.EmulateChromaLite)
        {
            if (chromaCustomColors.ContainsKey(@group))
                chromaCustomColors.Remove(@group);
        }

        if (chromaGradients.ContainsKey(group))
        {
            var gradientEvent = chromaGradients[group].GradientEvent;
            if (atsc.CurrentBeat >= gradientEvent.LightGradient.Duration + gradientEvent.Time ||
                !Settings.Instance.EmulateChromaLite)
            {
                StopCoroutine(chromaGradients[group].Routine);
                chromaGradients.Remove(group);
                chromaCustomColors.Remove(group);
            }
        }

        if (e.LightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            if (chromaGradients.ContainsKey(group))
            {
                StopCoroutine(chromaGradients[group].Routine);
                chromaGradients.Remove(group);
            }

            var gradient = new Gradient { GradientEvent = e, Routine = StartCoroutine(GradientRoutine(e, group)) };

            // If the gradient is over already then null is returned due to coroutine never yielding
            if (gradient.Routine != null) chromaGradients.Add(group, gradient);
        }

        //Set initial light values
        if (value <= 3)
        {
            mainColor = ColorBoost ? Colors.BlueBoostColor : Colors.BlueColor;
            invertedColor = Colors.RedColor;
        }
        else if (value <= 7)
        {
            mainColor = ColorBoost ? Colors.RedBoostColor : Colors.RedColor;
            invertedColor = Colors.BlueColor;
        }

        //Check if it is a PogU new Chroma event
        if ((e.CustomData?.HasKey("_color") ?? false) && Settings.Instance.EmulateChromaLite)
        {
            mainColor = invertedColor = e.CustomData["_color"];
            chromaCustomColors.Remove(group);
            if (chromaGradients.ContainsKey(group))
            {
                StopCoroutine(chromaGradients[group].Routine);
                chromaGradients.Remove(group);
            }
        }

        if (chromaCustomColors.ContainsKey(group) && Settings.Instance.EmulateChromaLite)
        {
            mainColor = invertedColor = chromaCustomColors[group];
            group.ChangeMultiplierAlpha(mainColor.a, group.ControllingLights);
        }

        //Check to see if we're soloing any particular event
        if (SoloAnEventType && e.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        IEnumerable<LightingEvent> allLights = group.ControllingLights;

        if (e.IsLightIdEvent && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = e.LightId;
            allLights = group.ControllingLights.FindAll(x => lightIDArr.Contains(x.LightID));

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
                case MapEvent.LightValueOff:
                    light.UpdateTargetAlpha(0, 0);
                    light.UpdateMultiplyAlpha();
                    break;
                case MapEvent.LightValueBlueON:
                case MapEvent.LightValueRedON:
                    light.UpdateMultiplyAlpha(color.a);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDRIntensity), 0);
                    light.UpdateTargetAlpha(1, 0);
                    break;
                case MapEvent.LightValueBlueFlash:
                case MapEvent.LightValueRedFlash:
                    light.UpdateTargetAlpha(1, 0);
                    light.UpdateMultiplyAlpha(color.a);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDRFlashIntensity), 0);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDRIntensity), LightsManager.FlashTime);
                    break;
                case MapEvent.LightValueBlueFade:
                case MapEvent.LightValueRedFade:
                    light.UpdateTargetAlpha(1, 0);
                    light.UpdateMultiplyAlpha(color.a);
                    light.UpdateTargetColor(color.Multiply(LightsManager.HDRFlashIntensity), 0);
                    if (light.CanBeTurnedOff)
                    {
                        light.UpdateTargetAlpha(0, LightsManager.FadeTime);
                        light.UpdateTargetColor(Color.black, LightsManager.FadeTime);
                    }
                    else
                    {
                        light.UpdateTargetColor(color.Multiply(LightsManager.HDRIntensity), LightsManager.FadeTime);
                    }

                    break;
            }
        }

        group.SetValue(value);
    }

    private IEnumerator GradientRoutine(MapEvent gradientEvent, LightsManager group)
    {
        var gradient = gradientEvent.LightGradient;
        var easingFunc = Easing.ByName[gradient.EasingType];

        float progress;
        while ((progress = (atsc.CurrentBeat - gradientEvent.Time) / gradient.Duration) < 1)
        {
            var lerped = Color.LerpUnclamped(gradient.StartColor, gradient.EndColor, easingFunc(progress));
            if (!SoloAnEventType || gradientEvent.Type == SoloEventType)
            {
                chromaCustomColors[group] = lerped;
                group.ChangeColor(lerped.WithAlpha(1), 0, group.ControllingLights);
                group.ChangeMultiplierAlpha(lerped.a, group.ControllingLights);
            }

            yield return new WaitForEndOfFrame();
        }

        chromaCustomColors[group] = gradient.EndColor;
        group.ChangeColor(chromaCustomColors[group].WithAlpha(1), 0, group.ControllingLights);
        group.ChangeMultiplierAlpha(chromaCustomColors[group].a, group.ControllingLights);
    }

    private class Gradient
    {
        public MapEvent GradientEvent;
        public Coroutine Routine;
    }
}
