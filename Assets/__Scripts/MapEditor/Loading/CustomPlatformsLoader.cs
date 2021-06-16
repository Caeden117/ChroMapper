using CustomFloorPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CustomFloorPlugin.TubeLight;

//TODO: Worklog
// - Rotating Lights >> Not yet fully supported by CustomPlatform
// - Increase track Lanes if ringCount > 15
// - Spectrogram
// - Support 'Unused' Lights Id

public class CustomPlatformsLoader : MonoBehaviour
{
    private static CustomPlatformsLoader _instance;
    public static CustomPlatformsLoader Instance => _instance ?? (_instance = Load());

    private CustomPlatformSettings customPlatformSettings = CustomPlatformSettings.Instance;
    private List<string> platformsOnly = new List<string>();
    private List<string> environmentsOnly = new List<string>();

    private PlatformDescriptor platformDescriptor;

    private int lightManagersCount = 0;
    private Dictionary<string, LightsManager> customLightsManager = new Dictionary<string, LightsManager>();

    public void Init()
    {
    }

    private void Awake()
    {
        //Always create new INSTANCES of materials. Or else you'd modify the actual file itself, and cause changes in Git.
        lightMaterial = Resources.Load("ControllableLight", typeof(Material)) as Material;

        useThisBlack = new Material(Resources.Load("Basic Black", typeof(Material)) as Material);
    }

