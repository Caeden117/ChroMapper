using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using CustomFloorPlugin;
using UnityEngine;
using static CustomFloorPlugin.TubeLight;

//TODO: Worklog
// - Rotating Lights >> Not yet fully supported by CustomPlatform
// - Increase track Lanes if ringCount > 15
// - Spectrogram
// - Support 'Unused' Lights Id

public class CustomPlatformsLoader : MonoBehaviour
{
    private static CustomPlatformsLoader instance;

    private readonly CustomPlatformSettings customPlatformSettings = CustomPlatformSettings.Instance;
    private readonly List<string> environmentsOnly = new List<string>();

    private readonly List<string> platformsOnly = new List<string>();

    //Always create new INSTANCES of materials. Or else you'd modify the actual file itself, and cause changes in Git.
    private Material lightMaterial;

    private PlatformDescriptor platformDescriptor;
    private Material useThisBlack;

    private static readonly int baseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");

    public static CustomPlatformsLoader Instance => instance != null ? instance : (instance = Load());

    private void Awake()
    {
        //Always create new INSTANCES of materials. Or else you'd modify the actual file itself, and cause changes in Git.
        lightMaterial = Resources.Load("ControllableLight", typeof(Material)) as Material;

        useThisBlack = new Material(Resources.Load("Basic Black", typeof(Material)) as Material);
    }

    public void Init()
    {
    }

    private static CustomPlatformsLoader Load()
    {
        var cpl = new GameObject("Custom Platforms Loader").AddComponent<CustomPlatformsLoader>();
        DontDestroyOnLoad(cpl.gameObject);

        //PlatformsOnly
        foreach (var platformName in cpl.GetAllEnvironmentIds())
        {
            var platform = cpl.LoadPlatform(platformName);

            var cp = cpl.FindCustomPlatformScript(platform);

            if (cp != null)
            {
                if (!cp.hideHighway && !cp.hideTowers && cp.hideDefaultPlatform && !cp.hideEQVisualizer &&
                    !cp.hideSmallRings && !cp.hideBigRings && !cp.hideBackColumns && !cp.hideBackLasers &&
                    !cp.hideDoubleLasers && !cp.hideDoubleColorLasers && !cp.hideRotatingLasers && !cp.hideTrackLights)
                {
                    cpl.platformsOnly.Add(platformName);
                }
                else
                {
                    cpl.environmentsOnly.Add(platformName);
                }
            }


            DestroyImmediate(platform, true);
        }


        return cpl;
    }

