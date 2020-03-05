using CustomFloorPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using static CustomFloorPlugin.TubeLight;
using static System.Collections.Generic.Dictionary<string, string>;

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

    private int lightManagersCount = 0;
    private Dictionary<string, LightsManager> customLightsManager = new Dictionary<string, LightsManager>();

    public void Init()
    {
    }

    private string CustomPlatformsFolder
    {
        get { return Settings.Instance.CustomPlatformsFolder; }
    }

    private static CustomPlatformsLoader Load()
    {
        CustomPlatformsLoader cpl = new CustomPlatformsLoader();

        //PlatformsOnly
        foreach (string platformName in cpl.GetAllEnvironmentIds())
        {
            GameObject platform = cpl.LoadPlatform(platformName);

            CustomPlatform cp = cpl.FindCustomPlatformScript(platform);

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

                //Rings
                int ringCount = 0;
                TrackRings[] trackRings = defaultEnvironmentInstance.GetComponentsInChildren<TrackRings>();
                foreach (TrackRings trackRing in trackRings)
                {
                    SetRings(trackRing.gameObject, trackRing, ringCount);
                    ringCount++;
                }

                //TubeLights
                PlatformDescriptor pd = defaultEnvironmentInstance.GetComponentInParent<PlatformDescriptor>();

                SetLightingEventsForTubeLights(defaultEnvironmentInstance, pd);

                return defaultEnvironmentInstance;
            }


            return customEnvironment;
        }
        catch (Exception e)
        {
            return Instantiate(defaultEnvironment, LoadInitialMap.PlatformOffset, Quaternion.identity);
        }
    }

    private void SetLightingEventsForTubeLights(GameObject gameObject, PlatformDescriptor pd)
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
                default:
                    //Unused 5 6 7 10 11 14 15
                    Debug.Log("Custom LightsID " + tubeLight.lightsID);
                    break;
            }

            LightsManager tubeLightsManager = pd.LightingManagers[eventId];

            MeshRenderer[] meshRenderers = tubeLight.gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in meshRenderers)
            {
                SetRendererMaterials(renderer, tubeLightsManager, tubeLight.width);
            }

            if (tubeLight.gameObject.GetComponent<MeshFilter>() != null)
            {
                MeshFilter mFilter = tubeLight.gameObject.GetComponent<MeshFilter>();
                if ((PrefabStageUtility.GetPrefabStage(mFilter.gameObject) == null) ? mFilter.sharedMesh == null : mFilter.mesh == null)
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

                    if (PrefabStageUtility.GetPrefabStage(mFilter.gameObject) == null)
                    {
                        tubeLight.gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
                    }
                    else
                    {
                        tubeLight.gameObject.GetComponent<MeshFilter>().mesh = mesh;
                    }
                }
            }
        }
    }

    private void SetShadersCorrectly(Renderer renderer)
    {
        Material[] materials = (PrefabStageUtility.GetPrefabStage(renderer.gameObject) == null) ? renderer.sharedMaterials : renderer.materials;

        if (materials.Length >= 1)
        {
            for (var i = 0; i < materials.Length; i++)
            {
                Material tempMaterial = materials[i];
                if ((tempMaterial?.shader?.name?.Contains("BeatSaber/Standard") ?? false) || (tempMaterial?.shader?.name?.Equals("Standard") ?? false))
                {
                    tempMaterial.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }
                materials[i] = tempMaterial;
            }
        }

        if (PrefabStageUtility.GetPrefabStage(renderer.gameObject) == null)
        {
            renderer.sharedMaterials = materials;
        }
        else
        {
            renderer.materials = materials;
        }
    }

    private void SetRendererMaterials(Renderer renderer, LightsManager lightsManager = null, float width = 1f)
    {
        Material[] materials = (PrefabStageUtility.GetPrefabStage(renderer.gameObject) == null) ? renderer.sharedMaterials : renderer.materials;

        if (materials.Length >= 1 && materials[0] != null)
        {
            if (materials[0] != null && (width >= 0.5f))
                Array.Resize<Material>(ref materials, materials.Length + 1);

            Material lastMaterial = lightMaterial;
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
            materials[0] = lightMaterial;
        }

        if (PrefabStageUtility.GetPrefabStage(renderer.gameObject) == null)
        {
            renderer.sharedMaterials = materials;
        }
        else
        {
            renderer.materials = materials;
        }

        if (lightsManager != null)
        {
            LightingEvent le = renderer.gameObject.AddComponent<LightingEvent>();
            le.LightMaterial = lightMaterial;
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
        }
        if (customPlatform.hideTowers)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "RocketCar");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "RocketCar (1)");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Pillars Object");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "BG");
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
        }
        if (customPlatform.hideTrackLights)
        {
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Center Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Road Lights");
        }
    }

    Material lightMaterial = Resources.Load("ControllableLight", typeof(Material)) as Material;

    Material useThisBlack = Resources.Load("Basic Black", typeof(Material)) as Material;

    private void ReplaceBetterBlack(GameObject gameObject)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

        foreach(Renderer renderer in renderers)
        {
            Material[] materials = null;

            if (PrefabStageUtility.GetPrefabStage(renderer.gameObject) == null)
            {
                materials = renderer.sharedMaterials;
            }
            else
            {
                materials = renderer.materials;
            }

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
                    if (PrefabStageUtility.GetPrefabStage(renderer.gameObject) == null)
                    {
                        renderer.gameObject.GetComponent<Renderer>().sharedMaterials = materials;
                    }
                    else
                    {
                        renderer.gameObject.GetComponent<Renderer>().materials = materials;
                    }
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

    public Dictionary<string, string> GetAllEnvironments()
    {
        Dictionary<string, string> environmentsOnly = new Dictionary<string, string>();

        return customPlatformSettings.CustomPlatformsDictionary;
    }

    public List<string> GetAllEnvironmentIds()
    {
        List<string> environmentIds = new List<string>();

        return CustomPlatformSettings.Instance.CustomPlatformsDictionary.Keys.ToList();
    }

    public int GetEnvironmentIdByPlatform(string platform)
    {
        return CustomPlatformSettings.Instance.CustomPlatformsDictionary.Keys.ToList().IndexOf(platform);
    }

    public List<string> GetPlatformOnlyEnvironments()
    {
        return platformsOnly;
    }

    public Dictionary<string, string> GetPlatformOnlyEnvironmentsWithHash()
    {
        Dictionary<string, string> envs = customPlatformSettings.CustomPlatformsDictionary;
        List<string> toRemove = new List<string>();
        foreach (string s in envs.Keys)
        {
            if (!environmentsOnly.Contains(s))
            {
                toRemove.Add(s);
            }
        }
        foreach (string r in toRemove)
        {
            envs.Remove(r);
        }

        return envs;
    }

    public List<string> GetEnvironments()
    {
        return environmentsOnly;
    }

    public Dictionary<string, string> GetEnvironmentsWithHash()
    {
        Dictionary<string, string> envs = customPlatformSettings.CustomPlatformsDictionary;
        List<string> toRemove = new List<string>();
        foreach (string s in envs.Keys)
        {
            if (!environmentsOnly.Contains(s))
            {
                toRemove.Add(s);
            }
        }
        foreach (string r in toRemove)
        {
            envs.Remove(r);
        }

        return envs;
    }

    CustomPlatform FindCustomPlatformScript(GameObject prefab)
    {
        return prefab.GetComponentInChildren<CustomPlatform>();
    }

    private void SetRings(GameObject gameObject, TrackRings trackRings, int ringCount)
    {
        PlatformDescriptor pd = gameObject.GetComponentInParent<PlatformDescriptor>();

        TrackLaneRingsManager ringManager = null;
        TrackLaneRingsRotationEffect rotationEffect = null;
        //BigRing
        if (gameObject.name.ToLower().Contains("big") || gameObject.name.ToLower().Contains("outer") || gameObject.name.ToLower().Equals("rings"))
        {
            if (pd.BigRingManager != null)
            {
                Destroy(pd.BigRingManager.rotationEffect);
                Destroy(pd.BigRingManager);
            }
                
            pd.BigRingManager = gameObject.AddComponent<TrackLaneRingsManager>();
            if (pd.RotationController == null)
                pd.RotationController = gameObject.AddComponent<GridRotationController>(); 
            ringManager = pd.BigRingManager;
        }
        else
        {
            if (pd.SmallRingManager != null)
            {
                Destroy(pd.SmallRingManager.rotationEffect);
                Destroy(pd.SmallRingManager);
            }
                
            pd.SmallRingManager = gameObject.AddComponent<TrackLaneRingsManager>();
            
                
            if (pd.RotationController == null)
                pd.RotationController = gameObject.AddComponent<GridRotationController>();
            ringManager = pd.SmallRingManager;
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
                default:
                    break;
            }

            if (eventId > 0)
            {
                LightsManager currentLightsManager = pd.LightingManagers[eventId];
                LightsManager newLightsManager = gameObject.AddComponent<LightsManager>();

                newLightsManager.ControllingLights = currentLightsManager.ControllingLights;
                newLightsManager.RotatingLights = currentLightsManager.RotatingLights;

                Destroy(currentLightsManager);


                pd.LightingManagers[eventId] = newLightsManager;
                break;
            }
        }

        if (tubeRingLights.Length == 0)
        {
            LightsManager tubeLightsManager = pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS];
            MeshRenderer[] meshRenderers = trackRings.trackLaneRingPrefab.GetComponentsInChildren<MeshRenderer>();
            
            foreach (MeshRenderer renderer in meshRenderers)
            {
                SetRendererMaterials(renderer, tubeLightsManager);
            }

            LightsManager newLightsManager = gameObject.AddComponent<LightsManager>();

            newLightsManager.ControllingLights = tubeLightsManager.ControllingLights;
            newLightsManager.RotatingLights = tubeLightsManager.RotatingLights;

            Destroy(tubeLightsManager);
            pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS] = newLightsManager;
        }

        rotationEffect = gameObject.AddComponent<TrackLaneRingsRotationEffect>();

        //LightsManager lm = pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS];
        ReplaceBetterBlack(trackRings.trackLaneRingPrefab);
        SetLightingEventsForTubeLights(trackRings.trackLaneRingPrefab, pd);

        TrackLaneRing tlr = trackRings.trackLaneRingPrefab.gameObject.AddComponent<TrackLaneRing>();
        ringManager.prefab = tlr;

        ringManager.ringCount = trackRings.ringCount;
        ringManager.minPositionStep = trackRings.minPositionStep;
        ringManager.maxPositionStep = trackRings.maxPositionStep;
        ringManager.moveSpeed = trackRings.moveSpeed;
        ringManager.rotationStep = trackRings.rotationStep;
        ringManager.propagationSpeed = trackRings.rotationPropagationSpeed;
        ringManager.flexySpeed = trackRings.rotationFlexySpeed;
        ringManager.rotationEffect = rotationEffect;
        ringManager.ringPositionStep = trackRings.ringPositionStep;

        rotationEffect.manager = ringManager;
        rotationEffect.startupRotationAngle = trackRings.startupRotationAngle;
        rotationEffect.startupRotationStep = trackRings.startupRotationStep;
        rotationEffect.startupRotationPropagationSpeed = trackRings.startupRotationPropagationSpeed;
        rotationEffect.startupRotationFlexySpeed = trackRings.startupRotationFlexySpeed;
    }
}
