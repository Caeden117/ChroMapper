using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using SimpleJSON;

public class CreateEventTypeLabels : MonoBehaviour {

    public TMP_FontAsset AvailableAsset;
    public TMP_FontAsset UtilityAsset;
    public TMP_FontAsset RedAsset;
    public GameObject LayerInstantiate;
    public Transform[] EventGrid;
    [SerializeField] private DarkThemeSO darkTheme;
    public RotationCallbackController RotationCallback;
    private bool loadedWithRotationEvents = false;
    [HideInInspector] public int NoRotationLaneOffset => loadedWithRotationEvents || RotationCallback.IsActive ? 0 : -2;

    private LightsManager[] LightingManagers;

    private readonly List<LaneInfo> laneObjs = new List<LaneInfo>();

	// Use this for initialization
	void Start () {
        loadedWithRotationEvents = BeatSaberSongContainer.Instance.map._events.Any(i => i.IsRotationEvent);
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
	}

    public void UpdateLabels(EventsContainer.PropMode propMode, int eventType, int lanes = 16)
    {
        foreach (Transform children in LayerInstantiate.transform.parent.transform)
        {
            if (children.gameObject.activeSelf)
                Destroy(children.gameObject);
        }
        laneObjs.Clear();

        for (int i = 0; i < lanes; i++)
        {
            int modified = (propMode == EventsContainer.PropMode.Off ? EventTypeToModifiedType(i) : i) + NoRotationLaneOffset;
            if (modified < 0 && propMode == EventsContainer.PropMode.Off) continue;

            var laneInfo = new LaneInfo(i, propMode != EventsContainer.PropMode.Off ? i : modified);

            GameObject instantiate = Instantiate(LayerInstantiate, LayerInstantiate.transform.parent);
            instantiate.SetActive(true);
            instantiate.transform.localPosition = new Vector3(propMode != EventsContainer.PropMode.Off ? i : modified, 0, 0);
            laneObjs.Add(laneInfo);

            try
            {
                TextMeshProUGUI textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                if (propMode != EventsContainer.PropMode.Off)
                {
                    textMesh.font = UtilityAsset;
                    if (i == 0)
                    {
                        textMesh.text = "All Lights";
                        textMesh.font = RedAsset;
                    }
                    else
                    {
                        textMesh.text = $"{LightingManagers[eventType].name} ID {i}";
                        if (i % 2 == 0)
                            textMesh.font = UtilityAsset;
                        else
                            textMesh.font = AvailableAsset;
                    }
                }
                else
                {
                    switch (i)
                    {
                        case MapEvent.EVENT_TYPE_RINGS_ROTATE:
                            textMesh.font = UtilityAsset;
                            textMesh.text = "Ring Rotation";
                            break;
                        case MapEvent.EVENT_TYPE_RINGS_ZOOM:
                            textMesh.font = UtilityAsset;
                            textMesh.text = "Ring Zoom";
                            break;
                        case MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED:
                            textMesh.text = "Left Laser Speed";
                            textMesh.font = UtilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED:
                            textMesh.text = "Right Laser Speed";
                            textMesh.font = UtilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_EARLY_ROTATION:
                            textMesh.text = "Rotation (Include)";
                            textMesh.font = UtilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_LATE_ROTATION:
                            textMesh.text = "Rotation (Exclude)";
                            textMesh.font = UtilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_BOOST_LIGHTS:
                            textMesh.text = "Boost Lights";
                            textMesh.font = UtilityAsset;
                            break;
                        default:
                            if (LightingManagers.Length > i)
                            {
                                LightsManager customLight = LightingManagers[i];
                                textMesh.text = customLight?.name;
                                textMesh.font = AvailableAsset;
                            }
                            else
                            {
                                Destroy(textMesh);
                                laneObjs.Remove(laneInfo);
                            }
                            break;
                    }
                    if (Settings.Instance.DarkTheme)
                    {
                        textMesh.font = darkTheme.TekoReplacement;
                    }
                }
                laneInfo.Name = textMesh.text;
            }
            catch { }
        }

        laneObjs.Sort();
    }

    void PlatformLoaded(PlatformDescriptor descriptor)
    {
        LightingManagers = descriptor.LightingManagers;

        UpdateLabels(EventsContainer.PropMode.Off, MapEvent.EVENT_TYPE_RING_LIGHTS);
    }