    private static CustomPlatformsLoader Load()
    {
        CustomPlatformsLoader cpl = new GameObject("Custom Platforms Loader").AddComponent<CustomPlatformsLoader>();
        DontDestroyOnLoad(cpl.gameObject);

        //PlatformsOnly
        foreach (string platformName in cpl.GetAllEnvironmentIds())
        {
            GameObject platform = cpl.LoadPlatform(platformName);

            CustomPlatform cp = cpl?.FindCustomPlatformScript(platform);

            if (cp != null)
            {
                if (!cp.hideHighway && !cp.hideTowers && cp.hideDefaultPlatform && !cp.hideEQVisualizer && !cp.hideSmallRings && !cp.hideBigRings && !cp.hideBackColumns && !cp.hideBackLasers && !cp.hideDoubleLasers && !cp.hideDoubleColorLasers && !cp.hideRotatingLasers && !cp.hideTrackLights)
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

    public GameObject LoadPlatform(string customEnvironmentString, GameObject defaultEnvironment = null, string customPlatformString = null)
    {
        try
        {
            GameObject defaultEnvironmentInstance = null;
            if (defaultEnvironment != null)
            {
                defaultEnvironmentInstance = Instantiate(defaultEnvironment, LoadInitialMap.PlatformOffset, Quaternion.identity);
                platformDescriptor = defaultEnvironmentInstance.GetComponentInParent<PlatformDescriptor>();
            }
            GameObject[] customEnvironments = customPlatformSettings.LoadPlatform(customEnvironmentString);
            GameObject customEnvironment = null;
            CustomPlatform environmentCP = null;
            foreach (GameObject g in customEnvironments)
            {
                environmentCP = FindCustomPlatformScript(g);
                if (environmentCP != null)
                {
                    customEnvironment = g;
                    RemoveHiddenElementsFromEnvironment(defaultEnvironmentInstance, environmentCP);
                    break;
                }

            }

            GameObject[] customPlatforms = null;
            GameObject customPlatform = null;
            if (customPlatformString != null)
            {
                customPlatforms = customPlatformSettings.LoadPlatform(customPlatformString);

                CustomPlatform platformCP = null;
                foreach (GameObject g in customPlatforms)
                {
                    platformCP = FindCustomPlatformScript(g);
                    if (platformCP != null)
                    {
                        customPlatform = g;
                        RemoveHiddenElementsFromEnvironment(defaultEnvironmentInstance, platformCP);
                        break;
                    }
                }

                GameObject customPlatformInstance = Instantiate(customPlatform, LoadInitialMap.PlatformOffset, Quaternion.identity);
                DisableElementsFromEnvironmentRecursive(customPlatformInstance, "Camera");
                customPlatformInstance.transform.SetParent(defaultEnvironmentInstance.transform);
                foreach (GameObject g in customPlatforms)
                {

                    if (g != customPlatform)
                    {
                        GameObject customPlatformInstance2 = Instantiate(g, LoadInitialMap.PlatformOffset, Quaternion.identity);
                        customPlatformInstance2.transform.SetParent(customPlatformInstance.transform);
                    }
                }
            }

            if (defaultEnvironment != null)
            {
                GameObject customEnvironmentInstance = Instantiate(customEnvironment, LoadInitialMap.PlatformOffset, Quaternion.identity);
                DisableElementsFromEnvironmentRecursive(customEnvironmentInstance, "Camera");
                customEnvironmentInstance.transform.SetParent(defaultEnvironmentInstance.transform);
                foreach (GameObject g in customEnvironments)
                {

                    if (g != customEnvironment)
                    {
                        GameObject customEnvironmentInstance2 = Instantiate(g, LoadInitialMap.PlatformOffset, Quaternion.identity);
                        customEnvironmentInstance2.transform.SetParent(customEnvironmentInstance.transform);
                    }
                }

                ReplaceBetterBlack(defaultEnvironmentInstance);
                foreach (Renderer renderer in defaultEnvironmentInstance.GetComponentsInChildren<Renderer>())
                    SetShadersCorrectly(renderer);

                //Set LightsManager Size correctly
                SetLightsManagerSize(defaultEnvironmentInstance);
                platformDescriptor.RefreshLightingManagers();

                //Rings
                int ringCount = 0;
                TrackRings[] trackRings = defaultEnvironmentInstance.GetComponentsInChildren<TrackRings>();
                foreach (TrackRings trackRing in trackRings)
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
        TubeLight[] tubeLights = gameObject.GetComponentsInChildren<TubeLight>();
        int maxSize = platformDescriptor.LightingManagers.Length;
        foreach (TubeLight tubeLight in tubeLights)
        {
            switch (tubeLight.lightsID)
            {
                case LightsID.Unused5:
                    maxSize = Math.Max(maxSize, MapEvent.EVENT_TYPE_BOOST_LIGHTS + 1);
                    break;
                case LightsID.Unused6:
                    maxSize = Math.Max(maxSize, MapEvent.EVENT_TYPE_CUSTOM_LIGHT_2 + 1);
                    break;
                case LightsID.Unused7:
                    maxSize = Math.Max(maxSize, MapEvent.EVENT_TYPE_CUSTOM_LIGHT_3 + 1);
                    break;
                case LightsID.Unused10:
                    maxSize = Math.Max(maxSize, MapEvent.EVENT_TYPE_CUSTOM_LIGHT_4 + 1);
                    break;
                case LightsID.Unused11:
                    maxSize = Math.Max(maxSize, MapEvent.EVENT_TYPE_CUSTOM_LIGHT_5 + 1);
                    break;
                default:
                    break;
            }
        }

        if (maxSize != platformDescriptor.LightingManagers.Length)
        {
            Array.Resize(ref platformDescriptor.LightingManagers, maxSize);
        }
    }

    private void SetLightingEventsForTubeLights(GameObject gameObject)
    {
        TubeLight[] tubeLights = gameObject.GetComponentsInChildren<TubeLight>();
        foreach (TubeLight tubeLight in tubeLights)
        {
            if (tubeLight.gameObject.GetComponent<LightingEvent>() != null)
            {
                continue;
            }

            int eventId = -MapEvent.EVENT_TYPE_BACK_LASERS;
            switch (tubeLight.lightsID)
            {
                case LightsID.Static:
                    eventId = MapEvent.EVENT_TYPE_BACK_LASERS;
                    break;
                case LightsID.BackLights:
                    eventId = MapEvent.EVENT_TYPE_BACK_LASERS;
                    break;
                case LightsID.BigRingLights:
                    eventId = MapEvent.EVENT_TYPE_RING_LIGHTS;
                    break;
                case LightsID.LeftLasers:
                    eventId = MapEvent.EVENT_TYPE_LEFT_LASERS;
                    break;
                case LightsID.RightLasers:
                    eventId = MapEvent.EVENT_TYPE_RIGHT_LASERS;
                    break;
                case LightsID.TrackAndBottom:
                    eventId = MapEvent.EVENT_TYPE_ROAD_LIGHTS;
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
                    eventId = MapEvent.EVENT_TYPE_BOOST_LIGHTS;
                    break;
                case LightsID.Unused6:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_2;
                    break;
                case LightsID.Unused7:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_3;
                    break;
                case LightsID.Unused10:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_4;
                    break;
                case LightsID.Unused11:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_5;
                    break;
                default:
                    //Unused 5 6 7 10 11 14 15
                    Debug.Log("Custom LightsID " + tubeLight.lightsID);
                    break;
            }

            LightsManager tubeLightsManager = platformDescriptor.LightingManagers[eventId];
            if (tubeLightsManager == null)
            {
                tubeLightsManager = tubeLight.transform.parent.gameObject.AddComponent<LightsManager>();
                tubeLightsManager.disableCustomInitialization = true;
                platformDescriptor.LightingManagers[eventId] = tubeLightsManager;
            }

            MeshRenderer[] meshRenderers = tubeLight.gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in meshRenderers)
            {
                SetRendererMaterials(renderer, tubeLightsManager, tubeLight.width);
            }

            if (tubeLight.gameObject.GetComponent<MeshFilter>() != null)
            {
                MeshFilter mFilter = tubeLight.gameObject.GetComponent<MeshFilter>();
                if (mFilter.sharedMesh == null)
                {
                    Vector3 cubeCenter = Vector3.up * (0.5f - tubeLight.center) * tubeLight.length;

                    Vector3 cubeSize = new Vector3(2 * tubeLight.width, tubeLight.length, 2 * tubeLight.width);
                    Mesh mesh = new Mesh();
                    mesh.name = "ScriptGenerated";

                    Vector3[] vertices = {
                        new Vector3 (0, 0, 0),
                        new Vector3 (tubeLight.width, 0, 0),
                        new Vector3 (tubeLight.width, tubeLight.length, 0),
                        new Vector3 (0, tubeLight.length, 0),
                        new Vector3 (0, tubeLight.length, tubeLight.width),
                        new Vector3 (tubeLight.width, tubeLight.length, tubeLight.width),
                        new Vector3 (tubeLight.width, 0, tubeLight.width),
                        new Vector3 (0, 0, tubeLight.width),
                    };

                    int[] triangles = {
                        0, 2, 1, //face front
	                    0, 3, 2,
                        2, 3, 4, //face top
	                    2, 4, 5,
                        1, 2, 5, //face right
	                    1, 5, 6,
                        0, 7, 4, //face left
	                    0, 4, 3,
                        5, 4, 7, //face back
	                    5, 7, 6,
                        0, 6, 7, //face bottom
	                    0, 1, 6
                    };

                    mesh.vertices = vertices;
                    mesh.triangles = triangles;

                    Color[] colors = new Color[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        colors[i] = tubeLight.color;
                    }
                    mesh.colors = colors;

                    Vector3 offset = tubeLight.transform.position - tubeLight.transform.TransformPoint(mesh.bounds.center);
                    tubeLight.transform.position = tubeLight.transform.position + offset;

                    tubeLight.gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                }
            }
        }

        SongEventHandler[] eventHandlers = gameObject.GetComponentsInChildren<SongEventHandler>();
        foreach (SongEventHandler eventHandler in eventHandlers)
        {
            if (eventHandler.gameObject.GetComponent<LightingEvent>() != null)
            {
                continue;
            }

            int eventId = (int)eventHandler.eventType;

            LightsManager tubeLightsManager = platformDescriptor.LightingManagers[eventId];
            if (tubeLightsManager == null)
            {
                tubeLightsManager = eventHandler.transform.parent.gameObject.AddComponent<LightsManager>();
                tubeLightsManager.disableCustomInitialization = true;
                platformDescriptor.LightingManagers[eventId] = tubeLightsManager;
            }

            Renderer[] meshRenderers = eventHandler.gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in meshRenderers)
            {
                SetRendererMaterials(renderer, tubeLightsManager, 1);
            }
        }
    }

    private void SetShadersCorrectly(Renderer renderer)
    {
        Material[] materials = renderer.sharedMaterials;

        if (materials.Length >= 1)
        {
            for (var i = 0; i < materials.Length; i++)
            {
                Material tempMaterial = materials[i];
                if ((tempMaterial?.shader?.name?.Contains("BeatSaber/Standard") ?? false) || (tempMaterial?.shader?.name?.Equals("Standard") ?? false))
                {
                    tempMaterial.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }
                if (tempMaterial?.name.ToUpper().Contains("GLOW_BLUE") ?? false)
                {
                    tempMaterial = new Material(lightMaterial);
                    tempMaterial.SetColor("_BaseColor", Color.white);
                    tempMaterial.EnableKeyword("_EMISSION");
                    tempMaterial.SetColor("_EmissionColor", BeatSaberSong.DEFAULT_RIGHTCOLOR * LightsManager.HDR_Intensity);
                }
                if (tempMaterial?.name.ToUpper().Contains("GLOW_RED") ?? false)
                {
                    tempMaterial = new Material(lightMaterial);
                    tempMaterial.SetColor("_BaseColor", Color.white);
                    tempMaterial.EnableKeyword("_EMISSION");
                    tempMaterial.SetColor("_EmissionColor", BeatSaberSong.DEFAULT_LEFTCOLOR * LightsManager.HDR_Intensity);
                }
                materials[i] = tempMaterial;
            }
        }

        renderer.sharedMaterials = materials;
    }

    private void SetRendererMaterials(Renderer renderer, LightsManager lightsManager = null, float width = 1f)
    {
        Material[] materials = renderer.sharedMaterials;

        if (materials.Length >= 1 && materials[0] != null)
        {
            if (materials[0] != null && (width >= 0.5f))
                Array.Resize<Material>(ref materials, materials.Length + 1);

            Material lastMaterial = new Material(lightMaterial);
            for (var i = 0; i < materials.Length; i++)
            {
                Material tempMaterial = materials[i];
                if ((tempMaterial?.shader?.name?.Equals("Unlit/Color") ?? false) && !(tempMaterial.color.r == 1 && tempMaterial.color.g == 1 && tempMaterial.color.b == 1))
                    tempMaterial = useThisBlack;
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
            LightingEvent le = renderer.gameObject.AddComponent<LightingEvent>();
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
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "PlayersPlace");
        }
        if (customPlatform.hideEQVisualizer)
        {
        }
        if (customPlatform.hideSmallRings)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Small Rings");
        }
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

    //Always create new INSTANCES of materials. Or else you'd modify the actual file itself, and cause changes in Git.
    Material lightMaterial = null;

    Material useThisBlack = null;

    private void ReplaceBetterBlack(GameObject gameObject)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            if (materials != null)
            {
                bool replaced = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].name != null && (materials[i].name.StartsWith("BetterBlack") || materials[i].name.StartsWith("_dark_replace")))
                    {
                        materials[i] = useThisBlack;
                        replaced = true;
                    }
                }
                if (replaced)
                {
                    renderer.gameObject.GetComponent<Renderer>().sharedMaterials = materials;
                }
            }
        }
    }

    private void RemoveHiddenElementsFromEnvironmentRecursive(GameObject gameObject, string name, List<string> keepIfChildren = null)
    {
        if (gameObject == null)
        {
            return;
        }

        if (gameObject.name.Equals(name))
        {
            if (keepIfChildren != null)
            {
                bool hasChild = false;
                foreach (Transform t in gameObject.transform)
                {
                    if (keepIfChildren.Contains(t.gameObject.name))
                    {
                        hasChild = true;
                    }
                }

                if (hasChild)
                {
                    foreach (Transform t in gameObject.transform)
                    {
                        if (!keepIfChildren.Contains(t.gameObject.name))
                        {
                            HideGameObjectRecursive(t.gameObject);
                        }
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
            {
                RemoveHiddenElementsFromEnvironmentRecursive(t.gameObject, name);
            }
        }
    }

    private void DisableElementsFromEnvironmentRecursive(GameObject gameObject, string name)
    {
        if (gameObject == null)
        {
            return;
        }

        if (gameObject.name.Equals(name))
        {
            gameObject.SetActive(false);
        }
        else
        {
            foreach (Transform t in gameObject.transform)
            {
                DisableElementsFromEnvironmentRecursive(t.gameObject, name);
            }
        }
    }

    void HideGameObjectRecursive(GameObject gameObject)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.enabled = false;
        }
        else
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }
        }
    }

    public Dictionary<string, PlatformInfo> GetAllEnvironments()
    {
        return customPlatformSettings.CustomPlatformsDictionary;
    }

    public List<string> GetAllEnvironmentIds()
    {
        return CustomPlatformSettings.Instance.CustomPlatformsDictionary.Keys.ToList();
    }

    public int GetEnvironmentIdByPlatform(string platform)
    {
        return CustomPlatformSettings.Instance.CustomPlatformsDictionary.Keys.ToList().IndexOf(platform);
    }

    CustomPlatform FindCustomPlatformScript(GameObject prefab)
    {
        if (prefab is null) return null;
        return prefab?.GetComponentInChildren<CustomPlatform>() ?? null;
    }

    private void SetRings(GameObject gameObject, TrackRings trackRings, int ringCount)
    {
        TrackLaneRingsManager ringManager;
        //BigRing
        if (gameObject.name.ToLower().Contains("big") || gameObject.name.ToLower().Contains("outer") || gameObject.name.ToLower().Equals("rings"))
        {
            if (platformDescriptor.BigRingManager != null)
            {
                foreach (var obj in platformDescriptor.BigRingManager.GetToDestroy())
                {
                    Destroy(obj);
                }
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
                Destroy(platformDescriptor.SmallRingManager.rotationEffect);
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
        TubeLight[] tubeRingLights = trackRings.trackLaneRingPrefab.GetComponentsInChildren<TubeLight>();
        foreach (TubeLight tubeLight in tubeRingLights)
        {
            int eventId = -1;
            switch (tubeLight.lightsID)
            {
                case LightsID.Static:
                    eventId = MapEvent.EVENT_TYPE_BACK_LASERS;
                    break;
                case LightsID.BackLights:
                    eventId = MapEvent.EVENT_TYPE_BACK_LASERS;
                    break;
                case LightsID.BigRingLights:
                    eventId = MapEvent.EVENT_TYPE_RING_LIGHTS;
                    break;
                case LightsID.LeftLasers:
                    eventId = MapEvent.EVENT_TYPE_LEFT_LASERS;
                    break;
                case LightsID.RightLasers:
                    eventId = MapEvent.EVENT_TYPE_RIGHT_LASERS;
                    break;
                case LightsID.TrackAndBottom:
                    eventId = MapEvent.EVENT_TYPE_ROAD_LIGHTS;
                    break;
                case LightsID.Unused5:
                    eventId = MapEvent.EVENT_TYPE_BOOST_LIGHTS;
                    break;
                case LightsID.Unused6:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_2;
                    break;
                case LightsID.Unused7:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_3;
                    break;
                case LightsID.Unused10:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_4;
                    break;
                case LightsID.Unused11:
                    eventId = MapEvent.EVENT_TYPE_CUSTOM_LIGHT_5;
                    break;
                default:
                    break;
            }

            if (eventId > 0)
            {
                LightsManager currentLightsManager = platformDescriptor.LightingManagers[eventId];
                LightsManager newLightsManager = gameObject.AddComponent<LightsManager>();

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
            LightsManager tubeLightsManager = platformDescriptor.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS];
            MeshRenderer[] meshRenderers = trackRings.trackLaneRingPrefab.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in meshRenderers)
            {
                SetRendererMaterials(renderer, tubeLightsManager);
            }

            LightsManager newLightsManager = gameObject.AddComponent<LightsManager>();

            newLightsManager.ControllingLights = tubeLightsManager.ControllingLights;
            newLightsManager.RotatingLights = tubeLightsManager.RotatingLights;
            newLightsManager.GroupLightsBasedOnZ();

            Destroy(tubeLightsManager);
            platformDescriptor.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS] = newLightsManager;
        }

        //LightsManager lm = pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS];
        ReplaceBetterBlack(trackRings.trackLaneRingPrefab);
        SetLightingEventsForTubeLights(trackRings.trackLaneRingPrefab);

        TrackLaneRing tlr = trackRings.trackLaneRingPrefab.gameObject.AddComponent<TrackLaneRing>();
        ringManager.prefab = tlr;

        ringManager.ringCount = trackRings.ringCount;
        if (trackRings.useStepEffect)
        {
            ringManager.minPositionStep = trackRings.minPositionStep;
            ringManager.maxPositionStep = trackRings.maxPositionStep;
        }
        else
        {
            ringManager.minPositionStep = ringManager.maxPositionStep = trackRings.ringPositionStep;
        }
        ringManager.moveSpeed = trackRings.moveSpeed;
        ringManager.rotationStep = trackRings.rotationStep;
        ringManager.propagationSpeed = Mathf.RoundToInt(trackRings.rotationPropagationSpeed);
        ringManager.flexySpeed = trackRings.rotationFlexySpeed;

        if (trackRings.useRotationEffect)
        {
            TrackLaneRingsRotationEffect rotationEffect = gameObject.AddComponent<TrackLaneRingsRotationEffect>();
            ringManager.rotationEffect = rotationEffect;

            rotationEffect.manager = ringManager;
            rotationEffect.startupRotationAngle = trackRings.startupRotationAngle;
            rotationEffect.startupRotationStep = trackRings.startupRotationStep;
            rotationEffect.startupRotationPropagationSpeed = Mathf.RoundToInt(trackRings.startupRotationPropagationSpeed);
            rotationEffect.startupRotationFlexySpeed = trackRings.startupRotationFlexySpeed;
        }
    }
}
