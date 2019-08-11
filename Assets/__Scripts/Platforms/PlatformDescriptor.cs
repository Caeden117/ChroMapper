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
    public TrackLaneRingsManager BigRingManager;
    [Header("Lighting Groups")]
    [Tooltip("Manually map an Event ID (Index) to a group of lights (Parent GameObject)")]
    public GameObject[] LightingGroups = new GameObject[] { };
    public Color RedColor = Color.red;
    public Color BlueColor = new Color(0, 0.282353f, 1, 1);
    [Tooltip("-1 = No Sorting | 0 = Default Sorting | 1 = Collider Platform Special")]
    public int SortMode = 0;


    private BeatmapObjectCallbackController callbackController;

    private Dictionary<GameObject, Color[]> ChromaCustomColors = new Dictionary<GameObject, Color[]>();
    private Dictionary<GameObject, LightingEvent[]> GroupToEvents = new Dictionary<GameObject, LightingEvent[]>();

    void Start()
    {
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding") StartCoroutine(FindEventCallback());
    }

    void OnDestroy()
    {
        callbackController.EventPassedThreshold -= EventPassed;
    }

    IEnumerator FindEventCallback()
    {
        yield return new WaitUntil(() => GameObject.Find("Vertical Grid Callback"));
        callbackController = GameObject.Find("Vertical Grid Callback").GetComponent<BeatmapObjectCallbackController>();
        callbackController.EventPassedThreshold += EventPassed;
        yield return new WaitForSeconds(0.1f); //Give time for back rings to spawn. This is way too much time to give LUL
        foreach (GameObject group in LightingGroups)
            if (group != null) GroupToEvents.Add(group, group.GetComponentsInChildren<LightingEvent>());
    }

    void EventPassed(bool initial, int index, BeatmapObject obj)
    {
        MapEvent e = obj as MapEvent;
        switch (e._type) { //FUN PART BOIS
            case 8:
                BigRingManager?.HandleRotationEvent();
                SmallRingManager?.HandleRotationEvent();
                break;
            case 9:
                BigRingManager?.HandleRotationEvent();
                SmallRingManager?.HandlePositionEvent();
                break;
            case 12:
                foreach (RotatingLights l in LightingGroups[2].GetComponentsInChildren<RotatingLights>())
                {
                    l.Offset = Random.Range(-10f, 10);
                    l.Speed = e._value;
                }
                break;
            case 13:
                foreach (RotatingLights r in LightingGroups[3].GetComponentsInChildren<RotatingLights>())
                {
                    r.Offset = Random.Range(-10f, 10);
                    r.Speed = e._value;
                }
                break;
            default:
                if (e._type <= LightingGroups.Length - 1 && LightingGroups[e._type] != null)
                    StartCoroutine(HandleLights(LightingGroups[e._type], e._value));
                break;
        }
    }

    IEnumerator HandleLights(GameObject group, int value)
    {
        if (group == null) yield break;
        Color color = Color.white;

        //Check if its a legacy Chroma RGB event
        if (value >= ColourManager.RGB_INT_OFFSET)
        {
            if (ChromaCustomColors.ContainsKey(group)) ChromaCustomColors[group] = new Color[] { ColourManager.ColourFromInt(value) };
            else ChromaCustomColors.Add(group, new Color[] { ColourManager.ColourFromInt(value) });
            yield break;
        }

        //Set initial light values
        if (value <= 3) color = BlueColor;
        else if (value <= 7) color = RedColor;
        if (ChromaCustomColors.ContainsKey(group))
        {
            if (ChromaCustomColors[group].Length == 0) color = Random.ColorHSV(0, 1, 1, 1);
            else color = ChromaCustomColors[group][Random.Range(0, ChromaCustomColors[group].Length)];
        }

        foreach (LightingEvent e in GroupToEvents[group])
        {
            if (value == MapEvent.LIGHT_VALUE_OFF) e.ChangeAlpha(0);
            else if (value == MapEvent.LIGHT_VALUE_BLUE_ON || value == MapEvent.LIGHT_VALUE_RED_ON) e.ChangeColor(color);
            else if (value == MapEvent.LIGHT_VALUE_BLUE_FLASH || value == MapEvent.LIGHT_VALUE_RED_FLASH) e.ChangeColor(color, LightingEvent.FlashTime);
            else if (value == MapEvent.LIGHT_VALUE_BLUE_FADE || value == MapEvent.LIGHT_VALUE_RED_FADE) e.StartCoroutine(e.Fade(color));
        }
    }
}
