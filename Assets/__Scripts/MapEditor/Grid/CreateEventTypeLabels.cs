using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreateEventTypeLabels : MonoBehaviour {

    public TMP_FontAsset AvailableAsset;
    public TMP_FontAsset UtilityAsset;
    public GameObject LayerInstantiate;
    public Transform[] EventGrid;

	// Use this for initialization
	void Start () {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
	}

    void PlatformLoaded(PlatformDescriptor descriptor)
    {
        for (int i = 0; i < 16; i++)
        {
            int modified = BeatmapEventContainer.EventTypeToModifiedType(i);
            GameObject instantiate = Instantiate(LayerInstantiate, LayerInstantiate.transform.parent);
            instantiate.transform.localPosition = new Vector3(modified, 0, 0);
            try
            {
                switch (i)
                {
                    case 8:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Ring Rotation";
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().font = UtilityAsset;
                        break;
                    case 9:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Small Ring Zoom";
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().font = UtilityAsset;
                        break;
                    case 12:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Left Laser Speed";
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().font = UtilityAsset;
                        break;
                    case 13:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Right Laser Speed";
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().font = UtilityAsset;
                        break;
                    case 14:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Rotation (Include Current Time)";
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().font = UtilityAsset;
                        break;
                    case 15:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Rotation (Exclude Current Time)";
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().font = UtilityAsset;
                        break;
                    default:
                        LightsManager e = descriptor.LightingManagers[i];
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = e?.gameObject.name;
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().font = AvailableAsset;
                        break;
                }
            }
            catch { }
        }
        LayerInstantiate.SetActive(false);
    }

    void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    }

}
