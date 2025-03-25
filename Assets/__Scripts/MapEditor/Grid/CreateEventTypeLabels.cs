using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using TMPro;
using UnityEngine;

public class CreateEventTypeLabels : MonoBehaviour
{
    private static readonly int[] modifiedToEventArray = { 14, 15, 0, 1, 2, 3, 4, 8, 9, 12, 13, 5, 6, 7, 10, 11 };
    private static readonly int[] eventToModifiedArray = { 2, 3, 4, 5, 6, 11, 12, 13, 7, 8, 14, 15, 9, 10, 0, 1 };

    private static readonly int[] eventToModifiedArrayInterscope =
    {
        5, 2, 4, 3, 6, 13, 7, 8, 9, 10, 16, 17, 11, 12, 0, 1, 14, 15
    };
    
    private static readonly int[] eventToModifiedArrayGaga =
    {
        9, 10, 5, 6, 2, 11, 4, 7, 18, 19, 3, 8, 14, 15, 0, 1, 13, 16, 12, 17
    };
    public TMP_FontAsset AvailableAsset;
    public TMP_FontAsset UtilityAsset;
    public TMP_FontAsset RedAsset;
    public GameObject LayerInstantiate;
    public Transform[] EventGrid;
    [SerializeField] private DarkThemeSO darkTheme;
    public RotationCallbackController RotationCallback;

    private readonly List<LaneInfo> laneObjs = new List<LaneInfo>();

    private LightsManager[] lightingManagers;
    private bool loadedWithRotationEvents;
    [HideInInspector] public int NoRotationLaneOffset => loadedWithRotationEvents || RotationCallback.IsActive ? 0 : -2;

    // Use this for initialization
    private void Start()
    {
        loadedWithRotationEvents = BeatSaberSongContainer.Instance.Map.Events.Any(i => i.IsLaneRotationEvent());
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    }

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;

    public void UpdateLabels(EventGridContainer.PropMode propMode, int eventType, int lanes = 16)
    {
        foreach (Transform children in LayerInstantiate.transform.parent.transform)
        {
            if (children.gameObject.activeSelf)
                Destroy(children.gameObject);
        }

        laneObjs.Clear();

        for (var i = 0; i < lanes; i++)
        {
            var modified = (propMode == EventGridContainer.PropMode.Off ? EventTypeToModifiedType(i) : i) +
                           NoRotationLaneOffset;
            if (modified < 0 && propMode == EventGridContainer.PropMode.Off) continue;

            var laneInfo = new LaneInfo(i, propMode != EventGridContainer.PropMode.Off ? i : modified);

            var instantiate = Instantiate(LayerInstantiate, LayerInstantiate.transform.parent);
            instantiate.SetActive(true);
            instantiate.transform.localPosition =
                new Vector3(propMode != EventGridContainer.PropMode.Off ? i : modified, 0, 0);
            laneObjs.Add(laneInfo);

            try
            {
                var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                if (propMode != EventGridContainer.PropMode.Off)
                {
                    textMesh.font = UtilityAsset;
                    if (i == 0)
                    {
                        textMesh.text = "All Lights";
                        textMesh.font = RedAsset;
                    }
                    else
                    {
                        textMesh.text = $"{lightingManagers[eventType].name} ID {EditorToLightID(eventType, i - 1)}";
                        if (i % 2 == 0)
                            textMesh.font = UtilityAsset;
                        else
                            textMesh.font = AvailableAsset;
                    }
                }
                else
                {
                    var envIndex = BeatSaberSongContainer.Instance.MapDifficultyInfo.EnvironmentNameIndex;
                    var environment = BeatSaberSongContainer.Instance.Info.EnvironmentNames[envIndex];
                    var isGaga = environment == "GagaEnvironment";
                    switch (i)
                    {
                        case (int)EventTypeValue.RingRotation:
                            textMesh.font = UtilityAsset;
                            textMesh.text = "Ring Rotation";
                            break;
                        case (int)EventTypeValue.RingZoom:
                            textMesh.font = UtilityAsset;
                            textMesh.text = "Ring Zoom";
                            break;
                        case (int)EventTypeValue.LeftLaserRotation:
                            var txtlrr = !isGaga ? "Left Laser Speed" : "Tower 3 Height";
                            textMesh.text = txtlrr;
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.RightLaserRotation:
                            var txtrlr = !isGaga ? "Right Laser Speed" : "Tower 4 Height";
                            textMesh.text = txtrlr;
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.EarlyLaneRotation:
                            textMesh.text = "Rotation (Include)";
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.LateLaneRotation:
                            textMesh.text = "Rotation (Exclude)";
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.ColorBoost:
                            textMesh.text = "Boost Lights";
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.UtilityEvent0:
                            var txtue0 = !isGaga ? "Utility Event 0" : "Tower 2 Height";
                            textMesh.text = txtue0;
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.UtilityEvent1:
                            var txtue1 = !isGaga ? "Utility Event 1" : "Tower 5 Height";
                            textMesh.text = txtue1;
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.UtilityEvent2:
                            var txtue2 = !isGaga ? "Utility Event 2" : "Tower 1 Height";
                            textMesh.text = txtue2;
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.UtilityEvent3:
                            var txtue3 = !isGaga ? "Utility Event 3" : "Tower 6 Height";
                            textMesh.text = txtue3;
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.SpecialEvent0:
                            textMesh.text = "Special Event 0";
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.SpecialEvent1:
                            textMesh.text = "Special Event 1";
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.SpecialEvent2:
                            textMesh.text = "Special Event 2";
                            textMesh.font = UtilityAsset;
                            break;
                        case (int)EventTypeValue.SpecialEvent3:
                            textMesh.text = "Special Event 3";
                            textMesh.font = UtilityAsset;
                            break;
                        default:
                            if (lightingManagers.Length > i)
                            {
                                var customLight = lightingManagers[i];
                                if (customLight != null)
                                {
                                    textMesh.text = customLight.name;
                                    textMesh.font = AvailableAsset;
                                }
                            }
                            else
                            {
                                Destroy(textMesh);
                                laneObjs.Remove(laneInfo);
                            }

                            break;
                    }
                    if (Settings.Instance.DarkTheme) textMesh.font = darkTheme.TekoReplacement;
                }

                laneInfo.Name = textMesh.text;
            }
            catch { }
        }

        laneObjs.Sort();
    }

