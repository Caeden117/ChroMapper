using CustomFloorPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static CustomFloorPlugin.TubeLight;
using static System.Collections.Generic.Dictionary<string, string>;

public class CustomPlatformsLoader : MonoBehaviour
{
    private static CustomPlatformsLoader _instance;
    public static CustomPlatformsLoader Instance => _instance ?? (_instance = Load());

    private CustomPlatformSettings customPlatformSettings = CustomPlatformSettings.Instance;
    private List<string> platformsOnly = new List<string>();
    private List<string> environmentsOnly = new List<string>();

    private int lightManagersCount = 0;
    private Dictionary<string, LightsManager> customLightsManager = new Dictionary<string, LightsManager>();

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
                break;
            }
                
        }


        RemoveHiddenElementsFromEnvironment(defaultEnvironmentInstance, environmentCP);

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
                    break;
                }
            }

            RemoveHiddenElementsFromEnvironment(defaultEnvironmentInstance, platformCP);

            GameObject customPlatformInstance = Instantiate(customPlatform, LoadInitialMap.PlatformOffset, Quaternion.identity);
            customPlatformInstance.transform.SetParent(defaultEnvironmentInstance.transform);
            foreach (GameObject g in customPlatforms)
            {
                GameObject customPlatformInstance2 = Instantiate(g, LoadInitialMap.PlatformOffset, Quaternion.identity);
                if (g != customPlatform)
                {
                    customPlatformInstance2.transform.SetParent(customPlatformInstance.transform);
                }
            }
        }

        if (defaultEnvironment != null)
        {
            GameObject customEnvironmentInstance = Instantiate(customEnvironment, LoadInitialMap.PlatformOffset, Quaternion.identity);
            customEnvironmentInstance.transform.SetParent(defaultEnvironmentInstance.transform);
            foreach (GameObject g in customEnvironments)
            {
                GameObject customEnvironmentInstance2 = Instantiate(g, LoadInitialMap.PlatformOffset, Quaternion.identity);
                if (g != customEnvironment)
                {
                    customEnvironmentInstance2.transform.SetParent(customEnvironmentInstance.transform);
                }
            }

            ReplaceBetterBlack(defaultEnvironmentInstance);

            FindInGOs(defaultEnvironmentInstance);

            return defaultEnvironmentInstance;
        }


        return customEnvironment;
    }

    private void RemoveHiddenElementsFromEnvironment(GameObject environment, CustomPlatform customPlatform)
    {
        if (customPlatform.hideHighway)
        {
            //Debug.Log("Destroy hideHighway");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Floor", new List<string> { "PlayersPlace" });
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Legs");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Top");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Platform");
        }
        if (customPlatform.hideTowers)
        {
            //Debug.Log("Destroy hideTowers");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "RocketCar");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "RocketCar (1)");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Pillars Object");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "BG");
        }
        if (customPlatform.hideDefaultPlatform)
        {
            //Debug.Log("Destroy hideDefaultPlatform");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "PlayersPlace");
        }
        if (customPlatform.hideEQVisualizer)
        {
            //Debug.Log("Destroy hideEQVisualizer");
        }
        if (customPlatform.hideSmallRings)
        {
            //Debug.Log("Destroy hideSmallRings");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Small Rings");
        }
        if (customPlatform.hideBigRings)
        {
            //Debug.Log("Destroy hideBigRings");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Big Ring Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Big Rings");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Platform Rings");
        }
        if (customPlatform.hideBackColumns)
        {
            //Debug.Log("Destroy hideBackColumns");
        }
        if (customPlatform.hideBackLasers)
        {
            //Debug.Log("Destroy hideBackLasers");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Back Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Back Lasers");
        }
        if (customPlatform.hideDoubleLasers)
        {
            //Debug.Log("Destroy hideDoubleLasers");
        }
        if (customPlatform.hideDoubleColorLasers)
        {
            //Debug.Log("Destroy hideDoubleColorLasers");
        }
        if (customPlatform.hideRotatingLasers)
        {
            //Debug.Log("Destroy hideRotatingLasers");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Left Rotating Lasers");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Right Rotating Lasers");
        }
        if (customPlatform.hideTrackLights)
        {
            //Debug.Log("Destroy hideTrackLights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Center Lights");
            RemoveHiddenElementsFromEnvironmentRecursive(environment, "Road Lights");
        }
    }

    Material lightMaterial = Resources.Load("ControllableLight", typeof(Material)) as Material; 
    Material useThisBlack = Resources.Load("Basic Black", typeof(Material)) as Material;

    private void ReplaceBetterBlack(GameObject gameObject)
    {
        if (gameObject.GetComponents<Renderer>().Length > 0)
        {
            Material[] materials = gameObject.GetComponent<Renderer>().materials;

            if (materials != null)
            {
                bool replaced = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i].name.StartsWith("BetterBlack") || materials[i].name.StartsWith("_dark_replace"))
                    {
                        Debug.Log("Replacing BetterBlack!!!");
                        materials[i] = useThisBlack;
                        replaced = true;
                    }
                }
                if (replaced)
                {
                    gameObject.GetComponent<Renderer>().materials = materials;
                }
            }
        }

        foreach (Transform t in gameObject.transform)
        {
            ReplaceBetterBlack(t.gameObject);
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
                            //Debug.Log("Destroy => "+t.gameObject.name);
                            DestroyImmediate(t.gameObject, true);
                        }
                    }
                }
                else
                {
                    //Debug.Log("Destroy => " + gameObject.name);
                    DestroyImmediate(gameObject, true);
                }
            }
            else
            {
                //Debug.Log("Destroy => " + gameObject.name);
                DestroyImmediate(gameObject, true);
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
        return FindCustomPlatformScriptRecursive(prefab);
    }

    private CustomPlatform FindCustomPlatformScriptRecursive(GameObject g)
    {
        if (g.GetComponents<CustomPlatform>().Length > 0)
        {
            CustomPlatform cP = g.GetComponents<CustomPlatform>()[0];
            /*Debug.Log("Found CustomPlatform " + cP.platName + " by " + cP.platAuthor);
            if (cP.hideHighway) { Debug.Log("hideHighway"); }
            if (cP.hideTowers) { Debug.Log("hideTowers"); }
            if (cP.hideDefaultPlatform) { Debug.Log("hideDefaultPlatform"); }
            if (cP.hideEQVisualizer) { Debug.Log("hideEQVisualizer"); }
            if (cP.hideSmallRings) { Debug.Log("hideSmallRings"); }
            if (cP.hideBigRings) { Debug.Log("hideBigRings"); }
            if (cP.hideBackColumns) { Debug.Log("hideBackColumns"); }
            if (cP.hideBackLasers) { Debug.Log("hideBackLasers"); }
            if (cP.hideDoubleLasers) { Debug.Log("hideDoubleLasers"); }
            if (cP.hideDoubleColorLasers) { Debug.Log("hideDoubleColorLasers"); }
            if (cP.hideRotatingLasers) { Debug.Log("hideRotatingLasers"); }
            if (cP.hideTrackLights) { Debug.Log("hideTrackLights"); }*/
            return cP;
        }
        foreach (Transform childT in g.transform)
        {
            CustomPlatform cP = FindCustomPlatformScriptRecursive(childT.gameObject);
            if (cP != null)
            {
                return cP;
            }
        }
        return null;
    }



    private void SetLightingEventOnLowestRecursive(Transform transform, LightsManager lm)
    {
        if (transform.childCount > 0)
        {
            foreach (Transform t in transform.transform)
            {
                SetLightingEventOnLowestRecursive(t, lm);
            }
        }
        else
        {
            //Debug.Log("Add LightingEvent");
            Material[] materials = transform.gameObject.GetComponentInChildren<Renderer>().materials;
            Array.Resize<Material>(ref materials, materials.Length + 1);
            if (materials[0] != null)
            {
                materials[1] = materials[0];
            }
            materials[0] = lightMaterial;
            transform.gameObject.GetComponentInChildren<Renderer>().materials = materials;

            LightingEvent le = transform.gameObject.AddComponent<LightingEvent>();
            le.LightMaterial = lightMaterial;
            lm.ControllingLights.Add(le);
        }
    }

    private void SetRotatingLightsOnSecondLowestRecursive(Transform transform)
    {
        if (transform.childCount > 0)
        {
            foreach (Transform t in transform.transform)
            {
                SetRotatingLightsOnSecondLowestRecursive(t);
            }
        }
        else
        {
            transform.parent.gameObject.AddComponent<RotatingLights>();
            RotatingLights rl = transform.parent.gameObject.GetComponent<RotatingLights>();
            rl.multiplier = 20;
        }
    }

    int go_count = 0, components_count = 0, missing_count = 0;
    private void FindInGOs(GameObject go)
    {
        lightManagersCount = 0;
        customLightsManager.Clear();
        FindInGO(go);
    }

    private void SetBigRings(Transform t, TrackRings trackRings = null)
    {
        if (trackRings == null)
        {
            LightsManager lm = t.gameObject.AddComponent<LightsManager>();
            t.gameObject.AddComponent<TrackLaneRingsManager>(); //Set Prefab to first child, RingCount? 15? Min/Max Pos? 7.5? MoveSpeed? 1? RotStep? 5? PropaSpeed? 1? FlexySpeed? 1? RotationEffect? self? PlatformRings 5 10 10 1 5 1 1
            t.gameObject.AddComponent<TrackLaneRingsRotationEffect>(); //Manager? self? RotAngle? 45? RotStep? 5? RotProp?1? RotFLex? 1? PlatformRings 0 10 10 0.5

            TrackLaneRingsManager btlrm = t.gameObject.GetComponent<TrackLaneRingsManager>();
            TrackLaneRingsRotationEffect btlrre = t.gameObject.GetComponent<TrackLaneRingsRotationEffect>();
            TrackLaneRing btlr = null;
            PlatformDescriptor pd = t.gameObject.GetComponentInParent<PlatformDescriptor>();
            pd.BigRingManager = btlrm;
            pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS] = lm;
            lightManagersCount++;
            customLightsManager.Add(t.gameObject.name, lm);
            Debug.Log("Adding LightsManager Nr. " + lightManagersCount);

            foreach (Transform tr in t.transform)
            {
                tr.gameObject.AddComponent<TrackLaneRing>();
                btlr = tr.gameObject.GetComponent<TrackLaneRing>();

                SetLightingEventOnLowestRecursive(tr.transform, lm);
            }

            btlrm.prefab = btlr;
            btlrre.manager = btlrm;

            if (t.gameObject.name.Replace(" ", "").ToLower() == "platformrings")
            {
                btlrm.ringCount = 5;
                btlrm.minPositionStep = 10;
                btlrm.maxPositionStep = 10;

                btlrre.startupRotationAngle = 0;
                btlrre.startupRotationStep = 10;
                btlrre.startupRotationPropagationSpeed = 10;
                btlrre.startupRotationFlexySpeed = 0.5F;
            }
            else
            {
                btlrm.ringCount = 15;
                btlrm.minPositionStep = 7.5F;
                btlrm.maxPositionStep = 7.5F;

                btlrre.startupRotationAngle = 45;
                btlrre.startupRotationStep = 5;
                btlrre.startupRotationPropagationSpeed = 1;
                btlrre.startupRotationFlexySpeed = 1;
            }
            btlrm.moveSpeed = 1;
            btlrm.rotationStep = 5;
            btlrm.propagationSpeed = 1;
            btlrm.flexySpeed = 1;
        }
        else
        {
            //Get component from parent
            LightsManager lm = t.gameObject.GetComponentInParent<LightsManager>();
            PlatformDescriptor pd = t.gameObject.GetComponentInParent<PlatformDescriptor>();
            if (pd.BigRingManager == null)
                pd.BigRingManager = t.gameObject.AddComponent<TrackLaneRingsManager>();
            if (pd.RotationController == null)
                pd.RotationController = t.gameObject.AddComponent<GridRotationController>();
            t.gameObject.AddComponent<TrackLaneRingsRotationEffect>();
            TrackLaneRingsManager btlrm = pd.BigRingManager;
            TrackLaneRingsRotationEffect btlrre = t.gameObject.GetComponent<TrackLaneRingsRotationEffect>();
            btlrm.rotationEffect = btlrre;
            btlrre.manager = btlrm;

            if (pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS] == null)
            {
                if (lm == null)
                {
                    lm = t.gameObject.transform.parent.gameObject.AddComponent<LightsManager>();
                }
                pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS] = lm;
                lightManagersCount++;
                Debug.Log("Adding LightsManager Nr. " + lightManagersCount);
                customLightsManager.Add(t.gameObject.name, lm);
            }
            else
            {
                lm = pd.LightingManagers[MapEvent.EVENT_TYPE_RING_LIGHTS];
                Debug.Log("Adding Lights to last Manager");
            }
            
            btlrm.ringCount = trackRings.ringCount;
            btlrm.minPositionStep = trackRings.minPositionStep;
            btlrm.maxPositionStep = trackRings.maxPositionStep;

            btlrre.startupRotationAngle = trackRings.startupRotationAngle;
            btlrre.startupRotationStep = trackRings.startupRotationStep;
            btlrre.startupRotationPropagationSpeed = trackRings.startupRotationPropagationSpeed;
            btlrre.startupRotationFlexySpeed = trackRings.startupRotationFlexySpeed;
            btlrm.moveSpeed = trackRings.moveSpeed;
            btlrm.rotationStep = trackRings.rotationStep;
            btlrm.propagationSpeed = trackRings.rotationPropagationSpeed;
            btlrm.flexySpeed = trackRings.rotationFlexySpeed;

            TrackLaneRing btlr = null;
            int i = 0;
            foreach (Transform tr in t.transform)
            {
                TrackLaneRing tlr = tr.gameObject.AddComponent<TrackLaneRing>();
                if (btlrm.prefab == null)
                    btlrm.prefab = tlr;

                if (i > 0)
                {
                    DestroyImmediate(tr.gameObject, true);
                }

                SetLightingEventOnLowestRecursive(tr.transform, lm);
                i++;
            }
        }
    }

    private void FindInGO(GameObject g, GameObject attachLightManagerTo = null)
    {
        if (!g.activeSelf)
            return;

        bool platformDescriptorDepthReached = false;
        if (g.GetComponents<CustomPlatform>().Length > 0)
        {
            Debug.Log("Found CustomPlatform");
            platformDescriptorDepthReached = true;
        }

        go_count++;
        //Debug.Log("GameObject: " + g.name);

        if (g.GetComponents<CustomPlatform>().Length > 0)
        {
            //CustomPlatform cP = g.GetComponents<CustomPlatform>()[0];
            /*Debug.Log("Found CustomPlatform " + cP.platName + " by " + cP.platAuthor);
            if (cP.hideHighway) { Debug.Log("hideHighway"); }
            if (cP.hideTowers) { Debug.Log("hideTowers"); }
            if (cP.hideDefaultPlatform) { Debug.Log("hideDefaultPlatform"); }
            if (cP.hideEQVisualizer) { Debug.Log("hideEQVisualizer"); }
            if (cP.hideSmallRings) { Debug.Log("hideSmallRings"); }
            if (cP.hideBigRings) { Debug.Log("hideBigRings"); }
            if (cP.hideBackColumns) { Debug.Log("hideBackColumns"); }
            if (cP.hideBackLasers) { Debug.Log("hideBackLasers"); }
            if (cP.hideDoubleLasers) { Debug.Log("hideDoubleLasers"); }
            if (cP.hideDoubleColorLasers) { Debug.Log("hideDoubleColorLasers"); }
            if (cP.hideRotatingLasers) { Debug.Log("hideRotatingLasers"); }
            if (cP.hideTrackLights) { Debug.Log("hideTrackLights"); }*/
        }

        if (g.GetComponents<TubeLight>().Length > 0)
        {
            TubeLight tl = g.GetComponent<TubeLight>();
            //Debug.Log("FOUND TUBELIGHT!!!");
            LightsManager lm = null;
            PlatformDescriptor pd = g.GetComponentInParent<PlatformDescriptor>();

            if (attachLightManagerTo != null)
            {
                if (attachLightManagerTo.GetComponents<LightsManager>().Length == 0)
                {
                    /*foreach (KeyValuePair<string, LightsManager> entry in customLightsManager)
                    {
                        if (entry.Key.StartsWith(g.name) || attachLightManagerTo.name.StartsWith(entry.Key))
                        {
                            lm = entry.Value;
                            Debug.Log("Reusing LightsManager due to name similarity "+ entry.Key +" and "+ attachLightManagerTo.name);
                        }
                    };*/

                    if (lm == null)
                    {
                        int eventId = -MapEvent.EVENT_TYPE_BACK_LASERS;
                        switch (tl.lightsID)
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
                                //SetRotatingLightsOnSecondLowestRecursive
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
                                break;
                        }
                        if (pd.LightingManagers[eventId] == null)
                        {
                            lm = attachLightManagerTo.AddComponent<LightsManager>();
                            pd.LightingManagers[lightManagersCount] = lm;
                            lightManagersCount++;
                            customLightsManager.Add(attachLightManagerTo.gameObject.name, lm);
                            Debug.Log("Adding LightsManager Nr. " + lightManagersCount);
                        }
                        else
                        {
                            lm = pd.LightingManagers[eventId];
                            Debug.Log("Adding Lights to existing Manager");
                        }
                    }
                }
                else
                {
                    lm = attachLightManagerTo.GetComponent<LightsManager>();
                }

                SetLightingEventOnLowestRecursive(g.transform, lm);
            }
            else
            {
                if (g.transform.childCount == 0)
                {
                    Debug.Log("No children");

                    /*if (g.transform.parent.gameObject.GetComponents<LightsManager>().Length == 0)
                    {
                        lm = g.transform.parent.gameObject.AddComponent<LightsManager>();
                    }
                    else
                    {
                        lm = g.transform.parent.gameObject.GetComponent<LightsManager>();
                    }*/
                    Material[] materials = g.GetComponentInChildren<Renderer>().materials;
                    Array.Resize<Material>(ref materials, materials.Length + 1);
                    materials[materials.Length - 1] = lightMaterial;
                    g.GetComponentInChildren<Renderer>().materials = materials;
                    LightingEvent le = g.AddComponent<LightingEvent>();
                    le.LightMaterial = lightMaterial;
                    lm?.ControllingLights.Add(le);
                }
                else
                {
                    lm = g.AddComponent<LightsManager>();
                    SetLightingEventOnLowestRecursive(g.transform, lm);
                }
            }
        }

        if (g.GetComponents<TrackRings>().Length > 0)
        {
            TrackRings tl = g.GetComponent<TrackRings>();
            Debug.Log("FOUND TrackRings!!!");
            SetBigRings(g.transform, tl);
        }

        if (g.GetComponents<SongEventHandler>().Length > 0)
        {
            Debug.Log("FOUND SongEventHandler!!!");
        }

        // Unusable because the class is private, is it even required?
        /*if (g.GetComponents<RotationEventEffect>().Length > 0)
        {
            Debug.Log("FOUND SongEventHandler!!!");
            t.gameObject.AddComponent<LightsManager>();
            SetLightingEventOnLowestRecursive(t);
            SetRotatingLightsOnSecondLowestRecursive(t);
        }*/



            if (g.GetComponents<Spectrogram>().Length > 0)
        {
            Debug.Log("FOUND Spectrogram!!!");
        }

        Component[] components = g.GetComponents<Component>();
        //Debug.Log("Check components: " + components.Length);
        for (int i = 0; i < components.Length; i++)
        {
            components_count++;
            if (components[i] == null)
            {
                missing_count++;
                string s = g.name;
                Transform t = g.transform;
                while (t.parent != null)
                {
                    s = t.parent.name + "/" + s;
                    t = t.parent;
                }
                Debug.Log(s + " has an empty script attached in position: " + i, g);
            }
        }
        // Now recurse through each child GO (if there are any):
        //Debug.Log("Check Transform: " + g.transform.childCount);
        foreach (Transform childT in g.transform)
        {
            //Debug.Log("Searching " + childT.name  + " " );
            FindInGO(childT.gameObject, attachLightManagerTo != null ? attachLightManagerTo : platformDescriptorDepthReached ? childT.gameObject : null);
        }
    }

    private void SetupPlatformRecursive(Transform transform)
    {

        foreach (Transform t in transform)
        {
            Debug.Log("Component name: " + t.gameObject.name + " with " + t.childCount.ToString() + " childrens");

            LightsManager lm = null;

            switch (t.gameObject.name.Replace(" ", "").ToLower())
            {
                case "rotatinglaserleft":
                case "rotatinglasersleft":
                case "leftrotatinglasers":
                case "leftrotatinglaser":
                    lm = t.gameObject.AddComponent<LightsManager>();
                    SetLightingEventOnLowestRecursive(t, lm);
                    SetRotatingLightsOnSecondLowestRecursive(t);
                    break;
                case "rotatinglaserright":
                case "rotatinglasersright":
                case "rightrotatinglasers":
                case "rightrotatinglaser":
                    lm = t.gameObject.AddComponent<LightsManager>();
                    SetLightingEventOnLowestRecursive(t, lm);
                    SetRotatingLightsOnSecondLowestRecursive(t);
                    break;
                case "lasers":
                case "extralights":
                    foreach (Transform tr in t.transform)
                    {
                        tr.gameObject.AddComponent<LightsManager>();
                    }
                    break;
                case "doublecolorlasers":
                case "centerlights":
                    lm = t.gameObject.AddComponent<LightsManager>();
                    SetLightingEventOnLowestRecursive(t, lm);
                    break;
                case "followingchevron":
                    lm = t.gameObject.AddComponent<LightsManager>();
                    t.gameObject.AddComponent<GridRotationController>();
                    SetLightingEventOnLowestRecursive(t, lm);
                    break;
                case "roadlights":
                case "pillarlights":
                case "largebacklights":
                case "backlights":
                case "backlasers":
                case "backlaser":
                case "lasersback":
                case "laserback":
                    lm = t.gameObject.AddComponent<LightsManager>();
                    SetLightingEventOnLowestRecursive(t.transform, lm);
                    break;
                case "smalltracklanerings":
                case "smallrings":
                    if (t.childCount == 1)
                    {
                        lm = t.gameObject.GetComponentInParent<LightsManager>();
                        t.gameObject.AddComponent<TrackLaneRingsManager>(); // 32 1 4 1 5 1 1
                        t.gameObject.AddComponent<TrackLaneRingsRotationEffect>();  // 45 5 1 1

                        TrackLaneRingsManager stlrm = t.gameObject.GetComponent<TrackLaneRingsManager>();
                        TrackLaneRingsRotationEffect stlrre = t.gameObject.GetComponent<TrackLaneRingsRotationEffect>();
                        TrackLaneRing stlr = null;
                        foreach (Transform tr in t.transform)
                        {
                            tr.gameObject.AddComponent<TrackLaneRing>();
                            stlr = tr.gameObject.GetComponent<TrackLaneRing>();
                            SetLightingEventOnLowestRecursive(tr, lm);
                        }

                        stlrm.prefab = stlr;
                        stlrm.ringCount = 32;
                        stlrm.minPositionStep = 1;
                        stlrm.maxPositionStep = 4;
                        stlrm.moveSpeed = 1;
                        stlrm.rotationStep = 5;
                        stlrm.propagationSpeed = 1;
                        stlrm.flexySpeed = 1;

                        stlrre.manager = stlrm;
                        stlrre.startupRotationAngle = 45;
                        stlrre.startupRotationStep = 5;
                        stlrre.startupRotationPropagationSpeed = 1;
                        stlrre.startupRotationFlexySpeed = 1;
                    }
                    else
                    {
                        foreach (Transform tr in t.transform)
                        {
                            lm = t.gameObject.AddComponent<LightsManager>();
                            SetLightingEventOnLowestRecursive(tr, lm);
                        }
                    }

                    break;
                case "platformrings":
                case "bigtracklanerings":
                case "bigrings":
                    SetBigRings(t);



                    break;
                default:
                    SetupPlatformRecursive(t);
                    break;
            }
        }
    }
}