    void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    }

    public int LaneIdToEventType(int laneId)
    {
        return laneObjs[laneId].Type;
    }

    public int EventTypeToLaneId(int eventType)
    {
        return laneObjs.FindIndex(it => it.Type == eventType);
    }

    public int? LightIdsToPropId(int type, int[] lightID)
    {
        if (type >= LightingManagers.Length)
            return null;

        return LightingManagers[type].ControllingLights.Find(x => lightID.Contains(x.lightID))?.propGroup;
    }

    public int[] PropIdToLightIds(int type, int propID)
    {
        if (type >= LightingManagers.Length)
            return new int[0];

        return LightingManagers[type].ControllingLights.Where(x => x.propGroup == propID).Select(x => x.lightID).OrderBy(x => x).Distinct().ToArray();
    }
    
    public JSONArray PropIdToLightIdsJ(int type, int propID)
    {
        var result = new JSONArray();
        foreach (var lightingEvent in PropIdToLightIds(type, propID))
        {
            result.Add(lightingEvent);
        }
        return result;
    }

    public int EditorToLightID(int type, int lightID)
    {
        return LightingManagers[type].LightIDPlacementMap[lightID];
    }

    public int LightIDToEditor(int type, int lightID)
    {
        if (LightingManagers[type].LightIDPlacementMapReverse.ContainsKey(lightID))
        {
            return LightingManagers[type].LightIDPlacementMapReverse[lightID];
        }
        return -1;
    }

    private static int[] ModifiedToEventArray = { 14, 15, 0, 1, 2, 3, 4, 8, 9, 12, 13, 5, 6, 7, 10, 11 };
    private static int[] EventToModifiedArray = { 2, 3, 4, 5, 6, 11, 12, 13, 7, 8, 14, 15, 9, 10, 0, 1 };

    /// <summary>
    /// Turns an eventType to a modified type for organizational purposes in the Events Grid.
    /// </summary>
    /// <param name="eventType">Type usually found in a MapEvent object.</param>
    /// <returns></returns>
    public static int EventTypeToModifiedType(int eventType)
    {
        if (BeatmapEventContainer.ModifyTypeMode == -1) return eventType;
        if (BeatmapEventContainer.ModifyTypeMode == 0)
        {
            if (!EventToModifiedArray.Contains(eventType))
            {
                Debug.LogWarning($"Event Type {eventType} does not have a modified type");
                return eventType;
            }
            return EventToModifiedArray[eventType];
        }
        else if (BeatmapEventContainer.ModifyTypeMode == 1)
            switch (eventType)
            {
                case 5: return 1;
                case 1: return 2;
                case 6: return 3;
                case 2: return 4;
                case 7: return 5;
                case 3: return 6;
                case 10: return 7;
                case 4: return 8;
                case 11: return 9;
                case 8: return 10;
                case 9: return 11;
                default: return eventType;
            }
        return -1;
    }

    /// <summary>
    /// Turns a modified type to an event type to be stored in a MapEvent object.
    /// </summary>
    /// <param name="modifiedType">Modified type (Usually from EventPreview)</param>
    /// <returns></returns>
    public static int ModifiedTypeToEventType(int modifiedType)
    {
        if (BeatmapEventContainer.ModifyTypeMode == -1) return modifiedType;
        if (BeatmapEventContainer.ModifyTypeMode == 0)
        {
            if (!ModifiedToEventArray.Contains(modifiedType))
            {
                Debug.LogWarning($"Event Type {modifiedType} does not have a valid event type! WTF!?!?");
                return modifiedType;
            }
            return ModifiedToEventArray[modifiedType];
        }
        else if (BeatmapEventContainer.ModifyTypeMode == 1)
            switch (modifiedType)
            {
                case 1: return 5;
                case 2: return 1;
                case 3: return 6;
                case 4: return 2;
                case 5: return 7;
                case 6: return 3;
                case 7: return 10;
                case 8: return 4;
                case 9: return 11;
                case 10: return 8;
                case 11: return 9;
                default: return modifiedType;
            }
        return -1;
    }

    public int MaxLaneId()
    {
        return laneObjs.Count - 1;
    }
}