    public GameObject LoadPlatform(string customEnvironmentString, GameObject defaultEnvironment = null,
        string customPlatformString = null)
    {
        try
        {
            GameObject defaultEnvironmentInstance = null;
            if (defaultEnvironment != null)
            {
                defaultEnvironmentInstance =
                    Instantiate(defaultEnvironment, LoadInitialMap.PlatformOffset, Quaternion.identity);
                platformDescriptor = defaultEnvironmentInstance.GetComponentInParent<PlatformDescriptor>();
            }

            var customEnvironments = customPlatformSettings.LoadPlatform(customEnvironmentString);
            GameObject customEnvironment = null;
            CustomPlatform environmentCp = null;
            foreach (var g in customEnvironments)
            {
                environmentCp = FindCustomPlatformScript(g);
                if (environmentCp != null)
                {
                    customEnvironment = g;
                    RemoveHiddenElementsFromEnvironment(defaultEnvironmentInstance, environmentCp);
                    break;
                }
            }

            GameObject[] customPlatforms = null;
            GameObject customPlatform = null;
            if (customPlatformString != null)
            {
                customPlatforms = customPlatformSettings.LoadPlatform(customPlatformString);

                CustomPlatform platformCp = null;
                foreach (var g in customPlatforms)
                {
                    platformCp = FindCustomPlatformScript(g);
                    if (platformCp != null)
                    {
                        customPlatform = g;
                        RemoveHiddenElementsFromEnvironment(defaultEnvironmentInstance, platformCp);
                        break;
                    }
                }

                var customPlatformInstance =
                    Instantiate(customPlatform, LoadInitialMap.PlatformOffset, Quaternion.identity);
                DisableElementsFromEnvironmentRecursive(customPlatformInstance, "Camera");
                customPlatformInstance.transform.SetParent(defaultEnvironmentInstance.transform);
                foreach (var g in customPlatforms)
                {
                    if (g != customPlatform)
                    {
                        var customPlatformInstance2 =
                            Instantiate(g, LoadInitialMap.PlatformOffset, Quaternion.identity);
                        customPlatformInstance2.transform.SetParent(customPlatformInstance.transform);
                    }
                }
            }

            if (defaultEnvironment != null)
            {
                var customEnvironmentInstance =
                    Instantiate(customEnvironment, LoadInitialMap.PlatformOffset, Quaternion.identity);
                DisableElementsFromEnvironmentRecursive(customEnvironmentInstance, "Camera");
                customEnvironmentInstance.transform.SetParent(defaultEnvironmentInstance.transform);
                foreach (var g in customEnvironments)
                {
                    if (g != customEnvironment)
                    {
                        var customEnvironmentInstance2 =
                            Instantiate(g, LoadInitialMap.PlatformOffset, Quaternion.identity);
                        customEnvironmentInstance2.transform.SetParent(customEnvironmentInstance.transform);
                    }
                }

                ReplaceBetterBlack(defaultEnvironmentInstance);
                foreach (var renderer in defaultEnvironmentInstance.GetComponentsInChildren<Renderer>())
                    SetShadersCorrectly(renderer);

                //Set LightsManager Size correctly
                SetLightsManagerSize(defaultEnvironmentInstance);
                platformDescriptor.RefreshLightingManagers();

                //Rings
                var ringCount = 0;
                var trackRings = defaultEnvironmentInstance.GetComponentsInChildren<TrackRings>();
                foreach (var trackRing in trackRings)
                {
                    SetRings(trackRing.gameObject, trackRing, ringCount);
                    ringCount++;
                }

                //TubeLights

                SetLightingEventsForTubeLights(defaultEnvironmentInstance);

                return defaultEnvironmentInstance;
            }


            return customEnvironment;
        }
        catch
        {
            return Instantiate(defaultEnvironment, LoadInitialMap.PlatformOffset, Quaternion.identity);
        }
    }

    private void SetLightsManagerSize(GameObject gameObject)
    {
        var tubeLights = gameObject.GetComponentsInChildren<TubeLight>();
        var maxSize = platformDescriptor.LightingManagers.Length;
        foreach (var tubeLight in tubeLights)
        {
            switch (tubeLight.lightsID)
            {
                case LightsID.Unused5:
                    maxSize = Math.Max(maxSize, (int)EventTypeValue.ColorBoost + 1);
                    break;
                case LightsID.Unused6:
                    maxSize = Math.Max(maxSize, (int)EventTypeValue.ExtraLeftLights + 1);
                    break;
                case LightsID.Unused7:
                    maxSize = Math.Max(maxSize, (int)EventTypeValue.ExtraRightLights + 1);
                    break;
                case LightsID.Unused10:
                    maxSize = Math.Max(maxSize, (int)EventTypeValue.ExtraLeftLasers + 1);
                    break;
                case LightsID.Unused11:
                    maxSize = Math.Max(maxSize, (int)EventTypeValue.ExtraRightLasers + 1);
                    break;
            }
        }

        if (maxSize != platformDescriptor.LightingManagers.Length)
            Array.Resize(ref platformDescriptor.LightingManagers, maxSize);
    }

