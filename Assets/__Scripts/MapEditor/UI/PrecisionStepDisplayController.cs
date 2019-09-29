using TMPro;
using UnityEngine;

public class PrecisionStepDisplayController : MonoBehaviour {

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TMP_InputField display;

    private void Start()
    {
        atsc.GridMeasureSnappingChanged += UpdateText;
    }

    // Update is called once per frame
    void UpdateText (int newSnapping) {
        display.text = newSnapping.ToString();
	}

    private void OnDestroy()
    {
        atsc.GridMeasureSnappingChanged -= UpdateText;
    }

    public void UpdateManualPrecisionStep(string result)
    {
        if (int.TryParse(result, out int newGridMeasureSnapping))
            atsc.gridMeasureSnapping = newGridMeasureSnapping;
    }
}
