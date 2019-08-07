using TMPro;
using UnityEngine;

public class PrecisionStepDisplayController : MonoBehaviour {

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TMP_InputField display;
    private int previousGridMeasureSnapping = 1;

	// Update is called once per frame
	void Update () {
        if (atsc.gridMeasureSnapping == previousGridMeasureSnapping) return;
        previousGridMeasureSnapping = atsc.gridMeasureSnapping;
        display.text = previousGridMeasureSnapping.ToString();
	}

    public void UpdateManualPrecisionStep(string result)
    {
        int newGridMeasureSnapping = 1;
        if (int.TryParse(result, out newGridMeasureSnapping))
        {
            previousGridMeasureSnapping = newGridMeasureSnapping;
            atsc.gridMeasureSnapping = newGridMeasureSnapping;
        }
    }
}
