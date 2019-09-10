using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreateEventTypeLabels : MonoBehaviour {

    public TMP_FontAsset AvailableAsset;
    public GameObject LayerInstantiate;
    [SerializeField]
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
            GameObject instantiate = Instantiate(LayerInstantiate, transform);
            instantiate.transform.localPosition = new Vector3(modified, 0, 0);
            try
            {
                switch (i)
                {
                    case 8:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Ring Rotation";
                        break;
                    case 9:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Small Ring Zoom";
                        break;
                    case 12:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Left Laser Speed";
                        break;
                    case 13:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "Right Laser Speed";
                        break;
                    case 15:
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = "BPM Changes";
                        break;
                    default:
                        LightsManager e = descriptor.LightingManagers[i];
                        instantiate.GetComponentInChildren<TextMeshProUGUI>().text = e.gameObject.name;
                        break;
                }
                instantiate.GetComponentInChildren<TextMeshProUGUI>().font = AvailableAsset;
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