    private void SetLightingEventsForTubeLights(GameObject gameObject)
    {
        var tubeLights = gameObject.GetComponentsInChildren<TubeLight>();
        foreach (var tubeLight in tubeLights)
        {
            if (tubeLight.gameObject.GetComponent<LightingEvent>() != null) continue;

            var eventId = -(int)EventTypeValue.BackLasers;
            switch (tubeLight.lightsID)
            {
                case LightsID.Static:
                    eventId = (int)EventTypeValue.BackLasers;
                    break;
                case LightsID.BackLights:
                    eventId = (int)EventTypeValue.BackLasers;
                    break;
                case LightsID.BigRingLights:
                    eventId = (int)EventTypeValue.RingLights;
                    break;
                case LightsID.LeftLasers:
                    eventId = (int)EventTypeValue.LeftLasers;
                    break;
                case LightsID.RightLasers:
                    eventId = (int)EventTypeValue.RightLasers;
                    break;
                case LightsID.TrackAndBottom:
                    eventId = (int)EventTypeValue.CenterLights;
                    break;
                case LightsID.RingsRotationEffect:
                    break;
                case LightsID.RingsStepEffect:
                    break;
                case LightsID.RingSpeedLeft:
                    break;
                case LightsID.RingSpeedRight:
                    break;
                case LightsID.Unused5:
                    eventId = (int)EventTypeValue.ColorBoost;
                    break;
                case LightsID.Unused6:
                    eventId = (int)EventTypeValue.ExtraLeftLights;
                    break;
                case LightsID.Unused7:
                    eventId = (int)EventTypeValue.ExtraRightLights;
                    break;
                case LightsID.Unused10:
                    eventId = (int)EventTypeValue.ExtraLeftLasers;
                    break;
                case LightsID.Unused11:
                    eventId = (int)EventTypeValue.ExtraRightLasers;
                    break;
                default:
                    //Unused 5 6 7 10 11 14 15
                    Debug.Log("Custom LightsID " + tubeLight.lightsID);
                    break;
            }

            var tubeLightsManager = platformDescriptor.LightingManagers[eventId];
            if (tubeLightsManager == null)
            {
                tubeLightsManager = tubeLight.transform.parent.gameObject.AddComponent<LightsManager>();
                tubeLightsManager.DisableCustomInitialization = true;
                platformDescriptor.LightingManagers[eventId] = tubeLightsManager;
            }

            var meshRenderers = tubeLight.gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers) SetRendererMaterials(renderer, tubeLightsManager, tubeLight.width);

            if (tubeLight.gameObject.GetComponent<MeshFilter>() != null)
            {
                var mFilter = tubeLight.gameObject.GetComponent<MeshFilter>();
                if (mFilter.sharedMesh == null)
                {
                    var mesh = new Mesh { name = "ScriptGenerated" };

                    Vector3[] vertices =
                    {
                        new Vector3(0, 0, 0), new Vector3(tubeLight.width, 0, 0),
                        new Vector3(tubeLight.width, tubeLight.length, 0), new Vector3(0, tubeLight.length, 0),
                        new Vector3(0, tubeLight.length, tubeLight.width),
                        new Vector3(tubeLight.width, tubeLight.length, tubeLight.width),
                        new Vector3(tubeLight.width, 0, tubeLight.width), new Vector3(0, 0, tubeLight.width)
                    };

                    int[] triangles =
                    {
                        0, 2, 1, //face front
                        0, 3, 2, 2, 3, 4, //face top
                        2, 4, 5, 1, 2, 5, //face right
                        1, 5, 6, 0, 7, 4, //face left
                        0, 4, 3, 5, 4, 7, //face back
                        5, 7, 6, 0, 6, 7, //face bottom
                        0, 1, 6
                    };

                    mesh.vertices = vertices;
                    mesh.triangles = triangles;

                    var colors = new Color[vertices.Length];
                    for (var i = 0; i < vertices.Length; i++) colors[i] = tubeLight.color;
                    mesh.colors = colors;

                    var offset = tubeLight.transform.position - tubeLight.transform.TransformPoint(mesh.bounds.center);
                    tubeLight.transform.position = tubeLight.transform.position + offset;

                    tubeLight.gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                }
            }
        }

