using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrecisionStepDisplayController : DisableActionsField
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TMP_InputField display;
    [SerializeField] private TMP_InputField secondDisplay;
    [SerializeField] private Outline firstOutline;
    [SerializeField] private Outline secondOutline;
    [SerializeField] private Color defaultOutlineColor;
    [SerializeField] private Color selectedOutlineColor;

    private bool firstActive;

    private void Start()
    {
        display.text = Settings.Instance.CursorPrecisionA.ToString();
        secondDisplay.text = Settings.Instance.CursorPrecisionB.ToString();

        atsc.GridMeasureSnappingChanged += UpdateText;
        SelectSnap(true);
    }

    void UpdateText(int newSnapping)
    {
        if (firstActive) {
            Settings.Instance.CursorPrecisionA = newSnapping;
            display.text = newSnapping.ToString();
        } else {
            Settings.Instance.CursorPrecisionB = newSnapping;
            secondDisplay.text = newSnapping.ToString();
        }
    }

    private void OnDestroy()
    {
        atsc.GridMeasureSnappingChanged -= UpdateText;
    }

    public void SelectSnap(bool first)
    {
        firstActive = first;
        firstOutline.effectColor = first ? selectedOutlineColor : defaultOutlineColor;
        secondOutline.effectColor = !first ? selectedOutlineColor : defaultOutlineColor;
        UpdateManualPrecisionStep(first ? display.text : secondDisplay.text);
    }

    public void SwapSelectedInterval() => SelectSnap(!firstActive);

    public void UpdateManualPrecisionStep(string result)
    {
        if (int.TryParse(result, out int newGridMeasureSnapping))
            atsc.gridMeasureSnapping = newGridMeasureSnapping;
    }
}
