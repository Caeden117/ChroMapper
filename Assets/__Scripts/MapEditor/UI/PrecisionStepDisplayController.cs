using TMPro;
using UnityEngine;

public class PrecisionStepDisplayController : MonoBehaviour {

    [SerializeField]
    private AudioTimeSyncController atsc;

    [SerializeField]
    private TextMeshProUGUI display;

	// Update is called once per frame
	void Update () {
        display.text = atsc.gridMeasureSnapping.ToString();
	}
}