        var eventHandlers = gameObject.GetComponentsInChildren<SongEventHandler>();
        foreach (var eventHandler in eventHandlers)
        {
            if (eventHandler.gameObject.GetComponent<LightingEvent>() != null) continue;

            var eventId = (int)eventHandler.eventType;

            var tubeLightsManager = platformDescriptor.LightingManagers[eventId];
            if (tubeLightsManager == null)
            {
                tubeLightsManager = eventHandler.transform.parent.gameObject.AddComponent<LightsManager>();
                tubeLightsManager.DisableCustomInitialization = true;
                platformDescriptor.LightingManagers[eventId] = tubeLightsManager;
            }

            var meshRenderers = eventHandler.gameObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in meshRenderers) SetRendererMaterials(renderer, tubeLightsManager);
        }
    }

    private void SetShadersCorrectly(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;

        if (materials.Length >= 1)
        {
            for (var i = 0; i < materials.Length; i++)
            {
                var tempMaterial = materials[i];

                if (tempMaterial == null || tempMaterial.shader == null) continue;

                if (tempMaterial.shader.name.Contains("BeatSaber/Standard") ||
                    tempMaterial.shader.name.Equals("Standard"))
                {
                    tempMaterial.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }

                if (tempMaterial.name.ToUpper().Contains("GLOW_BLUE"))
                {
                    tempMaterial = new Material(lightMaterial);
                    tempMaterial.SetColor(baseColor, Color.white);
                    tempMaterial.EnableKeyword("_EMISSION");
                    tempMaterial.SetColor(emissionColor,
                        BeatSaberSong.DefaultRightColor * LightsManager.HDRIntensity);
                }

                if (tempMaterial.name.ToUpper().Contains("GLOW_RED"))
                {
                    tempMaterial = new Material(lightMaterial);
                    tempMaterial.SetColor(baseColor, Color.white);
                    tempMaterial.EnableKeyword("_EMISSION");
                    tempMaterial.SetColor(emissionColor,
                        BeatSaberSong.DefaultLeftColor * LightsManager.HDRIntensity);
                }

                materials[i] = tempMaterial;
            }
        }

        renderer.sharedMaterials = materials;
    }

    private void SetRendererMaterials(Renderer renderer, LightsManager lightsManager = null, float width = 1f)
    {
        var materials = renderer.sharedMaterials;

        if (materials.Length >= 1 && materials[0] != null)
        {
            if (materials[0] != null && width >= 0.5f)
                Array.Resize(ref materials, materials.Length + 1);

            var lastMaterial = new Material(lightMaterial);
            for (var i = 0; i < materials.Length; i++)
            {
                var tempMaterial = materials[i];

                if (tempMaterial == null || tempMaterial.color == null) continue;

                if (tempMaterial.shader.name.Equals("Unlit/Color") &&
                    !(tempMaterial.color.r == 1 && tempMaterial.color.g == 1 && tempMaterial.color.b == 1))
                {
                    tempMaterial = useThisBlack;
                }

                materials[i] = lastMaterial;
                lastMaterial = tempMaterial;
            }
        }
        else
        {
            materials = new Material[1];
            materials[0] = new Material(lightMaterial);
        }

        renderer.sharedMaterials = materials;

        if (lightsManager != null)
        {
            var le = renderer.gameObject.AddComponent<LightingEvent>();
            lightsManager.ControllingLights.Add(le);
        }
    }

    private void RemoveHiddenElementsFromEnvironment(GameObject environment, CustomPlatform customPlatform)
    {
        if (customPlatform.hideHighway)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Floor", new List<string> { "PlayersPlace" });
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Legs");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Top");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Platform");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Mirror");
        }

        if (customPlatform.hideTowers)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "RocketCar");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "RocketCar (1)");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Pillars Object");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "BG");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "RocketArena");
        }

        if (customPlatform.hideDefaultPlatform)
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "PlayersPlace");
        if (customPlatform.hideEQVisualizer)
        {
        }

        if (customPlatform.hideSmallRings) RemoveHiddenElementsFromEnvironmentRecursive(environment, "Small Rings");
        if (customPlatform.hideBigRings)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Big Ring Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Big Rings");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Platform Rings");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Big Ring Neons");
        }

        if (customPlatform.hideBackColumns)
        {
        }

        if (customPlatform.hideBackLasers)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Back Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Back Lasers");
        }

        if (customPlatform.hideDoubleLasers)
        {
        }

        if (customPlatform.hideDoubleColorLasers)
        {
        }

        if (customPlatform.hideRotatingLasers)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Left Rotating Lasers");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Right Rotating Lasers");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Left Rotating Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Right Rotating Lights");
        }

        if (customPlatform.hideTrackLights)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Center Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Road Lights");
        }
    }

    private void ReplaceBetterBlack(GameObject gameObject)
    {
        var renderers = gameObject.GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;

            if (materials != null)
            {
                var replaced = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].name != null &&
                        (materials[i].name.StartsWith("BetterBlack") || materials[i].name.StartsWith("_dark_replace")))
                    {
                        materials[i] = useThisBlack;
                        replaced = true;
                    }
                }

                if (replaced) renderer.gameObject.GetComponent<Renderer>().sharedMaterials = materials;
            }
        }
    }

    private void RemoveHiddenElementsFromEnvironmentRecursive(GameObject gameObject, string name,
        List<string> keepIfChildren = null)
    {
        if (gameObject == null) return;

        if (gameObject.name.Equals(name))
        {
            if (keepIfChildren != null)
            {
                var hasChild = false;
                foreach (Transform t in gameObject.transform)
                {
                    if (keepIfChildren.Contains(t.gameObject.name))
                        hasChild = true;
                }

                if (hasChild)
                {
                    foreach (Transform t in gameObject.transform)
                    {
                        if (!keepIfChildren.Contains(t.gameObject.name))
                            HideGameObjectRecursive(t.gameObject);
                    }
                }
                else
                {
                    HideGameObjectRecursive(gameObject);
                }
            }
            else
            {
                HideGameObjectRecursive(gameObject);
            }
        }
        else
        {
            foreach (Transform t in gameObject.transform)
                RemoveHiddenElementsFromEnvironmentRecursive(t.gameObject, name);
        }
    }

    private void DisableElementsFromEnvironmentRecursive(GameObject gameObject, string name)
    {
        if (gameObject == null) return;

        if (gameObject.name.Equals(name))
        {
            gameObject.SetActive(false);
        }
        else
        {
            foreach (Transform t in gameObject.transform)
                DisableElementsFromEnvironmentRecursive(t.gameObject, name);
        }
    }

    private void HideGameObjectRecursive(GameObject gameObject)
    {
        var renderer = gameObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.enabled = false;
        }
        else
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
        }
    }

    public Dictionary<string, PlatformInfo> GetAllEnvironments() => customPlatformSettings.CustomPlatformsDictionary;

    public List<string> GetAllEnvironmentIds() =>
        CustomPlatformSettings.Instance.CustomPlatformsDictionary.Keys.ToList();

    public int GetEnvironmentIdByPlatform(string platform) =>
        CustomPlatformSettings.Instance.CustomPlatformsDictionary.Keys.ToList().IndexOf(platform);

    private CustomPlatform FindCustomPlatformScript(GameObject prefab)
    {
        if (prefab is null) return null;
        return prefab.GetComponentInChildren<CustomPlatform>();
    }

    private void SetRings(GameObject gameObject, TrackRings trackRings, int ringCount)
    {
        TrackLaneRingsManager ringManager;
        //BigRing
        if (gameObject.name.ToLower().Contains("big") || gameObject.name.ToLower().Contains("outer") ||
            gameObject.name.ToLower().Equals("rings"))
        {
            if (platformDescriptor.BigRingManager != null)
            {
                foreach (var obj in platformDescriptor.BigRingManager.GetToDestroy())
                    Destroy(obj);
            }

            platformDescriptor.BigRingManager = gameObject.AddComponent<TrackLaneRingsManager>();
            if (platformDescriptor.RotationController == null)
                platformDescriptor.RotationController = gameObject.AddComponent<GridRotationController>();
            if (platformDescriptor.BigRingManager is TrackLaneRingsManager tlrm)
                ringManager = tlrm;
            else
                ringManager = null;
        }
        else
        {
            if (platformDescriptor.SmallRingManager != null)
            {
                Destroy(platformDescriptor.SmallRingManager.RotationEffect);
                Destroy(platformDescriptor.SmallRingManager);
            }

            platformDescriptor.SmallRingManager = gameObject.AddComponent<TrackLaneRingsManager>();


            if (platformDescriptor.RotationController == null)
                platformDescriptor.RotationController = gameObject.AddComponent<GridRotationController>();
            ringManager = platformDescriptor.SmallRingManager;
        }

        if (ringManager == null)
            return;

        //Also overwrite LightsManager if applicable
        var tubeRingLights = trackRings.trackLaneRingPrefab.GetComponentsInChildren<TubeLight>();
        foreach (var tubeLight in tubeRingLights)
        {
            var eventId = -1;
            switch (tubeLight.lightsID)
            {
                case LightsID.Static:
                    eventId = (int)EventTypeValue.BackLasers;
                    break;
                case LightsID.BackLights:
                    eventId = (int)EventTypeValue.BackLasers;
                    break;
                case LightsID.BigRingLights:
                    eventId = (int)EventTypeValue.RingLights;
                    break;
                case LightsID.LeftLasers:
                    eventId = (int)EventTypeValue.LeftLasers;
                    break;
                case LightsID.RightLasers:
                    eventId = (int)EventTypeValue.RightLasers;
                    break;
                case LightsID.TrackAndBottom:
                    eventId = (int)EventTypeValue.CenterLights;
                    break;
                case LightsID.Unused5:
                    eventId = (int)EventTypeValue.ColorBoost;
                    break;
                case LightsID.Unused6:
                    eventId = (int)EventTypeValue.ExtraLeftLights;
                    break;
                case LightsID.Unused7:
                    eventId = (int)EventTypeValue.ExtraRightLights;
                    break;
                case LightsID.Unused10:
                    eventId = (int)EventTypeValue.ExtraLeftLasers;
                    break;
                case LightsID.Unused11:
                    eventId = (int)EventTypeValue.ExtraRightLasers;
                    break;
            }

            if (eventId > 0)
            {
                var currentLightsManager = platformDescriptor.LightingManagers[eventId];
                var newLightsManager = gameObject.AddComponent<LightsManager>();

                newLightsManager.ControllingLights = currentLightsManager.ControllingLights;
                newLightsManager.RotatingLights = currentLightsManager.RotatingLights;
                newLightsManager.GroupLightsBasedOnZ();

                Destroy(currentLightsManager);


                platformDescriptor.LightingManagers[eventId] = newLightsManager;
                break;
            }
        }

        if (tubeRingLights.Length == 0)
        {
            var tubeLightsManager = platformDescriptor.LightingManagers[(int)EventTypeValue.RingLights];
            var meshRenderers = trackRings.trackLaneRingPrefab.GetComponentsInChildren<MeshRenderer>();

            foreach (var renderer in meshRenderers) SetRendererMaterials(renderer, tubeLightsManager);

            var newLightsManager = gameObject.AddComponent<LightsManager>();

            newLightsManager.ControllingLights = tubeLightsManager.ControllingLights;
            newLightsManager.RotatingLights = tubeLightsManager.RotatingLights;
            newLightsManager.GroupLightsBasedOnZ();

            Destroy(tubeLightsManager);
            platformDescriptor.LightingManagers[(int)EventTypeValue.RingLights] = newLightsManager;
        }

        //LightsManager lm = pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS];
        ReplaceBetterBlack(trackRings.trackLaneRingPrefab);
        SetLightingEventsForTubeLights(trackRings.trackLaneRingPrefab);

        var tlr = trackRings.trackLaneRingPrefab.AddComponent<TrackLaneRing>();
        ringManager.Prefab = tlr;

        ringManager.RingCount = trackRings.ringCount;
        if (trackRings.useStepEffect)
        {
            ringManager.MINPositionStep = trackRings.minPositionStep;
            ringManager.MAXPositionStep = trackRings.maxPositionStep;
        }
        else
        {
            ringManager.MINPositionStep = ringManager.MAXPositionStep = trackRings.ringPositionStep;
        }

        ringManager.MoveSpeed = trackRings.moveSpeed;
        ringManager.RotationStep = trackRings.rotationStep;
        ringManager.PropagationSpeed = Mathf.RoundToInt(trackRings.rotationPropagationSpeed);
        ringManager.FlexySpeed = trackRings.rotationFlexySpeed;

        if (trackRings.useRotationEffect)
        {
            var rotationEffect = gameObject.AddComponent<TrackLaneRingsRotationEffect>();
            ringManager.RotationEffect = rotationEffect;

            rotationEffect.Manager = ringManager;
            rotationEffect.StartupRotationAngle = trackRings.startupRotationAngle;
            rotationEffect.StartupRotationStep = trackRings.startupRotationStep;
            rotationEffect.StartupRotationPropagationSpeed =
                Mathf.RoundToInt(trackRings.startupRotationPropagationSpeed);
            rotationEffect.StartupRotationFlexySpeed = trackRings.startupRotationFlexySpeed;
        }
    }
}
