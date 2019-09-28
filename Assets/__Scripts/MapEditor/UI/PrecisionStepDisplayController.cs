using TMPro;
using UnityEngine;

public class PrecisionStepDisplayController : MonoBehaviour {

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TMP_InputField display;
    private int previousGridMeasureSnapping = 1;

    private void Start()
    {
        atsc.GridMeasureSnappingChanged += UpdateText;
    }

    // Update is called once per frame
    void UpdateText (int newSnapping) {
        previousGridMeasureSnapping = newSnapping;
        display.text = newSnapping.ToString();
	}

    private void OnDestroy()
    {
        atsc.GridMeasureSnappingChanged -= UpdateText;
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
