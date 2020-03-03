using UnityEngine;
using TMPro;

public class CreateEventTypeLabels : MonoBehaviour {

    public TMP_FontAsset AvailableAsset;
    public TMP_FontAsset UtilityAsset;
    public GameObject LayerInstantiate;
    public Transform[] EventGrid;

    private LightsManager[] LightingManagers;

	// Use this for initialization
	void Start () {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
	}

    public void UpdateLabels(bool isRingPropagation, int lanes = 16)
    {
        foreach (Transform children in LayerInstantiate.transform.parent.transform)
        {
            if (children.gameObject.activeSelf)
                Destroy(children.gameObject);
        }

        for (int i = 0; i < lanes; i++)
        {
            int modified = BeatmapEventContainer.EventTypeToModifiedType(i);
            GameObject instantiate = Instantiate(LayerInstantiate, LayerInstantiate.transform.parent);
            instantiate.SetActive(true);
            instantiate.transform.localPosition = new Vector3(modified, 0, 0);
            try
            {
                TextMeshProUGUI textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                if (isRingPropagation)
                {
                    textMesh.font = UtilityAsset;
                    if (i == 0)
                        textMesh.text = "All rings";
                    else
                        textMesh.text = "RING " + i.ToString();
                }
                else
                {
                    switch (i)
                    {
                        case 8:
                            textMesh.font = UtilityAsset;
                            textMesh.text = "Ring Rotation";
                            break;
                        case 9:
                            textMesh.font = UtilityAsset;
                            textMesh.text = "Small Ring Zoom";
                            break;
                        case 12:
                            textMesh.text = "Left Laser Speed";
                            textMesh.font = UtilityAsset;
                            break;
                        case 13:
                            textMesh.text = "Right Laser Speed";
                            textMesh.font = UtilityAsset;
                            break;
                        case 14:
                            textMesh.text = "Rotation (Include Current Time)";
                            textMesh.font = UtilityAsset;
                            break;
                        case 15:
                            textMesh.text = "Rotation (Exclude Current Time)";
                            textMesh.font = UtilityAsset;
                            break;
                        default:
                            LightsManager e = LightingManagers[i];
                            textMesh.text = e?.gameObject.name;
                            textMesh.font = AvailableAsset;
                            break;
                    }
                }
            }
            catch { }
        }
    }

    void PlatformLoaded(PlatformDescriptor descriptor)
    {
        LightingManagers = descriptor.LightingManagers;

        UpdateLabels(false);
    }

    void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    }

}