    private void PlatformLoaded(PlatformDescriptor descriptor) => lightingManagers = descriptor.LightingManagers;

    public int LaneIdToEventType(int laneId) => laneObjs[laneId].Type;

    public int EventTypeToLaneId(int eventType) => laneObjs.FindIndex(it => it.Type == eventType);

    public int? LightIdsToPropId(int type, int[] lightID)
    {
        if (type >= lightingManagers.Length) return null;

        var light = lightingManagers[type].ControllingLights.Find(x => Array.IndexOf(lightID, x.LightID) > -1);

        return light != null ? light.PropGroup : (int?)null;
    }

    public int[] PropIdToLightIds(int type, int propID)
    {
        if (type >= lightingManagers.Length)
            return new int[0];

        return lightingManagers[type].ControllingLights.Where(x => x.PropGroup == propID).Select(x => x.LightID)
            .OrderBy(x => x).Distinct().ToArray();
    }

    public JSONArray PropIdToLightIdsJ(int type, int propID)
    {
        var result = new JSONArray();
        foreach (var lightingEvent in PropIdToLightIds(type, propID)) result.Add(lightingEvent);
        return result;
    }

    public int EditorToLightID(int type, int lightID) => lightingManagers[type].LightIDPlacementMap[lightID];

    public int LightIDToEditor(int type, int lightID)
    {
        if (lightingManagers[type].LightIDPlacementMapReverse.ContainsKey(lightID))
            return lightingManagers[type].LightIDPlacementMapReverse[lightID];
        return -1;
    }

    /// <summary>
    ///     Turns an eventType to a modified type for organizational purposes in the Events Grid.
    /// </summary>
    /// <param name="eventType">Type usually found in a MapEvent object.</param>
    /// <returns></returns>
    public static int EventTypeToModifiedType(int eventType)
    {
        if (EventContainer.ModifyTypeMode == -1) return eventType;
        if (EventContainer.ModifyTypeMode == 0)
        {
            if (!eventToModifiedArray.Contains(eventType))
            {
                Debug.LogWarning($"Event Type {eventType} does not have a modified type");
                return eventType;
            }

            return eventToModifiedArray[eventType];
        }

        if (EventContainer.ModifyTypeMode == 1)
        {
            return eventType switch
            {
                5 => 1,
                1 => 2,
                6 => 3,
                2 => 4,
                7 => 5,
                3 => 6,
                10 => 7,
                4 => 8,
                11 => 9,
                8 => 10,
                9 => 11,
                _ => eventType,
            };
        }

        if (EventContainer.ModifyTypeMode == 2) return eventToModifiedArrayInterscope[eventType];
        if (EventContainer.ModifyTypeMode == 3) return eventToModifiedArrayGaga[eventType];

        return -1;
    }

    /// <summary>
    ///     Turns a modified type to an event type to be stored in a MapEvent object.
    /// </summary>
    /// <param name="modifiedType">Modified type (Usually from EventPreview)</param>
    /// <returns></returns>
    public static int ModifiedTypeToEventType(int modifiedType)
    {
        if (EventContainer.ModifyTypeMode == -1) return modifiedType;
        if (EventContainer.ModifyTypeMode == 0)
        {
            if (!modifiedToEventArray.Contains(modifiedType))
            {
                Debug.LogWarning($"Event Type {modifiedType} does not have a valid event type! WTF!?!?");
                return modifiedType;
            }

            return modifiedToEventArray[modifiedType];
        }

        if (EventContainer.ModifyTypeMode == 1)
        {
            return modifiedType switch
            {
                1 => 5,
                2 => 1,
                3 => 6,
                4 => 2,
                5 => 7,
                6 => 3,
                7 => 10,
                8 => 4,
                9 => 11,
                10 => 8,
                11 => 9,
                _ => modifiedType,
            };
        }

        return -1;
    }

    public int MaxLaneId() => laneObjs.Count - 1;
}
